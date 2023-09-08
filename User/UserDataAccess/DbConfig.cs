using DryIocAttributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoMicroservices.Library.DataAccess.Bases;
using MonoMicroservices.UserDomain.DataModels;
using System.ComponentModel.Composition;

namespace MonoMicroservices.UserDataAccess;
[Export(typeof(DbConfigBase)), TransientReuse]
public class DbConfig : DbConfigBase
{
	internal const string ConnectionStringConfigName = "UserDbConnection";
	public override void AddDbContext(IServiceCollection services, IConfiguration config)
		=> DefaultAddDbContext<UserDbContext>(services, config, ConnectionStringConfigName)
				.AddIdentity<User, UserRole>()
				.AddEntityFrameworkStores<UserDbContext>();
}

