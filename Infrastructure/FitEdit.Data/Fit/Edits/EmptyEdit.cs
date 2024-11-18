namespace FitEdit.Data.Fit.Edits;

public class EmptyEdit : IEdit
{
  public FitFile Apply() => new();
}