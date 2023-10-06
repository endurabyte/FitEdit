#nullable enable
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Dauer.Data.Fit;
using Dauer.Model;

namespace Dauer.Data;

public class NullFileService : IFileService
{
  public UiFile? MainFile { get; set; }
  public ObservableCollection<UiFile> Files { get; set; } = new();

  public IObservable<LocalActivity> Deleted => deletedSubject_;
  private readonly ISubject<LocalActivity> deletedSubject_ = new Subject<LocalActivity>();

  public NullFileService()
  {
    int i = 0;

    var activity = new LocalActivity
    {
      Id = $"{Guid.NewGuid()}",
      Name = "Workout with imported, uploaded file",
      Description = "This is the activity description.\nIt can get pretty long, so make sure to provide enough space for it, but it should be responsive in case it is not very long.",
      File = new FileReference("fitfile.fit", Array.Empty<byte>()),
      Source = ActivitySource.GarminConnect,
      SourceId = "12345678",
    };

    Files.Add(new UiFile
    {
      Activity = activity,
      Progress = 66.7,
    });

    var file = new UiFile
    {
      Activity = new LocalActivity
      {
        Id = $"{Guid.NewGuid()}",
        Name = $"Workout with imported file but not uploaded",
        Description = null,
        File = new FileReference("fitfile.fit", Array.Empty<byte>()),
        SourceId = "",
      },
      Progress = 0,
    };

    Files.Add(file);

    AddFake(i++);
    AddFake(i++);
  }

  private UiFile AddFake(int i)
  {
    var file = new UiFile
    {
      Activity = new LocalActivity
      {
        Id = $"{Guid.NewGuid()}",
        Name = $"Workout {i}",
        Description = $"Description {i}"
      },
      Progress = 0,
    };

    Files.Add(file);
    return file;
  }

  public Task CreateAsync(FitFile fit) => Task.CompletedTask;
  public Task<bool> CreateAsync(LocalActivity? act, CancellationToken ct = default) => Task.FromResult(true);
  public Task<bool> DeleteAsync(LocalActivity? act) => Task.FromResult(true);
  public Task<List<string>> GetAllActivityIdsAsync(DateTime? after, DateTime? before) => Task.FromResult(new List<string>());
  public Task<List<LocalActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before, int limit) => Task.FromResult(new List<LocalActivity>());
  public Task<LocalActivity?> ReadAsync(string id, CancellationToken ct = default) => Task.FromResult((LocalActivity?)new LocalActivity { Id = id });
  public Task<bool> UpdateAsync(LocalActivity? act, CancellationToken ct = default) => Task.FromResult(true);
  public Task<bool> ActivityExistsAsync(string id) => Task.FromResult(false);
  public void Add(UiFile file) { }
  public Task LoadMore() => Task.CompletedTask;
}
