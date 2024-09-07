using Dynastream.Fit;
using FitEdit.Adapters.Fit.UnitTests.TestData;

namespace FitEdit.Adapters.Fit.UnitTests.Mesg;

using Mesg = Dynastream.Fit.Mesg;

public class MesgConstructedFromStream
{
  [Fact]
  public void HasLocalMesgNum() => GetMessage().LocalNum.Should().Be(2);

  [Fact]
  public void HasGlobalMesgNum() => GetMessage().Num.Should().Be(288);

  [Fact]
  public void HasNumFields() => GetMessage().Fields.Count.Should().Be(4);

  [Fact]
  public void FieldCountMatches() => GetFields().Should().HaveCount(4);

  [Fact]
  public void FieldNumsMatch()
  {
    List<Field> fields = GetFields();

    fields[0].Num.Should().Be(0xfd);
    fields[1].Num.Should().Be(0x02);
    fields[2].Num.Should().Be(0x00);
    fields[3].Num.Should().Be(0x01);
  }

  [Fact]
  public void FieldSizesMatch()
  {
    List<Field> fields = GetFields();

    fields[0].GetSize().Should().Be(4);
    fields[1].GetSize().Should().Be(0);
    fields[2].GetSize().Should().Be(0);
    fields[3].GetSize().Should().Be(0);
  }

  [Fact]
  public void FieldTypesMatch()
  {
    List<Field> fields = GetFields();

    fields[0].Type.Should().Be(0x86);
    fields[1].Type.Should().Be(0x84);
    fields[2].Type.Should().Be(0x01);
    fields[3].Type.Should().Be(0x01);
  }

  private List<Field> GetFields() => GetMessage().Fields.Select(kvp => kvp.Value).ToList();
  private Mesg GetMessage() => new(new MemoryStream(Messages.Num288.Message), GetDefinition());
  private MesgDefinition GetDefinition() => new(new MemoryStream(Messages.Num288.Definition));
}
