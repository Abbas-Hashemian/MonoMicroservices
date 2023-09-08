using MonoMicroservices.Library.DataAccess.Bases;

namespace MonoMicroservices.UserDataAccess;
public class DesignTimeDbContextFactory : DesignTimeDbContextFactoryBase<UserDbContext>
{
	public override UserDbContext CreateDbContext(string[] args) =>
		CreateDbContext(args, DbConfig.ConnectionStringConfigName, options => new UserDbContext(options));
}
