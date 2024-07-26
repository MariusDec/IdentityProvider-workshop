using IdentityProvider.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite("DataSource = identityDb; Cache=Shared");
}