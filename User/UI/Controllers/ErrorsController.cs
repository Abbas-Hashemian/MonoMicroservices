using Microsoft.AspNetCore.Mvc;

namespace MonoMicroservices.UI.Controllers;
public class ErrorsController : Controller
{
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Exception()
	{
		return View(/*new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }*/);
	}
}
