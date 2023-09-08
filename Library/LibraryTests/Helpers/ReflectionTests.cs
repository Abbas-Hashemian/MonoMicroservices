using MonoMicroservices.Library.Helpers;
using MonoMicroservices.TestUtils;

namespace MonoMicroservices.LibraryTests.Helpers;
[TestFixture]
public class ReflectionTests
{
	public enum TestEnum { A }
	[TestCaseGeneric(nameof(TestEnum.A), false, TestEnum.A, TypeArguments = new[] { typeof(TestEnum) })]
	[TestCaseGeneric("UnavailableEnum", true, null, TypeArguments = new[] { typeof(TestEnum) })]
	public void ToEnumTest<TEnum>(string enumAssociatedString, bool throws, TEnum? expectedResult)
	{
		if (throws)
			Assert.Throws<InvalidOperationException>(() => ReflectionHelper.ToEnum<TEnum>(enumAssociatedString));
		else
			Assert.That(ReflectionHelper.ToEnum<TEnum>(enumAssociatedString), Is.EqualTo(expectedResult));
	}
	//[TestCase]
	//public void a()
	//{
	//	var x = Type.GetType(typeof(IIocService).FullName);
	//}
}
