using DryIoc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.Helpers.Attributes;
using MonoMicroservices.Library.IoC;
using MonoMicroservices.Library.Microservices;
using MonoMicroservices.Library.Microservices.Attributes;
using MonoMicroservices.Library.Microservices.WebApiHandler;
using MonoMicroservices.TestUtils;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace MonoMicroservices.LibraryTests.Microservices.WebApiHandler;
[TestFixture]
public class DynamicWebApiHandlerTests
{
	private const int _asyncDelayMilliseconds = 200;
	private Mock<IConfigs> _configsMock = new Mock<IConfigs>();
	private Mock<IServerConnectionConfigs> _serverConnectionConfigsMock = new Mock<IServerConnectionConfigs>();
	private Mock<IHttpClientFactory> _httpClientFactoryMock = new Mock<IHttpClientFactory>();
	private Mock<HttpMessageHandler> _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
	private Mock<IServiceCollection> _serviceCollectionMock = new Mock<IServiceCollection>();
	private Mock<IDependencyRegistrar> _dependencyRegistrarsMock = new Mock<IDependencyRegistrar>();
	private Mock<IIocService> _iocServiceMock = new Mock<IIocService>();
	private Mock<IContainer> _containerMock = new Mock<IContainer>();
	private Mock<IResolverContext> _resolverContextMock = new Mock<IResolverContext>();
	private Mock<Action<IIocService, Type, Func<IResolverContext, object>, object>> _registerDelegateMock = new Mock<Action<IIocService, Type, Func<IResolverContext, object>, object>>();
	private Mock<Assembly> _assemblyMock = new Mock<Assembly>();
	private Mock<ILogger<DynamicWebApiHandler>> _loggerMock = new Mock<ILogger<DynamicWebApiHandler>>();
	private DynamicWebApiHandler _dynamicWebApiHandler;
	private IService _iserviceInstance;
	private Dictionary<object, IMultiImplement> _imultiImplementInstances;
	private readonly static IConfigurationRoot _configRoot = TestHelpers.DeserialzieToIConfigurationRoot(
		JsonSerializer.Serialize(
			new Dictionary<string, object>
			{
				["IsInMicroservicesMode"] = true,
				["MicroservicesConnections"] =
					JsonSerializer.Serialize(
						new Dictionary<string, object>
						{
							//LibraryTests : The second part of namespace (project name) would be the server address name.
							[typeof(DynamicWebApiHandlerTests).Namespace.Split(".")[1]] = "http://lt.local:81",
							["ConnectionGroupName"] = new Dictionary<string, string>
							{
								["ServiceKey1"] = "http://asd.local",
								["ServiceKey2"] = "http://asd2.local"
							}
						}
					)
			}
		)
	);
	public class Dto { public string Str { get; set; } }
	[WebApiService(SpecifiedMethodsOnly = false)]
	public interface IService
	{
		string ReturnStringApi(string p1, int p2, Dto dto);
		Dto ReturnDtoApi();
		bool ReturnBoolApi();
		void Void();
		Task<Dto> ReturnDtoApiAsync();
		Task ReturnVoidTaskApiAsync();
		[Async]
		void FireAndForgetApiAsync();
	}
	[WebApiService(ConnectionGroupName = "ConnectionGroupName")]
	public interface IMultiImplement
	{
		[WebApiEndpoint]
		void Void();
	}
	public class IShouldNotBeRegistered { }
	[SetUp]
	public void Setup()
	{
		_imultiImplementInstances = new Dictionary<object, IMultiImplement>();
		_configsMock.Setup(c => c[It.IsAny<string>()]).Returns((string key) => _configRoot[key]);
		_configsMock.Setup(c => c.GetSection2ndLevelFlatDictionary(It.IsAny<string>()))
			.Returns((string name) => StringHelper.GetJson2ndLevelFlatDictionary(_configRoot.GetSection(name).Get<string>()));
		_iocServiceMock.SetupGet(i => i.Container).Returns(_containerMock.Object);
		_httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
			.Returns((string baseAddressConfigName) =>
				new HttpClient(_httpMessageHandlerMock.Object)
				{
					BaseAddress = new Uri(StringHelper.GetJson2ndLevelFlatDictionary(_configRoot.GetSection("MicroservicesConnections").Get<string>())[baseAddressConfigName].ToString())
				}
			);

		_dynamicWebApiHandler = new DynamicWebApiHandler(_serverConnectionConfigsMock.Object, _configsMock.Object, _httpClientFactoryMock.Object, _iocServiceMock.Object, _loggerMock.Object);

		_registerDelegateMock.Reset();
		TestHelpers.SetByMockName<DynamicWebApiHandler>("RegisterDelegate", _registerDelegateMock.Object);
		_assemblyMock.Setup(a => a.GetTypes()).Returns(new[] { typeof(IService), typeof(IMultiImplement), typeof(IShouldNotBeRegistered) });
		_dynamicWebApiHandler.RegisterDynamicApiHandlers(new[] { _assemblyMock.Object }, _serviceCollectionMock.Object);
		_assemblyMock.Verify(a => a.GetTypes());
		Func<object, bool> multiImplInstanceSetter = (object serviceKey) => false;
		var instanceSetter = (Func<IResolverContext, object> deleg) =>
		{
			multiImplInstanceSetter = null;
			var instance = deleg(_resolverContextMock.Object);
			if (instance.GetType().IsImplementingServiceType(typeof(IService)))
				_iserviceInstance = (IService)instance;
			else if (instance.GetType().IsImplementingServiceType(typeof(IMultiImplement)))
				multiImplInstanceSetter = (object serviceKey) => { _imultiImplementInstances.Add(serviceKey, (IMultiImplement)instance); return true; };
			return true;
		};
		_registerDelegateMock.Verify(action => action(
			It.IsAny<IIocService>(),
			It.Is<Type>(type => type.EqualsAny(typeof(IService), typeof(IMultiImplement))),
			It.Is<Func<IResolverContext, object>>(deleg => instanceSetter(deleg)),
			It.Is<object>(serviceKey => serviceKey != null ? multiImplInstanceSetter(serviceKey) : true)
		), times: Times.Exactly(3));
	}

	[TestCase]
	public void RegisterDynamicApiHandlersTest()
	{
		Assert.IsNotNull(_iserviceInstance, message: $"RegisterDelegate is not called the way it's supposed to be for {nameof(IService)}.");
		Assert.That(
			_imultiImplementInstances.Count() == 2 && _imultiImplementInstances.ContainsKey("ServiceKey1") && _imultiImplementInstances.ContainsKey("ServiceKey2"),
			$"RegisterDelegate is not called the way it's supposed to be for {nameof(IMultiImplement)}."
		);
	}

	public static void SetupHttpRequestMessageSendAsync(Mock<HttpMessageHandler> _httpMessageHandlerMock, Func<string> getPostResultString, int? delayMilliSecs = null)
	{
		_httpMessageHandlerMock.Reset();
		_httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.Returns(
				async () =>
				{
					if (delayMilliSecs != null)
						await Task.Delay((int)delayMilliSecs);
					return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(getPostResultString()) };
				}
			);
	}
	private static void HttpRequestMessageSendAsyncVerification(
		Mock<HttpMessageHandler> _httpMessageHandlerMock,
		string connectionConigName,
		Type serviceInterfaceType,
		string methodName,
		string expectedQueryString,
		string expectedPostBody,
		int howManySendAsyncCalls
	)
	{
		Func<HttpRequestMessage, bool> requestTester =
			httpReqMsg =>
			{
				var connectionConfig = (string)StringHelper.GetJson2ndLevelFlatDictionary(_configRoot.GetSection("MicroservicesConnections").Get<string>())[connectionConigName].ToString();
				var expectedUrl = $"{connectionConfig}/{string.Join("/", serviceInterfaceType.FullName.Split(".").Skip(1))}/{methodName}";
				var realUrlWithoutQueryString = httpReqMsg.RequestUri.GetLeftPart(UriPartial.Path);
				var isUrlMatched = realUrlWithoutQueryString == expectedUrl;
				Assert.IsTrue(isUrlMatched, $"The reqested URL was not matched.\n Expected : \"{expectedUrl}\"\n Requested URL : \"{realUrlWithoutQueryString}\"");

				var realPostString = httpReqMsg.Content.ReadAsStringAsync().Result;
				var isPostStringMatched = realPostString == expectedPostBody;
				Assert.IsTrue(isPostStringMatched, $"The post string (json) was not matched.\n Expected : {expectedPostBody}\n Posted string : {realPostString}");

				var realQueryString = httpReqMsg.RequestUri.Query;
				var isQueryStringMatched = realQueryString == expectedQueryString;
				Assert.IsTrue(isPostStringMatched, $"The url QueryString was not matched.\n Expected : {expectedQueryString}\n Posted string : {realQueryString}");

				return isUrlMatched && isPostStringMatched && isQueryStringMatched;
			};
		try
		{
			_httpMessageHandlerMock.Protected().Verify(
				"SendAsync", Times.Exactly(howManySendAsyncCalls),
				ItExpr.Is<HttpRequestMessage>((httpReqMsg) => requestTester(httpReqMsg)),
				ItExpr.IsAny<CancellationToken>()
			);
		}
		catch (Exception ex)
		{
			throw new Exception(
				"HttpMessageHandler.SendAsync was not called the way that was supposed to be (the reason of invocation count verification error)!\n"
				+ ex.Message
			);
		}
	}
	private static void IServiceHttpRequestMessageSendAsyncVerification(
		Mock<HttpMessageHandler> _httpMessageHandlerMock,
		string methodName,
		string expectedPostBody,
		int howManySendAsyncCalls
	) => HttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, typeof(IService).Namespace.Split(".")[1], typeof(IService), methodName, "", expectedPostBody, howManySendAsyncCalls);

	[TestCase]
	public void SyncReturnStringWithDtoParamApiTest()
	{
		SetupHttpRequestMessageSendAsync(_httpMessageHandlerMock, () => JsonSerializer.Serialize("ReturnString1"));

		var r = _iserviceInstance.ReturnStringApi(p1: "aa", p2: 12, dto: new Dto { Str = "asdasd" });

		Assert.AreEqual("ReturnString1", r, "Returned value was not matched!");
		string jsonPostString = JsonSerializer.Serialize(new { p1 = "aa", p2 = 12, dto = new Dto { Str = "asdasd" } });
		IServiceHttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, nameof(IService.ReturnStringApi), jsonPostString, 1);
	}
	[TestCase]
	public void SyncReturnDtoApiTest()
	{
		SetupHttpRequestMessageSendAsync(_httpMessageHandlerMock, () => JsonSerializer.Serialize(new Dto { Str = "DtoStr2" }));

		var r = _iserviceInstance.ReturnDtoApi();

		Assert.AreEqual("DtoStr2", r.Str, "Returned value was not matched!");
		IServiceHttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, nameof(IService.ReturnDtoApi), "{}", 1);
	}
	[TestCase]
	public void SyncReturnBoolApiTest()
	{
		SetupHttpRequestMessageSendAsync(_httpMessageHandlerMock, () => "true");

		var r = _iserviceInstance.ReturnBoolApi();

		Assert.IsTrue(r, "Returned value was not matched!");
		IServiceHttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, nameof(IService.ReturnBoolApi), "{}", 1);
	}
	[TestCase]
	public void SyncVoidReturnTest()
	{
		var isCalled = false;
		SetupHttpRequestMessageSendAsync(_httpMessageHandlerMock, () => { isCalled = true; return ""; });

		_iserviceInstance.Void();

		IServiceHttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, nameof(IService.Void), "{}", 1);
		Assert.IsTrue(isCalled, "IService.Void didn't call the expected lambda.");
	}
	[TestCase]
	public async Task AsyncDtoResultTest()
	{
		SetupHttpRequestMessageSendAsync(_httpMessageHandlerMock, () => JsonSerializer.Serialize(new Dto { Str = "AsyncDtoStr2" }), _asyncDelayMilliseconds);
		Dto? result = null;

		await TestHelpers.AsyncDelayTester(async () => result = await _iserviceInstance.ReturnDtoApiAsync(), _asyncDelayMilliseconds, nameof(IService.ReturnDtoApiAsync));

		Assert.AreEqual("AsyncDtoStr2", result.Str, "Returned value was not matched!");
		IServiceHttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, nameof(IService.ReturnDtoApiAsync), "{}", 2);
	}
	[TestCase]
	public async Task AsyncVoidTaskResultTest()
	{
		SetupHttpRequestMessageSendAsync(_httpMessageHandlerMock, () => "", _asyncDelayMilliseconds);

		await TestHelpers.AsyncDelayTester(async () => await _iserviceInstance.ReturnVoidTaskApiAsync(), _asyncDelayMilliseconds, nameof(IService.ReturnVoidTaskApiAsync));

		IServiceHttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, nameof(IService.ReturnVoidTaskApiAsync), "{}", 2);
	}
	[TestCase]
	public async Task AsyncVoidFireAndForgetTest()
	{
		var isCalled = false;
		SetupHttpRequestMessageSendAsync(_httpMessageHandlerMock, () => { isCalled = true; return ""; }, _asyncDelayMilliseconds);

		var startTime = DateTime.Now;
		_iserviceInstance.FireAndForgetApiAsync();
		var milliSecsDifferential = (DateTime.Now - startTime).TotalMilliseconds;
		Console.WriteLine($"Milliseconds passed : {milliSecsDifferential}");

		Assert.That(milliSecsDifferential < _asyncDelayMilliseconds, $"{nameof(IService.FireAndForgetApiAsync)} took more time than intentional delay ({_asyncDelayMilliseconds}ms).");
		IServiceHttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, nameof(IService.FireAndForgetApiAsync), "{}", 1);
		await TestHelpers.IntervalChecks(returnTrueToBreak: () => isCalled, howManyTimes: 4, interval: _asyncDelayMilliseconds);
		Assert.IsTrue(isCalled, $"{nameof(IService.FireAndForgetApiAsync)} didn't call the expected lambda.");
	}
	[TestCase]
	public void MultiImplementationTest()
	{
		var isCalled = false;
		SetupHttpRequestMessageSendAsync(_httpMessageHandlerMock, () => { isCalled = true; return ""; });
		_imultiImplementInstances["ServiceKey1"].Void();
		HttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, "ConnectionGroupName:ServiceKey1", typeof(IMultiImplement), nameof(IMultiImplement.Void), $"?{DynamicWebApiHandler.ServiceKeyQueryStringParamName}=ServiceKey1", "{}", 1);
		Assert.IsTrue(isCalled, "IMultiImplement.Void ServiceKey1 didn't call the expected lambda.");

		isCalled = false;
		SetupHttpRequestMessageSendAsync(_httpMessageHandlerMock, () => { isCalled = true; return ""; });
		_imultiImplementInstances["ServiceKey2"].Void();
		HttpRequestMessageSendAsyncVerification(_httpMessageHandlerMock, "ConnectionGroupName:ServiceKey2", typeof(IMultiImplement), nameof(IMultiImplement.Void), $"?{DynamicWebApiHandler.ServiceKeyQueryStringParamName}=ServiceKey2", "{}", 1);
		Assert.IsTrue(isCalled, "IMultiImplement.Void ServiceKey2 didn't call the expected lambda.");
	}
}
