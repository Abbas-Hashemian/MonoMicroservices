using Microsoft.Extensions.Configuration;
using MonoMicroservices.Library.Enums;

namespace MonoMicroservices.Library.Helpers;
public interface IConfigs
{
	string this[string key] { get; }

	AppEnvironmentMode EnvMode { get; }

	IConfigurationSection GetSection(string key);
	IConfigurationRoot GetJsonFileConfigs(string configsBaseDirPath, string configFileName, Action<IConfigurationBuilder>? onBeforeBuild = null);
	Dictionary<string, object?> GetSection2ndLevelFlatDictionary(string sectionKey);
}
