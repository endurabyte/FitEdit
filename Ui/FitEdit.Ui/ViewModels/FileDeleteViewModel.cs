using System.Collections.ObjectModel;
using FitEdit.Data;
using FitEdit.Ui.Model.Supabase;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.ViewModels;

public class FileDeleteViewModel : ViewModelBase
{
  private readonly IFileService fileService_;
  private readonly ISupabaseAdapter supa_;
  private readonly ILogger<FileDeleteViewModel> log_;

  [Reactive] public bool IsConfirmingDelete { get; set; }
  [Reactive] public ObservableCollection<UiFile> FilesToDelete { get; set; } = new();

  public FileDeleteViewModel(
    IFileService fileService,
    ISupabaseAdapter supa,
    ILogger<FileDeleteViewModel> log
  )
  {
    fileService_ = fileService;
    supa_ = supa;
    log_ = log;

    this.ObservableForProperty(x => x.IsConfirmingDelete).Subscribe(_ =>
    {
      IsVisible = IsConfirmingDelete;
    });
  }
  
  public void BeginDelete(UiFile uif)
  {
    if (uif == null) { return; }

    FilesToDelete.Add(uif);
    IsConfirmingDelete = true;
  }

  public void HandleConfirmDeleteClicked()
  {
    IsConfirmingDelete = false;

    foreach (UiFile uif in FilesToDelete)
    {
      int index = fileService_.Files.IndexOf(uif);
      if (index < 0 || index >= fileService_.Files.Count)
      {
        log_.LogWarning("No file selected; cannot remove file");
        return;
      }

      Remove(index);
    }

    FilesToDelete.Clear();
  }

  public void HandleCancelDeleteClicked()
  {
    IsConfirmingDelete = false;
    FilesToDelete.Clear();
  }

  private void Remove(int index)
  {
    UiFile file = fileService_.Files[index];
    fileService_.Files.Remove(file);

    _ = Task.Run(async () =>
    {
      await fileService_.DeleteAsync(file.Activity);
      await supa_.DeleteAsync(file.Activity);
    });
  }

}
