using Vorn.Aaas.Interface.Domain.Attributes;

namespace Vorn.Aaas.Interface.Domain.Models
{
    public class VerifyEmailCodeModel : SendEmailCodeModel
    {
        [AuthenticationCode]
        public string Code { get; set; }
    }
}
