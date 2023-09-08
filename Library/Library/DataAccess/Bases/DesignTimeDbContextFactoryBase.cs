using DryIoc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.IoC;

namespace MonoMicroservices.Library.DataAccess.Bases;
public abstract class DesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
	where TDbContext : DbContext
{
	public abstract TDbContext CreateDbContext(string[] args);

	public TDbContext CreateDbContext(
		string[] args,
		string connectionStringConfigName,
		Func<DbContextOptions<TDbContext>, TDbContext> dbContextInstantiation
	)
	{
		//It's design-time, no services are registered through Program.cs/Startup.cs so we can't Resolve IConfigs or any other similar one
		if (Configs.AppSettings == null)
			throw new Exception($"The {nameof(Configs)}.{nameof(Configs.AppSettings)} is null.");

		var container = new Container();
		container.Register<IIocService, IocService>();
		var iocService = container.Resolve<IIocService>();
		iocService.RegisterServicesAndGetFactory(new ServiceCollection(), container);

		iocService.Resolve<IDbConnectionConfigService>()?.SetDbConnectionsUserSecretsForDevTime(Configs.AppSettings);
		var builder = new DbContextOptionsBuilder<TDbContext>();
		builder.UseSqlServer(Configs.AppSettings.GetConnectionString(connectionStringConfigName));
		return dbContextInstantiation(builder.Options);
	}
}
