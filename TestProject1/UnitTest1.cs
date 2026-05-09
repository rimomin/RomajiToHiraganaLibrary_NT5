using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject1
{
    /// <summary>
    /// 単体テスト
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// RomajiToHiraganaメソッドによる変換結果をテストデータセットと比較する。
        /// </summary>
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\TestData.xml", "row", DataAccessMethod.Sequential), DeploymentItem("TestProject1\\TestData.xml"), TestMethod]
        public void TestMethod1()
        {
            string romaji = (string)TestContext.DataRow["input"];
            string hiragana = (string)TestContext.DataRow["output"];
            string temp = RomajiToHiraganaLibrary.RomajiToHiragana.Convert(romaji);
            Assert.AreEqual(temp, hiragana, "ローマ字置換失敗");
        }
    }
}