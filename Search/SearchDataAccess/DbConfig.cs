using DryIocAttributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoMicroservices.Library.DataAccess.Bases;
using System.ComponentModel.Composition;

namespace MonoMicroservices.SearchDataAccess;
[Export(typeof(DbConfigBase)), TransientReuse]
public class DbConfig : DbConfigBase
{
	internal const string ConnectionStringConfigName = "SearchDbConnection";
	public override void AddDbContext(IServiceCollection services, IConfiguration config)
		=> DefaultAddDbContext<SearchDbContext>(services, config, ConnectionStringConfigName);
}

