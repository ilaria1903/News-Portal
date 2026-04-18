using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Lab07.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public List<Article> Articles { get; set; } = [];
}
