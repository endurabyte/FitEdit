using Newtonsoft.Json;
using NUnit.Framework;

namespace Dauer.Data.UnitTests.Fit.Reader
{
    [TestFixture]
    public class ReadMethod
    {
        const string _source = @"..\..\..\..\data\devices\forerunner-945\sports\running\treadmill\2019-12-17\"
           + @"steep-1mi-easy-2x[2mi 2min rest]\garmin-connect\activity.fit";

        [Test]
        public void ReadsFile()
        {
            var fitFile = new Data.Fit.Reader().ReadAsync(_source);
            Assert.NotNull(fitFile);
        }

        [Test]
        public void DumpsToJson()
        {
            var fitFile = new Data.Fit.Reader().ReadAsync(_source);

            Assert.DoesNotThrow(() =>
            {
                var json = JsonConvert.SerializeObject(fitFile, Formatting.Indented);
                Assert.IsNotEmpty(json);
            });
        }
    }
}