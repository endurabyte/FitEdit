#nullable enable
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Dauer.Model;

namespace Dauer.Data;

public class NullFileService : IFileService
{
  public UiFile? MainFile { get; set; }
  public ObservableCollection<UiFile> Files { get; set; } = new();

  public IObservable<DauerActivity> Deleted => deletedSubject_;
  private readonly ISubject<DauerActivity> deletedSubject_ = new Subject<DauerActivity>();

  public NullFileService()
  {
    int i = 0;

    AddFake(i++);
    AddFake(i++);
    AddFake(i++);
    var file = AddFake(i++);

    file.Activity = new DauerActivity
    {
      Name = "Activity name",
      Description = "This is the activity description.\nIt can get pretty long, so make sure to provide enough space for it, but it should be responsive in case it is not very long.",
      File = new FileReference("fitfile.fit", Array.Empty<byte>())
    };
  }

  private UiFile AddFake(int i)
  {
    var file = new UiFile
    {
      Activity = new DauerActivity
      {
        Id = $"{Guid.NewGuid()}",
        Name = $"Workout {i}",
        Description = $"Description {i}"
      },
      Progress = 66.7,
    };

    Files.Add(file);
    return file;
  }

  public Task<bool> CreateAsync(DauerActivity? act, CancellationToken ct = default) => Task.FromResult(true);
  public Task<bool> DeleteAsync(DauerActivity? act) => Task.FromResult(true);
  public Task<List<string>> GetAllActivityIdsAsync(DateTime? after, DateTime? before) => Task.FromResult(new List<string>());
  public Task<List<DauerActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before) => Task.FromResult(new List<DauerActivity>());
  public Task<DauerActivity?> ReadAsync(string id, CancellationToken ct = default) => Task.FromResult((DauerActivity?)new DauerActivity { Id = id });
  public Task<bool> UpdateAsync(DauerActivity? act, CancellationToken ct = default) => Task.FromResult(true);
  public void Add(UiFile file) { }
}
