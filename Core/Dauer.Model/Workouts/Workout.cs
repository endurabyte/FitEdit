namespace Dauer.Model.Workouts
{
  public class Workout
  {
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public List<Lap> Laps { get; set; } = new();
    public List<Speed> Speeds => Laps.Select(lap => lap.Speed).ToList();
    public List<Distance> Distances => Laps.Select(lap => lap.Distance).ToList();

    public Workout() { }
    public Workout(params Lap[] laps)
    {
      Laps = laps.ToList();
    }

    public Workout Add(Lap lap)
    {
      Laps.Add(lap);
      return this;
    }
  }
}