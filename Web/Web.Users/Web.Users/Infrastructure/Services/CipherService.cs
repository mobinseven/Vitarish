using Microsoft.AspNetCore.DataProtection;

namespace Vitarish.Web.Users.Infrastructure.Services
{
    public class CipherService(IDataProtectionProvider dataProtectionProvider) : ICipherService
    {
        private string Key => typeof(CipherService).Assembly.FullName!;

        public string Encrypt(string input)
        {
            IDataProtector protector = dataProtectionProvider.CreateProtector(Key);
            return protector.Protect(input);
        }

        public string Decrypt(string cipherText)
        {
            IDataProtector protector = dataProtectionProvider.CreateProtector(Key);
            return protector.Unprotect(cipherText);
        }
    }

    public interface ICipherService
    {
        string Encrypt(string input);
        string Decrypt(string cipherText);
    }
}
