
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Infrastructure.Data;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : IdentityDbContext<User>(options);
