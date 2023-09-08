namespace MonoMicroservices.Library.Microservices.Attributes;
/// <summary>
/// Marks the a method as WebAPI endpoint. The surrounding interface must be marked with <see cref="WebApiServiceAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class WebApiEndpointAttribute : Attribute
{
	/// <summary>
	/// Uses RabbitMq or other event bus to handle callback asynchronously.
	/// </summary>
	public bool UseEventBusQueue { get; set; } = false;
	/// <summary>
	/// CAUTION! This might be bug-prone, it doesn't have a unit test
	/// By default, the url is extracted based on the service interface name , its namespace and the method name, this helps to override this behaviour.
	/// </summary>
	public string? ExplicitEndPointUrl { get; set; } = null;
}
