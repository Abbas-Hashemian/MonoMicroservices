using MonoMicroservices.Library.DataAccess.Bases;
using MonoMicroservices.SearchDomain.DataModels;
using System.ComponentModel.Composition;

namespace MonoMicroservices.SearchDataAccess.Repositories
{
  [Export(typeof(IWebPageRepository))]
	internal class WebPageRepository : RepositoryBase<SearchDbContext>, IWebPageRepository
	{
		public WebPageRepository(SearchDbContext context) : base(context)
		{
		}

		//public IQueryable<WebPage> WebPages => Context.WebPages;
	}
}
