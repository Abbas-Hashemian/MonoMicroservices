using Microsoft.Extensions.Configuration;
using MonoMicroservices.Library.Enums;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.TestUtils;
using System.Text.Json;

namespace MonoMicroservices.LibraryTests.Helpers;
[TestFixture]
public class ConfigsTests
{
	[TestCase(nameof(AppEnvironmentMode.Development), null, AppEnvironmentMode.Development)]
	[TestCase("asd", nameof(AppEnvironmentMode), null)]
	public void SetEnvironmentTest(string env, string? expectedWordsInExceptionMessage, AppEnvironmentMode? expectedResult)
	{
		if (expectedWordsInExceptionMessage != null)
			Assert.Throws<Exception>(() => Configs.SetEnvironmentMode(env)).ExpectedExceptionMessageIs(expectedWordsInExceptionMessage);
		else
			Assert.That(Configs.SetEnvironmentMode(env), Is.EqualTo(expectedResult));
	}

	[TestCase(Description = "At DbMigration time, the Dev/Prod env vars must generate a correct DbConnection string (including user secrets in Dev mode).")]
	public void DesignTimeConnectionStringTest()
	{
		TestHelpers.SetByMockName<Configs>("_appSettings", null);
		Directory.SetCurrentDirectory(Directory.GetCurrentDirectory() + "/../../");//LibraryTests/bin/debug/../../ to access the correct location of launchSettings.json
		Assert.That(
			Configs.SetConfigurationsIfNotSet().GetConnectionString("UserDbConnection")?.IndexOf("User Id=") ?? -1,
			Is.GreaterThan(-1),
			message: "No propper UserDbConnection was found in appSettings.json by Configs.SetConfigurationsIfNotSet."
		);
		Assert.That(
			Configs.SetConfigurationsIfNotSet().GetConnectionString("UserDbConnection")?.IndexOf("User Id=;") ?? -1,
			Is.LessThan(0),
			message: "UserDbConnection \"User Id\" is not set."
		);
	}

	[TestCase]
	public void GetSection2ndLevelFlatDictionaryTest()
	{
		TestHelpers.SetByMockName<Configs>("_appSettings", null);
		Configs.SetConfigurationsIfNotSet(
			TestHelpers.DeserialzieToIConfigurationRoot(
				JsonSerializer.Serialize(
					new Dictionary<string, object>
					{
						["IsInMicroservicesMode"] = true,
						["MicroservicesConnections"] =
							JsonSerializer.Serialize(
								new Dictionary<string, object>
								{
									["ServiceA"] = "http://lt.local:81",
									["ConnectionGroupName"] = new Dictionary<string, string>
									{
										["ServiceKey1"] = "http://asd.local",
										["ServiceKey2"] = "http://asd2.local"
									}
								}
							)
					}
				)
			)
		);

		var configs = new Configs();
		var r = configs.GetSection2ndLevelFlatDictionary("MicroservicesConnections");

		Assert.That((r["ConnectionGroupName:ServiceKey1"]?.ToString() ?? "") == "http://asd.local");
		Assert.That((r["ServiceA"]?.ToString() ?? "") == "http://lt.local:81");
	}
}
