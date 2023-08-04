using Dauer.Model.Extensions;

namespace Dauer.Adapters.Fit;

public class FitMessage
{
  /// <summary>
  /// The byte index in the source file where this message definition was read from.
  /// </summary>
  public long SourceIndex { get; set; }

  /// <summary>
  /// The byte size in the source file of this message definition.
  /// </summary>
  public long SourceSize { get; set; }

  /// <summary>
  /// Source data of this message definition.
  /// </summary>
  public byte[] SourceData { get; set; }

  public FitMessage(FitMessage other)
  {
    SourceData = other?.SourceData?.ToArray() ?? Array.Empty<byte>();
    SourceSize = other?.SourceSize ?? 0;
    SourceIndex = other?.SourceIndex ?? 0;
  }

  public FitMessage(Stream source)
  {
    ReadSource(source);
  }

  protected void ReadSource(Stream source)
  {
    var position = source.Position;
    source.Position = 0;
    SourceData = source?.ReadAllBytes() ?? Array.Empty<byte>();
    SourceSize = SourceData.Length;
    source.Position = position;
  }
}
