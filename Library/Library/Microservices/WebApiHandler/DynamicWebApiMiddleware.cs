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
using Microsoft.AspNetCore.Http;
using MonoMicroservices.Library.Helpers;
using MonoMicroservices.Library.Helpers.Attributes;
using MonoMicroservices.Library.IoC;
using MonoMicroservices.Library.Microservices.Attributes;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MonoMicroservices.Library.Microservices.WebApiHandler;
public class DynamicWebApiMiddleware
{
	private readonly RequestDelegate _next;
	private const string _validationRegex = /* language=regex */ @"^(?<projectName>\w+)(\/(?<typeNameParts>\w+(\+\w+)?))+(\/(?<methodName>\w+))$";//"+" is the sub-Types separator Namespace.ClassX+ClassY

	[MockName("HttpRequestReadFromJsonAsync")]
	private static Func<HttpContext, Type, Task<object?>> _httpRequestReadFromJsonAsync =
		async (context, paramsDtoType) => await context.Request.ReadFromJsonAsync(paramsDtoType, new JsonSerializerOptions { PropertyNameCaseInsensitive = false, IncludeFields = true });
	[MockName("HttpResponsWriteAsJsonAsync")]
	private static Func<HttpContext, object, Task> _httpResponseWriteAsJsonAsync =
		async (context, obj) => await context.Response.WriteAsJsonAsync(obj, new JsonSerializerOptions { PropertyNameCaseInsensitive = false, IncludeFields = true });
	[MockName("HttpResponsWriteAsync")]
	private static Func<HttpContext, string, Task> _httpResponseWriteAsync =
		async (context, str) => await context.Response.WriteAsync(str);

	public DynamicWebApiMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext context, IIocService iocService)
	{
		if (context.Response.HasStarted) goto Next;

		var url = context.Request.Path.ToString().Trim('/', ' ');
		//Note: The namespace root (solution main namespace) is the only part that is not included, the project name is considered as variable.
		//So : MainRootNamespace.Project.Subdivision.IService.Method -> "Project.Subdivision.IService.Method"
		var regMatches = Regex.Match(url, _validationRegex);
		if (!regMatches.Success) goto Next;
		var projectName = regMatches.Groups["projectName"].ToString();
		var typeNameParts = regMatches.Groups["typeNameParts"].Captures;
		var interfaceFullName = $"{Consts.SolutionName}.{projectName}.{string.Join(".", typeNameParts)}";
		var assemblyFullName = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == $"{Consts.SolutionName}.{projectName}").FullName;
		var methodName = regMatches.Groups["methodName"].ToString();

		var interfaceType = Type.GetType($"{interfaceFullName}, {assemblyFullName}");
		if (interfaceType == null) goto Next;
		var serviceAttr = interfaceType.GetCustomAttribute<WebApiServiceAttribute>();
		if (serviceAttr == null) throw new InvalidOperationException();
		var methodInfo = interfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
		if (methodInfo == null) throw new InvalidOperationException();
		var endpointAttr = methodInfo.GetCustomAttribute<WebApiEndpointAttribute>();
		if ((serviceAttr?.SpecifiedMethodsOnly ?? false) && endpointAttr == null) throw new InvalidOperationException();

		var serviceKey = context.Request.Query?[DynamicWebApiHandler.ServiceKeyQueryStringParamName].ToString();
		var serviceInstance = !string.IsNullOrEmpty(serviceKey) ? iocService.Resolve(interfaceType, serviceKey) : iocService.Resolve(interfaceType);
		if (serviceInstance == null) throw new NotImplementedException();

		var paramInfos = methodInfo.GetParameters();
		object[]? @params = null;
		if (paramInfos.Any())
		{
			var paramsDtoType = ReflectionHelper.GenerateMethodParamsType(methodInfo);
			var paramsDto = await _httpRequestReadFromJsonAsync(context, paramsDtoType);
			@params = (object[]?)paramInfos.Select(p => paramsDtoType.GetField(p.Name ?? "", BindingFlags.Public | BindingFlags.Instance)?.GetValue(paramsDto)).ToArray();
		}

		var rawResult = methodInfo.Invoke(serviceInstance, @params);
#pragma warning disable 8600, 8602
		object? result = rawResult;
		if(typeof(Task).IsAssignableFrom(methodInfo.ReturnType) && methodInfo.ReturnType != typeof(Task))
		{
			await ((Task)rawResult).ConfigureAwait(false);
			result = rawResult.GetType().GetProperty(nameof(Task<object>.Result)).GetValue(rawResult, null);
		}
#pragma warning restore 8600, 8602
		if (methodInfo.ReturnType == typeof(Task) /*void task*/ || methodInfo.ReturnType == typeof(void))
		{
			if (rawResult != null) await (Task)rawResult;
			await _httpResponseWriteAsync(context, "");  //intentionally empty
			return;
		}
		else
			await _httpResponseWriteAsJsonAsync(context, result ?? new { });

		return;
	Next:
		await _next(context);
	}
}
