using MonoMicroservices.Library.DataAccess.Bases;

namespace MonoMicroservices.SearchDataAccess;
public class DesignTimeDbContextFactory : DesignTimeDbContextFactoryBase<SearchDbContext>
{
	public override SearchDbContext CreateDbContext(string[] args) =>
		CreateDbContext(args, DbConfig.ConnectionStringConfigName, options => new SearchDbContext(options));
}
