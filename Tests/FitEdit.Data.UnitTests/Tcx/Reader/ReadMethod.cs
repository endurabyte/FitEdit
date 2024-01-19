using FitEdit.UnitTests.Shared;
using NUnit.Framework;

namespace FitEdit.Data.UnitTests.Tcx.Reader
{
    [TestFixture]
    public class ReadMethod
    {
        [Test]
        public void ReadsString()
        {
            var treadmill = Data.Tcx.Reader.Read(TcxFixtures.GetTreadmillWorkout());
            var gps = Data.Tcx.Reader.Read(TcxFixtures.GetGpsWorkout());
        }
    }
}
