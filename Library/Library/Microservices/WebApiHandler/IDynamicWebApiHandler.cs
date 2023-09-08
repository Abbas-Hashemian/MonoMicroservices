using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MonoMicroservices.Library.Microservices.WebApiHandler;

/// <summary>
/// For Domain layer interfaces that are marked with WebApiService attr, this creates fake implementation/handler for their methods.
/// </summary>
public interface IDynamicWebApiHandler
{
	/// <summary>
	/// This app can work both as Monolith and Microservices. All Domain dlls (interfaces) are available in all containers but their implementations are on their own container only.<br/>
	/// Resolving a service and calling a method is followed by a WebAPI callback.<br/>
	/// If an interface is implemented multiple times on different containers or/and multiple times on one container,
	/// they must have serviceKey(as DryIoc named it) or contractName(as MS Mef named it in ExportAttribute).
	/// The serviceKeys must be mapped to the server addresses in appSettings.json, MicroservicesConnections section.
	/// <code>
	///  "MicroservicesConnections": {
	///    "User": ""
	///  }
	///
	/// </code>
	/// </summary>
	/// <remarks>
	/// Container must be created before.
	/// </remarks>
	void RegisterDynamicApiHandlers(IEnumerable<Assembly> assemblies, IServiceCollection services);
}