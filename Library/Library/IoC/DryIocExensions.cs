using DryIoc;

namespace MonoMicroservices.Library.IoC;
internal static class DryIocExensions
{
	/// <summary>
	/// Configures property injection for Controllers, ensure that you've added `AddControllersAsServices` in `ConfigureServices`
	/// </summary>
	/// <param name="controllerBaseType"><inheritdoc cref="SetupDiAndReturnIocFactory(Type)" path="/param[@name='controllerBaseType']"/></param>
	public static Rules WithControllersPropertyInjection(this Rules rules, Type? controllerBaseType = null)
	{
		if (controllerBaseType == null)
			return rules;
		return rules.With(
			propertiesAndFields: request => request.ServiceType.IsAssignableTo(controllerBaseType) ? PropertiesAndFields.Properties()(request) : null
		);
	}
}
