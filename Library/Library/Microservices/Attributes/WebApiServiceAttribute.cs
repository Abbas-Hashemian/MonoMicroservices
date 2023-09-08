namespace MonoMicroservices.Library.Microservices.Attributes;
/// <summary>
/// Marks the methods of a service interface as WebAPI.<br/>
/// The IoC resolver <see cref="WebApiHandler.IDynamicWebApiHandler.RegisterDynamicApiHandlers"/> resolves them using a lambda returning a dynamically generated Type.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class WebApiServiceAttribute : Attribute
{
	/// <summary>
	/// Only the methods marked with <see cref="WebApiEndpointAttribute"/> attr can be invoked remotely.
	/// </summary>
	public bool SpecifiedMethodsOnly { get; set; } = true;
	/// <summary>
	/// The structure of MicroservicesConnections in appSettings :
	/// <code>
	///		{
	///			"MicroservicesConnections":{
	///				"ConnectionGroupName1": { "<b>ServiceKey</b>1": "http://...", ... },
	///				/*ServiceKey = contractName = connectionLogicName*/
	///				"ConnectionGroupName2": { ... },
	///				"DomainNamespace": "http://..."
	///			}
	///		}
	/// </code>
	/// ServiceKey is the DryIoC's ServiceKey or contractName param of ExportAttribute again used by DryIoC , also the connection logic name used by HttpClient(connectionLogicName).<br/>
	/// A service interface can either :
	/// <list type="bullet">
	///		<item>Have one implementation, one server address and no need for a service key</item>
	///		<item>Have multiple implementations over different containers wih different service keys (multiple service keys might be in one container)</item>
	/// </list>
	/// If a service interface has <see cref="ConnectionGroupName"/> it means it must be registered multiple times for different implementations over different containers.
	/// So multiple <see cref="DryIoc.Registrator.RegisterDelegate(DryIoc.IRegistrator, Type, Func{DryIoc.IResolverContext, object}, DryIoc.IReuse, DryIoc.Setup, DryIoc.IfAlreadyRegistered?, object)"/>
	/// will be called for different <b>"ServceKeys"</b> defined in target ConnectionGroupName in appSettings.
	/// </summary>
	public string? ConnectionGroupName { get; set; } = null;
}
