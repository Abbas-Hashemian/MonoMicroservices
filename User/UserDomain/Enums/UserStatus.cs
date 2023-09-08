namespace MonoMicroservices.UserDomain.Enums;
public enum UserStatus
{
	Pending,
	Active,

	/// <summary>
	/// When there is no reason
	/// </summary>
	Disabled,

	Suspended,
	Blocked
}
