using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Dauer.Data.Fit;
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

public class DisplayedMessageGroup : ReactiveObject
{
  public int Num { get; set; }
  public string? Name { get; set; }
  [Reactive] public DataGrid? DataGrid { get; set; }
}

public class RecordViewModel : ViewModelBase, IRecordViewModel
{
  public ObservableCollection<DisplayedMessageGroup> Groups { get; set; } = new();

  [Reactive] public int SelectedIndex { get; set; }
  [Reactive] public int SelectionCount { get; set; }

  private readonly IFileService fileService_;
  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;
  private DisplayedMessageGroup? records_;

  public RecordViewModel(
    IFileService fileService
  )
  {
    fileService_ = fileService;

    fileService.ObservableForProperty(x => x.MainFile).Subscribe(property => HandleMainFileChanged(fileService.MainFile));

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      if (fileService_.MainFile == null) { return; }
      fileService_.MainFile.SelectedIndex = property.Value;
    });

  }

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
    foreach (var kvp in ff.MessagesByDefinition)
    {
      MesgDefinition def = ff.MessageDefinitions[kvp.Key];
      Mesg mesg = Profile.GetMesg(def.GlobalMesgNum);
      List<string> fields = mesg.FieldsByName.Keys.ToList();

      if (!fields.Any())
      {
        // If the definition defines no fields, Fall back to fields defined on the first message
        fields = kvp.Value.FirstOrDefault()?.FieldsByName.Keys.ToList() ?? new List<string>();
      }

      var converter = new MesgFieldValueConverter();
      var columns = fields.Select(field => new DataGridTextColumn
      {
        Header = field,
        Binding = new Binding
        {
          Converter = converter,
          ConverterParameter = field,
          Mode = BindingMode.OneWay,
        },
      });

      var dg = new DataGrid
      {
        ItemsSource = kvp.Value,
        CanUserSortColumns = true,
        CanUserResizeColumns = true,
        CanUserReorderColumns = true,
      };

      foreach (var column in columns)
      {
        dg.Columns.Add(column);
      }

      Groups.Add(new DisplayedMessageGroup
      {
        Num = mesg.Num,
        Name = $"{(mesg.Name == "unknown" ? $"Message Num {mesg.Num}" : mesg.Name)} "
             + $"({kvp.Value.Count} {(kvp.Value.Count == 1 ? "row" : "rows")})",
        DataGrid = dg
      });
    }

    var records = Groups.FirstOrDefault(g => g.Num == MesgNum.Record);
    if (records == null) { return; }
    if (records.DataGrid == null) { return; }

    records.DataGrid.SelectionChanged += HandleRecordSelectionChanged;
    records_ = records;
  }
}
