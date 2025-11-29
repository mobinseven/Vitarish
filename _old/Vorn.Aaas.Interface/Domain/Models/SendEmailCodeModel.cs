using System.ComponentModel.DataAnnotations;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Interface.Domain.Models
{
    public class SendEmailCodeModel
    {
        public User User { get; set; }
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
