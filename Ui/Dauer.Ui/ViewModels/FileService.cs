using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IFileService
{
  UiFile? MainFile { get; set; }
  ObservableCollection<UiFile> Files { get; set; }
}

/// <summary>
/// Encapsulates the shared state of the loaded file and which record is currently visualized / editable.
/// </summary>
public class FileService : ReactiveObject, IFileService
{
  [Reactive] public UiFile? MainFile { get; set; }
  [Reactive] public ObservableCollection<UiFile> Files { get; set; } = new();
}

