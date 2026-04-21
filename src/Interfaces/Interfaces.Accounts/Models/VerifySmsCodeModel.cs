using Vitarish.Interfaces.Base.Extensions;

namespace Vitarish.Interfaces.Accounts.Models;

public class VerifySmsCodeModel : SendSmsCodeModel
{
    [AuthenticationCode]
    public string Code
    {
        get;
        set => field = value.ConvertToEnglishNumerals();
    }
}
