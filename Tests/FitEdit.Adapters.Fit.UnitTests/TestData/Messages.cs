namespace FitEdit.Adapters.Fit.UnitTests.TestData;

internal class Messages
{
  // 288 is an unknown mesg
  internal class Num288
  {
    public static byte[] Definition => 
    [

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

    public static byte[] Message => 
    [
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

  public static class FileId
  {
    public static byte[] Definition => 
    [
      64, // header: local mesg num
      0, // reserved
      0, // architecture
      0, 0, // global message num (UInt16)
      7, // num fields

      // num, size, type
      3, 4, 140, // uint32z
      4, 4, 134, // uint32
      7, 4, 134,
      1, 2, 132, // uint16
      2, 2, 132,
      5, 2, 132,
      0, 1, 0, // enum
    ];
  }
}
