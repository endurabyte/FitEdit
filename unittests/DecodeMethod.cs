using fitsharp;
using Newtonsoft.Json;
using NUnit.Framework;

namespace unittests
{
    [TestFixture]
    public class DecodeMethod
    {
        const string _source = @"..\..\..\..\data\devices\forerunner-945\sports\running\treadmill\2019-12-17\"
           + @"steep-1mi-easy-2x[2mi 2min rest]\garmin-connect\activity.fit";

        [Test]
        public void ReadsFile()
        {
            var fitFile = new FitDecoder().Decode(_source);
            Assert.NotNull(fitFile);
        }

        [Test]
        public void DumpsToJson()
        {
            var fitFile = new FitDecoder().Decode(_source);

            Assert.DoesNotThrow(() =>
            {
                var json = JsonConvert.SerializeObject(fitFile, Formatting.Indented);
                Assert.IsNotEmpty(json);
            });
        }
    }
}