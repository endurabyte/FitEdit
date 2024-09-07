using Dynastream.Fit;
using FitEdit.Adapters.Fit.Extensions;

namespace FitEdit.Adapters.Fit.UnitTests.Mesg;

using Fit = Dynastream.Fit.Fit;

public class FieldFactoryFromTypeMethod
{
  [Theory]
  [InlineData(0xfd, Fit.UInt32)]
  [InlineData(0x02, Fit.UInt16)]
  [InlineData(0x00, Fit.SInt8)]
  [InlineData(0x01, Fit.SInt8)]
  public void HasExactNum(byte num, byte type)
  {
    Field field = FieldFactory.FromType(num, type).WithInvalidValue();
    field.Num.Should().Be(num);
  }

  [Theory]
  [InlineData(0xfd, Fit.UInt32, 4)]
  [InlineData(0x02, Fit.UInt16, 2)]
  [InlineData(0x00, Fit.SInt8, 1)]
  [InlineData(0x01, Fit.SInt8, 1)]
  public void HasExactSize(byte num, byte type, byte expectedSize)
  {
    Field field = FieldFactory.FromType(num, type).WithInvalidValue();
    byte size = field.GetSize();
    size.Should().Be(expectedSize);
  }

  [Theory]
  [InlineData(0xfd, Fit.UInt32, typeof(uint))]
  [InlineData(0x02, Fit.UInt16, typeof(ushort))]
  [InlineData(0x00, Fit.SInt8, typeof(sbyte))]
  [InlineData(0x01, Fit.SInt8, typeof(sbyte))]
  public void HasExactType(byte num, byte type, Type systemType)
  {
    Field field = FieldFactory.FromType(num, type).WithInvalidValue();
    field.Type.Should().Be(FitTypes.TypeIndexMap[type].baseTypeField);
    field.GetValue().Should().BeOfType(systemType);
  }
}
