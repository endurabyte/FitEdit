﻿#nullable enable
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Model.GarminConnect;

public class NullGarminConnectClient : ReactiveObject, IGarminConnectClient
{
  public GarminConnectConfig Config { get; set; } = new();
  [Reactive] public double AuthenticateProgress { get; private set; }
  [Reactive] public bool IsSignedIn { get; set; }
  public Dictionary<string, Cookie>? Cookies { get => null; set => IsSignedIn = false; }

  public NullGarminConnectClient()
  {
    _ = Task.Run(AuthenticateAsync);
  }

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
  public Task<byte[]> DownloadActivityFile(long activityId, ActivityFileType fileFormat) => Task.FromResult(Array.Empty<byte>());
  public Task<List<GarminActivity>> LoadActivities(int limit, int start, DateTime after, DateTime before, CancellationToken ct = default) => Task.FromResult(new List<GarminActivity>());
  public Task<GarminActivity> LoadActivity(long activityId) => Task.FromResult(new GarminActivity());
  public Task<List<ActivityType>> LoadActivityTypes() => Task.FromResult(new List<ActivityType>());
  public Task<GarminFitnessStats> GetLifetimeFitnessStats(CancellationToken ct = default) => Task.FromResult(new GarminFitnessStats());
  public Task<List<GarminFitnessStats>> GetYearyFitnessStats(CancellationToken ct = default) => Task.FromResult(new List<GarminFitnessStats>());
  public Task<bool> SetActivityDescription(long activityId, string description) => Task.FromResult(true);
  public Task<bool> SetActivityName(long activityId, string activityName) => Task.FromResult(true);
  public Task<bool> SetActivityType(long activityId, ActivityType activityType) => Task.FromResult(true);
  public Task<bool> DeleteActivity(long activityId) => Task.FromResult(true);
  public Task<bool> SetEventType(long activityId, ActivityType eventType) => Task.FromResult(true);
  public Task<(bool Success, long ActivityId)> UploadActivity(Stream stream, FileFormat fileFormat) => Task.FromResult((true, 0L));
  public Task<(bool Success, long ActivityId)> UploadActivity(string fileName, FileFormat fileFormat) => Task.FromResult((true, 0L));
}