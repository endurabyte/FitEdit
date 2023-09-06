#nullable enable
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Model.GarminConnect;

public class NullGarminConnectClient : ReactiveObject, IGarminConnectClient
{
  public GarminConnectConfig Config { get; set; } = new();
  [Reactive] public double AuthenticateProgress { get; private set; }
  [Reactive] public bool IsSignedIn { get; set; }
  public Dictionary<string, Cookie>? Cookies { get => null; set => IsSignedIn = false; }

  public async Task<bool> AuthenticateAsync()
  {
    AuthenticateProgress = 0;
    await Task.Delay(200);
    AuthenticateProgress = 20;
    await Task.Delay(200);
    AuthenticateProgress = 40;
    await Task.Delay(200);
    AuthenticateProgress = 60;
    await Task.Delay(200);
    AuthenticateProgress = 80;
    await Task.Delay(200);
    AuthenticateProgress = 100;
    IsSignedIn = true;

    return true;
  }

  public Task<bool> LogoutAsync()
  {
    IsSignedIn = false;
    return Task.FromResult(true);
  }

  public Task<bool> IsAuthenticatedAsync() => Task.FromResult(IsSignedIn);
  public Task<Stream> DownloadActivityFile(long activityId, ActivityFileType fileFormat) => Task.FromResult(new MemoryStream() as Stream);
  public Task<List<Activity>> LoadActivities(int limit, int start, DateTime from) => Task.FromResult(new List<Activity>());
  public Task<Activity> LoadActivity(long activityId) => Task.FromResult(new Activity());
  public Task<List<ActivityType>> LoadActivityTypes() => Task.FromResult(new List<ActivityType>());
  public Task SetActivityDescription(long activityId, string description) => Task.CompletedTask;
  public Task SetActivityName(long activityId, string activityName) => Task.CompletedTask;
  public Task SetActivityType(long activityId, ActivityType activityType) => Task.CompletedTask;
  public Task<(bool Success, long ActivityId)> UploadActivity(string fileName, Stream stream, FileFormat fileFormat) => Task.FromResult((true, 0L));
  public Task<(bool Success, long ActivityId)> UploadActivity(string fileName, FileFormat fileFormat) => Task.FromResult((true, 0L));
}