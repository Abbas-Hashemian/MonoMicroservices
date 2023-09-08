using MonoMicroservices.Library.Helpers;
using System.Reflection;

namespace MonoMicroservices.LibraryTests.Helpers;
[TestFixture]
public class Reflection_GenerateMethodParamsTypeTests
{
	public enum GenerateDynamicTypeFromMethodParamsEnum { X, Y, Z }
	public record GenerateDynamicTypeFromMethodParamsDto { public int Number { get; set; } }
	private void GenerateMethodParamsTypeMethod(int num, string str, GenerateDynamicTypeFromMethodParamsEnum enumVar, GenerateDynamicTypeFromMethodParamsDto dto) { }

	[TestCase]
	public void GenerateMethodParamsTypeTest()
	{
		Type generatedType = ReflectionHelper.GenerateMethodParamsType(GetType().GetMethod(nameof(GenerateMethodParamsTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance));
		Assert.AreEqual(generatedType.FullName, $"{typeof(Reflection_GenerateMethodParamsTypeTests).FullName.Replace("+", ".")}.{nameof(GenerateMethodParamsTypeMethod)}_ParamsDto");
		dynamic instance = Activator.CreateInstance(generatedType);
		Assert.DoesNotThrow(() =>
		{
			instance.num = 9;
			instance.str = "123";
			instance.enumVar = GenerateDynamicTypeFromMethodParamsEnum.Y;
			instance.dto = new GenerateDynamicTypeFromMethodParamsDto { Number = 99 };
		});
		Assert.That(instance.num == 9);
		Assert.That(instance.str == "123");
		Assert.That(instance.enumVar == GenerateDynamicTypeFromMethodParamsEnum.Y);
		Assert.That(instance.dto == new GenerateDynamicTypeFromMethodParamsDto { Number = 99 });
	}
}
