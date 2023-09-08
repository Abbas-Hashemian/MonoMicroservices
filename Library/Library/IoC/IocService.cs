using DryIoc;
using DryIoc.MefAttributedModel;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MonoMicroservices.Library.Helpers;

namespace MonoMicroservices.Library.IoC;

internal class IocService : IIocService
{
	private static IContainer _container;
	private static IServiceProvider _serviceProvider;
	public IContainer Container => _container;
	public IServiceProvider ServiceProvider => _serviceProvider;

	public T Resolve<T>() => _serviceProvider.GetService<T>() ?? _container.Resolve<T>();
	public T Resolve<T>(object serviceKey) => _container.Resolve<T>(serviceKey);
	public object Resolve(Type serviceType) => _container.Resolve(serviceType);
	public object Resolve(Type serviceType, object serviceKey) => _container.Resolve(serviceType, serviceKey);

	/// <param name="dependencyRegistrars">If null it loops all registered IDependencyRegistrar cases.</param>
	public DryIocServiceProviderFactory RegisterServicesAndGetFactory(IServiceCollection services, IContainer container, Type? controllerBaseType = null)
	{
		var assemblies = AssemblyHelper.LoadAllSolutionAssemblies();

		_container = CreateChildContainerWithNewRules(container, rules =>
			rules.WithControllersPropertyInjection(controllerBaseType)
		//.WithDefaultIfAlreadyRegistered(IfAlreadyRegistered.Keep)
		//.WithMefAttributedModel()
		);
		_container.RegisterExports(assemblies);

		var dependencyRegistrars = GetDependencyRegistrars();
		foreach (var registrar in dependencyRegistrars)
			registrar.OnBeforeBuildServiceProvider(_container, services, assemblies);
		_serviceProvider = BuildIntegratedServiceProvider(_container, services);
		foreach (var registrar in dependencyRegistrars)
			registrar.OnAfterBuildServiceProvider(_container, services, assemblies);

		return new DryIocServiceProviderFactory(_container/*, ExampleOfCustomRegisterDescriptor*/);
	}

	protected virtual IContainer CreateChildContainerWithNewRules(IContainer container, Func<Rules, Rules> rules) => container.With(rules);
	protected virtual IServiceProvider BuildIntegratedServiceProvider(IContainer container, IServiceCollection services) => container.WithDependencyInjectionAdapter(services).BuildServiceProvider();
	protected virtual IEnumerable<IDependencyRegistrar> GetDependencyRegistrars() => _container.ResolveMany<IDependencyRegistrar>();


	//	private static bool ExampleOfCustomRegisterDescriptor(IRegistrator registrator, ServiceDescriptor descriptor)
	//	{
	//#if DEBUG
	//		if (descriptor.ServiceType == typeof(ILoggerFactory))
	//			Console.WriteLine($"{descriptor.ServiceType.Name} is registered as instance: {descriptor}");
	//#endif
	//		return false; // fallback to the default registration logic
	//	}
}
