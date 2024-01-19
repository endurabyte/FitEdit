using FitEdit.Model.Workouts;
using NUnit.Framework;
using Units;

namespace FitEdit.Model.UnitTests;

[TestFixture]
public class SpeedTests
{
  [TestCase(6.7, 2.995168)]
  public void Convert_FromMilesPerHour_ToMetersPerSecond_Correct(double milesPerHour, double expectedMetersPerSecond)
  {
    var speed = new Speed(milesPerHour, Unit.MilesPerHour);
    var converted = speed.Convert(Unit.MetersPerSecond);

    Assert.AreEqual(Unit.MetersPerSecond, converted.Unit);
    Assert.AreEqual(expectedMetersPerSecond, converted.Value, 1e-6);
  }
}