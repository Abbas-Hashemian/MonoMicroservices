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
using DryIoc.ImTools;
using MonoMicroservices.Library.Helpers.Attributes;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace MonoMicroservices.Library.Helpers;
public class InterfaceImplementor
{
	public const string SyncHandlerFieldName = "__syncHandler";
	public const string SyncVoidHandlerFieldName = "__syncVoidHandler";
	public const string AsyncHandlerFieldName = "__asyncHandler";
	public const string AsyncVoidTaskHandlerFieldName = "__asyncVoidTaskHandler";
	public const string AsyncVoidHandlerFieldName = "__asyncVoidHandler";
	public static ConcurrentDictionary<string, object> _implementationLock = new ConcurrentDictionary<string, object>();

	public const string MethodInfoExtractorFieldName = "__methodInfoExtractor";

	public class Handlers
	{
		public Func<dynamic, MethodInfo, IEnumerable<object?>, object?>? SyncHandler = null;
		public Action<dynamic, MethodInfo, IEnumerable<object?>>? SyncVoidHandler = null;
		public Func<dynamic, MethodInfo, IEnumerable<object?>, Task<object?>>? AsyncHandler = null;
		public Func<dynamic, MethodInfo, IEnumerable<object?>, Task>? AsyncVoidTaskHandler = null;
		public Action<dynamic, MethodInfo, IEnumerable<object?>>? AsyncVoidHandler = null;
	}

	public class ExtensionDefiner
	{
		public readonly Func<TypeBuilder, object?> BuildMemberFactory;
		public readonly Action<ILGenerator, object>? Emitter;
		public readonly Action<Type>? OnAfterDynamicTypeGenerateEvent;

		/// <param name="buildMemberFactory">
		///	 <code>
		///			(TypeBuilder typeBuilder) =&gt; new[] { typeBuilder.DefineField(...), typeBuilder.DefineMethod(...) }
		///			//or (whatever you return will be used for "emitter" object param) :
		///			(TypeBuilder typeBuilder) =&gt; typeBuilder.DefineField(...) }
		///	 </code>
		/// </param>
		/// <param name="emitter">(ILGenerator.il, object theReturnValueOfBuildMemberFactory) =&gt; ...</param>
		/// <param name="onAfterDynamicTypeGenerateEvent">(dynamicallyGeneratedType) =&gt; ... /*to populate static members in case of need*/</param>
		public ExtensionDefiner(Func<TypeBuilder, object?> buildMemberFactory, Action<ILGenerator, object>? emitter = null, Action<Type>? onAfterDynamicTypeGenerateEvent = null)
		{
			BuildMemberFactory = buildMemberFactory;
			Emitter = emitter;
			OnAfterDynamicTypeGenerateEvent = onAfterDynamicTypeGenerateEvent;
		}
	}

	private enum MethodTypes { Sync, SyncVoid, AsyncGeneric, AsyncVoidTask, AsyncVoid }

#pragma warning disable 8604, 8602, 8603, 8600, 8524
	/// <summary>
	/// Uses handlers to implement the methods of targetInterfaceType (service) in a dynamic generated Type (DynamicModule/Assembly) in a thread-safe manner.<br/>
	/// Then it can be instantiated :
	/// <code>Activator.CreateInstance(dynamicallyGeneratedType)</code>
	/// </summary>
	/// <param name="targetInterfaceType">The name of genrated Type will be {targetInterfaceType.Name}_DynamicImpl at the same namespace or "namespaceForDynamicType"</param>
	/// <param name="handlers">
	/// Remember: SyncHandler will not be used to handle void methods of the interface and will raise an err up, same rule is applied to async methods with Task&lt;T&gt;, Task and async void methods each one separately.
	///	 <code>
	///		new <see cref="Handlers"/>{
	///			<see cref="Handlers.SyncHandler"/> = (
	///				MethodInfo //of the interface method which includes its attrs,
	///				IEnumerable&lt;object&gt; //list of param values
	///			) =&gt; object? //the result of service according to the result type of MethodInfo
	///		}
	///	</code>
	///	</param>
	/// <param name="parentType">Additional parent Type for dynamically generated type</param>
	/// <returns>A dynamicallyGeneratedType implementing targetInterfaceType's methods using the implementationDynamicHandler.</returns>
	internal static Type GetMethodsDynamicImplementations(
		Type targetInterfaceType,
		Handlers handlers,
		Type? parentType = null,
		string? namespaceForDynamicType = null,
		ExtensionDefiner? extensionDefiner = null
	)
	{
		var typeFullName = (namespaceForDynamicType != null ? (namespaceForDynamicType + "." + targetInterfaceType.Name) : targetInterfaceType.FullName.Replace("+", ".")) + "_DynamicImpl";
		var type = AssemblyHelper.GetSharedDynamicAssemblyModuleBuilder().GetType(typeFullName);
		if (type != null) return type;
		lock (_implementationLock.GetOrAdd(typeFullName, new object()))
		{
			type = AssemblyHelper.GetSharedDynamicAssemblyModuleBuilder().GetType(typeFullName);
			if (type != null) return type;
			type = GenerateDynamicType(typeFullName, targetInterfaceType, handlers, parentType, namespaceForDynamicType, extensionDefiner);
			_implementationLock.TryRemove(typeFullName, out var obj);
		}
		return type;
	}
	private static Type GenerateDynamicType(
		string typeFullName,
		Type targetInterfaceType,
		Handlers handlers,
		Type? parentType = null,
		string? namespaceForDynamicType = null,
		ExtensionDefiner? extensionDefiner = null
	)
	{
		var typeBuilder = AssemblyHelper.GetSharedDynamicAssemblyModuleBuilder().DefineType(
			typeFullName,
			TypeAttributes.Public,
			parent: parentType,
			interfaces: new[] { targetInterfaceType }
		);

		var methodInfos = targetInterfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

		//Func<int, IEnumerable<object?>, object?> handler =
		//	(methodHash, @params) => implementationDynamicSyncHandler(methodInfos.Single(mi => mi.GetHashCode() == methodHash), @params);
		//var handlerFieldBuilder = typeBuilder.DefineField("Handler", handler.GetType(), FieldAttributes.Private | FieldAttributes.Static);

		FieldBuilder getFieldBuilder(string fieldName, object? lambda) => lambda != null ? typeBuilder.DefineField(fieldName, lambda.GetType(), FieldAttributes.Private | FieldAttributes.Static) : null;
		var __syncHandlerFieldBuilder = getFieldBuilder(SyncHandlerFieldName, handlers.SyncHandler);
		var __syncVoidHandlerFieldBuilder = getFieldBuilder(SyncVoidHandlerFieldName, handlers.SyncVoidHandler);
		var __asyncHandlerFieldBuilder = getFieldBuilder(AsyncHandlerFieldName, handlers.AsyncHandler);
		var __asyncVoidTaskHandlerFieldBuilder = getFieldBuilder(AsyncVoidTaskHandlerFieldName, handlers.AsyncVoidTaskHandler);
		var __asyncVoidHandlerFieldBuilder = getFieldBuilder(AsyncVoidHandlerFieldName, handlers.AsyncVoidHandler);
		object? extensionBuildMemberFactoryResult = extensionDefiner?.BuildMemberFactory(typeBuilder);

		Func<int, MethodInfo> __methodInfoExtractor = (methodToken) => methodInfos.Single(mi => mi.MetadataToken == methodToken);
		var __methodInfoExtractorFieldBuilder = typeBuilder.DefineField(MethodInfoExtractorFieldName, __methodInfoExtractor.GetType(), FieldAttributes.Private | FieldAttributes.Static);

		//implement all interface methods
		foreach (var iMethodInfo in methodInfos)
		{
			var isAsync = iMethodInfo.IsAsync();
			var isVoidReturnType = iMethodInfo.ReturnType == typeof(void);
			MethodTypes methodType = !isAsync
				? (!isVoidReturnType ? MethodTypes.Sync : MethodTypes.SyncVoid)
				: (iMethodInfo.ReturnType == typeof(Task) ? MethodTypes.AsyncVoidTask : (isVoidReturnType ? MethodTypes.AsyncVoid : MethodTypes.AsyncGeneric));

			if (
				(methodType == MethodTypes.Sync && handlers.SyncHandler == null)
				|| (methodType == MethodTypes.SyncVoid && handlers.SyncVoidHandler == null)
				|| (methodType == MethodTypes.AsyncGeneric && handlers.AsyncHandler == null)
				|| (methodType == MethodTypes.AsyncVoidTask && handlers.AsyncVoidTaskHandler == null)
				|| (methodType == MethodTypes.AsyncVoid && handlers.AsyncVoidHandler == null)
			)
				throw new NotImplementedException($"Proper dynamic implementation is not provided for {(isAsync ? "async " : "")}method \"{targetInterfaceType.Namespace}.{targetInterfaceType.Name}.{iMethodInfo.Name}\".");

			if (
				methodType.EqualsAny(MethodTypes.AsyncGeneric, MethodTypes.AsyncVoidTask, MethodTypes.AsyncVoid)
				&& iMethodInfo.ReturnType != typeof(void)
				&& !typeof(Task).IsAssignableFrom(iMethodInfo.ReturnType)
			)
				throw new Exception($"Methods that are marked with {nameof(AsyncAttribute)} must have Task<T>, Task or void return type.");

			var methodBuilder = typeBuilder.DefineMethod(
				iMethodInfo.Name,
				MethodAttributes.Public | MethodAttributes.Virtual,
				iMethodInfo.ReturnType,
				parameterTypes: iMethodInfo.GetParameters().Select(p => p.ParameterType).ToArray()
			);
			ILGenerator il = methodBuilder.GetILGenerator();

			if (extensionDefiner?.Emitter != null)
				extensionDefiner?.Emitter(il, extensionBuildMemberFactoryResult);

			//Collect a list of parameters
			var listType = typeof(List<object?>);
			var paramsListLocalBuilder = il.DeclareLocal(listType);
			il.Emit(OpCodes.Newobj, listType.GetConstructor(Type.EmptyTypes));
			il.Emit(OpCodes.Stloc, paramsListLocalBuilder);
			var paramInfos = iMethodInfo.GetParameters();
			for (var paramInfoIndex = 0; paramInfoIndex < paramInfos.Count(); paramInfoIndex++)
			{
				var paramInfo = paramInfos[paramInfoIndex];
				il.Emit(OpCodes.Ldloc, paramsListLocalBuilder);
				il.Emit(OpCodes.Ldarg, paramInfoIndex + 1);
				if (paramInfo.ParameterType.IsValueType)
					il.Emit(OpCodes.Box, paramInfo.ParameterType);
				il.Emit(OpCodes.Callvirt, listType.GetMethod(nameof(List<object?>.Add), new[] { typeof(object) }));
			}

			//Dynamic handler call
			var methodName = methodType switch
			{
				MethodTypes.Sync => nameof(DynamicImplementationHandlers.SyncHandler),
				MethodTypes.SyncVoid => nameof(DynamicImplementationHandlers.SyncVoidHandler),
				MethodTypes.AsyncGeneric => nameof(DynamicImplementationHandlers.AsyncHandler),
				MethodTypes.AsyncVoidTask => nameof(DynamicImplementationHandlers.AsyncVoidTaskHandler),
				MethodTypes.AsyncVoid => nameof(DynamicImplementationHandlers.AsyncVoidHandler)
			};
			MethodInfo staticHandlerMethodInfo = typeof(DynamicImplementationHandlers).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
			if (methodType == MethodTypes.Sync)
				staticHandlerMethodInfo = staticHandlerMethodInfo.MakeGenericMethod(iMethodInfo.ReturnType);
			if (methodType == MethodTypes.AsyncGeneric)
				staticHandlerMethodInfo = staticHandlerMethodInfo.MakeGenericMethod(iMethodInfo.ReturnType.GenericTypeArguments[0]);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld,
				methodType switch
				{
					MethodTypes.Sync => __syncHandlerFieldBuilder,
					MethodTypes.SyncVoid => __syncVoidHandlerFieldBuilder,
					MethodTypes.AsyncGeneric => __asyncHandlerFieldBuilder,
					MethodTypes.AsyncVoidTask => __asyncVoidTaskHandlerFieldBuilder,
					MethodTypes.AsyncVoid => __asyncVoidHandlerFieldBuilder
				}
			);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, __methodInfoExtractorFieldBuilder);
			il.Emit(OpCodes.Ldc_I4, iMethodInfo.MetadataToken);
			il.Emit(OpCodes.Callvirt, __methodInfoExtractor.GetType().GetMethod(nameof(Func<MethodInfo>.Invoke), new[] { typeof(int) }));
			il.Emit(OpCodes.Ldloc, paramsListLocalBuilder);
			il.Emit(OpCodes.Call, staticHandlerMethodInfo);
			//if (!isVoidReturnType) //Must always be returned even for void
			il.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(methodBuilder, iMethodInfo);
		}

		var dynamicallyGeneratedType = typeBuilder.CreateType();
		void setHanlerField(string fieldName, object? lambda)
		{
			if (lambda != null)
				dynamicallyGeneratedType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, lambda);
		}
		setHanlerField(SyncHandlerFieldName, handlers.SyncHandler);
		setHanlerField(SyncVoidHandlerFieldName, handlers.SyncVoidHandler);
		setHanlerField(AsyncHandlerFieldName, handlers.AsyncHandler);
		setHanlerField(AsyncVoidTaskHandlerFieldName, handlers.AsyncVoidTaskHandler);
		setHanlerField(AsyncVoidHandlerFieldName, handlers.AsyncVoidHandler);
		dynamicallyGeneratedType.GetField(MethodInfoExtractorFieldName, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, __methodInfoExtractor);
		if (extensionDefiner?.OnAfterDynamicTypeGenerateEvent != null)
			extensionDefiner?.OnAfterDynamicTypeGenerateEvent(dynamicallyGeneratedType);
		return dynamicallyGeneratedType;
	}

	public abstract class DynamicImplementationHandlers
	{
		public static TResult SyncHandler<TResult>(object thisInstance, Func<object, MethodInfo, IEnumerable<object?>, object?> handler, MethodInfo methodInfo, IEnumerable<object?> @params)
			=> (TResult)handler(thisInstance, methodInfo, @params);
		public static void SyncVoidHandler(object thisInstance, Action<object, MethodInfo, IEnumerable<object?>> handler, MethodInfo methodInfo, IEnumerable<object?> @params)
			=> handler(thisInstance, methodInfo, @params);
		public static async Task<TResult> AsyncHandler<TResult>(object thisInstance, Func<object, MethodInfo, IEnumerable<object?>, Task<object?>> handler, MethodInfo methodInfo, IEnumerable<object?> @params)
			=> (TResult)await handler(thisInstance, methodInfo, @params);
		public static async Task AsyncVoidTaskHandler(object thisInstance, Func<object, MethodInfo, IEnumerable<object?>, Task> handler, MethodInfo methodInfo, IEnumerable<object?> @params)
			=> await handler(thisInstance, methodInfo, @params);
#pragma warning disable 1998
		public static async void AsyncVoidHandler(object thisInstance, Action<object, MethodInfo, IEnumerable<object?>> handler, MethodInfo methodInfo, IEnumerable<object?> @params)
			=> handler(thisInstance, methodInfo, @params);
#pragma warning restore 1998
	}
#pragma warning restore 8604, 8602, 8603, 8600, 8524
}
