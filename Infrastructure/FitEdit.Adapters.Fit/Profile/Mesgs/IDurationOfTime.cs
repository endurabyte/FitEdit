namespace Dynastream.Fit;

public interface IDurationOfTime : IInstantOfTime
{
  DateTime GetStartTime();
  void SetStartTime(DateTime dt);
}
