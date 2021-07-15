using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.InnerRing.Utils;

namespace Neo.FileStorage.InnerRing.Tests.Util
{
    [TestClass]
    public class ConvertUtilTests
    {
        [TestMethod]
        public void ConvertTest()
        {
            ConvertUtil convertUtil = new ConvertUtil();
            Assert.AreEqual(convertUtil.Convert(1, 10, 1), 1000000000);
            Assert.AreEqual(convertUtil.Convert(10, 1, 1000000000), 1);
        }

        [TestMethod]
        public void ToBasePrecisionAndToTargetPrecisionTest()
        {
            ConvertUtil convertUtil = new ConvertUtil();
            convertUtil.BasePrecision = 1;
            convertUtil.TargetPrecision = 10;
            convertUtil.Factor = 1000000000;
            Assert.AreEqual(convertUtil.ToBasePrecision(1000000000), 1);
            Assert.AreEqual(convertUtil.ToTargetPrecision(1), 1000000000);
        }

        [TestMethod]
        public void ToFixed8AndToBalancePrecisionTest()
        {
            Fixed8ConverterUtil fixed8ConverterUtil = new Fixed8ConverterUtil();
            fixed8ConverterUtil = new Fixed8ConverterUtil(1);
            Assert.AreEqual(fixed8ConverterUtil.ToFixed8(1), 10000000);
            fixed8ConverterUtil = new Fixed8ConverterUtil(9);
            Assert.AreEqual(fixed8ConverterUtil.ToFixed8(1000000000), 100000000);
            Assert.AreEqual(fixed8ConverterUtil.ToBalancePrecision(100000000), 1000000000);
        }
    }
}
