using Dauer.Model;
using NUnit.Framework;
using Units;

namespace Dauer.Services.UnitTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Recalculates()
        {
            var service = new WorkoutService();

            service.Recalculate
            (
                GetWorkout(),
                new List<double> { 2.0 }, 
                Unit.MetersPerSecond
            );
        }

        private Workout GetWorkout()
        {
            var now = DateTime.Now;

            return new Workout
            {
                Sequences = new List<ISequence>
                {
                    new NodeSequence
                    {
                        Sequences = new List<ISequence>
                        {
                            new LeafSequence
                            {
                                Samples = new List<ISample>
                                {
                                    new GpsRunSample { Speed = 1.0, Distance = 0, When = now },
                                    new GpsRunSample { Speed = 1.0, Distance = 1, When = now.AddSeconds(1) },
                                    new GpsRunSample { Speed = 1.0, Distance = 2, When = now.AddSeconds(2) },
                                    new GpsRunSample { Speed = 1.0, Distance = 3, When = now.AddSeconds(3) },
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}