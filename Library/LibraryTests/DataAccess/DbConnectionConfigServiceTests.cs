using Microsoft.Extensions.Configuration;
using Moq;
using MonoMicroservices.Library.DataAccess;
using MonoMicroservices.Library.DataAccess.Bases;
using MonoMicroservices.Library.IoC;

namespace MonoMicroservices.LibraryTests.DataAccess;
[TestFixture]
public class DbConnectionConfigServiceTests
{
	private Mock<IEnumerable<DbConfigBase>> _dbConfigBasesEnumerableMock = new Mock<IEnumerable<DbConfigBase>>();
	private DbConnectionConfigService dbConnectionConfigService;
	[SetUp]
	public void SetUp()
	{
		dbConnectionConfigService = new DbConnectionConfigService(_dbConfigBasesEnumerableMock.Object);
	}

	public static object[] SetDbConnectionsUsernameAndPasswordTestData = {
		new object[] {
			new Dictionary<string, string> {
				["ConnectionStrings:AAA"] = "asd={;x={Db2:Id2};y={Db2:Password2}",
				["Db2:Id2"] = "ee}e",
				["Db2:Password2"] = "ddd{%"
			},
			(IConfiguration confg) => confg["ConnectionStrings:AAA"] == "asd={;x=ee}e;y=ddd{%"
		}
	};
	[TestCaseSource(nameof(SetDbConnectionsUsernameAndPasswordTestData))]
	public void SetDbConnectionsUsernameAndPasswordTest(Dictionary<string, string> configsDic, Func<IConfiguration, bool> predication)
	{
		var configs = new ConfigurationBuilder().AddInMemoryCollection(configsDic).Build();

		var returnedVal = dbConnectionConfigService.SetDbConnectionsUserSecretsForDevTime(configs);

		Assert.That(returnedVal, Is.AssignableTo<IDbConnectionConfigService>());
		Assert.That(predication(configs), Is.True);
	}
}
