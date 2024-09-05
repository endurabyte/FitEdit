using FitEdit.Model.Workouts;
using Units;

namespace FitEdit.Model.UnitTests;

public class SpeedTests
{
  [Theory]
  [InlineData(6.7, 2.995168)]
  public void Convert_FromMilesPerHour_ToMetersPerSecond_Correct(double milesPerHour, double expectedMetersPerSecond)
  {
    var speed = new Speed(milesPerHour, Unit.MilesPerHour);
    var converted = speed.Convert(Unit.MetersPerSecond);

    converted.Unit.Should().Be(Unit.MetersPerSecond);
    converted.Value.Should().BeApproximately(expectedMetersPerSecond, 1e-6);
  }
}