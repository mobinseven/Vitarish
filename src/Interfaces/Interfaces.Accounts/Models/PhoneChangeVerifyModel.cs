using Vitarish.Interfaces.Accounts.Attributes;

namespace Vitarish.Interfaces.Accounts.Models;

public sealed class PhoneChangeVerifyModel : VerifyCodeModel
{
    [PhoneNumber]
    public string PhoneNumber { get; set; } = string.Empty;
}
