using MonoMicroservices.UserDomain.IServices;
using System.ComponentModel.Composition;

namespace MonoMicroservices.UserService;

[Export(typeof(IUserService))]
internal class UserService : IUserService
{
	public string a() => "";
}
