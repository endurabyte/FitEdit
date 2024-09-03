namespace FitEdit.Model;

public class Cookie
{
  public required string Name { get; set; }
  public required string Value { get; set; }
  public required string Domain { get; set; }
  public required string Path { get; set; }
  public bool HttpOnly { get; set; }
  public bool IsSecure { get; set; }
  public DateTime Expires { get; set; }
}
