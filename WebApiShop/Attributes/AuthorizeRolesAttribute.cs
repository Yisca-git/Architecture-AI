using Microsoft.AspNetCore.Authorization;

namespace EventDressRental.Attributes
{
    /// <summary>
    /// Custom authorization attribute for role-based access control.
    /// Usage: [AuthorizeRoles("Admin")] or [AuthorizeRoles("Admin", "User")]
    /// </summary>
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }
    }
}
