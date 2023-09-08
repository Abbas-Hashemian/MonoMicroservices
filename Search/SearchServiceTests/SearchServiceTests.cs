using MonoMicroservices.SearchDomain.IServices;
using SearchServiceType = MonoMicroservices.SearchService.SearchService;

namespace MonoMicroservices.SearchServiceTests;
[TestFixture]
public class SearchServiceTests
{
	private ISearchService _searchService;

	[SetUp]
	public void SetUp()
	{
		_searchService = new SearchServiceType();
	}
}
