using Vitarish.Search.Contracts.Errors;
namespace Vitarish.Search.Contracts.Results;
public sealed record SearchClientResult<T>(T? Value, SearchError? Error) { public bool IsSuccess => Error is null; public static SearchClientResult<T> Success(T value)=>new(value,null); public static SearchClientResult<T> Failed(SearchError error)=>new(default,error); }
