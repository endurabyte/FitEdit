using FitEdit.Adapters.Fit.UnitTests.TestData;
using FitEdit.UnitTests.Shared;

namespace FitEdit.Adapters.Fit.UnitTests.MesgDefinition;

using MesgDefinition = Dynastream.Fit.MesgDefinition;

public class WriteMethod
{
  [Fact]
  public void PreservesRoundtrip()
  {
    MemoryStream ms = new(Messages.FileId.Definition);
    var def = new MesgDefinition(ms);

    ms = new MemoryStream();
    def.Write(ms);
    ms.Position = 0;

    var def2 = new MesgDefinition(ms);

    FitAssert.AreEqual(def, def2);
  }
}
