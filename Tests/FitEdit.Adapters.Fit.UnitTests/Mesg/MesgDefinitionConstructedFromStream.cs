using Dynastream.Fit;
using FitEdit.Adapters.Fit.UnitTests.TestData;

namespace FitEdit.Adapters.Fit.UnitTests.Mesg;

using MesgDefinition = Dynastream.Fit.MesgDefinition;

public class MesgDefinitionConstructedFromStream
{
  [Fact]
  public void HasLocalMesgNum() => GetDefinition().LocalMesgNum.Should().Be(2);

  [Fact]
  public void HasArchitecture() => GetDefinition().IsBigEndian.Should().BeFalse();

  [Fact]
  public void HasGlobalMesgNum() => GetDefinition().GlobalMesgNum.Should().Be(288);

  [Fact]
  public void HasNumFields() => GetDefinition().NumFields.Should().Be(4);

  [Fact]
  public void FieldCountMatches() => GetFields().Should().HaveCount(4);

  [Fact]
  public void FieldNumsMatch()
  {
    List<FieldDefinition> fields = GetFields();

    fields[0].Num.Should().Be(0xfd);
    fields[1].Num.Should().Be(0x02);
    fields[2].Num.Should().Be(0x00);
    fields[3].Num.Should().Be(0x01);
  }

  [Fact]
  public void FieldSizesMatch()
  {
    List<FieldDefinition> fields = GetFields();

    fields[0].Size.Should().Be(4);
    fields[1].Size.Should().Be(2);
    fields[2].Size.Should().Be(1);
    fields[3].Size.Should().Be(1);
  }

  [Fact]
  public void FieldTypesMatch()
  {
    List<FieldDefinition> fields = GetFields();

    fields[0].Type.Should().Be(0x86);
    fields[1].Type.Should().Be(0x84);
    fields[2].Type.Should().Be(0x01);
    fields[3].Type.Should().Be(0x01);
  }

  private List<FieldDefinition> GetFields() => GetDefinition().GetFields();
  private MesgDefinition GetDefinition() => new(new MemoryStream(Messages.Num288.Definition));
}
