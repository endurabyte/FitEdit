﻿using System.Globalization;

namespace FitEdit.Adapters.Mtp;

public static class UsbVendor
{
  private static readonly Dictionary<string, uint> supportedVendors_ = new()
  {
    ["GARMIN"] = 0x091E,
  };

  public static bool IsSupported(string vendorId)
  {
    // Linux and Windows report the vendor ID in hex.
    // macOS reports in decimal
    // So we check both, i.e Garmin 0x091E == 2334
    bool isSupported =  uint.TryParse(vendorId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hex) && IsSupported(hex);
    isSupported |= uint.TryParse(vendorId, out uint dec) && IsSupported(dec);

    return isSupported;
  }

  public static bool IsSupported(uint vendorId) => supportedVendors_.Values.Any(id => id == vendorId);
}
