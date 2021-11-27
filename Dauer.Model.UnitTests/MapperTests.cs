using Dauer.UnitTests.Shared;
using NUnit.Framework;

namespace Dauer.Model.UnitTests
{
    public class MapperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Map_MapsWorkoutToTcx()
        {

        }

        [Test]
        public void Map_MapsTcxToWorkout()
        {
            var xml = TcxFixtures.GetGpsWorkout();
            var db = Data.Tcx.Reader.Read(xml);
            var workout = new Data.Tcx.Mapper().Map(db);
        }

        [Test]
        public void Map_MapsWorkoutToFit()
        {
        }

        [Test]
        public void Map_MapsFitToWorkout()
        {
            const string source = @"..\..\..\..\data\devices\forerunner-945\sports\running\"
                + @"generic\2019-12-18\35min-easy-4x20s-strides\garmin-connect\activity.fit";

            var fit = new Data.Fit.Reader().Read(source);
            var workout = new Data.Fit.Mapper().Map(fit);
        }
    }
}