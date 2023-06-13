namespace Dauer.Model.Web;

public interface IBrowserAdapter
{
  Task<bool> Run(IStep step);
  Task Close();
}