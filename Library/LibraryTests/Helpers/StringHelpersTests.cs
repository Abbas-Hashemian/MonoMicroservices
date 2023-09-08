using MonoMicroservices.Library.Helpers;
using System.Text.Json;

namespace MonoMicroservices.LibraryTests.Helpers;
[TestFixture]
public class StringHelpersTests
{
	public static object[] Substring2TestData =
		{
			new object[] { "aa2bb", 1, -2, "a2" },
			new object[] { "aa2bb", 1, 0, "a2bb" },
			new object[] { "ac2bb", 1, 1, "c" }
		};
	[TestCaseSource(nameof(Substring2TestData))]
	public void Substring2Test(string mainString, int startIndex, int endIndexOrLength, string expectedResult)
	{
		var result = mainString.Substring2(startIndex, endIndexOrLength);
		Assert.AreEqual(expectedResult, result);
	}


	[TestCase]
	public void GetJson2ndLevelFlatDictionaryTest()
	{
		var json = JsonSerializer.Serialize(
			new Dictionary<string, object>
			{
				["A"] = "aa",
				["B"] = new Dictionary<string, string>
				{
					["X"] = "xx",
					["Y"] = "yy"
				}
			}
		);

		var r = Library.Helpers.StringHelper.GetJson2ndLevelFlatDictionary(json);
		Assert.That((r["B:X"]?.ToString() ?? "") == "xx");
	}
}
