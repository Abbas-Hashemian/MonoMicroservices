using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MonoMicroservices.Library.IoC;
using System.Reflection;

namespace MonoMicroservices.LibraryTests.IoC;
[TestFixture]
public class IocServiceTests
{

	private Mock<IServiceCollection> _serviceCollectionMock = new Mock<IServiceCollection>();
	private Mock<IDependencyRegistrar> _dependencyRegistrarMock;
	private Mock<IContainer> _containerMock;
	private TestableIocService _iocService;

	//TODO: More protected methods must be created for RegisterExports and ResolveMany extension methods, is it worth it?
	//With is substituting the _container object so we have to mock it, ResolveMany was for IDependencyRegistrar so we had to hve it too
	private class TestableIocService : IocService
	{
		public Mock<IContainer> ContainerMock = new Mock<IContainer>();
		public Mock<IServiceProvider> ServiceProviderMock = new Mock<IServiceProvider>();
		public Mock<IDependencyRegistrar> DependencyRegistrarMock = new Mock<IDependencyRegistrar>();
		protected override IContainer CreateChildContainerWithNewRules(IContainer container, Func<Rules, Rules> rules) => ContainerMock.Object;
		protected override IServiceProvider BuildIntegratedServiceProvider(IContainer container, IServiceCollection services) => ServiceProviderMock.Object;
		protected override IEnumerable<IDependencyRegistrar> GetDependencyRegistrars() => new[] { DependencyRegistrarMock.Object };
	}

	[SetUp]
	public void Setup()
	{
		_iocService = new TestableIocService();
		_containerMock = _iocService.ContainerMock;
		_dependencyRegistrarMock = _iocService.DependencyRegistrarMock;
	}

	[TestCase]
	public void RegisterServicesAndGetFactoryTests()
	{
		_iocService.RegisterServicesAndGetFactory(_serviceCollectionMock.Object, _containerMock.Object);

		//Extension methods can't be mocked or verified
		//_containerMock.Verify(c => c.With(It.IsAny<Func<Rules, Rules>>(), It.Is<IScopeContext>(c => c == null)));
		//_containerMock.Verify(c => c.RegisterExports(It.IsAny<Assembly>()));
		//_containerMock.Verify(c => c.ResolveMany<IDependencyRegistrar>(null, ResolveManyBehavior.AsLazyEnumerable, null, null));

		_dependencyRegistrarMock.Verify(dr =>
			dr.OnBeforeBuildServiceProvider(
				It.IsAny<IContainer>(),
				_serviceCollectionMock.Object,
				It.IsAny<IEnumerable<Assembly>>()
			)
		);
		//_containerMock.Verify(c => c.WithDependencyInjectionAdapter(It.Is<IServiceCollection>(s => s == _serviceCollectionMock.Object), null, RegistrySharing.Share));
		_dependencyRegistrarMock.Verify(dr =>
			dr.OnAfterBuildServiceProvider(
				It.IsAny<IContainer>(),
				_serviceCollectionMock.Object,
				It.IsAny<IEnumerable<Assembly>>()
			)
		);
	}

}
