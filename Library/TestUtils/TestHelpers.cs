using Microsoft.Extensions.Configuration;
using MonoMicroservices.Library.Helpers.Attributes;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Text;

namespace MonoMicroservices.TestUtils;
public static class TestHelpers
{
	public static IEnumerable ConvertToNamedTestCaseDataEnumerable(this Dictionary<string, object[]> namedData)
	{
		foreach (var item in namedData)
			yield return new TestCaseData(item.Value).SetArgDisplayNames(item.Key);
	}
	public static object[] ConvertToNamedTestCaseData(this Dictionary<string, object[]> namedData) =>
		namedData.ConvertToNamedTestCaseDataEnumerable().Cast<object>().ToArray();

	public enum ExceptionMsgPart { Message = 2, StackTrace = 4, InnerExceptionMessage = 8 }
	/// <summary>
	/// Verifies the expected word/phrase was mentioned in the Exception message or not.
	/// </summary>
	public static void ExpectedExceptionMessageIs(
		this Exception? exception,
		string excpectedWordsInErrMsg,
		ExceptionMsgPart includedExceptionMsgParts = ExceptionMsgPart.Message | ExceptionMsgPart.InnerExceptionMessage | ExceptionMsgPart.StackTrace
	)
	{
		if (exception == null)
			throw new Exception("No exception was happened or the exception object was null!");
		Assert.That(
			(
				((includedExceptionMsgParts & ExceptionMsgPart.Message) > 0 ? exception?.Message ?? "" : "")
				+ ((includedExceptionMsgParts & ExceptionMsgPart.InnerExceptionMessage) > 0 ? exception?.InnerException?.Message ?? "" : "")
				+ ((includedExceptionMsgParts & ExceptionMsgPart.StackTrace) > 0 ? exception?.StackTrace ?? "" : "")
			).IndexOf(excpectedWordsInErrMsg),
			Is.GreaterThan(-1),
			message: $"Expected word/phrase \"{excpectedWordsInErrMsg}\" were not found in the Exception msg."
		);
	}

	public static IConfigurationRoot DeserialzieToIConfigurationRoot(string anAppSettingsJson)
	{
		if (string.IsNullOrEmpty(anAppSettingsJson))
			throw new Exception("anAppSettingsJson for Configs setup can't be empty");
		var confBuilder = new ConfigurationBuilder();
		confBuilder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(anAppSettingsJson)));
		return confBuilder.Build();
	}

	/// <summary>
	/// await <see cref="AsyncDelayTester"/>(async () => ..., expextedDelayMilliseconds, nameof(IService.MyMethod))<br/>
	/// CAUTION! This helper makes "2" calls, async and await, to calculate the delay.
	/// </summary>
	/// <param name="whatIsTestedForDelay">This name is used for log and error messages.</param>
	public static async Task AsyncDelayTester(Func<Task> action, int expextedDelayMilliseconds, string whatIsTestedForDelay)
	{
		var startTime = DateTime.Now;
#pragma warning disable 4014
		action();
#pragma warning restore 4014
		var passedMilliseconds = (DateTime.Now - startTime).TotalMilliseconds;
		Console.Write($"{passedMilliseconds} milliseconds passed for \"{whatIsTestedForDelay}\" without await.\n");
		Assert.That(passedMilliseconds < expextedDelayMilliseconds, $"\"{whatIsTestedForDelay}\" without await is taking more than {expextedDelayMilliseconds} milliseconds.");

		startTime = DateTime.Now;
		await action();
		passedMilliseconds = (DateTime.Now - startTime).TotalMilliseconds;
		Console.Write($"{passedMilliseconds} milliseconds passed for \"{whatIsTestedForDelay}\" with await (min expectation : {expextedDelayMilliseconds}).\n");
		Assert.That(expextedDelayMilliseconds <= passedMilliseconds, $"await \"{whatIsTestedForDelay}()\" is taking less than {expextedDelayMilliseconds} milliseconds.");
	}

	public static async Task IntervalChecks(Func<bool> returnTrueToBreak, int howManyTimes, int interval)
	{
		for (int i = 0; i < howManyTimes; i++)
		{
			await Task.Delay(interval);
			if (returnTrueToBreak())
				break;
		}
	}

	/// <summary>
	/// Sets the value of a static Property or Field marked with <see cref="MockNameAttribute"/>.<br/>
	/// See also : <seealso cref="MockNameAttribute"/>
	/// </summary>
	/// <typeparam name="TargetServiceType">The static prop belongs to this service class</typeparam>
	/// <param name="propMockName">The name used in [<see cref="MockNameAttribute"/>("propMockName")] above the prop/field</param>
	public static void SetByMockName<TargetServiceType>(string mockName, object valueOrDelegate)
	{
		var fieldInfo = typeof(TargetServiceType).GetFields(BindingFlags.Static | BindingFlags.NonPublic).Single(f => f.GetCustomAttribute<MockNameAttribute>()?.MockName == mockName);
		if (fieldInfo != null)
		{
			fieldInfo.SetValue(null, valueOrDelegate);
			return;
		}
		var propInfo = typeof(TargetServiceType).GetProperties(BindingFlags.Static | BindingFlags.NonPublic).Single(f => f.GetCustomAttribute<MockNameAttribute>()?.MockName == mockName);
		if (propInfo != null)
			propInfo.SetValue(null, valueOrDelegate);
	}
}
