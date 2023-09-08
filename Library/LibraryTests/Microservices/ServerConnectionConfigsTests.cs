using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.Microservices;
using MonoMicroservices.TestUtils;
using System.Text.Json;

namespace MonoMicroservices.LibraryTests.Microservices;
[TestFixture]
public class ServerConnectionConfigsTests
{
	private Mock<IConfigs> _configsMock = new Mock<IConfigs>();
	private Mock<IServiceCollection> _serviceCollectionMock = new Mock<IServiceCollection>();
	private Mock<HttpClient> _httpClientMock = new Mock<HttpClient>();
	private ServerConnectionConfigs _serverConnectionsConfigs;
	private Mock<IHttpClientBuilder> _httpClientBuilderMock = new Mock<IHttpClientBuilder>();
	private Mock<Func<IServiceCollection, string, Action<HttpClient>, IHttpClientBuilder>> _addHttpClientMock = new Mock<Func<IServiceCollection, string, Action<HttpClient>, IHttpClientBuilder>>();

	[SetUp]
	public void Setup()
	{
		var configRoot = TestHelpers.DeserialzieToIConfigurationRoot(
			JsonSerializer.Serialize(
				new Dictionary<string, object>
				{
					["IsInMicroservicesMode"] = true,
					["MicroservicesConnections"] =
						JsonSerializer.Serialize(
							new Dictionary<string, object>
							{
								["SingleImplementationService"] = "http://SingleImplementationService",
								["ConnectionGroupName"] = new Dictionary<string, string>
								{
									["ServiceKey1"] = "http://ServiceKey1.local",
									["ServiceKey2"] = "http://ServiceKey2.local"
								}
							}
						)
				}
			)
		);
		_serverConnectionsConfigs = new ServerConnectionConfigs(_configsMock.Object);
		TestHelpers.SetByMockName<ServerConnectionConfigs>("AddHttpClient", _addHttpClientMock.Object);
		_addHttpClientMock.Setup(f => f(It.IsAny<IServiceCollection>(), It.IsAny<string>(), It.IsAny<Action<HttpClient>>())).Returns(_httpClientBuilderMock.Object);
		_configsMock.Setup(c => c[It.IsAny<string>()]).Returns((string key) => configRoot[key]);
		_configsMock.Setup(c => c.GetSection2ndLevelFlatDictionary(It.IsAny<string>()))
			.Returns((string name) => StringHelper.GetJson2ndLevelFlatDictionary(configRoot.GetSection(name).Get<string>()));
	}

	[TestCase]
	public void AddHttpClientServicesTest()
	{
		_serverConnectionsConfigs.AddHttpClientServices(_serviceCollectionMock.Object);

		_configsMock.Verify(c => c["IsInMicroservicesMode"]);
		_configsMock.Verify(c => c.GetSection2ndLevelFlatDictionary("MicroservicesConnections"));

		Func<Action<HttpClient>, string, bool> httpClientTest =
			(lambda, expectedBaseAddress) =>
			{
				lambda(_httpClientMock.Object);
				return (_httpClientMock.Object.BaseAddress?.OriginalString ?? "") == expectedBaseAddress;
			};
		_addHttpClientMock.Verify(f => f(
			It.IsAny<IServiceCollection>(),
			"SingleImplementationService",
			It.Is<Action<HttpClient>>(lambda => httpClientTest(lambda, "http://SingleImplementationService"))
		));
		_addHttpClientMock.Verify(f => f(
			It.IsAny<IServiceCollection>(),
			It.Is<string>(name => name == "ConnectionGroupName:ServiceKey1"),
			It.Is<Action<HttpClient>>(lambda => httpClientTest(lambda, "http://ServiceKey1.local"))
		));
		_addHttpClientMock.Verify(f => f(
			It.IsAny<IServiceCollection>(),
			It.Is<string>(name => name == "ConnectionGroupName:ServiceKey2"),
			It.Is<Action<HttpClient>>(lambda => httpClientTest(lambda, "http://ServiceKey2.local"))
		));
		_addHttpClientMock.VerifyNoOtherCalls();//it's important to prevent null base adresses
	}
}
