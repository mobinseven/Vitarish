namespace Vitarish.Interfaces.Accounts.Models
{
    public class SendEmailCodeModel
    {
        public required User User { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
