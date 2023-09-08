using DryIoc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using MonoMicroservices.Library.IoC;
using MonoMicroservices.Library.Microservices.Attributes;
using MonoMicroservices.Library.Microservices.WebApiHandler;
using MonoMicroservices.TestUtils;

namespace MonoMicroservices.LibraryTests.Microservices.WebApiHandler;

[TestFixture]
public class DynamicWebApiMiddlewareTests
{
	/// <summary>
	/// _requestDelegateMock is the "next()"
	/// </summary>
	private Mock<RequestDelegate> _middlewareNextMock = new Mock<RequestDelegate>();
	private Mock<HttpContext> _httpContextMock = new Mock<HttpContext>();
	private Mock<HttpRequest> _httpRequestMock = new Mock<HttpRequest>();
	private Mock<IQueryCollection> _queryCollectionMock = new Mock<IQueryCollection>();
	private Mock<HttpResponse> _httpResponseMock = new Mock<HttpResponse>();
	private Mock<IIocService> _iocServiceMock = new Mock<IIocService>();
	private Mock<IContainer> _containerMock = new Mock<IContainer>();
	private Mock<IWebApiService> _webApiServiceMock = new Mock<IWebApiService>();
	private Mock<IWebApiServiceWithSpecificEndpoints> _webApiServiceWithSpecificEndpointsMock = new Mock<IWebApiServiceWithSpecificEndpoints>();
	private Mock<Func<HttpContext, Type, Task<object?>>> _httpRequestReadFromJsonAsyncMock = new Mock<Func<HttpContext, Type, Task<object?>>>();
	private Mock<Func<HttpContext, object, Task>> _httpResponseWriteAsJsonAsyncMock = new Mock<Func<HttpContext, object, Task>>();
	private Mock<Func<HttpContext, string, Task>> _httpResponseWriteAsyncMock = new Mock<Func<HttpContext, string, Task>>();
	private DynamicWebApiMiddleware _middleware;
	private static string _pathToThisClass = $"/{string.Join("/", typeof(DynamicWebApiMiddlewareTests).FullName.Split(".").Skip(1))}";

	public record Dto { public string A { get; set; } public int B { get; set; } }
	[WebApiService(SpecifiedMethodsOnly = false)]
	public interface IWebApiService
	{
		Dto Endpoint1(int number, string str, Dto dto);
		Task AsyncMethod();
	}
	[WebApiService]
	public interface IWebApiServiceWithSpecificEndpoints
	{
		[WebApiEndpoint]
		string Endpoint2();
		void NormalMethod();
	}
	public interface INormalService { }


	[SetUp]
	public void Setup()
	{
		_httpRequestReadFromJsonAsyncMock.Reset();
		_httpResponseWriteAsyncMock.Reset();
		_httpResponseWriteAsJsonAsyncMock.Reset();
		_iocServiceMock.Reset();
		_webApiServiceMock.Reset();
		_webApiServiceWithSpecificEndpointsMock.Reset();
		_middlewareNextMock.Reset();

		_httpContextMock.SetupGet(c => c.Request).Returns(_httpRequestMock.Object);
		_httpContextMock.SetupGet(c => c.Response).Returns(_httpResponseMock.Object);
		_iocServiceMock.Setup(i => i.Resolve(typeof(IWebApiService))).Returns(_webApiServiceMock.Object);
		_iocServiceMock.Setup(i => i.Resolve(typeof(IWebApiServiceWithSpecificEndpoints))).Returns(_webApiServiceWithSpecificEndpointsMock.Object);
		_iocServiceMock.Setup(i => i.Resolve(typeof(IWebApiServiceWithSpecificEndpoints), "ServiceKey1")).Returns(_webApiServiceWithSpecificEndpointsMock.Object);
		_iocServiceMock.Setup(i => i.Resolve(typeof(IWebApiServiceWithSpecificEndpoints), "ServiceKey2")).Returns(_webApiServiceWithSpecificEndpointsMock.Object);

		_middleware = new DynamicWebApiMiddleware(_middlewareNextMock.Object);

		TestHelpers.SetByMockName<DynamicWebApiMiddleware>("HttpRequestReadFromJsonAsync", _httpRequestReadFromJsonAsyncMock.Object);
		TestHelpers.SetByMockName<DynamicWebApiMiddleware>("HttpResponsWriteAsJsonAsync", _httpResponseWriteAsJsonAsyncMock.Object);
		TestHelpers.SetByMockName<DynamicWebApiMiddleware>("HttpResponsWriteAsync", _httpResponseWriteAsyncMock.Object);
	}

	private void CustomizedSetup(string path, Func<Type, object>? getPostJsonObj, string? queryString = null)
	{
		_httpRequestMock.Reset();
		_httpRequestMock.SetupGet(r => r.Path).Returns(path);
		if (!string.IsNullOrEmpty(queryString))
			_httpRequestMock.SetupGet(r => r.QueryString).Returns(new QueryString(queryString));
		_httpRequestMock.SetupGet(r => r.Query).Returns(_queryCollectionMock.Object);
		if (getPostJsonObj != null)
			_httpRequestReadFromJsonAsyncMock.Setup(func => func(It.IsAny<HttpContext>(), It.IsAny<Type>()))
				.Returns(
					(Func<HttpContext, Type, Task<object?>>)(async (HttpContext cntx, Type paramsDtoType) => await Task.Run(() => getPostJsonObj(paramsDtoType)))
				);
	}

	[TestCase]
	public async Task ReturnDtoWithDtoParamTest()
	{
		CustomizedSetup(
			path: $"{_pathToThisClass}+{nameof(IWebApiService)}/{nameof(IWebApiService.Endpoint1)}/",
			getPostJsonObj: (Type paramsDtoType) =>
			{
				dynamic paramsDto = Activator.CreateInstance(paramsDtoType);
				paramsDto.number = 22;
				paramsDto.str = "asd";
				paramsDto.dto = new Dto { A = "asdasd" };
				return paramsDto;
			}
		);
		_webApiServiceMock.Setup(s => s.Endpoint1(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Dto>())).Returns(new Dto { A = "aa", B = 11 });

		await _middleware.InvokeAsync(_httpContextMock.Object, _iocServiceMock.Object);

		_middlewareNextMock.VerifyNoOtherCalls();//no next() call
		_webApiServiceMock.Verify(s => s.Endpoint1(/*number:*/22, /*str:*/"asd", /*dto:*/new Dto { A = "asdasd" }));
		_httpResponseWriteAsJsonAsyncMock.Verify(func => func(It.IsAny<HttpContext>(), It.Is<Dto>(d => d == new Dto { A = "aa", B = 11 })));
	}
	[TestCase]
	public async Task SpecifiedMethodsOnlyEnabledTest()
	{
		CustomizedSetup(
			path: $"{_pathToThisClass}+{nameof(IWebApiServiceWithSpecificEndpoints)}/{nameof(IWebApiServiceWithSpecificEndpoints.Endpoint2)}/",
			getPostJsonObj: null
		);
		_webApiServiceWithSpecificEndpointsMock.Setup(s => s.Endpoint2()).Returns("SpecifiedMethodsOnlyEnabledTest");

		await _middleware.InvokeAsync(_httpContextMock.Object, _iocServiceMock.Object);

		_middlewareNextMock.VerifyNoOtherCalls();//no next() call
		_webApiServiceWithSpecificEndpointsMock.Verify(s => s.Endpoint2());
		_httpResponseWriteAsJsonAsyncMock.Verify(func => func(It.IsAny<HttpContext>(), "SpecifiedMethodsOnlyEnabledTest"));
	}
	[TestCase]
	public async Task ServiceKeyTest()
	{
		CustomizedSetup(
			path: $"{_pathToThisClass}+{nameof(IWebApiServiceWithSpecificEndpoints)}/{nameof(IWebApiServiceWithSpecificEndpoints.Endpoint2)}/",
			getPostJsonObj: null,
			queryString: $"?{DynamicWebApiHandler.ServiceKeyQueryStringParamName}=ServiceKey1"
		);
		_queryCollectionMock.Setup(q => q[It.Is<string>(s => s == DynamicWebApiHandler.ServiceKeyQueryStringParamName)]).Returns((string key) => new StringValues("ServiceKey1"));
		_webApiServiceWithSpecificEndpointsMock.Setup(s => s.Endpoint2()).Returns(() => "ServiceKeyTest");

		await _middleware.InvokeAsync(_httpContextMock.Object, _iocServiceMock.Object);

		_middlewareNextMock.VerifyNoOtherCalls();//no next() call
		_webApiServiceWithSpecificEndpointsMock.Verify(s => s.Endpoint2());
		_httpResponseWriteAsJsonAsyncMock.Verify(func => func(It.IsAny<HttpContext>(), "ServiceKeyTest"));
	}
	[TestCase]
	public async Task AsyncMethodTest()
	{
		CustomizedSetup(
			path: $"{_pathToThisClass}+{nameof(IWebApiService)}/{nameof(IWebApiService.AsyncMethod)}/",
			getPostJsonObj: null
		);
		_webApiServiceMock.Setup(s => s.AsyncMethod()).Returns(async () => await Task.Delay(200));

		await TestHelpers.AsyncDelayTester(async () => await _middleware.InvokeAsync(_httpContextMock.Object, _iocServiceMock.Object), 200, $"Middleware InvokeAsync ({nameof(IWebApiService.AsyncMethod)})");
	}

	//------- Err tests

	[TestCase]
	public void NormalServiceMethodInvocationErrTest()
	{
		CustomizedSetup(
			path: $"{_pathToThisClass}+{nameof(IWebApiServiceWithSpecificEndpoints)}/{nameof(IWebApiServiceWithSpecificEndpoints.NormalMethod)}/",
			getPostJsonObj: null
		);

		Assert.ThrowsAsync(
			typeof(InvalidOperationException),
			async () => await _middleware.InvokeAsync(_httpContextMock.Object, _iocServiceMock.Object),
			"Invocation of an unavailable service method, which is not marked as API, didn't throw an InvalidOperationException."
		);

		_middlewareNextMock.VerifyNoOtherCalls();//no next() call
		_iocServiceMock.VerifyNoOtherCalls();
		_webApiServiceWithSpecificEndpointsMock.VerifyNoOtherCalls();
	}
	[TestCase]
	public void NormalServiceInvocationErrTest()
	{
		CustomizedSetup(path: $"{_pathToThisClass}+{nameof(INormalService)}/Method", getPostJsonObj: null);

		Assert.ThrowsAsync(
			typeof(InvalidOperationException),
			async () => await _middleware.InvokeAsync(_httpContextMock.Object, _iocServiceMock.Object),
			"Invocation of an unavailable service interface, which is not marked as API, didn't throw an InvalidOperationException."
		);

		_middlewareNextMock.VerifyNoOtherCalls();//no next() call
		_iocServiceMock.VerifyNoOtherCalls();
	}
	[TestCase]
	public void InvalidUrlForServiceApisErrTest()
	{
		CustomizedSetup(path: "/InvalidName/Invalid\"/Invalid/Invalid", getPostJsonObj: null);

		_httpRequestReadFromJsonAsyncMock.VerifyNoOtherCalls();
		_httpResponseWriteAsJsonAsyncMock.VerifyNoOtherCalls();
		_httpResponseWriteAsyncMock.VerifyNoOtherCalls();
		_middlewareNextMock.Verify();//next()
		_iocServiceMock.VerifyNoOtherCalls();
	}
}
