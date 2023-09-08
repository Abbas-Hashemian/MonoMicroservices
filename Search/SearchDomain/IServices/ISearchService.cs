using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.Microservices.Attributes;
using MonoMicroservices.SearchDomain.Dtos;

namespace MonoMicroservices.SearchDomain.IServices;

[WebApiService]
public interface ISearchService
{
	[WebApiEndpoint]
	ServiceResult<string> GetSomeString(string p);
	[WebApiEndpoint]
	ServiceResult<SomeDto> GetSomeDto();
}
