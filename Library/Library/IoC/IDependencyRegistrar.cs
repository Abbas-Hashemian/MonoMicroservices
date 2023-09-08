using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MonoMicroservices.Library.IoC;
public interface IDependencyRegistrar
{
	/// <summary>
	/// To register manual services in the container before the integration of IServiceCollection and DryIoc IContainer (before building the IServiceBuilder).<br/>
	/// .net internal services must be registered in the IServiceCollection at this point like services.AddHttpClient() but they must not be resolved/injected at this point.
	/// </summary>
	void OnBeforeBuildServiceProvider(IContainer container, IServiceCollection services, IEnumerable<Assembly> assemblies);
	void OnAfterBuildServiceProvider(IContainer container, IServiceCollection services, IEnumerable<Assembly> assemblies);
}
