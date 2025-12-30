namespace Vitarish.Accounts.Domain.Models
{
    public class SendEmailCodeModel
    {
        public User User { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
