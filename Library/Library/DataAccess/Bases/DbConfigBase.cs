using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MonoMicroservices.Library.DataAccess.Bases;
/// <summary>
/// To be implemented by Domain-based DAL projects.</br>
/// Keep it transient (one-time use)
/// </summary>
public abstract class DbConfigBase
{

  public IServiceCollection DefaultAddDbContext<TDbContext>(IServiceCollection services, IConfiguration config, string connectionStringConfigName)
    where TDbContext : DbContext
    => services.AddDbContext<TDbContext>(
      //Note! This opts.UseSqlServer can be done in "override OnConfiguring" virtual method of DbContext base in YourAppDbContext
      opts => opts.UseSqlServer(config.GetConnectionString(connectionStringConfigName))
    );

  /// <summary>
  /// Use <see cref="DefaultAddDbContext{TDbContext}(IServiceCollection, IConfiguration, string)"/> or write your own implementation
  /// </summary>
  public abstract void AddDbContext(IServiceCollection services, IConfiguration config);
}
