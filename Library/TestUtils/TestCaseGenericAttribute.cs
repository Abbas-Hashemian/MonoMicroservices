using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace MonoMicroservices.TestUtils;

/// <summary>
/// Allows to have generic-types for our TestCase method, e.g.
/// <code>
///		[TestCaseGeneric(...allParamsOfTestMethod..., TypeArguments = new[] {typeof(Type1), typeof(Type2)})]
///		public void ToEnumTest&lt;T1, T2&gt;(...allParamsOfTestMethod...){...}
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TestCaseGenericAttribute : TestCaseAttribute, ITestBuilder
{
	public TestCaseGenericAttribute(params object?[] arguments)
		: base(arguments)
	{
	}

	public Type[] TypeArguments { get; set; }

	IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test? suite)
	{
		if (!method.IsGenericMethodDefinition)
			return base.BuildFrom(method, suite);

		if (TypeArguments == null || TypeArguments.Length != method.GetGenericArguments().Length)
		{
			var parms = new TestCaseParameters { RunState = RunState.NotRunnable };
			parms.Properties.Set(PropertyNames.SkipReason, $"{nameof(TypeArguments)} should have {method.GetGenericArguments().Length} elements");
			return new[] { new NUnitTestCaseBuilder().BuildTestMethod(method, suite, parms) };
		}

		var genMethod = method.MakeGenericMethod(TypeArguments);
		return base.BuildFrom(genMethod, suite);
	}
}
