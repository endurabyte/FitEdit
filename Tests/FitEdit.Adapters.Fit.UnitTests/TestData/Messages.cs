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

  public static class DuplicateProductNameDeviceInfo
  {
    public static byte[] Definition =>
    [
      0x42, // header: local mesg num
      0x00, // reserved
      0x00, // architecture
      0x17, 0x00, // global message num (UInt16)
      0x08, // num fields
     
      // num, size, type
      0xFD, 0x04, 0x86,
      0x00, 0x01, 0x02,
      0x13, 0x11, 0x07,
      0x1B, 0x09, 0x07,
      0x19, 0x01, 0x00,
      0x02, 0x02, 0x84,
      0x1B, 0x09, 0x07,
      0x03, 0x04, 0x8C
    ];

    public static byte[] Message =>
    [
      0x02,
      0xE0,0x3A,0x86,0x41, // Timestamp
      0x00, // DeviceIndex
      0x57,0x6F,0x72,0x6B,0x4F,0x75,0x74,0x44,0x6F,0x6F,0x72,0x73,0x20,0x28,0x38,0x29,0x00, // Descriptor (== "WorkOutDoors (8)\n")
      0x57,0x61,0x74,0x63,0x68,0x36,0x2C,0x38,0x00, // ProductName (== "Watch6,8\n")
      0x05, // SourceType
      0xFF,0x00, // Manufacturer
      0x57,0x61,0x74,0x63,0x68,0x36,0x2C,0x38,0x00, // ProductName (== "Watch6,8\n")
      0x69,0x6C,0x00,0x00 // SerialNumber
    ];
  }
}
