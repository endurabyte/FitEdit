namespace FitEdit.Adapters.Fit.Extensions;

public enum DecodeResult
{
  Init,
  OkReadSomeMessages,
  OkEndOfFile, // A stream can contain multiple FIT files
  ErrEndOfStream,
  ErrFitException,
  ErrInitialRead,
  ErrCrc,
}

