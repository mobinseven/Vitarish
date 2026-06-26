using Vitarish.Sea.Contracts.Errors;

namespace Vitarish.Sea.Interface.ViewState;

public sealed record SeaLoadingState(bool IsLoading, bool IsEmpty, SeaError? Error);
