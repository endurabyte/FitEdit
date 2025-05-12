namespace FitEdit.Adapters.Fit
{
  public static class FitConfig
  {
    /// <summary>
    /// Save loaded byte data in Messages and Fields. Supports debugging and hex editor.
    /// </summary>
    public static bool CacheSourceData { get; set; } = true;

    public class Discard
    {
      public class DefinitionMessages
      {
        public static bool RedefiningGlobalMesgNum { get; set; } = false;
        public static bool ContainingUnknownType { get; set; } = false;
        public static bool WithBigUnknownMessageNum { get; set; } = false;
        public static bool WithUnknownArchitecture { get; set; } = false;
      }

      public class DataMessages
      {
        public static bool OfLargeSize { get; set; } = false;
        public static bool WithLargeLatitudeChange { get; set; } = true;
        public static bool WithLargeLongitudeChange { get; set; } = true;
        public static bool WithLargeTimestampChange { get; set; } = false;

        /// <summary>
        /// Ignore any FileId message other than the first or any FileId message that is not at the beginning of the file.
        /// In corrupted files, we see a lot of spurious messages with global and local num == 0 ( == MesgNum.FileId).
        /// </summary>
        public static bool UnexpectedFileIdMessage { get; set; } = true;
      }
    }
  }
}
