using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoMicroservices.Library.DataAccess.Bases;
using MonoMicroservices.Library.Helpers;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;

namespace MonoMicroservices.Library.DataAccess;
[Export(typeof(IDbConnectionConfigService))]
internal class DbConnectionConfigService : IDbConnectionConfigService
{
	private readonly IEnumerable<DbConfigBase> _dbConfigBase;

	public DbConnectionConfigService(IEnumerable<DbConfigBase> dbConfigBase)
	{
		_dbConfigBase = dbConfigBase;
	}

	/// <inheritdoc/>
	public IDbConnectionConfigService SetDbConnectionsUserSecretsForDevTime(IConfiguration configs)
	{
		foreach (var connectionString in configs.GetSection("ConnectionStrings").GetChildren())
		{
			connectionString.Value = Regex.Replace(
				connectionString.Value,
				@"(\{\w+(\:\w+)*\})",
				delegate (Match match) { return match.Value[0] == '{' ? configs[match.Value.Substring2(1, -1)] : match.Value; }
			);
		}
		return this;
	}

	/// <inheritdoc/>
	public IDbConnectionConfigService AddDbContexts(IServiceCollection services, IConfiguration configs)
	{
		foreach (var dbSetup in _dbConfigBase)
		{
			if (dbSetup == null)
				throw new Exception("Resolving an IDbContextSetup resulted in a Null reference, does the implementation have an internal/private modifier?");
			dbSetup.AddDbContext(services, configs);
		}
		return this;
	}
}
