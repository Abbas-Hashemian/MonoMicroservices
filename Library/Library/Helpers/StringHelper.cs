using System.Text.Json;

namespace MonoMicroservices.Library.Helpers;
public static class StringHelper
{
	/// <summary>
	/// Negative input in place of length works as negative index. "asd".Substring2(1, -1) == "s"
	/// </summary>
	/// <param name="endIndexOrLength">0 means to the end : "asd".Substring2(1, 0) == "sd"</param>
	public static string Substring2(this string mainString, int startIndex, int endIndexOrLength) =>
		mainString.Substring(startIndex, endIndexOrLength switch { < 1 => mainString.Length - startIndex + endIndexOrLength, _ => endIndexOrLength });
	/// <summary>
	/// For {"x":"str","y":{"a":"..."}} returns a Dictionary like this { "x":"str", "y:a":"..." }
	/// </summary>
	public static Dictionary<string, object?> GetJson2ndLevelFlatDictionary(string json)
	{
		var result = new Dictionary<string, object?>();
		var items = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
		if (items != null)
			foreach (var item in items)
			{
				try
				{
					var subItems = JsonSerializer.Deserialize<Dictionary<string, object?>>(item.Value?.ToString() ?? "");
					if (subItems != null)
						foreach (var subItem in subItems)
							result.Add($"{item.Key}:{subItem.Key}", subItem.Value);

				}
				catch
				{
					result.Add(item.Key, item.Value);
				}
			}
		return result;
	}

}
