using MonoMicroservices.UserDomain.IServices;
using UserServiceType = MonoMicroservices.UserService.UserService;

namespace MonoMicroservices.UserServiceTests;
[TestFixture]
public class UserServiceTests
{
	private IUserService _userService;

	[SetUp]
	public void SetUp()
	{
		_userService = new UserServiceType();
	}
}
