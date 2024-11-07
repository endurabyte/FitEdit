using Dynastream.Fit;
using FitEdit.Adapters.Fit.UnitTests.TestData;

namespace FitEdit.Adapters.Fit.UnitTests.Mesg;

using Mesg = Dynastream.Fit.Mesg;
using MesgDefinition = Dynastream.Fit.MesgDefinition;

public class Constructor
{
  [Fact]
  public void IgnoresDuplicateProductName()
  {
    // Arrange
    var mesg = GetMessage();
    var def = GetDefinition();
    var ms = new MemoryStream();

    // Act
    mesg.Write(ms, def);

    // Assert
    byte[] bytes = ms.ToArray();
    bytes.Should().BeEquivalentTo(Messages.DuplicateProductNameDeviceInfo.Message);
  }

  private List<Field> GetFields() => GetMessage().Fields.Select(kvp => kvp.Value).ToList();
  private Mesg GetMessage() => new(new MemoryStream(Messages.DuplicateProductNameDeviceInfo.Message), GetDefinition());
  private MesgDefinition GetDefinition() => new(new MemoryStream(Messages.DuplicateProductNameDeviceInfo.Definition));
}
