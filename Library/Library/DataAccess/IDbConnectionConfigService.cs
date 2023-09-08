using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MonoMicroservices.Library.DataAccess;
public interface IDbConnectionConfigService
{
  /// <summary>
  /// Sets the development phase db credentials based on "%APPDATA%\Microsoft\UserSecrets" or "~/.microsoft/usersecrets" for linux<br/>
  /// To add your db credentials run these commands in UI project shell :
  /// <code>
  /// dotnet tool install --global dotnet-user-secrets /*in case of trouble install global tools package first or check the global.json and change the version to what err msg says*/
  /// /*then set your db credentials this way : */
  /// dotnet user-secrets set "Db:Id" "..."
  /// dotnet user-secrets set "Db:Password" "..."
  /// </code>
  /// </summary>
  IDbConnectionConfigService SetDbConnectionsUserSecretsForDevTime(IConfiguration configs);

  /// <summary>
  /// Registers the DbContext of all DAL projs
  /// </summary>
  IDbConnectionConfigService AddDbContexts(IServiceCollection services, IConfiguration configs);
}
