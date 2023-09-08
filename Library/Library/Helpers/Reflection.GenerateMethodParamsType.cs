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
using System.Collections.Concurrent;
using System.Reflection;

namespace MonoMicroservices.Library.Helpers;
public static partial class ReflectionHelper
{
	public static ConcurrentDictionary<string, object> _implementationLock = new ConcurrentDictionary<string, object>();
	/// <summary>
	/// Generates a dynamic Type of which the properties are created based on the method params. It was supposed to be used in json deserialize (but System.Text.Json.JsonDeserializer doesn't support).<br/>
	/// </summary>
	/// <param name="methodInfo">The Method has to belong to a Type (interface,class,...) becuase a Type is generated in a namespace based on the Method's class namespace.</param>
	/// <returns>A dynamic generated Type to be used as Dto.</returns>
	/// <exception cref="Exception">It throws an exception if the method does not belong to a Type or the the Type had no Fullname.</exception>
	public static Type GenerateMethodParamsType(MethodInfo methodInfo)
	{
		var declaringType = methodInfo.DeclaringType ?? throw new Exception("Method must be member of a Type.");
		if (declaringType.FullName == null) throw new Exception("The parent Type of the Method must have a Fullname.");
		var typeFullName = $"{declaringType.FullName?.Replace("+", ".")}.{methodInfo.Name}_ParamsDto";

		var type = AssemblyHelper.GetSharedDynamicAssemblyModuleBuilder().GetType(typeFullName);
		if (type != null) return type;
		lock (_implementationLock.GetOrAdd(typeFullName, new object()))
		{
			type = AssemblyHelper.GetSharedDynamicAssemblyModuleBuilder().GetType(typeFullName);
			if (type != null) return type;

			var typeBuilder = AssemblyHelper.GetSharedDynamicAssemblyModuleBuilder().DefineType(typeFullName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);
			foreach (var param in methodInfo.GetParameters())
				typeBuilder.DefineField(param.Name ?? "" /*How can be null?!*/, param.ParameterType, FieldAttributes.Public);
			type = typeBuilder.CreateType();

			_implementationLock.TryRemove(typeFullName, out var obj);
		}
#pragma warning disable 8603
		return type;
#pragma warning restore 8603
	}
}
