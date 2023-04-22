namespace Dauer.Data.Fit
{
  public delegate void ProgressEvent(long position, long length);

  public class ProgressStream : Stream
  {
    private readonly Stream s_;
    private long lastPosition_ = 0;
    public long ResolutionBytes { get; set; } = 1024; // Report progress every kBA

    public event ProgressEvent ReadProgressChanged;

    public ProgressStream(Stream s, long resolutionBytes)
    {
      s_ = s;
      ResolutionBytes = resolutionBytes;
    }

    public override bool CanRead => s_.CanRead;
    public override bool CanSeek => s_.CanSeek;
    public override bool CanWrite => s_.CanWrite;
    public override long Length => s_.Length;
    public override long Position
    {
      get => s_.Position;
      set => s_.Position = value;
    }

    public override void Flush() => s_.Flush();
    public override int Read(byte[] buffer, int offset, int count)
    {
      if (Position - ResolutionBytes > lastPosition_)
      {
        ReadProgressChanged?.Invoke(Position, Length);
        lastPosition_ = Position;
      }
      return s_.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin) => s_.Seek(offset, origin);
    public override void SetLength(long value) => s_.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => s_.Write(buffer, offset, count);
  }
}