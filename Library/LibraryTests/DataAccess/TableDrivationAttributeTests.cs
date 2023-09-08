using MonoMicroservices.Library.DataAccess.Attributes;
using MonoMicroservices.TestUtils;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace MonoMicroservices.LibraryTests.DataAccess;
[TestFixture]
public class TableDrivationAttributeTests
{
	private const string ExpectedTableName = "Models";
	[Table(ExpectedTableName)]
	public class ModelWithTable { }
	public class ModelWithoutTableAttr { }
	//[Table("")]
	//public class ModelWithoutTableName { }
	[TableDerivation(typeof(ModelWithTable))]
	public class ModelWithDerivedTable { }

	public static object[] ExtractTableNameTestParams =
		new Dictionary<string, object[]>
		{
			[$"{nameof(ModelWithTable)}, no throw, \"{ExpectedTableName}\""] = new object[] { typeof(ModelWithTable), null, ExpectedTableName },
			[$"{nameof(ModelWithoutTableAttr)}, throw, null"] = new object[] { typeof(ModelWithoutTableAttr), "No TableAttribute", null },
			//[$"{nameof(ModelWithoutTableName)}, throw, null"] = new object[] { typeof(ModelWithoutTableName), "No Table Name", null }, //GetCustomAttribute throws its own exception for [Table("")]
			//[$"{nameof(ModelWithoutTableName)}, throw, null"] = new object[] { typeof(ModelWithoutTableName), "", null },
		}.ConvertToNamedTestCaseData();

	[TestCaseSource(nameof(ExtractTableNameTestParams)), Sequential]
	public void ExtractTableNameTest(Type targetModel, string? expectedWordsInErrMsg, string expectedTableName)
	{
		var method = typeof(TableDerivationAttribute).GetMethod("ExtractTableName", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.That(method, Is.Not.Null);
		if (expectedWordsInErrMsg != null)
		{
			var ex = Assert.Throws<TargetInvocationException>(
				() => method?.Invoke(null, new object[] { targetModel }),
				"No exception was thrown for a target model without Table attr or table name."
			);
			if (expectedWordsInErrMsg != "") ex.ExpectedExceptionMessageIs(expectedWordsInErrMsg);
		}
		else
			Assert.That(method?.Invoke(null, new object[] { targetModel })?.ToString(), Is.EqualTo(expectedTableName));
	}

	[TestCase]
	public void TableDerivationAttributeTest()
	{
		var attr = typeof(ModelWithDerivedTable).GetCustomAttribute<TableAttribute>();
		Assert.That(attr, Is.Not.Null);
		Assert.That(attr?.Name, Is.EqualTo(ExpectedTableName));
	}
}
