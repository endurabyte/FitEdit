namespace Dynastream.Fit;

public interface IInstantOfTime
{
  DateTime GetTimestamp();
  void SetTimestamp(DateTime dt);
}
