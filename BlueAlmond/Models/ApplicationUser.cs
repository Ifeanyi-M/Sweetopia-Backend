using Microsoft.AspNetCore.Identity;

namespace BlueAlmond.Models
{
    public class ApplicationUser: IdentityUser
    {
        public string Name { get; set; }
    }
}
