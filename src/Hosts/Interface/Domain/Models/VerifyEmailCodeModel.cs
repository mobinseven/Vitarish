namespace Vitarish.Interface.Domain.Models
{
    public class VerifyEmailCodeModel : SendEmailCodeModel
    {
        [AuthenticationCode]
        public string Code { get; set; }
    }
}
