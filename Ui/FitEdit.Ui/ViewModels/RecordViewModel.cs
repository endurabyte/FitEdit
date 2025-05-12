﻿using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using DynamicData.Binding;
using Dynastream.Fit;
using FitEdit.Data;
using FitEdit.Data.Fit;
using FitEdit.Data.Fit.Edits;
using FitEdit.Model.Extensions;
using FitEdit.Ui.Converters;
using FitEdit.Ui.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.ViewModels;

public interface IRecordViewModel
{
  int SelectedIndex { get; set; }
  int SelectionCount { get; set; }
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

  private FitFile? fitFile_;

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

  public bool CanSplit => TabName_.Contains("Record") 
    && SelectedIndex > 0 
    && SelectedIndex < fitFile_?.Records.Count - 1;

  [Reactive] public int SelectionCount { get; set; }

  [Reactive] public bool HideUnusedFields { get; set; } = true;
  [Reactive] public bool HideUnnamedFields { get; set; } = true;
  [Reactive] public bool HideUnnamedMessages { get; set; } = true;
  [Reactive] public bool PrettifyFields { get; set; } = true;
  [Reactive] public bool HaveUnsavedChanges{ get; set; } = false;

  private readonly IFileService fileService_;
  private readonly IWindowAdapter window_;
  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;
  private readonly ConcurrentDictionary<IDisposable, IDisposable> messageSubs_ = new();
  private DataGridWrapper? records_;
  private MessageWrapper? selectedMessage_;

  public RecordViewModel(
    IFileService fileService,
    IWindowAdapter window
  )
  {
    fileService_ = fileService;
    window_ = window;

    // When the window resizes, the selection can go out of view.
    // Scroll it back into view.
    window_.Resized.Subscribe(_ => ScrollToSelection());

    fileService.ObservableForProperty(x => x.MainFile).Subscribe(property => HandleMainFileChanged(fileService.MainFile));

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      if (fileService_.MainFile == null) { return; }
      fileService_.MainFile.SelectedIndex = property.Value;

      this.RaisePropertyChanged(nameof(CanSplit));
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
        
        return Task.CompletedTask;
      });
    });

    this.ObservableForProperty(x => x.PrettifyFields).Subscribe(_ =>
    {
      PreserveCurrentTab(async () =>
      {
        if (fileService_.MainFile?.FitFile == null) { return; }
        await Show(fileService_.MainFile);
      });
    });

    this.ObservableForProperty(x => x.TabIndex).Subscribe(_ => HandleTabIndexChanged());
  }

  public async Task SaveChanges()
  {
    if (fitFile_ is null) { return; }

    UiFile? uif = fileService_.MainFile;
    if (uif == null) { return; }
      
    fitFile_.ForwardfillEvents();
    fitFile_.Sessions.Sorted(MessageExtensions.SortByStartTime);
    fitFile_.Laps.Sorted(MessageExtensions.SortByStartTime);
    fitFile_.Records.Sorted(MessageExtensions.SortByTimestamp);
    fitFile_.BackfillEvents();
    uif.Commit(fitFile_);
    
    await fileService_.UpdateAsync(uif.Activity);

    // Reload changes
    fileService_.MainFile = null;
    fileService_.MainFile = uif;
      
    HaveUnsavedChanges = false;
  }

  private void ScrollToSelection()
  {
    if (TabIndex < 0 || TabIndex >= ShownData.Count) { return; }
    DataGridWrapper data = ShownData[TabIndex];
    if (data?.DataGrid is null) { return; }

    data.DataGrid.ScrollIntoView(data.DataGrid.SelectedItem, null);
  }

  private void HandleTabIndexChanged()
  {
    this.RaisePropertyChanged(nameof(CanSplit));

    if (!TabIndexIsValid_) { return; }
    DataGridWrapper data = ShownData[TabIndex];

    if (data?.DataGrid?.SelectedItem is not MessageWrapper wrapper) { return; }
  }

  /// <summary>
  /// Preserve the current tab selection if possible
  /// </summary>
  private void PreserveCurrentTab(Func<Task> f)
  {
    string? tabName = TabName_;
    f();
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

    dg.ScrollIntoView(dg.SelectedItem, dg.CurrentColumn);
    selectedMessage_ = dg.SelectedItem as MessageWrapper;

    if (selectedMessage_?.Mesg is RecordMesg)
    {
      SelectedIndex = dg.SelectedIndex;
    }
  }

  private void HandleMainFileChanged(UiFile? file)
  {
    selectedIndexSub_?.Dispose();
    selectedCountSub_?.Dispose();
    foreach (var sub in messageSubs_)
    {
      sub.Value.Dispose();
    }
    messageSubs_.Clear();

    PreserveCurrentTab(async () => await Show(file));

    if (file == null) { return; }
    
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
      if (i < 0 || i >= items.Count) { break; }
      dg.SelectedItems.Add(items[i]);
    }
  }

  protected async Task Show(UiFile? uif)
  {
    if (uif is null) { return; }
    fitFile_ = uif.FitFile;

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

    if (fitFile_ != null)
    {
      foreach (var kvp in fitFile_.MessagesByDefinition)
      {
        DataGridWrapper group = await CreateGroup(fitFile_.MessageDefinitions[kvp.Key], kvp.Value);
        group.IsExpanded = expandedGroups.ContainsKey(group.Name ?? "");
        if (group.DataGrid != null)
        {
          group.DataGrid.CellPointerPressed += (sender, e) => HandleCellPointerPressed(group.DataGrid, e);
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
    var definedValues = MesgExtensions.GetNamedValues(messageName, field);

    // Don't add actual values if there are no defined values.
    if (definedValues == null) { return null; }

    namedValues.AddRange(definedValues);

    // Add actual values
    var actualValues = wrappers.Select(wrapper => wrapper.Mesg.GetFieldValue(fieldName, PrettifyFields)).ToList();
      
    namedValues.AddRange(actualValues);

    return namedValues;
  }

  private async Task<DataGridWrapper> CreateGroup(MesgDefinition def, List<Mesg> mesgs)
  {
    Mesg defMesg = Profile.GetMesg(def.GlobalMesgNum);

    var defMessage = new MessageWrapper(defMesg);
    var wrappers = new ObservableCollection<MessageWrapper>(mesgs.Select(mesg => new MessageWrapper(mesg)));
    wrappers.CollectionChanged += (sender, e) => HaveUnsavedChanges = true;

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

    List<ColumnWrapper> columns = new();

    await Task.Run(() =>
    {
      columns.AddRange(onEither.Select(fieldName => new ColumnWrapper
      {
        Name = fieldName,
        NamedValues = GetNamedValues(wrappers, defMesg.Name, fieldName),
        IsUsed = !defOnly.Contains(fieldName),
      }));
    });


    var dg = new DataGrid
    {
      ItemsSource = wrappers,
      CanUserSortColumns = true,
      CanUserResizeColumns = true,
      CanUserReorderColumns = true,
      IsReadOnly = false,
    };

    // Create columns
    foreach (var column in columns)
    {
      // Use a ComboBox if it makes sense
      if (PrettifyFields && (column.NamedValues?.Any() ?? false))
      {
        column.Column = new DataGridTemplateColumn
        {
          Header = column.Name,
          IsVisible = GetIsVisible(column),

          CellTemplate = new FuncDataTemplate(model => true, (model, scope) =>
          {
            if (model is not MessageWrapper wrapper) { return null; }
            var converter = new MessageWrapperFieldValueConverter(wrapper, prettify: PrettifyFields);

            var cb = new ComboBox
            {
              Name = column.Name,
              HorizontalAlignment = HorizontalAlignment.Stretch,
              HorizontalContentAlignment = HorizontalAlignment.Stretch,
              ItemsSource = column.NamedValues
                .Select(o => $"{o}") // Convert to string since the converted value is a string
                .OrderBy(s => s),

              [!ComboBox.SelectedValueProperty] = new Binding
              {
                Path = nameof(MessageWrapper.Mesg),
                Converter = converter,
                ConverterParameter = column.Name,
                Mode = BindingMode.OneWay,
              }
            };

            cb.SelectionChanged += (sender, e) =>
            {
              // This handler is called when drawing the ComboBox and when the user or code changes the value
              // We only care about the latter case.
              // We can discern if this is the case by checking if there are any Removed items.
              if (e.RemovedItems.Count == 0 ) { return; }

              wrapper.SetFieldValue(column?.Name ?? "", cb.SelectedValue, PrettifyFields);
              HaveUnsavedChanges = true;
            };

            return cb;
          }),
        };
      }
      else
      {
        var converter = new MesgFieldValueConverter(prettify: PrettifyFields);
        column.Column = new DataGridTextColumn
        {
          Header = column.Name,
          IsReadOnly = false,
          IsVisible = GetIsVisible(column),
          Binding = new Binding
          {
            Path = nameof(MessageWrapper.Mesg),
            Converter = converter,
            ConverterParameter = column.Name,
            Mode = BindingMode.OneWay,
          },
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

    AddContextMenus(defMesg.Name, dg);

    return group;
  }

  private void AddContextMenus(string mesgName, DataGrid dg)
  {
    var menu = new ContextMenu();

    var duplicate = new MenuItem
    {
      Header = "Duplicate",
      Command = ReactiveCommand.Create(() => DuplicateRows(dg)),
    };
    menu.Items.Add(duplicate);

    var delete = new MenuItem
    {
      Header = "Delete",
      Command = ReactiveCommand.Create(() => DeleteRows(dg)),
    };
    ToolTip.SetTip(delete, "Remove the given record. Do not motify subsequent records.");
    menu.Items.Add(delete);
    var deleteAndRecalculate = new MenuItem
    {
      Header = "Delete and subtract distance",
      Command = ReactiveCommand.Create(() => DeleteRows(dg, subtractDistance: true)),
    };
    ToolTip.SetTip(delete, @"Remove the given record and subtract its distance from subsequent records.");
    menu.Items.Add(deleteAndRecalculate);

    if (mesgName == "Record")
    {
      var menuItem = new MenuItem
      {
        Header = "Split Activity Here",
        Command = ReactiveCommand.Create(SplitActivity),
      };
      ToolTip.SetTip(menuItem, 
          "Split the activity at the selected record."
        + "\nThe first activity will contain all messages up to and including the selected row."
        + "\nThe second activity will contain all messages after the selected row.");
      menu.Items.Add(menuItem);
    }
    
    if (mesgName == "Record")
    {
      var menuItem = new MenuItem
      {
        Header = "Split Lap Here",
        Command = ReactiveCommand.Create(SplitLap),
      };
      ToolTip.SetTip(menuItem, "Split the lap at the selected record into two laps.");
      menu.Items.Add(menuItem);
    }
    
    if (mesgName == "Lap")
    {
      var menuItem = new MenuItem
      {
        Header = "Merge",
        Command = ReactiveCommand.Create(() => MergeSelectedLaps(dg)),
      };

      ToolTip.SetTip(menuItem, "Merge the selected laps.\nLaps in between the first and last will be merged even if not selected");
      menu.Items.Add(menuItem);
    }

    var setAllTextBox = new TextBox
    {
      Name = "SetAll",
      HorizontalAlignment = HorizontalAlignment.Stretch,
      HorizontalContentAlignment = HorizontalAlignment.Stretch,
    };
    menu.Items.Add(setAllTextBox);

    var setAllButton = new MenuItem() { Name = "SetAll" };
    ToolTip.SetTip(setAllButton, "Set all selected cells to the same value");
    menu.Items.Add(setAllButton);

    dg.ContextMenu = menu;
  }

  private void DeleteRows(DataGrid dg, bool subtractDistance = false)
  {
    if (dg.ItemsSource is not ObservableCollection<MessageWrapper> list) { return; }
    var selection = dg.SelectedItems.Cast<MessageWrapper>().ToList();

    foreach (var item in selection)
    {
      list.Remove(item);
      fitFile_?.Remove(item.Mesg);
    
      if (subtractDistance)
      {
        SubtractDistance(item);
        // Since we removed a record message and are doing arithmetic with record indices,
        // we need to update the records list
        fitFile_.ForwardfillEvents();
      }
    }
    
    if (subtractDistance)
    {
      // Cause the data grid to show the new distances by calling NotifyPropertyChanged
      foreach (var enumerable in dg.ItemsSource)
      {
        if (enumerable is not MessageWrapper wrapper)
          continue;
          
        wrapper.NotifyPropertyChanged(nameof(MessageWrapper.Mesg));
      }
    }
    HaveUnsavedChanges = true;
  }

  /// <summary>
  /// Subtract the distance of the given record (relative to is predecessor) from all subsequent records.
  /// </summary>
  private void SubtractDistance(MessageWrapper item)
  {
    if (fitFile_ is null)
      return;
      
    if (item.Mesg is not RecordMesg record)
      return;
     
    int index = fitFile_.Records.IndexOf(record);
    // Index of the record after the deleted record
    int nextIndex = index + 1;
    
    if (index < 0)
      return;
    
    // All records after the deleted record
    List<RecordMesg> subsequentRecords = Enumerable
      .Range(nextIndex, fitFile_.Records.Count - nextIndex)
      .Select(i => fitFile_.Records[i])
      .ToList();

    RecordMesg prevRecord = fitFile_.Records[Math.Max(0, index - 1)];
    double? prevDist = prevRecord.GetDistance();
    double? dist = record.GetDistance();
    double? diff = dist - prevDist;
    subsequentRecords.AddDistance(-1 * diff ?? 0);
  }

  private void DuplicateRows(DataGrid dg)
  {
    if (dg.ItemsSource is not ObservableCollection<MessageWrapper> list) { return; }

    var selection = dg.SelectedItem as MessageWrapper;
    if (selection is null) { return; }

    var index = list.IndexOf(selection);
    if (index < 0) { return; }

    var dupe = new MessageWrapper(MessageFactory.Create(selection.Mesg));

    list.Insert(index, dupe);
    fitFile_?.Add(dupe.Mesg);

    dg.SelectedItem = dupe;
  }

  private void MergeSelectedLaps(DataGrid dg)
  {
    if (dg.ItemsSource is not ObservableCollection<MessageWrapper> list) { return; }

    var allLaps = dg.ItemsSource.Cast<MessageWrapper>().ToList();
    var selection = dg.SelectedItems.Cast<MessageWrapper>().ToList();

    MessageWrapper? merged = new MessageWrapperMerger().Merge(allLaps, selection);

    if (merged == null) { return; }

    int index = allLaps.IndexOf(selection.First());

    if (index < 0) { return; }

    foreach (var lap in selection)
    {
      // Remove from table
      list.Remove(lap);

      // Remove from backing data store (FitFile)
      // Do not modify FitFile.Laps. They are generated from MessagesByDefinition on BackfillEvents
      fitFile_?.Remove(lap.Mesg);
    }

    // Add to table
    list.Insert(index, merged);

    // Add to backing data store (FitFile)
    fitFile_?.Add(merged.Mesg);

    dg.SelectedItem = merged;
  }

  public async Task SplitActivity()
  {
    if (fitFile_ is null) { return; }
    if (!CanSplit) { return; }
  
    System.DateTime at = fitFile_.Records[SelectedIndex].InstantOfTime();
    (FitFile first, FitFile second) = fitFile_.SplitAt(at);
   
    await fileService_.CreateAsync(first, "(Split 1)");
    await fileService_.CreateAsync(second, "(Split 2)");
  }
  
  public void SplitLap()
  {
    if (fitFile_ is null) { return; }
    IEdit edit = new SplitLapEdit(fitFile_, fitFile_.Records[SelectedIndex]);
    edit.Apply();
    
    HaveUnsavedChanges = true;
  }

  private void HandleCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
  {
    var column = e.Column as DataGridTextColumn;
    var row = e.Row;

    var fieldName = column?.Header as string;
    if (fieldName == null) { return; }

    var message = row.DataContext as MessageWrapper;
    if (message == null) { return; }

    var editingElement = e.EditingElement as TextBox;
    var newValue = editingElement?.Text;

    bool changed = newValue != message.GetFieldValue(fieldName, PrettifyFields)?.ToString();
    if (!changed) { return; }

    message.SetFieldValue(fieldName, newValue, PrettifyFields);
    HaveUnsavedChanges = true;
  }

  private void HandleCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
  {
    if (e.Column is null) { return; }

    var dg = sender as DataGrid;
    if (dg is null) { return; }

    string? fieldName = e.Column.Header.ToString();
    if (fieldName == null) { return; }

    // Add to the context menu a textbox and button to set all selected cells to the same value
    var menu = dg.ContextMenu;
    if (menu is null) { return; }

    // Find the previously created context menu items
    var setAllTextBox = menu.Items.OfType<TextBox>().FirstOrDefault(x => x.Name == "SetAll");
    var setAllButton = menu.Items.OfType<MenuItem>().FirstOrDefault(x => x.Name == "SetAll" );

    if (setAllTextBox is null) { return; }
    if (setAllButton is null) { return; }

    var messages = dg.SelectedItems.Cast<MessageWrapper>().ToList();
    setAllButton.Command = ReactiveCommand.Create(() => SetFieldValues(messages, fieldName, setAllTextBox.Text));
    setAllButton.Header = $"Set {messages.Count} items";
  }

  private void SetFieldValues(List<MessageWrapper> messages, string fieldName, object? newValue)
  {
    foreach (var message in messages)
    {
      message.SetFieldValue(fieldName, newValue, PrettifyFields);
    }
    HaveUnsavedChanges = true;
  }
}
