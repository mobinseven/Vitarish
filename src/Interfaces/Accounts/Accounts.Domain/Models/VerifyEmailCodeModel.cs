namespace Vitarish.Accounts.Domain.Models
{
    public class VerifyEmailCodeModel : SendEmailCodeModel
    {
        [AuthenticationCode]
        public string Code { get; set; }
    }
}
