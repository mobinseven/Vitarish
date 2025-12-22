using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Domain.Models;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string FullName => $"{FirstName} {LastName}";

    public UserDto(User user)
    {
        Id = user.Id;
        UserName = user.UserName;
        FirstName = user.FirstName;
        LastName = user.LastName;
        Email = user.Email;
        PhoneNumber = user.PhoneNumber;
    }
}
