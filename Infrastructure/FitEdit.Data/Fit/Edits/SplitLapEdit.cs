using Dynastream.Fit;

namespace FitEdit.Data.Fit.Edits;

public class SplitLapEdit(FitFile fit, RecordMesg rec) : IEdit
{
  public FitFile Apply()
  {
    var lap = FindLapContaining(fit, rec);
    
    var records1 = fit.GetRecords(lap.Start(), rec.InstantOfTime());
    var records2 = fit.GetRecords(rec.InstantOfTime(), lap.End());
    
    var start1 = records1.First().InstantOfTime();
    var end1 = records1.Last().InstantOfTime();
    
    var start2 = records2.First().InstantOfTime();
    var end2 = records2.Last().InstantOfTime();
    
    var lap1 = FitFileExtensions.ReconstructLap(records1, start1, end1);
    var lap2 = FitFileExtensions.ReconstructLap(records2, start2, end2);
    
    fit.Remove(lap);
    fit.Add(lap1);
    fit.Add(lap2);
    
    fit.ForwardfillEvents();
    return fit;
  }
  
  // Find the lap the RecordMesg belongs to
  private static LapMesg FindLapContaining(FitFile fit, RecordMesg record) => fit.Get<LapMesg>().FirstOrDefault(lap => record.IsBetween(lap.Start(), lap.End()));
}