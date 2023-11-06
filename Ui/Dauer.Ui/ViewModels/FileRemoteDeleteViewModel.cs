using System.Collections.ObjectModel;
using Dauer.Data;
using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.GarminConnect;
using Dauer.Model.Strava;
using Dauer.Ui.Model.Supabase;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public class FileRemoteDeleteViewModel : ViewModelBase
{
  private readonly IGarminConnectClient garmin_;
  private readonly IStravaClient strava_;
  private readonly ISupabaseAdapter supa_;
  private readonly ILogger<FileRemoteDeleteViewModel> log_;

  [Reactive] public bool IsConfirmingDelete { get; set; }
  [Reactive] public ObservableCollection<UiFile> FilesToDelete { get; set; } = new();

  public FileRemoteDeleteViewModel(
    IGarminConnectClient garmin,
    IStravaClient strava,
    ISupabaseAdapter supa,
    ILogger<FileRemoteDeleteViewModel> log
  )
  {
    garmin_ = garmin;
    strava_ = strava;
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

  public async Task HandleConfirmDeleteClicked()
  {
    IsConfirmingDelete = false;

    foreach (UiFile uif in FilesToDelete)
    {
      if (uif.Activity == null) { return; }
      if (!long.TryParse(uif.Activity.SourceId, out long id)) { return; }

      bool ok = uif.Activity.Source switch
      {
        ActivitySource.GarminConnect => await garmin_.DeleteActivity(id).AnyContext(),
        ActivitySource.Strava => await strava_.DeleteActivityAsync(id).AnyContext(),
        _ => false,
      };

      if (ok)
      {
        uif.Activity.Source = ActivitySource.File; // Must be File to be re-uploadable
        uif.Activity.SourceId = "";
        await supa_.UpdateAsync(uif.Activity);
      }
    }

    FilesToDelete.Clear();
  }

  public void HandleCancelDeleteClicked()
  {
    IsConfirmingDelete = false;
    FilesToDelete.Clear();
  }
}
