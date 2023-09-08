using Microsoft.Extensions.DependencyInjection;

namespace MonoMicroservices.Library.Microservices;

public interface IServerConnectionConfigs
{
	void AddHttpClientServices(IServiceCollection services);
	string GetBaseAddress(string key);
}