using MonoMicroservices.SearchDomain.IServices;
using System.ComponentModel.Composition;
using System.Reflection;

namespace MonoMicroservices.SearchService;
[Export("Service1", typeof(ICollectorService))]
internal class CollectorService1 : ICollectorService
{
	public async Task<string> GetStringAsync()
	{
		await Task.Delay(100);
		return await Task.Run(() => "Service1.GetStringAsync");
	}
	public string GetServiceKey() => GetType().GetCustomAttribute<ExportAttribute>()?.ContractName ?? "";
}
