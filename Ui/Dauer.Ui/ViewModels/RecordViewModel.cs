using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Dauer.Data.Fit;
using Dauer.Model.Extensions;
using Dauer.Ui.Converters;
using Dynastream.Fit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IRecordViewModel
{
  int SelectedIndex { get; set; }
  int SelectionCount { get; set; }
}

public class DesignRecordViewModel : RecordViewModel
{
  public DesignRecordViewModel() : base(new FileService())
  {
  }
}

public class RecordViewModel : ViewModelBase, IRecordViewModel
{
  /// <summary>
  /// All groups, even those which are not shown
  /// </summary>
  private ObservableCollection<DataGridWrapper> AllGroups_ { get; set; } = new();

  /// <summary>
  /// Shown groups, i.e. only those which are shown in a tab
  /// </summary>
  public ObservableCollection<DataGridWrapper> Groups { get; set; } = new();

  /// <summary>
  /// Name of the currently selected tab
  /// </summary>
  private string TabName_ => TabIndexIsValid_
    ? UnformatTabName(Groups[TabIndex]?.Name) ?? DefaultTabName_ 
    : DefaultTabName_;

  private const string DefaultTabName_ = "Record";

  /// <summary>
  /// Index of the currently selected tab
  /// </summary>
  [Reactive] public int TabIndex { get; set; }
  private bool TabIndexIsValid_ => TabIndex >= 0 && TabIndex < Groups.Count;

  /// <summary>
  /// The index of the currently shown GPS coordinate shown in the chart, map, and records tab.
  /// </summary>
  [Reactive] public int SelectedIndex { get; set; }

  [Reactive] public int SelectionCount { get; set; }
  [Reactive] public bool HideUnusedFields { get; set; } = true;
  [Reactive] public bool HideUnnamedFields { get; set; } = true;
  [Reactive] public bool HideUnnamedMessages { get; set; } = true;
  [Reactive] public bool PrettifyFields { get; set; } = true;

  private readonly IFileService fileService_;
  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;
  private DataGridWrapper? records_;
  private readonly MesgFieldValueConverter converter_;

  public RecordViewModel(
    IFileService fileService
  )
  {
    fileService_ = fileService;
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
        Groups.Clear();

        foreach (var group in AllGroups_)
        {
          group.IsVisible = !HideUnnamedMessages || group.IsNamed;
          if (group.IsVisible)
          {
            Groups.Add(group);
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
    DataGridWrapper? match = Groups.FirstOrDefault(g => UnformatTabName(g.Name) == tabName);
    TabIndex = match != null ? Groups.IndexOf(match) : 0;
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
    foreach (var group in AllGroups_)
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

  private void HandleRecordSelectionChanged(object? sender, SelectionChangedEventArgs e)
  {
    if (sender is not DataGrid dg) { return; }
    dg.ScrollIntoView(dg.SelectedItem, dg.CurrentColumn);
  }

  private void HandleMainFileChanged(SelectedFile? file)
  {
    if (file == null) { return; }
    if (file.FitFile == null) { return; }

    PreserveCurrentTab(() => Show(file.FitFile));

    selectedIndexSub_?.Dispose();
    selectedCountSub_?.Dispose();

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
    var expandedGroups = AllGroups_
      .Where(g => g.IsExpanded)
      .ToDictionary(g => g.Name ?? "", g => g.IsExpanded);

    if (records_?.DataGrid != null)
    {
      records_.DataGrid.SelectionChanged -= HandleRecordSelectionChanged;
    }

    foreach (var group in AllGroups_)
    {
      if (group.DataGrid != null)
      {
        group.DataGrid.CellEditEnding -= HandleCellEditEnding;
      }
    }

    AllGroups_.Clear();
    Groups.Clear();

    foreach (var kvp in ff.MessagesByDefinition)
    {
      DataGridWrapper group = CreateGroup(ff.MessageDefinitions[kvp.Key], kvp.Value);
      group.IsExpanded = expandedGroups.ContainsKey(group.Name ?? "");
      if (group.DataGrid != null)
      {
        group.DataGrid.CellEditEnding += HandleCellEditEnding;
      }

      AllGroups_.Add(group);
      if (group.IsVisible)
      {
        Groups.Add(group);
      }
    }

    // When the slider moves, higlight it in the records DataGrid
    var records = AllGroups_.FirstOrDefault(g => g.Num == MesgNum.Record);
    if (records == null) { return; }
    if (records.DataGrid == null) { return; }

    records.DataGrid.SelectionChanged += HandleRecordSelectionChanged;
    records_ = records;
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

  private DataGridWrapper CreateGroup(MesgDefinition def, List<Mesg> mesgs)
  {
    Mesg defMesg = Profile.GetMesg(def.GlobalMesgNum);

    var defMessage = new MessageWrapper(defMesg);
    var wrappers = mesgs.Select(mesg => new MessageWrapper(mesg));

    // Categories of fields:
    // Known/unknown fields:
    //   known fields have a name, e.g. "timestamp" or "manufacturer"
    //   unknown fields do not have a name.
    //     They only have a message num e.g. 15, 253.
    //     Garmin labels them "unknown" which we replace with "Field <Num>"
    // Used/unused fields:
    //   used fields appear on the definition or on a amessage
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
