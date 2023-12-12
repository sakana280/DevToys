using DevToys.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevToys.Tests.Helpers
{
    [TestClass]
    public class JsonTableHelperTests
    {
        [DataTestMethod]
        [DataRow(null, false)]
        [DataRow("\"foo\"", false)]
        [DataRow("123", false)]
        [DataRow("", false)]
        [DataRow(" ", false)]
        [DataRow("   {  }  ", false)]
        [DataRow("   [  ]  ", false)]
        [DataRow("   { \"foo\": 123 }  ", false)]
        [DataRow("   [{ \"foo\": 123 }]  ", true)]
        [DataRow("   [{ \"foo\": 123 }, { \"bar\": 456 }]  ", true)]
        [DataRow("   [{  }]  ", false)]
        [DataRow("   [{  }, {  }]  ", false)]
        public void IsValid(string input, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, JsonTableHelper.IsValid(input));
        }

        [DataTestMethod]
        [DataRow("{\"a\":{\"b\":{\"c\":1}}}", "{\"a_b_c\":1}")]
        [DataRow("{\"a\":{\"b\":{\"c\":[]}}}", "{}")]
        [DataRow("{\"a\":{\"b\":1,\"c\":{\"d\":2},\"e\":[3,4]},\"f\":[5,6]}", "{\"a_b\":1,\"a_c_d\":2}")]
        public void Flatten(string inputJson, string expectedJson)
        {
            var obj = JsonConvert.DeserializeObject(inputJson) as JObject;
            JObject flattened = JsonTableHelper.FlattenJsonObject(obj);
            string serialized = JsonConvert.SerializeObject(flattened);
            Assert.AreEqual(expectedJson, serialized);
        }
    }
}
