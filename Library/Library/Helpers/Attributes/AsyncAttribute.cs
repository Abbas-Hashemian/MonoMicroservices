namespace MonoMicroservices.Library.Helpers.Attributes;
/// <summary>
/// Marks a void-return method as async (Fire and forget).<br/>
/// For example <see cref="InterfaceImplementor.GetMethodsDynamicImplementations"/> uses this attr to identify async methods that return void in an interface<br/>
/// <code>
///		public interface IService {
///			[Async]
///			/*async : "async" keyword can only be used at implementation*/ void MyMethodAsync(...);
///		}
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AsyncAttribute : Attribute
{
}
