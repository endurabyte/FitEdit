using FitEdit.Model.Extensions;
using Dynastream.Fit;
#nullable enable

namespace FitEdit.Data.Fit;

public class MessageWrapperMerger
{
  /// <summary>
  /// Merge all selected laps between and including the first and last.
  /// Note: unselected laps between the first and last are also merged!
  /// </summary>
  public MessageWrapper? Merge(List<MessageWrapper> allWrappers, List<MessageWrapper> selectedWrappers)
  {
    var allLaps = allWrappers.Select(mesg => (LapMesg)mesg.Mesg).ToList();
    var selectedLaps = selectedWrappers.Select(mesg => (LapMesg)mesg.Mesg).ToList();

    if (selectedLaps.Count < 2) { return null; }

    if (allLaps.Any(l => l == null))
    {
      throw new ArgumentException("Can only merge lap messages");
    }

    // Sort laps by start time (they should already be sorted but just in case)
    allLaps.Sorted((a, b) => a.Start().CompareTo(b.Start()));
    selectedLaps.Sorted((a, b) => a.Start().CompareTo(b.Start()));

    LapMesg? first = selectedLaps.FirstOrDefault();
    LapMesg? last = selectedLaps.LastOrDefault();

    if (first == null) { return null; }
    if (last == null) { return null; }

    var toMerge = selectedLaps
      .Where(l => l.Start() >= first.Start() && l.End() <= last.End())
      .ToList();

    LapMesg? merged = new LapMerger().Merge(toMerge);

    return merged == null
      ? null
      : new MessageWrapper(merged);
  }
}
