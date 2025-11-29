namespace Vorn.Aaas.Users.Infrastructure.Data;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Users.Domain.Entities;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : IdentityDbContext<User>(options);
