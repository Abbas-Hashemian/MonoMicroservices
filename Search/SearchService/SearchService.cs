using MonoMicroservices.Library.Helpers;
using MonoMicroservices.SearchDomain.Dtos;
using MonoMicroservices.SearchDomain.Enums;
using MonoMicroservices.SearchDomain.IServices;
using System.ComponentModel.Composition;

namespace MonoMicroservices.SearchService;

[Export(typeof(ISearchService))]
internal class SearchService : ISearchService
{
	public ServiceResult<string> GetSomeString(string p) => ServiceResult.Ok(p + "Result");
	public ServiceResult<SomeDto> GetSomeDto() => ServiceResult.Ok(new SomeDto { Str = "Some string", SomeEnum = SomeEnum.B });
}
