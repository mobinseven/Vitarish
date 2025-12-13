using Vitarish.Accounts.Domain.Attributes;

namespace Vitarish.Accounts.Domain.Models;

public sealed class PhoneChangeVerifyModel : VerifyCodeModel
{
    [PhoneNumber]
    public string PhoneNumber { get; set; } = string.Empty;
}
