using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using MonoMicroservices.Library.IoC;
using MonoMicroservices.Library.Microservices.WebApiHandler;
using System.ComponentModel.Composition;
using System.Reflection;

namespace MonoMicroservices.Library.Microservices;
[Export(typeof(IDependencyRegistrar))]
internal class DependencyRegistrar : IDependencyRegistrar
{
	private readonly IServerConnectionConfigs _serverConnectionConfigs;
	private readonly IIocService _iocService;

	public DependencyRegistrar(IServerConnectionConfigs serverConnectionConfigs, IIocService iocService)
	{
		_serverConnectionConfigs = serverConnectionConfigs;
		_iocService = iocService;
	}

	public void OnBeforeBuildServiceProvider(IContainer container, IServiceCollection services, IEnumerable<Assembly> assemblies)
	{
		services.AddHttpClient();
		_serverConnectionConfigs.AddHttpClientServices(services);
	}

	public void OnAfterBuildServiceProvider(IContainer container, IServiceCollection services, IEnumerable<Assembly> assemblies)
	{
		_iocService.Resolve<IDynamicWebApiHandler>()?.RegisterDynamicApiHandlers(assemblies, services);
	}
}
