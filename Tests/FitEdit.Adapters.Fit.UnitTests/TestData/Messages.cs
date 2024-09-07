namespace FitEdit.Adapters.Fit.UnitTests.TestData;

internal class Messages
{
  internal class Num288
  {
    public static byte[] Definition => [ // 288 is an unknown mesg
      0x02, // header: local mesg num
      0x0, // reserved
      0x0, // architecture
      0x20, 0x01, // global mesg num (UInt16)
      0x04, // Num fields
      0xfd, 0x04, 0x86, // num, size, type (UInt32)
      0x02, 0x02, 0x84, // num, size, type (UInt16)
      0x00, 0x01, 0x01, // num, size, type (SInt8)
      0x01, 0x01, 0x01, // num, size, type (SInt8)
    ];

    public static byte[] Message => [
      0x02, // header: local mesg num

      // UInt32, value == 0xf0765b38 (== 945518320) (is a timestamp)
      0xf0,
      0x76,
      0x5b,
      0x38,

      // UInt16, value == 0xffff (== 65535) (== invalid)
      0xff,
      0xff,

      // SInt8, value == 0x7f (== 127) (== invalid)
      0x7f,

      // SInt8, value == 0x7f (== 127) (== invalid)
      0x7f
    ];
  }
}
