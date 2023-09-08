using MonoMicroservices.Library.Helpers.Attributes;
using System.Reflection;

namespace MonoMicroservices.Library.Helpers;
public static partial class ReflectionHelper
{
	public static TEnum ToEnum<TEnum>(this string enumAssociatedString)
		=> ((TEnum[])typeof(TEnum).GetEnumValues()).First(e => (e?.ToString() ?? "") == enumAssociatedString);
	public static bool IsAsync(this MethodInfo methodInfo) => typeof(Task).IsAssignableFrom(methodInfo.ReturnType) || methodInfo.GetCustomAttribute<AsyncAttribute>() != null;
}
