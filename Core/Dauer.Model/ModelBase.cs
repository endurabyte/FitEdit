using System.Runtime.CompilerServices;

namespace Dauer.Model
{
  public class ModelBase : PropertyChangedBase
  {
    public bool Set<T>(ref T current, T newValue, [CallerMemberName] string propertyName = null)
    {
      if (EqualityComparer<T>.Default.Equals(current, newValue))
      {
        return false;
      }

      current = newValue;
      NotifyPropertyChanged(propertyName);
      return true;
    }
  }
}
