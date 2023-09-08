/*
The MIT License (MIT)

Copyright (c) 2023 Abbas Ali Hashemian

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.Helpers.Attributes;
using MonoMicroservices.Library.IoC;
using MonoMicroservices.Library.Microservices.Attributes;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace MonoMicroservices.Library.Microservices.WebApiHandler;

/// <inheritdoc/>
[Export(typeof(IDynamicWebApiHandler))]
internal class DynamicWebApiHandler : IDynamicWebApiHandler
{
	public const string ServiceKeyQueryStringParamName = "serviceKey";
	private readonly IConfigs _configs;
	private readonly IServerConnectionConfigs _serverConnectionConfigs;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IIocService _iocService;
	private readonly ILogger<DynamicWebApiHandler> _logger;
	private static readonly ConcurrentDictionary<Type, object> _generatingDynamicTypesLock = new ConcurrentDictionary<Type, object>();
	private static readonly Dictionary<Type, Type> _generatedDynamicTypes = new Dictionary<Type, Type>();

	public DynamicWebApiHandler(IServerConnectionConfigs serverConnections, IConfigs configs, IHttpClientFactory httpClientFactory, IIocService iocService, ILogger<DynamicWebApiHandler> logger)
	{
		_serverConnectionConfigs = serverConnections;
		_configs = configs;
		_httpClientFactory = httpClientFactory;
		_iocService = iocService;
		_logger = logger;
	}

	[MockName("RegisterDelegate")]
	private static Action<IIocService, Type, Func<IResolverContext, object>, object?> RegisterDelegate =
		(IIocService iocService, Type serviceType, Func<IResolverContext, object> factoryDelegate, object? serviceKey) =>
		{
			if (!iocService.Container.IsRegistered(serviceType, serviceKey))
				iocService.Container.RegisterDelegate(serviceType, factoryDelegate, serviceKey: serviceKey);
		};
	/// <inheritdoc/>
	public void RegisterDynamicApiHandlers(IEnumerable<Assembly> assemblies, IServiceCollection services)
	{
		if ((_configs["IsInMicroservicesMode"] ?? "false") == "false")
			return;

		void registerDelegateByServiceKey(Type interfaceType, object? serviceKey = null)
		{
			RegisterDelegate(
				_iocService,
				interfaceType,
				context =>
				{
					if (!_generatedDynamicTypes.ContainsKey(interfaceType))
						lock (_generatingDynamicTypesLock.GetOrAdd(interfaceType, new object()))
						{
							if (!_generatedDynamicTypes.ContainsKey(interfaceType))
								_generatedDynamicTypes.Add(
									interfaceType,
									InterfaceImplementor.GetMethodsDynamicImplementations(
										interfaceType,
										new InterfaceImplementor.Handlers
										{
											SyncHandler = (thisInstance, methodInfo, @params) => ApiHandler(interfaceType, thisInstance.__ServiceKey, methodInfo, @params),
											SyncVoidHandler = (thisInstance, methodInfo, @params) => ApiHandler(interfaceType, thisInstance.__ServiceKey, methodInfo, @params),
											AsyncHandler = async (thisInstance, methodInfo, @params) => await ApiHandlerAsync(interfaceType, thisInstance.__ServiceKey, methodInfo, @params),
											AsyncVoidTaskHandler = async (thisInstance, methodInfo, @params) => await PostAsJsonAsync(interfaceType, thisInstance.__ServiceKey, methodInfo, @params),
											AsyncVoidHandler = async (thisInstance, methodInfo, @params) => await PostAsJsonAsync(interfaceType, thisInstance.__ServiceKey, methodInfo, @params)
										},
										extensionDefiner: new InterfaceImplementor.ExtensionDefiner(typeBuilder => typeBuilder.DefineField("__ServiceKey", typeof(object), FieldAttributes.Public))
									)
								);
							_generatingDynamicTypesLock.TryRemove(interfaceType, out var obj);
						}
					var type = _generatedDynamicTypes[interfaceType];
					dynamic instance = Activator.CreateInstance(type) ?? throw new Exception("Dynamic Web API counldn't be instantiated!");
					instance.__ServiceKey = serviceKey;
					return instance;
				},
				serviceKey
			);
		}

		var microservicesConnections = _configs.GetSection2ndLevelFlatDictionary("MicroservicesConnections");
		foreach (var interfaceType in assemblies.SelectMany(a => a.GetTypes().Where(t => t.IsInterface)))
		{
			var webApiAttr = interfaceType.GetCustomAttribute<WebApiServiceAttribute>();
			if (_iocService.Container.IsRegistered(interfaceType) && webApiAttr?.ConnectionGroupName == null)
				continue;
			if (webApiAttr != null)
			{
				if (webApiAttr?.ConnectionGroupName == null)
					registerDelegateByServiceKey(interfaceType);
				else
				{
					var foundTheConectionGroupConfig = false;
					foreach (var connection in microservicesConnections.Where(c => c.Key.StartsWith(webApiAttr.ConnectionGroupName + ":")))
					{
						foundTheConectionGroupConfig = true;
						registerDelegateByServiceKey(interfaceType, connection.Key.Replace(webApiAttr.ConnectionGroupName + ":", ""));
					}
					if (!foundTheConectionGroupConfig)
						throw new Exception($"The connection group config was not found : {webApiAttr.ConnectionGroupName}");
				}
			}
		}
	}

	private async Task<HttpResponseMessage> PostAsJsonAsync(Type interfaceType, object? serviceKey, MethodInfo methodInfo, IEnumerable<object?> paramsList)
	{
		var serviceAttr = interfaceType.GetCustomAttribute<WebApiServiceAttribute>();
		var endpointAttr = methodInfo.GetCustomAttribute<WebApiEndpointAttribute>();
		if ((serviceAttr?.SpecifiedMethodsOnly ?? false) && endpointAttr == null)
			throw new Exception($"In interfaces with property {nameof(WebApiServiceAttribute)}.{nameof(WebApiServiceAttribute.SpecifiedMethodsOnly)} true, api methods must have {nameof(WebApiEndpointAttribute)}.");

		string[] fullnameParts = interfaceType.FullName?.Split('.') ?? new string[] { };

		var serverBaseAddressConfigName =
			serviceAttr?.ConnectionGroupName != null
				? $"{serviceAttr?.ConnectionGroupName}:{serviceKey}"
				: fullnameParts.Count() > 1 ? fullnameParts[1] : throw new Exception("No proper namespace to extract server connection config name (Count < 2).");
		//TODO: ExplicitEndPointUrl does not have UnitTest t pass
		var endpointUrl =
			!string.IsNullOrEmpty(endpointAttr?.ExplicitEndPointUrl)
				? endpointAttr?.ExplicitEndPointUrl
				: (fullnameParts.Count() > 2
					? $"{string.Join('/', fullnameParts.Skip(1))}/{methodInfo.Name}"
					: throw new Exception("No proper namespace for dynamic WebApi service endpoint (Count < 2)."));

		var paramsMap =
			methodInfo.GetParameters()
				.Select((p, i) => (Name: p.Name ?? ""/*How can a parameter have no name?!*/, Value: paramsList.Count() > i ? paramsList.ElementAt(i) : null))
				.ToDictionary(p => p.Name, p => p.Value);
		//var postContent = new StringContent(paramsMap.Any() ? JsonSerializer.Serialize(paramsMap) : "");
		//var paramsDtoType = Reflection.GenerateMethodParamsType(methodInfo);
		var url = endpointUrl + (serviceKey != null ? $"?{ServiceKeyQueryStringParamName}={serviceKey}" : "");
		var result = await _httpClientFactory.CreateClient(serverBaseAddressConfigName).PostAsJsonAsync(url, paramsMap, new JsonSerializerOptions { PropertyNameCaseInsensitive = false });
		if (result.StatusCode != HttpStatusCode.OK)
			_logger.Log(LogLevel.Error, $"{nameof(DynamicWebApiHandler)}.{nameof(PostAsJsonAsync)} request status was not 200 (server config name:{{0}}, url:{{1}}).", serverBaseAddressConfigName, url);
		return result;
	}
	private object? ApiHandler(Type interfaceType, object? serviceKey, MethodInfo methodInfo, IEnumerable<object?> paramsList)
	{
		var response = PostAsJsonAsync(interfaceType, serviceKey, methodInfo, paramsList).Result;
		if (methodInfo.ReturnType == typeof(void))
			return null;
		return response.Content.ReadFromJsonAsync(methodInfo.ReturnType, new JsonSerializerOptions { PropertyNameCaseInsensitive = false, IncludeFields = true }).Result;
	}
	private async Task<object?> ApiHandlerAsync(Type interfaceType, object? serviceKey, MethodInfo methodInfo, IEnumerable<object?> paramsList)
	{
		var response = await PostAsJsonAsync(interfaceType, serviceKey, methodInfo, paramsList);
		return await response.Content.ReadFromJsonAsync(methodInfo.ReturnType.GenericTypeArguments[0]);
	}

	///// <summary>
	///// This app can work both as Monolith and Microservices. Domain dlls (interfaces) of all services are available in all containers.<br/>
	///// Resolving a service and calling a method results in a WebAPI callback.<br/>
	///// <see cref="WithMicroservicesApiDynamicHandlers(Rules)"/> is the client where you we use the interface(Domain layer) without implementation (BLL)
	///// </summary>
	//public static Rules WithMicroservicesApiDynamicHandlers(this Rules rules) {
	//	if ((Configs.Configurations["IsInMicroservicesMode"] ?? "false") == "false")
	//		return rules;
	//	return rules.WithDynamicRegistrationsAsFallback(
	//		Rules.AutoFallbackDynamicRegistrations(
	//			(type, obj) =>
	//			{
	//			}
	//		)
	//	);
	//}
}
