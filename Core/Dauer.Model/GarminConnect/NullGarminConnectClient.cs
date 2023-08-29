#nullable enable
namespace Dauer.Model.GarminConnect;

public class NullGarminConnectClient : IGarminConnectClient
{
  public GarminConnectConfig Config { get; set; } = new();

  public void AddCookies(Dictionary<string, Cookie>? cookies) { }
  public Dictionary<string, Cookie> GetCookies() => new();
  public Task<bool> AuthenticateAsync() => Task.FromResult(true);
  public Task<bool> IsAuthenticatedAsync() => Task.FromResult(true);
  public Task<Stream> DownloadActivityFile(long activityId, ActivityFileType fileFormat) => Task.FromResult(new MemoryStream() as Stream);
  public Task<List<Activity>> LoadActivities(int limit, int start, DateTime from) => Task.FromResult(new List<Activity>());
  public Task<Activity> LoadActivity(long activityId) => Task.FromResult(new Activity());
  public Task<List<ActivityType>> LoadActivityTypes() => Task.FromResult(new List<ActivityType>());
  public Task SetActivityDescription(long activityId, string description) => Task.CompletedTask;
  public Task SetActivityName(long activityId, string activityName) => Task.CompletedTask;
  public Task SetActivityType(long activityId, ActivityType activityType) => Task.CompletedTask;
  public Task<(bool Success, long ActivityId)> UploadActivity(string fileName, FileFormat fileFormat) => Task.FromResult((true, 0L));
}