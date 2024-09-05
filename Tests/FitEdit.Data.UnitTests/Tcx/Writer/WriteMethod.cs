using FitEdit.UnitTests.Shared;

namespace FitEdit.Data.UnitTests.Tcx.Writer;

  public class WriteMethod
  {
      [Fact]
      public void WritesString()
      {
          var db = TcxFixtures.GetTrainingCenterDatabase();
          string xml = Data.Tcx.Writer.Write(db);
      }
  }