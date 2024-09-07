using Dynastream.Fit;
using FitEdit.Adapters.Fit.UnitTests.TestData;
using FitEdit.Data.Fit;

namespace FitEdit.Adapters.Fit.UnitTests.Mesg;

using Mesg = Dynastream.Fit.Mesg;

public class WriteMethod
{
  [Fact]
  public void DoesNotModifyFields()
  {
    // Arrange
    var def = GetDefinition();
    var mesg = GetMessage();

    var localNum = mesg.LocalNum;
    var num = mesg.Num;
    var fieldValues = mesg.Fields.Select(kvp => kvp.Value.GetValue());

    // Act
    mesg.Write(new MemoryStream(), def);

    // Assert
    mesg.LocalNum.Should().Be(localNum);
    mesg.Num.Should().Be(num);
    mesg.Fields.Select(kvp => kvp.Value.GetValue()).Should().BeEquivalentTo(fieldValues);
  }

  [Fact]
  public void RoundtripPreservesFields()
  {
    // Arrange
    var def = GetDefinition();
    var mesg = GetMessage();
    var ms = new MemoryStream();

    // Act
    mesg.Write(ms, def);
    ms.Position = 0;

    var mesg2 = new Mesg(ms, def);

    // Assert
    AssertEqual(mesg, mesg2);
  }

  [Fact]
  public void RoundtripPreservesByteArray()
  {
    // Arrange
    var def = GetDefinition();
    var mesg = GetMessage();
    var ms = new MemoryStream();

    // Act
    mesg.Write(ms, def);
    ms.Position = 0;

    var buf = Messages.Num288.Message;
    var buf2 = ms.ToArray();

    // Assert
    buf2.Should().BeEquivalentTo(buf);
  }

  [Fact]
  public void PreservesFieldStringValue()
  {
    SportMesg mesg = MessageFactory.Create<SportMesg>();
    mesg.SetName("asdf1234");

    var ms = new MemoryStream();
    mesg.Write(ms);
    ms.Position = 0;

    var mesg2 = MessageFactory.Create<SportMesg>(ms, new MesgDefinition(mesg));
    mesg2.GetNameAsString().Should().Be("asdf1234");
  }

  private static void AssertEqual(Mesg mesg, Mesg mesg2)
  {
    mesg2.LocalNum.Should().Be(mesg.LocalNum);
    mesg2.Num.Should().Be(mesg.Num);
    mesg2.Fields.Count.Should().Be(mesg.Fields.Count);

    foreach (var kvp in mesg.Fields)
    {
      var field = mesg2.Fields[kvp.Key];
      field.Num.Should().Be(kvp.Value.Num);
      field.GetSize().Should().Be(kvp.Value.GetSize());
      field.Type.Should().Be(kvp.Value.Type);
      field.GetValue().Should().BeEquivalentTo(kvp.Value.GetValue());
    }
  }

  private Mesg GetMessage() => new(new MemoryStream(Messages.Num288.Message), GetDefinition());
  private MesgDefinition GetDefinition() => new(new MemoryStream(Messages.Num288.Definition));
}