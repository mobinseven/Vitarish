using Vitarish.Modules.Aggregator.Interface.Extensions;

namespace Vitarish.Interfaces.Accounts.Models
{
    public class VerifyEmailCodeModel : SendEmailCodeModel
    {
        [AuthenticationCode]
        public string Code
        {
            get;
            set => field = value.ConvertToEnglishNumerals();
        }
    }
}
