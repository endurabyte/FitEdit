using NUnit.Framework;

namespace Dauer.Data.UnitTests.Tcx.Reader
{
    [TestFixture]
    public class ReadMethod
    {
        [Test]
        public void ReadsString()
        {
            var treadmill = Data.Tcx.Reader.Read(Fixtures.GetTreadmillWorkout());
            var gps = Data.Tcx.Reader.Read(Fixtures.GetGpsWorkout());
        }
    }
}
