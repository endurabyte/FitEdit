#nullable enable
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Model.Strava;

public interface IStravaClient
{
  StravaConfig Config { get; set; }
  bool IsSignedIn { get;  }
  Dictionary<string, Cookie>? Cookies { get; set; }

  /// <summary>
  /// Progress 0-100 of the last call to <see cref="AuthenticateAsync"/>
  /// 100 shall not be a programmatic indicator of completion; for that, use the return value of <see cref="AuthenticateAsync"/>
  /// </summary>
  double AuthenticateProgress { get; }

  /// <summary>
  /// Authenticates this instance.
  /// </summary>
  /// <returns>Tuple of Cookies and HTTP handler</returns>
  Task<bool> AuthenticateAsync();

  /// <summary>
  /// Return true if the SESSIONID cookie is present and a request to Garmin Connect succeeds
  /// </summary>
  Task<bool> IsAuthenticatedAsync();

  Task<bool> LogoutAsync();

  Task<List<StravaActivity>> ListAllActivitiesAsync(UserTask task, CancellationToken ct = default);
  Task<byte[]> DownloadActivityFileAsync(long id, CancellationToken ct = default);
  Task<(bool Success, long ActivityId)> UploadActivityAsync(Stream stream);
  Task<bool> DeleteActivityAsync(long id);
}

public class NullStravaClient : ReactiveObject, IStravaClient
{
  public StravaConfig Config { get; set; } = new();
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
  public Task<List<StravaActivity>> ListAllActivitiesAsync(UserTask task, CancellationToken ct = default) => Task.FromResult(new List<StravaActivity>());
  public Task<byte[]> DownloadActivityFileAsync(long id, CancellationToken ct = default) => Task.FromResult(Array.Empty<byte>());
  public Task<(bool Success, long ActivityId)> UploadActivityAsync(Stream stream) => Task.FromResult((false, -1L));
  public Task<bool> DeleteActivityAsync(long id) => Task.FromResult(false);
}
