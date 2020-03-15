namespace UnitTests
{
    using hmac.Core;
    using NUnit.Framework;

    public class Tests
    {

        [Test]
        public void Test1()
        {
            var config = new Config
            {
                Adapter = AdapterType.SHA256, 
                IgnoreNullValue = true,
                OutputType = OutputType.Hex
            };


            var value = ConfigMapper.MapToString(config);
            var resultConfig = ConfigMapper.FromString(value);

            Assert.AreEqual(AdapterType.SHA256, resultConfig.Adapter);
            Assert.AreEqual(OutputType.Hex, resultConfig.OutputType);
            Assert.AreEqual(true, resultConfig.IgnoreNullValue);
        }
    }
}