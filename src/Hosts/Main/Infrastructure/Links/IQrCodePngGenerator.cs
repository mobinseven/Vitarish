namespace Vitarish.Main.Infrastructure.Links;

public interface IQrCodePngGenerator
{
    byte[] Generate(string payload);
}
