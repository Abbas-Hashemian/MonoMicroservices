using DryIoc;
using Microsoft.AspNetCore.Mvc;
using MonoMicroservices.Library.DataAccess;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.IoC;

namespace MonoMicroservices.UI;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		//var builder = WebApplication.CreateBuilder(
		//	new WebApplicationOptions
		//	{
		//		Args = args,
		//		//ContentRootPath = Directory.GetCurrentDirectory(),
		//	}
		//);

		Configs.SetEnvironmentMode(builder.Configuration["ASPNETCORE_ENVIRONMENT"]);
		Configs.SetConfigurationsIfNotSet(builder.Configuration);

		//IoC config. CAUTION! DO NOT TOUCH THIS PART
		var container = new Container();
		container.Register<IIocService, IocService>();//It must be registered to be used in other sublayer services
		var iocService = container.Resolve<IIocService>();
		builder.Host.UseServiceProviderFactory(iocService.RegisterServicesAndGetFactory(builder.Services, container, typeof(Controller)));

		builder.Services
			.AddMvc(options => options.EnableEndpointRouting = false)
			.AddControllersAsServices();

		//DataAccess config
		iocService.Resolve<IDbConnectionConfigService>()?//It must be from iocService to use the scoped container not root
			.SetDbConnectionsUserSecretsForDevTime(builder.Configuration)// Loads Db Id/Pass from User-Secrets for Dev env
			.AddDbContexts(builder.Services, builder.Configuration);

		//Auth db config
		/** All is done through <see cref="IDbConnectionConfigService.AddDbContexts"/> (some lines above) and using <see cref="Library.DataAccess.Bases.DbConfigBase"/> implementation in User DAL */

		//var authService = DI.Resolve<IAuthService>();
		//services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
		//	.AddJwtBearer(opts =>
		//		{
		//			opts.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
		//			{
		//				RequireSignedTokens = true,
		//				ValidateLifetime = true,
		//				ValidateAudience = false,
		//				ValidateIssuer = false,
		//				IssuerSigningKey = authService.GetSecurityKey()
		//			};
		//		}
		//	);

		// Logging
		builder.Logging.ClearProviders();
		builder.Logging.AddConsole();

		//-----------------------------------------------------------
		var app = builder.Build();

		if (app.Environment.IsDevelopment())
			app.UseDeveloperExceptionPage();
		else
			app.UseExceptionHandler("/Errors/Exception").UseStatusCodePages();

		app.UseMvc()
			//.UseHttpsRedirection()
			.UseStaticFiles()
			.UseRouting();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}"
			);
		});

		app.Run();
	}

	//public static IHostBuilder CreateHostBuilder(string[] args) =>
	//	Host.CreateDefaultBuilder(args)
	//		.ConfigureLogging(
	//			(hostingContext, logging) =>
	//			{
	//				logging.ClearProviders(); // removes all providers from LoggerFactory
	//				logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
	//				logging.AddDebug();
	//			}
	//		)

	//		// Plug-in the DryIoc
	//		.UseServiceProviderFactory(DI.GetPreparedIoCFactory(typeof(Controller)))

	//		.ConfigureWebHostDefaults(
	//			webBuilder => webBuilder.UseStartup<Startup>()
	//		);
}
