using Dauer.Model.Units;

namespace Dauer.Model.Workouts
{
  public class Lap
  {
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public TimeSpan Duration => End - Start;
    public Distance Distance { get; set; }
    public Speed Speed { get; set; }

    public Lap() { }

    public Lap WithDistance(Distance distance)
    {
      Distance = distance;
      return this;
    }

    public Lap WithSpeed(Speed speed)
    {
      Speed = speed;
      return this;
    }

    /// <summary>
    /// Recalculate distance from Duration and Speed
    /// Distance = Speed * Duration.
    /// </summary>
    public Lap UpdateDistance()
    {
      Distance.Value = Speed.MetersPerSecond() * Duration.TotalSeconds;
      Distance.Unit = DistanceUnit.Meter;
      return this;
    }

    /// <summary>
    /// Recalculate speed from Distance and Duration.
    /// Speed = Distance / Duration
    /// </summary>
    public Lap UpdateSpeed()
    {
      Speed.Value = Distance.Meters() / Duration.Seconds;
      Speed.Unit = SpeedUnit.MetersPerSecond;
      return this;
    }
  }
}