using System.Text.Json.Serialization;
using MonoMicroservices.Library.Enums;

namespace MonoMicroservices.Library.Helpers;
public static class ServiceResult
{
  public static ServiceResult<TResult> Ok<TResult>(TResult value)
    => new ServiceResult<TResult>(value);
  public static ServiceResult<TResult> Error<TResult>(ErrorType errorType, string? errorMessage = null)
    => new ServiceResult<TResult>(errorType: errorType, errorMessage: errorMessage);

}
public class ServiceResult<TResult>
{
  public TResult? Value;
  public ErrorType ErrorType;
  public string? ErrorMessage;
  [JsonIgnore]
  public bool Success => ErrorType == ErrorType.None;
  public ServiceResult() { }
	public ServiceResult(TResult? value = default, ErrorType errorType = ErrorType.None, string? errorMessage = null)
  {
    Value = value;
    ErrorType = errorType;
    ErrorMessage = errorMessage;
  }
}
