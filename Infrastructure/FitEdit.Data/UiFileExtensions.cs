#nullable enable
using FitEdit.Data.Fit;
using FitEdit.Model;

namespace FitEdit.Data;

public static class UiFileExtensions
{
  /// <summary>
  /// Commit the FIT file bytes back to the activity.
  /// Does not save to the DB.
  /// </summary>
  public static void Commit(this UiFile uif, FitFile fit)
  {
    if (uif.Activity is null) { return; }
    
    uif.Activity.File = new FileReference(uif.Activity.Name ?? "New file", fit.GetBytes());
  }
}