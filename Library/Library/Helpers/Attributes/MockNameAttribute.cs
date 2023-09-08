namespace MonoMicroservices.Library.Helpers.Attributes;
/// <summary>
/// It's the name used in UnitTests to find the private static delegate Properties/Fields. It makes them independent from the real name of the prop.
/// Extension methods can't be mocked, the best solution is to wrap them in a Func/Action in a prop of main service wthout creating any wrapper Type.<br/>
/// Example:
/// <code>
///		public class DynamicWebApiMiddleware
///		{
///			[MockName("HttpRequestReadFromJsonAsync")]
///			private static Func&lt;HttpContext, Type, Task&lt;object?&gt;&gt; _httpRequestReadFromJsonAsync =
///				async (context, paramsDtoType) => await context.Request.ReadFromJsonAsync(paramsDtoType);
///			...
///		}
///		//and in the UnitTest SetUp :
///		TestHelpers.SetByMockName&lt;DynamicWebApiMiddleware&gt;("HttpRequestReadFromJsonAsync", HttpRequestReadFromJsonAsyncMock.Object);
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public class MockNameAttribute : Attribute
{
	public string MockName { get; set; }

	public MockNameAttribute(string mockName)
	{
		MockName = mockName;
	}
}
