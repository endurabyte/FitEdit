#nullable enable

namespace Dauer.Model.Extensions;

public static class ObjectExtensions
{
  public static bool TryGetInt(this object? o, out int result)
  {
    result = 0;

    if (o == null || !o.GetType().IsIntlike()) { return false; }

    try
    {
      result = Convert.ToInt32(o);
      return true;
    }
    catch (Exception)
    {
      return false;
    }
  }
}
