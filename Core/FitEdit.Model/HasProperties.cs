using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FitEdit.Model
{
  public class HasProperties : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

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

    public void NotifyPropertyChanged(params string[] propertyNames)
    {
      foreach (string name in propertyNames)
      {
        NotifyPropertyChanged(name);
      }
    }

    public void NotifyPropertyChanged([CallerMemberName] string propertyName = default)
      => NotifyPropertyChanged(this, new PropertyChangedEventArgs(propertyName));

    public void NotifyPropertyChanged(object sender, PropertyChangedEventArgs args)
      => PropertyChanged?.Invoke(sender, args);
  }
}
