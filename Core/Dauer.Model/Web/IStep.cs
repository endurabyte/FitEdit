namespace Dauer.Model.Web;

public interface IStep
{
  string Name { get; }
  Task<bool> Run();
}