using MonoMicroservices.SearchDomain.IServices;
using System.ComponentModel.Composition;
using System.Reflection;

namespace MonoMicroservices.SearchService;
[Export("Service2", typeof(ICollectorService))]
internal class CollectorService2 : ICollectorService
{
	public async Task<string> GetStringAsync()
	{
		await Task.Delay(100);
		return await Task.Run(() => "Service2.GetStringAsync");
	}
	public string GetServiceKey() => GetType().GetCustomAttribute<ExportAttribute>()?.ContractName ?? "";
}
