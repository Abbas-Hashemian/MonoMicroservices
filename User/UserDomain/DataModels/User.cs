using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonoMicroservices.UserDomain.DataModels;

[Table("Users")]
public class User : IdentityUser<int>
{
	/// <inheritdoc/>
	[ProtectedPersonalData, Column("Username")]
	public override string UserName { get; set; }

	/// <inheritdoc/>
	[Column("NormalizedUsername")]
	public override string NormalizedUserName { get; set; }

	//[Key]
	//public int Id { get; set; }


	//[Required]
	//public string Username { get; set; }
	//[Required]
	//public string NormalizedUsername { get; set; }


	//[Required]
	//public string Email { get; set; }
	//[Required]
	//public string NormalizedEmail { get; set; }
	//[Required]
	//public bool EmailConfirmed { get; set; }


	//[Required]
	//public string PasswordHash { get; set; }
	///// <summary>
	///// A random value that must change whenever a users credentials change (password changed, login removed)
	///// </summary>
	//public string SecurityStamp { get; set; }
	///// <summary>
	///// A random value that must change whenever a user is persisted to the store
	///// </summary>
	//public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
	//public int AccessFailedCount { get; set; }


	//public string PhoneNumber { get; set; }
	//public bool PhoneNumberConfirmed { get; set; }
	//public bool TwoFactorEnabled { get; set; }



	//[Required]
	//public UserStatus Status { get; set; }
	//public DateTimeOffset? LockoutEnd { get; set; }
	//public bool LockoutEnabled { get; set; }

}
