namespace FitEdit.Data.Fit.Edits;

public interface IEdit
{
  FitFile Apply(FitFile file);
}

public class RemoveGapsEdit : IEdit
{
  public FitFile Apply(FitFile file)
  {
    var minGap = TimeSpan.FromSeconds(60);

    var copy = new FitFile(file);
    var records = copy.Records;

    // Algorithm:
    // For each pair of successive records i and i + 1
    // where time span between them differs by more than the min gap,
    // make the time span between them 1s and update all later timestamps accordingly.
    foreach (int i in Enumerable.Range(0, records.Count - 1))
    {
      DateTime start = records[i].GetTimestamp().GetDateTime();
      DateTime end = records[i + 1].GetTimestamp().GetDateTime();
      TimeSpan gap = end - start;

      if (gap < minGap)
        continue;

      SetRecordStartTimeCascading(records, i + 1, start + TimeSpan.FromSeconds(1));
    }

    copy.BackfillEvents();

    return copy;
  }

  /// <summary>
  /// Set record i to the given start time, and shift all subsequent record timestamps by the time diff.
  /// </summary>
  private static void SetRecordStartTimeCascading(List<Dynastream.Fit.RecordMesg> records, int i, DateTime start)
  {
    // Set record i to the given start time. Keep track of the time diff.
    DateTime oldEnd = records[i].GetTimestamp().GetDateTime();
    records[i].SetTimestamp(new Dynastream.Fit.DateTime(start));
    DateTime newEnd = records[i].GetTimestamp().GetDateTime();

    TimeSpan diff = oldEnd - newEnd;

    // Shift all subsequent record timestamps
    ShiftTimestamps(records, i + 1, diff);
  }

  private static void ShiftTimestamps(List<Dynastream.Fit.RecordMesg> records, int i, TimeSpan diff)
  {
    for (int j = i; j < records.Count - 1; j++)
    {
      DateTime start = records[j].GetTimestamp().GetDateTime();

      records[j].SetTimestamp(new Dynastream.Fit.DateTime(start - diff));
    }
  }
}
