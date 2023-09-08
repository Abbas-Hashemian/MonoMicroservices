using MonoMicroservices.Library.DataAccess.Bases;
using MonoMicroservices.UserDomain.DataModels;
using System.ComponentModel.Composition;

namespace MonoMicroservices.UserDataAccess.Repositories
{
  [Export(typeof(IUserRepository))]
	internal class UserRepository : RepositoryBase<UserDbContext>, IUserRepository
	{
		public UserRepository(UserDbContext context) : base(context)
		{
		}

		public IQueryable<User> Users => Context.Users;
	}
}
