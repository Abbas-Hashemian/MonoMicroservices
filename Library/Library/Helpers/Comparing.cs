namespace MonoMicroservices.Library.Helpers;
public static class Comparing
{
	public static bool EqualsAny<T>(this T obj, params T[] comparisonTargets) =>
		comparisonTargets.Any(t => EqualityComparer<T>.Default.Equals(t, obj));

	public static bool NotEqualsAll<T>(this T obj, params T[] comparisonTargets) =>
		comparisonTargets.All(t => !EqualityComparer<T>.Default.Equals(t, obj));
}
