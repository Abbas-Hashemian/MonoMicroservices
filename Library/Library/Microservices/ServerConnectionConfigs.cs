using Microsoft.Extensions.DependencyInjection;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.Helpers.Attributes;
using System.ComponentModel.Composition;

namespace MonoMicroservices.Library.Microservices;

[Export(typeof(IServerConnectionConfigs))]
internal class ServerConnectionConfigs : IServerConnectionConfigs
{
	private readonly IConfigs _configs;

	public ServerConnectionConfigs(IConfigs configs)
	{
		_configs = configs;
	}

	public string GetBaseAddress(string key) => _configs.GetSection("MicroservicesConnections")[key];

	[MockName("AddHttpClient")]
	private static Func<IServiceCollection, string, Action<HttpClient>, IHttpClientBuilder> AddHttpClient =
		(IServiceCollection services, string name, Action<HttpClient> configClient)
			=> services.AddHttpClient(name, client => configClient(client));

	public void AddHttpClientServices(IServiceCollection services)
	{
		if ((_configs["IsInMicroservicesMode"] ?? "false") == "false")
			return;
		foreach (var connection in _configs.GetSection2ndLevelFlatDictionary("MicroservicesConnections"))
			AddHttpClient(services, connection.Key, client => { client.BaseAddress = new Uri(connection.Value?.ToString() ?? ""); });
	}
}
