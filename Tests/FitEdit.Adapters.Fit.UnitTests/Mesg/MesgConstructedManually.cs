using Dynastream.Fit;
using FitEdit.Adapters.Fit.Extensions;

namespace FitEdit.Adapters.Fit.UnitTests.Mesg;

using Mesg = Dynastream.Fit.Mesg;
using Fit = Dynastream.Fit.Fit;

public class MesgConstructedManually
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
    fields[1].GetSize().Should().Be(2);
    fields[2].GetSize().Should().Be(1);
    fields[3].GetSize().Should().Be(1);
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

  private Mesg GetMessage()
  {
    var mesg = new Mesg("", 288) // 288 == unknown mesg
    {
      LocalNum = 2,
    };

    mesg.SetField(FieldFactory.FromType(0xfd, Fit.UInt32).WithInvalidValue());
    mesg.SetField(FieldFactory.FromType(0x02, Fit.UInt16).WithInvalidValue());
    mesg.SetField(FieldFactory.FromType(0x00, Fit.SInt8).WithInvalidValue());
    mesg.SetField(FieldFactory.FromType(0x01, Fit.SInt8).WithInvalidValue());

    return mesg;
  }

}
