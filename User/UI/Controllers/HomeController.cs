using Microsoft.AspNetCore.Mvc;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.IoC;
using MonoMicroservices.SearchDomain.IServices;
using System.Text.Json;

namespace MonoMicroservices.UI.Controllers;

public class HomeController : Controller
{
	public ILogger<HomeController> _logger { private get; set; }
	public ISearchService SearchService { get; set; }
	public IIocService IocService { get; set; }
	public IConfigs Configs { get; set; }
	public async Task<IActionResult> Index()
	{
		var isInMicroservicesMode = Configs["IsInMicroservicesMode"] == "true";
		ViewBag.IsInMicroservicesMode = isInMicroservicesMode;
		ViewBag.StringResult = SearchService.GetSomeString("aa").Value;
		ViewBag.DtoResult = JsonSerializer.Serialize(SearchService.GetSomeDto().Value);
		var collectorAsyncResults = new Dictionary<string, string>();
		var collectorServices = IocService.Resolve<IEnumerable<ICollectorService>>();
		if (collectorServices != null)
			foreach (var service in collectorServices)
			{
				var serviceKey = isInMicroservicesMode
					? (string)((dynamic)service).__ServiceKey //of course we could use service.GetServiceKey() here to, this is just to mention the "__ServiceKey"
					: service.GetServiceKey();
				ViewBag.BeforeAwait = DateTime.Now.Millisecond;
				collectorAsyncResults.Add(serviceKey, await service.GetStringAsync());
				ViewBag.AfterAwait = DateTime.Now.Millisecond;
			}
		ViewBag.CollectorAsyncResults = collectorAsyncResults;
		ViewBag.BeforeAsync = DateTime.Now.Millisecond;
		var task = IocService.Resolve<ICollectorService>("Service2")?.GetStringAsync();
		ViewBag.AfterAsync = DateTime.Now.Millisecond;
		return View();
	}

	public IActionResult Privacy()
	{
		return View();
	}
}
