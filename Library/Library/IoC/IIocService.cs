using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MonoMicroservices.Library.IoC;

public interface IIocService
{
	IContainer Container { get; }
	IServiceProvider ServiceProvider { get; }

	T? Resolve<T>();
	DryIocServiceProviderFactory RegisterServicesAndGetFactory(IServiceCollection services, IContainer container, Type? controllerBaseType = null);
	T? Resolve<T>(object serviceKey);
	object Resolve(Type serviceType);
	object Resolve(Type serviceType, object serviceKey);
}