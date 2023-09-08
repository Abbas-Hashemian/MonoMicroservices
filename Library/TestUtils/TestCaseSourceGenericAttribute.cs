using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace MonoMicroservices.TestUtils;

/// <summary>
/// <inheritdoc cref="TestCaseGenericAttribute"/><br/>
/// <see cref="TestCaseSourceGenericAttribute"/> is similar but we can have a source to test the specfied types. To test different types with same or different sources repeat the attr.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TestCaseSourceGenericAttribute : TestCaseSourceAttribute, ITestBuilder
{
	public TestCaseSourceGenericAttribute(string sourceName)
			: base(sourceName)
	{
	}

	public Type[]? TypeArguments { get; set; }

	IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test? suite)
	{
		if (!method.IsGenericMethodDefinition)
			return base.BuildFrom(method, suite);

		if ((TypeArguments?.Length ?? 0) != method.GetGenericArguments().Length)
		{
			var parms = new TestCaseParameters { RunState = RunState.NotRunnable };
			parms.Properties.Set(PropertyNames.SkipReason, $"{nameof(TypeArguments)} should have {method.GetGenericArguments().Length} elements");
			return new[] { new NUnitTestCaseBuilder().BuildTestMethod(method, suite, parms) };
		}

		var genMethod = method.MakeGenericMethod(TypeArguments);
		return base.BuildFrom(genMethod, suite);
	}
}