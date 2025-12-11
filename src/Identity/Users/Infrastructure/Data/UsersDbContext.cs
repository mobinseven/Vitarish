
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Data;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : IdentityDbContext<User>(options);
