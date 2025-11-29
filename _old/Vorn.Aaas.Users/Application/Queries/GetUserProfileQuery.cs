using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetUserProfileQuery(string UserId) : IRequest<Dictionary<string, string>>;

public class GetUserProfileQueryValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileQueryValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }
}

internal class GetUserProfileQueryHandler(UserManager<User> userManager) : IRequestHandler<GetUserProfileQuery, Dictionary<string, string>>
{
    public async Task<Dictionary<string, string>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId) ?? throw new InvalidDataException("User ID is invalid");
        return user.ProfilePreferences;
    }
}
