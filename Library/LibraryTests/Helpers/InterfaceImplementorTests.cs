using Microsoft.CSharp.RuntimeBinder;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.Helpers.Attributes;
using MonoMicroservices.TestUtils;
using System.Reflection;

namespace MonoMicroservices.LibraryTests.Helpers;
[TestFixture]
public class InterfaceImplementorTests
{
	private const int AsyncDelayMilliseconds = 100;
	private Type _syncServiceGeneratedType;
	private ISyncService _syncServiceObj;
	private Func<dynamic, MethodInfo, IEnumerable<object?>, object?>? _injectedSyncServiceSyncHandler = (dynamic thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => null;
	private Action<dynamic, MethodInfo, IEnumerable<object?>>? _injectedSyncServiceSyncVoidHandler = (dynamic thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => { };
	public class Dto { public int Integer { get; set; } }
	public enum MyEnum { A, B }
	public interface ISyncService
	{
		int GetInt(int integer, Dto dto);
		string GetString(string str);
		MyEnum GetEnum(MyEnum e);
		Dto GetDto();
		void Action(Dto dto);
	}
	public interface IAsyncService
	{
		Task<int> GetAsync();
		[Async]
		void VoidAsync();
	}
	public interface INoAsyncHandlerErrService
	{
		Task<int> GetAsync();
	}
	public interface IInvalidAsyncAttrErrService
	{
		[Async]
		int GetAsync();
	}
	public interface IEmptyService { }

	[SetUp]
	public void Setup()
	{
		if (_syncServiceGeneratedType == null)
		{
			_syncServiceGeneratedType = InterfaceImplementor.GetMethodsDynamicImplementations(
				typeof(ISyncService),
				new InterfaceImplementor.Handlers
				{
					SyncHandler = (dynamic thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => _injectedSyncServiceSyncHandler(thisInstance, methodInfo, @params),
					SyncVoidHandler = (dynamic thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => _injectedSyncServiceSyncVoidHandler(thisInstance, methodInfo, @params)
				}
			);
			_syncServiceObj = (ISyncService)Activator.CreateInstance(_syncServiceGeneratedType);
		}

	}
	[TestCase]
	public void ImplementationAndExtensionTests()
	{
		var extensionDefiner = new InterfaceImplementor.ExtensionDefiner(
				buildMemberFactory: typeBuilder =>
				{
					typeBuilder.DefineField("static", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
					typeBuilder.DefineField("instance", typeof(int), FieldAttributes.Public);
					return null;
				},
				onAfterDynamicTypeGenerateEvent: type => type.GetField("static", BindingFlags.Public | BindingFlags.Static).SetValue(null, 22)
			);
		var type = InterfaceImplementor.GetMethodsDynamicImplementations(typeof(IEmptyService), new InterfaceImplementor.Handlers { }, extensionDefiner: extensionDefiner);
		var type2 = InterfaceImplementor.GetMethodsDynamicImplementations(typeof(IEmptyService), new InterfaceImplementor.Handlers { }, extensionDefiner: extensionDefiner);
		Assert.That(type == type2, "Two differet types for same interface!");
		Assert.That(type.GetInterfaces().Contains(typeof(IEmptyService)), Is.True, message: $"Returned type is not derived from the interface!");

		dynamic obj = Activator.CreateInstance(type);
		Assert.AreEqual(22, type.GetField("static", BindingFlags.Public | BindingFlags.Static).GetValue(null));
		obj.instance = 33;
		Assert.AreEqual(33, (int)obj.instance);
		Assert.Throws<RuntimeBinderException>(() => obj.instance2 = 22);
	}
	[TestCase]
	public void ThisInstanceParamTest()
	{
		_injectedSyncServiceSyncHandler = (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => thisInstance?.GetType().GetInterfaces().Contains(typeof(ISyncService)) ?? false ? 1 : 0;
		Assert.That(_syncServiceObj.GetInt(0, new Dto { Integer = 0 }) == 1, message: "thisInstance is null or is not implementing IService!");
	}
	[TestCase]
	public void IntReturnWithDtoParamSyncTest()
	{
		_injectedSyncServiceSyncHandler = (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => (object)((int)@params.ElementAt(0) + ((Dto)@params.ElementAt(1)).Integer);
		Assert.That(_syncServiceObj.GetInt(2, new Dto { Integer = 5 }), Is.EqualTo(7));
	}
	public void StringAndEnumReturnSyncTests()
	{
		_injectedSyncServiceSyncHandler = (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => @params.ElementAt(0);
		Assert.That(_syncServiceObj.GetString("aaa"), Is.EqualTo("aaa"));
		Assert.That(_syncServiceObj.GetEnum(MyEnum.B), Is.EqualTo(MyEnum.B));
		Assert.AreNotEqual(MyEnum.B, _syncServiceObj.GetEnum(MyEnum.A));
	}
	public void DtoReturnSyncTest()
	{
		_injectedSyncServiceSyncHandler = (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => new Dto { Integer = 33 };
		Assert.That(_syncServiceObj.GetDto().Integer, Is.EqualTo(33));
	}
	public void VoidReturnSyncTest()
	{
		_injectedSyncServiceSyncVoidHandler = (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => { ((Dto)@params.ElementAt(0)).Integer = 3; };
		var dto = new Dto { Integer = 1 };
		_syncServiceObj.Action(dto);
		Assert.That(dto.Integer, Is.EqualTo(3));
	}

	[TestCase]
	public async Task AsyncTest()
	{
		var type = InterfaceImplementor.GetMethodsDynamicImplementations(
			typeof(IAsyncService),
			new InterfaceImplementor.Handlers
			{
				AsyncHandler = async (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) =>
				{
					await Task.Delay(AsyncDelayMilliseconds);
					return 56;
				},
				AsyncVoidHandler = async (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => { await Task.Delay(1); }
			}
		);
		var svc = (IAsyncService)Activator.CreateInstance(type);
		int result = 0;
		await TestHelpers.AsyncDelayTester(async () => result = await svc.GetAsync(), AsyncDelayMilliseconds, nameof(IAsyncService.GetAsync));
		Assert.AreEqual(56, result);

		svc.VoidAsync();
	}

	[TestCase]
	public void NoProperHandlerTest()
	{
		Assert.Throws<NotImplementedException>(() =>
			InterfaceImplementor.GetMethodsDynamicImplementations(
				typeof(INoAsyncHandlerErrService),
				new InterfaceImplementor.Handlers
				{
					SyncHandler = (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => 1
				}
			)
		).ExpectedExceptionMessageIs("Proper dynamic implementation is not provided for async method");
	}

	[TestCase]
	public void AsyncMethodReturnTypeErrTest()
	{
		Assert.Throws<Exception>(() =>
			InterfaceImplementor.GetMethodsDynamicImplementations(
				typeof(IInvalidAsyncAttrErrService),
				new InterfaceImplementor.Handlers
				{
					AsyncHandler = async (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => { await Task.Delay(1); return new Task<int>(() => 1); }
				}
			)
		).ExpectedExceptionMessageIs($"Methods that are marked with {nameof(AsyncAttribute)} must have Task<T>, Task or void return type.");
	}
	//My older methods were param-based
	//public interface ISimilarMethodsService
	//{
	//	string GetString(string str);
	//}
	//public static object[] SyncTestsParams =
	//	new Dictionary<string, object[]>
	//	{
	//		...
	//		[$"Parallel services test"] = new object[] {
	//			typeof(ISimilarMethodsService),
	//			new InterfaceImplementor.Handlers { SyncHandler = (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => "bb" },
	//			(object obj, Type type) =>
	//			{
	//				Assert.That(type.GetInterfaces().Contains(typeof(ISimilarMethodsService)), Is.True, message: $"Returned type is not derived from the interface!");
	//				Assert.That(((ISimilarMethodsService)obj).GetString("aa"), Is.EqualTo("bb"));
	//			}
	//		},
	//		...
	//	}.ConvertToNamedTestCaseData();
	//[TestCaseSource(nameof(SyncTestsParams))]
	//public void SyncTests(
	//	Type interfaceType,
	//	InterfaceImplementor.Handlers? handlers,
	//	Action<object, Type> expectedTests
	//)
	//{
	//	var defaultSyncHandler = (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => (object?)null;
	//	var defaultSyncVoidHandler = (object thisInstance, MethodInfo methodInfo, IEnumerable<object?> @params) => { };
	//	if (handlers == null)
	//		handlers = new InterfaceImplementor.Handlers { SyncHandler = defaultSyncHandler, SyncVoidHandler = defaultSyncVoidHandler };
	//	if (handlers.SyncHandler == null)
	//		handlers.SyncHandler = defaultSyncHandler;
	//	if (handlers.SyncVoidHandler == null)
	//		handlers.SyncVoidHandler = defaultSyncVoidHandler;

	//	var type = InterfaceImplementor.GetMethodsDynamicImplementations(interfaceType, handlers,
	//		extensionDefiner: new InterfaceImplementor.ExtensionDefiner(
	//			buildMemberFactory: typeBuilder =>
	//			{
	//				typeBuilder.DefineField("static", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
	//				typeBuilder.DefineField("instance", typeof(int), FieldAttributes.Public);
	//				return null;
	//			},
	//			onAfterDynamicTypeGenerateEvent: type => type.GetField("static", BindingFlags.Public | BindingFlags.Static).SetValue(null, 22)
	//		)
	//	);
	//	dynamic obj = Activator.CreateInstance(type);
	//	expectedTests(obj, type);
	//}
}
