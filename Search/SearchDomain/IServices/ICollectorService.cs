using MonoMicroservices.Library.Microservices.Attributes;

namespace MonoMicroservices.SearchDomain.IServices;
[WebApiService(ConnectionGroupName = "CollectorService", SpecifiedMethodsOnly = false)]
public interface ICollectorService
{
	string GetServiceKey();
	Task<string> GetStringAsync();
}
