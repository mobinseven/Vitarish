namespace Vitarish.Sea.Api;

public static class SeaContractApplicationMapper
{
    public static T PassThrough<T>(T value) => value;
}
