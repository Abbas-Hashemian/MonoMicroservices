using DryIocAttributes;

namespace MonoMicroservices.Library.IoC;
/// <summary>
/// WebRequestAttribute is not working in .net core but CurrentScopeReuseAttribute works. It's safer to avoid using it in all services.
/// </summary>
public class CoreWebRequestReuseAttribute : CurrentScopeReuseAttribute
{
}
