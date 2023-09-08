using Microsoft.Extensions.Configuration;
using MonoMicroservices.Library.DataAccess;
using MonoMicroservices.Library.Enums;
using MonoMicroservices.Library.Helpers.Attributes;
using System.ComponentModel.Composition;

namespace MonoMicroservices.Library.Helpers;

[Export(typeof(IConfigs))]
internal class Configs : IConfigs
{
	/// <summary>
	/// Used for design time only
	/// </summary>
	private const string _programRoot = "../../User/UI";

	[MockName("_appSettings")]
	private static IConfigurationRoot? _appSettings = null;
	/// <summary>
	/// DO NOT USE ME IN Program.cs. And to mock key-specific configs in UnitTests, do not use me to get a key directly in services to avoid anti-pattern pitfall, use the IConfigs instead.<br/>
	/// This prop is directly used in DesignTimeDbContextFactory only. It's design-time and no services are registered through Program.cs/Startup.cs over there.
	/// </summary>
	internal static IConfigurationRoot AppSettings => _appSettings ?? SetConfigurationsIfNotSet();
	private static AppEnvironmentMode? _environmentMode = null;
	private static AppEnvironmentMode EnvironmentMode => _environmentMode ?? SetEnvironmentMode();
	/// <summary>
	/// DO NOT USE ME IN Program.cs
	/// </summary>
	public AppEnvironmentMode EnvMode => EnvironmentMode;

	public string this[string key] => key.Contains(':') ? AppSettings.GetSection(key).Value : AppSettings[key];
	public IConfigurationSection GetSection(string key) => AppSettings.GetSection(key);
	/// <inheritdoc cref="Helpers.StringHelper.GetJson2ndLevelFlatDictionary"/>
	public Dictionary<string, object?> GetSection2ndLevelFlatDictionary(string sectionKey)
		=> Helpers.StringHelper.GetJson2ndLevelFlatDictionary(GetSection(sectionKey).Get<string>());

	/// <summary>
	/// To be used in Startup.cs|Programs.cs only. Also DbDesignTimeFactory is using it implicitly, indirectly
	/// </summary>
	public static AppEnvironmentMode SetEnvironmentMode(string? env = null)
	{
		try
		{
			if (env != null)
				_environmentMode = env.ToEnum<AppEnvironmentMode>();
			else
			{
				var currentDir = Directory.GetCurrentDirectory();
				var settingsFilePath = $"{currentDir}/{_programRoot}/Properties/launchSettings.json";
				if (!File.Exists(settingsFilePath))
					throw new Exception("The lauchSettings.json file was not found!");
				var launchSettings =
					new ConfigurationBuilder()
						.SetBasePath(currentDir)
						.AddJsonFile(settingsFilePath)
						.Build();
				_environmentMode = launchSettings["profiles:UI:environmentVariables:ASPNETCORE_ENVIRONMENT"].ToEnum<AppEnvironmentMode>();
			}
		}
		catch
		{
			throw new Exception($"The ASPNETCORE_ENVIRONMENT \"{env}\" is not applicable to {nameof(AppEnvironmentMode)} enum");
		}
		return (AppEnvironmentMode)_environmentMode;
	}

	/// <summary>
	/// Sets global configs of the application<br/>
	/// To be used in Startup.cs|Programs.cs only<br/>
	/// Design-time db migrations do not pass the appSettings param in, but Program.cs|Startup.cs does. When appSettings is null this method analyses the launchSettings.json and appSettings.json.
	/// </summary>
	public static IConfigurationRoot SetConfigurationsIfNotSet(IConfigurationRoot? appSettings = null)
	{
		if (_appSettings != null)
			return _appSettings;
		if (appSettings != null)
			return _appSettings = appSettings;

		//Design time db migration configs also service WebAPI projects :
		return _appSettings = StaticGetJsonFileConfigs(
			$"{Directory.GetCurrentDirectory()}/{_programRoot}",
			$"/appsettings{(EnvironmentMode != AppEnvironmentMode.Production ? "." + EnvironmentMode : "")}.json",
			(IConfigurationBuilder builder) =>
			{
				if (EnvironmentMode == AppEnvironmentMode.Development)
					builder = builder.AddUserSecrets<DbConnectionConfigService>();
			}
		);
	}

	/// <summary>
	/// Does not set the app global configs, just loads a json file and returns the builded configs
	/// </summary>
	private static IConfigurationRoot StaticGetJsonFileConfigs(string configsBaseDirPath, string configFileName, Action<IConfigurationBuilder>? onBeforeBuild = null)
	{
		var settingsFilePath = configsBaseDirPath + configFileName;
		if (!File.Exists(settingsFilePath))
			throw new Exception("The settings json file was not found!");
		var configBuilder = new ConfigurationBuilder()
			.SetBasePath(configsBaseDirPath)
			.AddJsonFile(settingsFilePath);

		if (onBeforeBuild != null)
			onBeforeBuild(configBuilder);

		return configBuilder.Build();
	}
	public IConfigurationRoot GetJsonFileConfigs(string configsBaseDirPath, string configFileName, Action<IConfigurationBuilder>? onBeforeBuild = null)
		=> StaticGetJsonFileConfigs(configsBaseDirPath, configFileName, onBeforeBuild);
}
