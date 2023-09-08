using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonoMicroservices.UserDomain.DataModels;

namespace MonoMicroservices.UserDataAccess;

public class UserDbContext : IdentityDbContext<User, UserRole, int>
{
	public UserDbContext(DbContextOptions<UserDbContext> options)
		: base(options) { }
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);
	}
	protected override void OnModelCreating(ModelBuilder builder) {
		base.OnModelCreating(builder);
		//builder.Entity
	}
}

