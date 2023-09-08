using System.Reflection;
using System.Reflection.Emit;

namespace MonoMicroservices.Library.Helpers;

internal static class AssemblyHelper
{
	public const string DynamicAssemblyName = "DynamicAssembly";
	public const string DynamicModuleName = "DynamicModule";
	private static AssemblyBuilder _sharedDynamicAssmbelyBuilder;
	private static readonly object _sharedDynamicAssmbelyBuilderLock = new object();
	private static ModuleBuilder _sharedDynamicAssemblyModuleBuilder;
	private static readonly object _sharedDynamicAssemblyModuleBuilderLock = new object();

	public static IEnumerable<Assembly> LoadAllSolutionAssemblies()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		if (!directory.Exists)
			throw new Exception($"{Consts.SolutionName} assemblies directory was not found!");

		return AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name != DynamicAssemblyName)
			.Union(
				directory
					.GetFiles($"{Consts.SolutionName}.*.dll", SearchOption.TopDirectoryOnly)
					.Where(file => file.Name is not $"{Consts.SolutionName}.Library.dll" and not $"{Consts.SolutionName}.UI.dll")
					.Select(file => Assembly.LoadFrom(file.FullName))
			);
	}
	public static AssemblyBuilder GetSharedDynamicAssemblyBuilder()
	{
		if (_sharedDynamicAssmbelyBuilder == null)
			lock (_sharedDynamicAssmbelyBuilderLock)
			{
				if (_sharedDynamicAssmbelyBuilder == null)
					_sharedDynamicAssmbelyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(DynamicAssemblyName), AssemblyBuilderAccess.Run);
			}
		return _sharedDynamicAssmbelyBuilder;
	}
	public static ModuleBuilder GetSharedDynamicAssemblyModuleBuilder()
	{
		if (_sharedDynamicAssemblyModuleBuilder == null)
			lock (_sharedDynamicAssemblyModuleBuilderLock)
			{
				if (_sharedDynamicAssemblyModuleBuilder == null)
					_sharedDynamicAssemblyModuleBuilder = GetSharedDynamicAssemblyBuilder().DefineDynamicModule(DynamicModuleName/*, emitSymbolInfo: true required for SetLocalSymInfo but it's only aailable in mscorlib.dll not System.Reflection.Emit.dll*/);
			}
		return _sharedDynamicAssemblyModuleBuilder;
	}
}