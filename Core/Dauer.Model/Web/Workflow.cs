namespace Dauer.Model.Web;

public class Workflow : List<IStep>
{
  public string Name { get; set; } = string.Empty;
}