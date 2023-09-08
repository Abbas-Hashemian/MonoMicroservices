using DryIoc;
using Microsoft.AspNetCore.Mvc;
using MonoMicroservices.Library.DataAccess;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.IoC;
using MonoMicroservices.Library.Microservices.WebApiHandler;

namespace MonoMicroservices.SearchWebApi;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		Configs.SetEnvironmentMode(builder.Configuration["ASPNETCORE_ENVIRONMENT"]);
		Configs.SetConfigurationsIfNotSet(builder.Configuration);

		//IoC config. CAUTION! DO NOT TOUCH THIS PART
		var container = new Container();
		container.Register<IIocService, IocService>();//It must be registered to be used in other sublayer services
		var iocService = container.Resolve<IIocService>();
		builder.Host.UseServiceProviderFactory(iocService.RegisterServicesAndGetFactory(builder.Services, container, typeof(Controller)));

		//DataAccess config
		iocService.Resolve<IDbConnectionConfigService>()?//It must be from iocService to use the scoped container not root
			.SetDbConnectionsUserSecretsForDevTime(builder.Configuration)// Loads Db Id/Pass from User-Secrets for Dev env
			.AddDbContexts(builder.Services, builder.Configuration);


		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		//builder.Services.AddEndpointsApiExplorer();
		//builder.Services.AddSwaggerGen();

		//-----------------------------------------------------------
		var app = builder.Build();

		app.UseMiddleware<DynamicWebApiMiddleware>();

		// Configure the HTTP request pipeline.
		//if (app.Environment.IsDevelopment())
		//{
		//	app.UseSwagger();
		//	app.UseSwaggerUI();
		//}

		//app.UseHttpsRedirection();
		app.UseRouting();
		//app.UseAuthentication();
		//app.UseAuthorization();
		//app.MapControllers();

		app.Run();
	}
}
