using Microsoft.AspNetCore.DataProtection;
using Vitarish.Accounts.Infrastructure.Services;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class CipherServiceTests
{
    [Fact]
    public void CipherService_EncryptThenDecrypt_RoundTrips()
    {
        IDataProtectionProvider provider = DataProtectionProvider.Create("cipher-tests");
        CipherService sut = new(provider);

        string cipher = sut.Encrypt("secret");
        string plain = sut.Decrypt(cipher);

        plain.Should().Be("secret");
    }

    [Fact]
    public void CipherService_Encrypt_NullInput_ThrowsOrReturnsEmpty()
    {
        IDataProtectionProvider provider = DataProtectionProvider.Create("cipher-tests");
        CipherService sut = new(provider);

        Action act = () => sut.Encrypt(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CipherService_Decrypt_InvalidPayload_Throws()
    {
        IDataProtectionProvider provider = DataProtectionProvider.Create("cipher-tests");
        CipherService sut = new(provider);

        Action act = () => sut.Decrypt("invalid");

        act.Should().Throw<Exception>();
    }
}
