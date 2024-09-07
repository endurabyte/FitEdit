using FitEdit.UnitTests.Shared;

namespace FitEdit.Data.UnitTests.Tcx.Reader;

  public class ReadMethod
  {
      [Fact]
      public void ReadsString()
      {
          var treadmill = Data.Tcx.Reader.Read(TcxFixtures.GetTreadmillWorkout());
          var gps = Data.Tcx.Reader.Read(TcxFixtures.GetGpsWorkout());
      }
  }
