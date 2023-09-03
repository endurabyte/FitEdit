using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Dauer.Data;
using Dauer.Data.Fit;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Ui.Converters;
using Dauer.Ui.Model;
using DynamicData.Binding;
using Dynastream.Fit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IRecordViewModel
{
  int SelectedIndex { get; set; }
  int SelectionCount { get; set; }

  bool ShowHexData { get; set; }
  long HexDataSelectionStart { get; set; }
  long HexDataSelectionEnd { get; set; }
  string HexData { get; }
}

public class DesignRecordViewModel : RecordViewModel
{
  public DesignRecordViewModel() : base(new NullFileService(), new NullWindowAdapter())
  {
  }
}

public class RecordViewModel : ViewModelBase, IRecordViewModel
{
  /// <summary>
  /// All data grids, even those which are not shown in a tab
  /// </summary>
  private ObservableCollection<DataGridWrapper> AllData_ { get; set; } = new();

  /// <summary>
  /// Shown data grids, i.e. only those which are shown in a tab
  /// </summary>
  public ObservableCollection<DataGridWrapper> ShownData { get; set; } = new();

  /// <summary>
  /// Name of the currently selected tab
  /// </summary>
  private string TabName_ => TabIndexIsValid_
    ? UnformatTabName(ShownData[TabIndex]?.Name) ?? DefaultTabName_ 
    : DefaultTabName_;

  private const string DefaultTabName_ = "Record";

  /// <summary>
  /// Index of the currently selected tab
  /// </summary>
  [Reactive] public int TabIndex { get; set; }
  private bool TabIndexIsValid_ => TabIndex >= 0 && TabIndex < ShownData.Count;

  /// <summary>
  /// The index of the currently shown GPS coordinate shown in the chart, map, and records tab.
  /// </summary>
  [Reactive] public int SelectedIndex { get; set; }

  [Reactive] public int SelectionCount { get; set; }
  [Reactive] public bool HideUnusedFields { get; set; } = true;
  [Reactive] public bool HideUnnamedFields { get; set; } = true;
  [Reactive] public bool HideUnnamedMessages { get; set; } = true;
  [Reactive] public bool PrettifyFields { get; set; } = true;
  [Reactive] public bool ShowHexData { get; set; } = false;

  [Reactive] public string HexData { get; set; } = "(No Data)";
  [Reactive] public long HexDataSelectionStart { get; set; }
  [Reactive] public long HexDataSelectionEnd { get; set; }

  private readonly IFileService fileService_;
  private readonly IWindowAdapter window_;
  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;
  private readonly HashSet<IDisposable> messageSubs_ = new();
  private DataGridWrapper? records_;
  private MessageWrapper? selectedMessage_;
  private readonly MesgFieldValueConverter converter_;

  public RecordViewModel(
    IFileService fileService,
    IWindowAdapter window
  )
  {
    fileService_ = fileService;
    window_ = window;

    window_.Resized.Subscribe(tup =>
    {
      DataGridWrapper data = ShownData[TabIndex];
      if (data?.DataGrid is null) { return; }
      data.DataGrid.ScrollIntoView(data.DataGrid.SelectedItem, null);
    });

    converter_ = new MesgFieldValueConverter(prettify: PrettifyFields);

    fileService.ObservableForProperty(x => x.MainFile).Subscribe(property => HandleMainFileChanged(fileService.MainFile));

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      if (fileService_.MainFile == null) { return; }
      fileService_.MainFile.SelectedIndex = property.Value;
    });

    this.ObservableForProperty(x => x.HideUnusedFields).Subscribe(_ => UpdateColumnVisibility());
    this.ObservableForProperty(x => x.HideUnnamedFields).Subscribe(_ => UpdateColumnVisibility());
    this.ObservableForProperty(x => x.HideUnnamedMessages).Subscribe(_ =>
    {
      PreserveCurrentTab(() =>
      {
        ShownData.Clear();

        foreach (var group in AllData_)
        {
          group.IsVisible = !HideUnnamedMessages || group.IsNamed;
          if (group.IsVisible)
          {
            ShownData.Add(group);
          }
        }
      });
    });

    this.ObservableForProperty(x => x.PrettifyFields).Subscribe(_ =>
    {
      PreserveCurrentTab(() =>
      {
        converter_.Prettify = PrettifyFields;
        if (fileService_.MainFile?.FitFile == null) { return; }
        Show(fileService_.MainFile.FitFile);
      });
    });

    this.ObservableForProperty(x => x.TabIndex).Subscribe(_ => HandleTabIndexChanged());
    this.ObservableForProperty(x => x.ShowHexData).Subscribe(_ => InitHexData());
  }

  private void HandleTabIndexChanged()
  {
    if (!TabIndexIsValid_) { return; }
    DataGridWrapper data = ShownData[TabIndex];
    if (data?.DataGrid?.SelectedItem is not MessageWrapper wrapper) { return; }

    SelectHexData(wrapper);
  }

  /// <summary>
  /// Preserve the current tab selection if possible
  /// </summary>
  private void PreserveCurrentTab(Action a)
  {
    string? tabName = TabName_;
    a();
    SelectTab(tabName);
  }

  /// <summary>
  /// Selec the tab with the given name, or the first tab if no tab with the given name exists.
  /// </summary>
  private void SelectTab(string? tabName)
  {
    DataGridWrapper? match = ShownData.FirstOrDefault(g => UnformatTabName(g.Name) == tabName);
    TabIndex = match != null ? ShownData.IndexOf(match) : 0;
  }

  /// <summary>
  /// Remove the count of items in a group, e.g. "Record (123)" -> "Record"
  /// </summary>
  private static string? UnformatTabName(string? s) => s?.Split('(')[0].Trim();

  /// <summary>
  /// Get the tab name for a given message definition and count of messages.
  /// Replace "unknown" with "Message # {num}".
  /// For example, "Record" -> "Record (123)".
  /// For example, "unknown" -> "Message # 233 (123)"
  /// </summary>
  private static string? FormatTabName(Mesg defMesg, int count) =>
       $"{(defMesg.Name == "unknown" 
         ? $"Message # {defMesg.Num}" : defMesg.Name)} "
     + $"({count})";

  private void UpdateColumnVisibility()
  {
    foreach (var group in AllData_)
    {
      foreach (var header in group.Headers)
      {
        if (header.Column == null) { continue; }
        bool prev = header.Column.IsVisible;
        bool next = GetIsVisible(header);

        // Performance: Only update changed columns
        bool changed = prev != next;
        if (!changed) { continue; }

        // Force update
        header.Column.IsVisible = next;
        group.DataGrid?.Columns.Remove(header.Column);
        group.DataGrid?.Columns.Add(header.Column);
      }
    }
  }

  private bool GetIsVisible(ColumnWrapper header) => (header.IsUsed || !HideUnusedFields) && (header.IsNamed || !HideUnnamedFields);

  private void HandleDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
  {
    if (sender is not DataGrid dg) { return; }
    if (dg.SelectedItem is not MessageWrapper wrapper) { return; }

    SelectHexData(wrapper);
    dg.ScrollIntoView(dg.SelectedItem, dg.CurrentColumn);
    selectedMessage_ = dg.SelectedItem as MessageWrapper;

    if (selectedMessage_?.Mesg is RecordMesg)
    {
      SelectedIndex = dg.SelectedIndex;
    }
  }

  private void SelectHexData(MessageWrapper? wrapper)
  {
    if (wrapper is null) { return; }
    const int width = 3; // 2 hex digits + space;
    SelectHexData(
      width * wrapper.Mesg.SourceIndex, 
      width * (wrapper.Mesg.SourceIndex + wrapper.Mesg.SourceLength) - 1); // -1: omit trailing space
  }

  private void SelectHexData(long start, long end)
  {
    HexDataSelectionStart = start;
    HexDataSelectionEnd = end;
  }

  private void InitHexData()
  {
    if (!ShowHexData) { return; }
    if (fileService_.MainFile?.Activity?.File == null) { return; }
    HexData = string.Join(" ", fileService_.MainFile.Activity.File.Bytes.Select(b => $"{b:X2}"));
    SelectHexData(0, 0);
  }

  private void HandleMainFileChanged(UiFile? file)
  {
    if (file == null) { return; }
    if (file.FitFile == null) { return; }

    selectedIndexSub_?.Dispose();
    selectedCountSub_?.Dispose();
    foreach (var sub in messageSubs_)
    {
      sub.Dispose();
    }
    messageSubs_.Clear();

    InitHexData();
    PreserveCurrentTab(() => Show(file.FitFile));

    selectedIndexSub_ = file.ObservableForProperty(x => x.SelectedIndex).Subscribe(e => HandleSelectedIndexChanged(e.Value));
    selectedCountSub_ = file.ObservableForProperty(x => x.SelectionCount).Subscribe(e => HandleSelectionCountChanged(e.Value));
  }

  private void HandleSelectedIndexChanged(int index)
  {
    if (SelectedIndex == index) { return; }
    SelectedIndex = index;

    if (records_ == null) { return; }
    if (records_.DataGrid == null) { return; }

    records_.DataGrid.SelectedIndex = index;
  }

  private void HandleSelectionCountChanged(int count)
  {
    SelectionCount = count;

    if (records_?.DataGrid == null) { return; }
    var dg = records_.DataGrid;

    if (dg.ItemsSource == null) { return; }
    dg.SelectedItems.Clear();

    var items = new DataGridCollectionView(dg.ItemsSource);
    foreach (int i in Enumerable.Range(SelectedIndex, SelectionCount))
    {
      dg.SelectedItems.Add(items[i]);
    }
  }

  private void Show(FitFile ff)
  {
    // Remeber which groups were expanded
    var expandedGroups = AllData_
      .Where(g => g.IsExpanded)
      .ToDictionary(g => g.Name ?? "", g => g.IsExpanded);

    foreach (var group in AllData_)
    {
      if (group.DataGrid != null)
      {
        group.DataGrid.CellEditEnding -= HandleCellEditEnding;
      }
    }

    AllData_.Clear();
    ShownData.Clear();

    foreach (var kvp in ff.MessagesByDefinition)
    {
      DataGridWrapper group = CreateGroup(ff.MessageDefinitions[kvp.Key], kvp.Value);
      group.IsExpanded = expandedGroups.ContainsKey(group.Name ?? "");
      if (group.DataGrid != null)
      {
        group.DataGrid.CellEditEnding += HandleCellEditEnding;
        group.DataGrid.SelectionChanged += HandleDataGridSelectionChanged;
        group.DataGrid.CurrentCellChanged += HandleCurrentCellChanged;
      }

      AllData_.Add(group);
      if (group.IsVisible)
      {
        ShownData.Add(group);
      }
    }

    // When the slider moves, higlight it in the records DataGrid
    var records = AllData_.FirstOrDefault(g => g.Num == MesgNum.Record);
    if (records == null) { return; }
    if (records.DataGrid == null) { return; }

    records_ = records;
  }

  private void HandleCurrentCellChanged(object? sender, EventArgs e)
  {
    if (sender is not DataGrid dg) { return; }
    //MessageWrapper wrapper = dg.GetCurrentItem;
  }

  private HashSet<object?>? GetNamedValues(IEnumerable<MessageWrapper> wrappers, string messageName, string fieldName)
  {
    Dictionary<string, Field> mergedFields = new();

    // Get at least one value for each field so we can reflect on it
    foreach (var kvp in wrappers.SelectMany(msg => msg.Mesg.FieldsByName))
    {
      if (kvp.Value == null || mergedFields.TryGetValue(kvp.Key, out Field? _))
      {
        continue;
      }

      object? value = kvp.Value.GetValue();

      if (value == null)
      {
        continue;
      }

      mergedFields.Add(kvp.Key, kvp.Value);
    }

    if(!mergedFields.TryGetValue(fieldName, out Field? field))
    {
      return null;
    }


    // Include named values and unnamed values. E.g. DeviceInfo.DeviceIndex = {Creator = 0, 1, 2, 3, 4, 5, 7, Invalid = 255 }
    var namedValues = new HashSet<object?>();

    // Add defined values.
    var definedValues = MessageWrapper.GetNamedValues(messageName, field);

    // Don't add actual values if there are no defined values.
    if (definedValues == null) { return null; }

    namedValues.AddRange(definedValues);

    // Add actual values
    var actualValues = wrappers.Select(wrapper => wrapper.GetValue(fieldName, PrettifyFields)).ToList();
      
    namedValues.AddRange(actualValues);

    return namedValues;
  }

  private void HandleMessagePropertyChanged(MessageWrapper wrapper)
  {
    if (!ShowHexData) { return; }

    // Get the new bytes for the message
    var ms = new MemoryStream();
    wrapper.Mesg.Write(ms);
    wrapper.Mesg.CacheData(ms);
    HexData = UpdateHexData(HexData, wrapper);
  }

  /// <summary>
  /// Replace the relevant segment of the given data with new bytes
  /// </summary>
  private static string UpdateHexData(string hexData, MessageWrapper wrapper)
  { 
    Mesg mesg = wrapper.Mesg;
    var newBytes = mesg.SourceData;

    const int width = 3; // 2 hex digits + space
    int start = (int)(mesg.SourceIndex * width);
    int end = (int)((mesg.SourceIndex + mesg.SourceLength) * width);

    if (start >= hexData.Length || end >= hexData.Length) 
    { 
      Dauer.Model.Log.Error($"Hex data index out of range: {start} {end} {hexData.Length}");
      return "There was a problem showing the data. Try again or contact support@fitedit.io.";
    }

    ReadOnlySpan<char> span = hexData.AsSpan();
    var sb = new StringBuilder(hexData.Length);
    var mid = string.Join(" ", newBytes.Select(b => $"{b:X2}"));
    sb.Append(span[..start]);
    sb.Append(mid);
    sb.Append(' ');
    sb.Append(span[end..]);
    var newHexData = sb.ToString();

    if (hexData.Length != sb.Length)
    {
      Dauer.Model.Log.Warn($"Hex data length changed from {sb.Length} to {hexData.Length}");
    }

    return newHexData;
  }

  private DataGridWrapper CreateGroup(MesgDefinition def, List<Mesg> mesgs)
  {
    Mesg defMesg = Profile.GetMesg(def.GlobalMesgNum);

    var defMessage = new MessageWrapper(defMesg);
    var wrappers = mesgs.Select(mesg => new MessageWrapper(mesg)).ToList();

    foreach (var wrapper in wrappers)
    {
      IDisposable sub = wrapper
        .WhenPropertyChanged(x => x.Mesg, notifyOnInitialValue: false)
        .Subscribe(_ => HandleMessagePropertyChanged(wrapper));
      messageSubs_.Add(sub);
    }

    // Categories of fields:
    // Known/unknown fields:
    //   known fields have a name, e.g. "timestamp" or "manufacturer"
    //   unknown fields do not have a name.
    //     They only have a message num e.g. 15, 253.
    //     Garmin labels them "unknown" which we replace with "Field <Num>"
    // Used/unused fields:
    //   used fields appear on the definition or on a message
    //   unused fields fields are on the definition but not on any message

    var definedFieldNames = new HashSet<string>(defMesg.FieldsByName.Keys);
    var actualFieldNames = new HashSet<string>(mesgs
      .SelectMany(m => m.FieldsByName.Keys
      // Replace "unknown" with "Field <Num>"
      .Select(key => key == "unknown" ? $"Field {m.Num}" : key)));

    // On both the definition and at least one message
    var onBoth = new HashSet<string>(definedFieldNames);

    // On either the definition or a message
    var onEither = new HashSet<string>(definedFieldNames);

    // On only the definition
    var defOnly = new HashSet<string>(definedFieldNames);

    // On only a message
    var msgOnly = new HashSet<string>(actualFieldNames);

    onBoth.IntersectWith(actualFieldNames);
    onEither.UnionWith(actualFieldNames);
    defOnly.ExceptWith(actualFieldNames);
    msgOnly.ExceptWith(definedFieldNames);

    List<ColumnWrapper> columns = onEither.Select(fieldName => new ColumnWrapper
    {
      Name = fieldName,
      NamedValues = GetNamedValues(wrappers, defMesg.Name, fieldName),
      IsUsed = !defOnly.Contains(fieldName),
    }).ToList();

    var dg = new DataGrid
    {
      ItemsSource = wrappers,
      CanUserSortColumns = true,
      CanUserResizeColumns = true,
      CanUserReorderColumns = true,
      IsReadOnly = false,
    };

    // Create columns
    var converter = new MesgFieldValueConverter(prettify: PrettifyFields);

    foreach (var column in columns)
    {
      // Use a ComboBox if it makes sense
      if (PrettifyFields && (column.NamedValues?.Any() ?? false))
      {
        column.Column = new DataGridTemplateColumn
        {
          Header = column.Name,

          CellTemplate = new FuncDataTemplate(model => true, (model, scope) =>
          {
            if (model is not MessageWrapper wrapper) { return null; }

            object? initialValue = converter.Convert(model, typeof(string), column.Name, CultureInfo.CurrentCulture);

            var cb = new ComboBox
            {
              Name = column.Name,
              HorizontalAlignment = HorizontalAlignment.Stretch,
              HorizontalContentAlignment = HorizontalAlignment.Stretch,
              ItemsSource = column.NamedValues
                .Select(_ => $"{_}") // Convert to string since the converted value is a string
                .OrderBy(s => s),
              SelectedValue = initialValue,
            };

            cb.SelectionChanged += (sender, e) =>
            {
              if (cb.SelectedValue == null) { return; }
              var newValue = cb.SelectedValue.ToString();
              converter.ConvertBack(wrapper, typeof(string), (column.Name, newValue), CultureInfo.CurrentCulture);
            };

            return cb;
          }),
        };
      }
      else
      {
        column.Column = new DataGridTextColumn
        {
          Header = column.Name,
          Binding = new Binding
          {
            Converter = converter,
            ConverterParameter = column.Name,
            Mode = BindingMode.TwoWay,
          },
          IsReadOnly = false,
          IsVisible = GetIsVisible(column),
        };
      }
    }

    foreach (var header in columns)
    {
      dg.Columns.Add(header.Column);
    }

    var group = new DataGridWrapper
    {
      Num = defMesg.Num,
      Name = FormatTabName(defMesg, mesgs.Count),
      DataGrid = dg,
      IsNamed = defMessage.IsNamed,
      IsVisible = defMessage.IsNamed || !HideUnnamedMessages,
    };
    group.Headers.AddRange(columns);

    return group;
  }

  private void HandleCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
  {
    var dg = sender as DataGrid;

    var column = e.Column as DataGridTextColumn;
    var row = e.Row;

    var binding = column?.Binding as Binding;
    var converter = binding?.Converter as MesgFieldValueConverter;

    var header = column?.Header as string;
    var message = row.DataContext as MessageWrapper;

    var editingElement = e.EditingElement as TextBox;
    var newContent = editingElement?.Text;

    if (message == null) { return; }
    if (header == null) { return; }
    if (converter == null) { return; }

    converter.ConvertBack(message, typeof(MessageWrapper), (header, newContent), CultureInfo.CurrentCulture);
  }
}
