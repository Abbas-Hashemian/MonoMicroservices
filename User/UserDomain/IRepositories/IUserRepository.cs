using MonoMicroservices.UserDomain.DataModels;

namespace MonoMicroservices.UserDomain.IRepositories;

internal interface IUserRepository
{
	IQueryable<User> Users { get; }
}
