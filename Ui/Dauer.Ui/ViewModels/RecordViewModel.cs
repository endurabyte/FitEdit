
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Dauer.Data.Fit;
using Dauer.Ui.Converters;
using DynamicData;
using Dynastream.Fit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

public class DisplayedHeader : ReactiveObject
{
  public string? Name { get; set; }
  public bool IsNamed => !Name?.StartsWith("Field ") ?? false;
  public bool IsUsed { get; set; }
  public DataGridColumn? Column { get; set; }
}

public class DisplayedMessageGroup : ReactiveObject
{
  public int Num { get; set; }
  public string? Name { get; set; }
  public bool IsNamed { get; set; }
  public List<DisplayedHeader> Headers { get; set; } = new();

  [Reactive] public bool IsVisible { get; set; }
  [Reactive] public bool IsExpanded { get; set; }
  [Reactive] public DataGrid? DataGrid { get; set; }
}

public class RecordViewModel : ViewModelBase, IRecordViewModel
{
  public ObservableCollection<DisplayedMessageGroup> Groups { get; set; } = new();

  [Reactive] public int SelectedIndex { get; set; }
  [Reactive] public int SelectionCount { get; set; }
  [Reactive] public bool HideUnusedFields { get; set; } = true;
  [Reactive] public bool HideUnnamedFields { get; set; } = true;
  [Reactive] public bool HideUnnamedMessages { get; set; } = true;
  [Reactive] public bool PrettifyFields { get; set; } = true;


  private readonly IFileService fileService_;
  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;
  private DisplayedMessageGroup? records_;
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
      foreach (var group in Groups)
      {
        group.IsVisible = !HideUnnamedMessages || group.IsNamed;
      }
    });
    this.ObservableForProperty(x => x.PrettifyFields).Subscribe(_ =>
    {
      converter_.Prettify = PrettifyFields;
      if (fileService_.MainFile?.FitFile == null) { return; }
      Show(fileService_.MainFile.FitFile);
    });
  }

  private void UpdateColumnVisibility()
  {
    foreach (var group in Groups)
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

  private bool GetIsVisible(DisplayedHeader header) => (header.IsUsed || !HideUnusedFields) && (header.IsNamed || !HideUnnamedFields);

  private void HandleRecordSelectionChanged(object? sender, SelectionChangedEventArgs e)
  {
    if (sender is not DataGrid dg) { return; }
    dg.ScrollIntoView(dg.SelectedItem, dg.CurrentColumn);
  }

  private void HandleMainFileChanged(SelectedFile? file)
  {
    if (file == null) { return; }
    if (file.FitFile == null) { return; }
    Show(file.FitFile);

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
    var expandedGroups = Groups
      .Where(g => g.IsExpanded)
      .ToDictionary(g => g.Name ?? "", g => g.IsExpanded);

    if (records_?.DataGrid != null)
    {
      records_.DataGrid.SelectionChanged -= HandleRecordSelectionChanged;
    }

    foreach (var group in Groups)
    {
      if (group.DataGrid != null)
      {
        group.DataGrid.CellEditEnding -= HandleCellEditEnding;
      }
    }

    Groups.Clear();

    foreach (var kvp in ff.MessagesByDefinition)
    {
      DisplayedMessageGroup group = CreateGroup(ff.MessageDefinitions[kvp.Key], kvp.Value);
      group.IsExpanded = expandedGroups.ContainsKey(group.Name ?? "");
      if (group.DataGrid != null)
      {
        group.DataGrid.CellEditEnding += HandleCellEditEnding;
      }
      Groups.Add(group);
    }

    // When the slider moves, higlight it in the records DataGrid
    var records = Groups.FirstOrDefault(g => g.Num == MesgNum.Record);
    if (records == null) { return; }
    if (records.DataGrid == null) { return; }

    records.DataGrid.SelectionChanged += HandleRecordSelectionChanged;
    records_ = records;
  }

  private DisplayedMessageGroup CreateGroup(MesgDefinition def, List<Mesg> messages)
  {
    Mesg defMesg = Profile.GetMesg(def.GlobalMesgNum);

    var defMessage = new Message(defMesg);
    var msgs = messages.Select(msg => new Message(msg));

    // Categories of fields:
    // Known/unknown fields:
    //   known fields have a name, e.g. "timestamp" or "manufacturer"
    //   unknown fields do not have a name.
    //     They only have a message num e.g. 15, 253.
    //     Garmin labels them "unknown" which we replace with "Field <Num>"
    // Used/unused fields:
    //   used fields appear on the definition or on a amessage
    //   unused fields fields are on the definition but not on any message

    var defNames = new HashSet<string>(defMesg.FieldsByName.Keys);
    var fieldNames = new HashSet<string>(messages
      .SelectMany(m => m.FieldsByName.Keys
      // Replace "unknown" with "Field <Num>"
      .Select(key => key == "unknown" ? $"Field {m.Num}" : key)));

    // On both the definition and at least one message
    var onBoth = new HashSet<string>(defNames);

    // On either the definition or a message
    var onEither = new HashSet<string>(defNames);

    // On only the definition
    var defOnly = new HashSet<string>(defNames);

    // On only a message
    var msgOnly = new HashSet<string>(fieldNames);

    onBoth.IntersectWith(fieldNames);
    onEither.UnionWith(fieldNames);
    defOnly.ExceptWith(fieldNames);
    msgOnly.ExceptWith(defNames);

    List<DisplayedHeader> headers = onEither.Select(name => new DisplayedHeader
    {
      Name = name,
      IsUsed = !defOnly.Contains(name),
    }).ToList();

    var dg = new DataGrid
    {
      ItemsSource = msgs,
      CanUserSortColumns = true,
      CanUserResizeColumns = true,
      CanUserReorderColumns = true,
      IsReadOnly = false,
    };

    // Create columns
    var converter = new MesgFieldValueConverter(prettify: PrettifyFields);
    foreach (var header in headers)
    {
      var col = new DataGridTextColumn
      {
        Header = header.Name,
        Binding = new Binding
        {
          Converter = converter,
          ConverterParameter = header.Name,
          Mode = BindingMode.TwoWay,
        },
        IsReadOnly = false,
        IsVisible = GetIsVisible(header),
      };

      header.Column = col;
    }

    foreach (var header in headers)
    {
      dg.Columns.Add(header.Column);
    }

    var group = new DisplayedMessageGroup
    {
      Num = defMesg.Num,
      Name = $"{(defMesg.Name == "unknown" ? $"Message # {defMesg.Num}" : defMesg.Name)} "
           + $"({messages.Count})",
      DataGrid = dg,
      IsNamed = defMessage.IsNamed,
      IsVisible = defMessage.IsNamed || !HideUnnamedMessages,
    };
    group.Headers.AddRange(headers);

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
    var message = row.DataContext as Message;

    var editingElement = e.EditingElement as TextBox;
    var newContent = editingElement?.Text;

    if (message == null) { return; }
    if (header == null) { return; }
    if (converter == null) { return; }

    converter.ConvertBack(message, typeof(Message), (header, newContent), CultureInfo.CurrentCulture);
  }
}
