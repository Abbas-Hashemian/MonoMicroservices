# MonoMicroservices

MonoMicroservices is a simple dotnet core template that runs the application on separate containers or on single IIS server simultaneously. If it looks like a wheel reinvention, please read the disclaimer.

## Why?

- An entrepreneur starts a startup as a monolith but they know one day the project must run on containers, so if we think about it today we save a lot of money and reduce the risks.
- There are some big companies having different customers, big clients with enough budget and many users to seek more availability and scalable system in Microservices. But small customers need a single server with limited resources. And some of them don't like cloud systems. It would be good to have a code that supports both, though the old codes still need to be upgraded to a reliable Ioc system.
- Our business code will be simpler for RPC calls (just like a simple ioc).

## How?
```cs
//We have : MySolution.DomainName.MoreSpaces.IServiceInterface.Method(myparams)
private readonly IServiceInterface _service;
var result = _service.Method(myparams);
var resultAsync = await _service.MethodAsync(myparams);
//makes two post requests like this:
//http://...:port/DomainName/MoreSpaces/IServiceInterface/Method
//http://...:port/DomainName/MoreSpaces/IServiceInterface/MethodAsync
```
All method parameters and the method returned value are sent/received through json.

If we disable a single configuration env var named "IsInMicroservicesMode" the entire application falls back to the Monolithic mode (we put this config in an env var in the shared docker-compose.shared.yml so we don't need to change it).

 > We don't need to define Controllers/Actions in service WebApi, the service interface works through the dotnet Middlewares.

There is no gRPC proto, though gRPC can be added later. The contract between containers works this way:

	MySolution
	|	User	(Will be a container)
	|	|	UserDomain (Service interfaces, Dtos, DataModels, enums)
	|	|	UserService (The service implementations)
	|	|	UserDataAccess	(DAL)
	|	Search	(Will be another container)
	|	|	SearchDomain
	|	|	SearchService
	|	|	...
	|	|	AnotherDomain
	|	|	AnotherService

The domain layer of all services will be replicated to all containers, so all other services know the interface but not the implementations or DAL.

 > Note: Any new Domain must have a COPY command in the single Dockerfile which is used by docker compose. Domain projects are referenced as ProjectReference in the client projects, this handles the following processes restore/build/publish implicitly. Any new container must be added to docker-compose.yml and its override.

Only those interfaces with "WebApiServiceAttribute" behave like a WepApi and <b>by default</b> only those methods that are marked with "WebApiEndpointAttribute", though we can set "SpecifiedMethodsOnly" prop of "WebApiService" to false:
```cs
[WebApiService]
public interface ISearchService
{
	[WebApiEndpoint]
	ServiceResult<string> GetSomeString(string p);
	[WebApiEndpoint]
	ServiceResult<SomeDto> GetSomeDto();
}
//or
[WebApiService(SpecifiedMethodsOnly=false)]
public interface ISearchService
{
	ServiceResult<string> GetSomeString(string p);
	ServiceResult<SomeDto> GetSomeDto();
}

```

"MonoMicroservices.Library.Microservices.ServerConnectionConfigs" checks 2 Env vars "IsInMicroservicesMode" and the "MicroservicesConnections" defined in "docker-compose.shared.yml".<br/>
```yml
- IsInMicroservicesMode=true
- 'MicroservicesConnections={
	"SearchDomain":"http://host.docker.internal:5102",
	
	"CollectorService":{"Service1":"http://host.docker.internal:5102","Service2":"http://host.docker.internal:5102"}
}'
#"SearchDomain" is the 2nd part of interface's namespace 
#For our earlier example it is the "DomainName" : MySolution.DomainName.MoreSpaces.IServiceInterface.Method(myparams)
#We have one interface which is implemented in different projects running on different containers.
#"CollectorService" is a group name for multi-resolves. (Described in "CSharp side")
```
CSharp side:
```cs
//"Service1" and "Service2" are actually the DryIoc ServiceKeys (MEF Export ContractNames) used on top of implementations :
[Export("Service2", typeof(ICollectorService))]
internal class CollectorService2 : ICollectorService
{
	public async Task<string> GetStringAsync()
	{
		await Task.Delay(100);
		return await Task.Run(() => "Service2.GetStringAsync");
	}
	public string GetServiceKey() => GetType().GetCustomAttribute<ExportAttribute>()?.ContractName ?? "";
}
//"CollectorService" or the group name used on top of the interface :
[WebApiService(ConnectionGroupName = "CollectorService", SpecifiedMethodsOnly = false)]
public interface ICollectorService
{
	string GetServiceKey();
	Task<string> GetStringAsync();
}
```

### More details
"MonoMicroservices.Library.Helpers.InterfaceImplementor" uses ILGenerator to implement the interface (this phase runs at startup and is cached in app life cycle so it is thread-safe).<br>
The method "GetMethodsDynamicImplementations" of this class accepts lambda expressions for 5 method categories :
- Sync
- SyncVoid
- AsyncGeneric
- AsyncVoidTask
- and AsyncVoid.

"MonoMicroservices.Library.Microservices.WebApiHandler.DynamicWebApiMiddleware.cs" is added to app in "Program.cs" and interferes the web requests.
If the web request passes the regex condition and matches with a qualified service interface (marked with WebApi attributes) that has implementation on current container,
middleware extracts params json and calls the method and makes a json from the returned result to send back to client codes.

"MonoMicroservices.Library.Microservices.WebApiHandler.DynamicWebApiHandler" takes the part of handling interface method calls and make the web requests.
It registers the interfaces in DryIoc container and handles te service key part, all at Startup, thread-safe and process is started through "DependencyRegistrar" events which are called by IocService.

## What is supported
 1. Dto (record,class,...) parameters
 2. Generic type returns like ServiceResult&lt;MyDto&gt;
 3. Async generic Task&lt;MyDto&gt; methods
 4. Async void Task methods
 5. Async void methods
 6. Multiple interface implementations on different containers :<br>	
```cs
public interface IMyService {}
[Export("ContainerOneServiceKey", typeof(IMyService))]
public class MyService : IMyService {}	//in the namespace of container one
[Export("ContainerTwoServiceKey", typeof(IMyService))]
public class MyService : IMyService {} //in the namespace of container two
IocService.Resolve<IEnumerable<IMyService>>().Selecy(service=>...service.__ServiceKey...);
IocService.Resolve<IMyService>("ContainerTwoServiceKey")
//Note: in microservices mode, for multi resolves, there is a magic property in service
// named "__ServiceKey" that will not be available in monolith.
//Also in Microservices mode we don't have access to the implementation to know
// what was the "ContractName" of "ExportAttribute".
//So the best solution would be to have a method (because properties are not supported yet)
// that returns the service key.
```

### What is not supported yet
 1. Type parameters (like MyMethod&lt;ADto&gt;()), I was thinking the external published services (WebAPIs) would most likely be simple.
 2. "ref" and "out" parameters.
 3. Properties and Fields of service interface
 4. gRPC
 5. Many other things ...

### Requirements :
- .net6
- [DryIoc](https://github.com/dadhi/DryIoc)
- Tested on Docker desktop windows, and linux containers.

## Disclaimer!
 > It was a few weeks of work and as a newbie to Microservices, NUnit and dotnet core and don't think about ILGenerator,
 I was going to improve my skills, but this project may be a simple inspiration for other newcomers.
