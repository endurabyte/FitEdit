using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IFileService
{
  SelectedFile? MainFile { get; set; }
  ObservableCollection<SelectedFile> Files { get; set; }
}

/// <summary>
/// Encapsulates the shared state of the loaded file and which record is currently visualized / editable.
/// </summary>
public class FileService : ReactiveObject, IFileService
{
  [Reactive] public SelectedFile? MainFile { get; set; }
  [Reactive] public ObservableCollection<SelectedFile> Files { get; set; } = new();
}

