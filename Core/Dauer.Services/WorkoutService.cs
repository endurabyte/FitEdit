using Dauer.Model;
using Units;

namespace Dauer.Services
{
  public interface IWorkoutService
  {
    void Recalculate(Workout workout, List<double> lapSpeeds, Unit unit);
  }

  public class WorkoutService : IWorkoutService
  {
    public void Recalculate(Workout workout, List<double> lapSpeeds, Unit unit)
    {
      var leaves = SelectLeaves(workout);

      if (lapSpeeds.Count != leaves.Count)
      {
        throw new ArgumentException($"Found {lapSpeeds.Count} laps but {leaves.Count} speeds");
      }

      //double distance = 0.0; // cumulative distance;
      //DateTime time; // lap completion
    }

    private List<LeafSequence> SelectLeaves(Workout workout)
    {
      return workout.Sequences
          .Where(s => s is NodeSequence)
          .SelectMany(s => SelectLeaves(s as NodeSequence))
          .OrderBy(s => s.When)
          .ToList();
    }

    private List<LeafSequence> SelectLeaves(NodeSequence sequence)
    {
      var nodes = sequence.Sequences
          .Where(s => s is NodeSequence)
          .Select(s => s as NodeSequence)
          .ToList();

      var leaves = sequence.Sequences
          .Where(s => s is LeafSequence)
          .Select(s => s as LeafSequence)
          .ToList();

      foreach (var node in nodes)
      {
        leaves.AddRange(SelectLeaves(node as NodeSequence));
      }

      return leaves;
    }
  }
}
