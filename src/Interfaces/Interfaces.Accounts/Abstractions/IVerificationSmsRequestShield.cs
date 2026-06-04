using Vitarish.Interfaces.Accounts.Services;

namespace Vitarish.Interfaces.Accounts.Abstractions;

public interface IVerificationSmsRequestShield
{
    VerificationSmsShieldDecision TryAcquireSendSlot(string key);

    void ReleaseSendSlot(string key);
}
