using FitEdit.Model;
using FitEdit.Model.Extensions;

namespace FitEdit.Adapters.Fit.Extensions;

public class MessageBase : HasProperties
{
  /// <summary>
  /// The byte index in the source file where this message definition was read from.
  /// </summary>
  public long SourceIndex { get; set; }

  /// <summary>
  /// The byte size in the source file of this message definition.
  /// </summary>
  public long SourceLength { get; set; }

  /// <summary>
  /// Source data of this message definition.
  /// </summary>
  public byte[] SourceData { get; set; }

  public MessageBase(MessageBase other)
  {
    SourceData = other?.SourceData?.ToArray() ?? Array.Empty<byte>();
    SourceLength = other?.SourceLength ?? 0;
    SourceIndex = other?.SourceIndex ?? 0;
  }

  public MessageBase(Stream source)
  {
    CacheData(source);
  }

  /// <summary>
  /// Update <see cref="SourceData"/>, <see cref="SourceIndex"/>, and <see cref="SourceLength"/> from <paramref name="source"/>.
  /// </summary>
  public void CacheData(Stream source)
  {
    if (!FitConfig.CacheSourceData) { return; }

    var position = source.Position;
    source.Position = 1; // 0 = local mesg num (from the message header). 1 = first field
    SourceData = source?.ReadAllBytes() ?? Array.Empty<byte>();
    SourceLength = SourceData.Length;
    source.Position = position;
  }
}
