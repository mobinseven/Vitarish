using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Domain.Models;

public class UserDto(User user)
{
    public string Id { get; set; } = user.Id;
    public string? UserName { get; set; } = user.UserName;
    public string? FirstName { get; set; } = user.FirstName;
    public string? LastName { get; set; } = user.LastName;
    public string? Email { get; set; } = user.Email;
    public string? PhoneNumber { get; set; } = user.PhoneNumber;
    public string? IdentificationCode { get; set; } = user.IdentificationCode;
    public DateOnly? DateOfBirth { get; set; } = user.DateOfBirth;
    public string? BirthCountryCode { get; set; } = user.BirthCountryCode;
    public string? BirthCity { get; set; } = user.BirthCity;
    public string FullName => $"{FirstName} {LastName}";
}
