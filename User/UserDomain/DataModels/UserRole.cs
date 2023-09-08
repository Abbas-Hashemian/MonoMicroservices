using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonoMicroservices.UserDomain.DataModels;

[Table("UserRoles")]
public class UserRole : IdentityRole<int>
{
}
