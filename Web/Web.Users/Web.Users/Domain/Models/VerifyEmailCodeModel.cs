using Vitarish.Web.Users.Domain.Attributes;

namespace Vitarish.Web.Users.Domain.Models
{
    public class VerifyEmailCodeModel : SendEmailCodeModel
    {
        [AuthenticationCode]
        public string Code { get; set; }
    }
}
