namespace Vorn.Aaas.Interface.Tests.TestInfrastructure;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

internal static class ComponentTestHelper
{
    public static void SetProperty(object instance, string propertyName, object? value, Type? declaringType = null)
    {
        var property = GetProperty(instance, propertyName, declaringType);
        property.SetValue(instance, value);
    }

    public static void SetField(object instance, string fieldName, object? value, Type? declaringType = null)
    {
        var field = GetField(instance, fieldName, declaringType);
        field.SetValue(instance, value);
    }

    public static T? GetFieldValue<T>(object instance, string fieldName, Type? declaringType = null)
    {
        var field = GetField(instance, fieldName, declaringType);
        return (T?)field.GetValue(instance);
    }

    public static T? GetPropertyValue<T>(object instance, string propertyName, Type? declaringType = null)
    {
        var property = GetProperty(instance, propertyName, declaringType);
        return (T?)property.GetValue(instance);
    }

    public static async Task InvokeAsync(object instance, string methodName, Type? declaringType = null, params object?[]? parameters)
    {
        var method = GetMethod(instance, methodName, declaringType, parameters);
        var result = method.Invoke(instance, parameters);
        switch (result)
        {
            case Task task:
                await task.ConfigureAwait(false);
                break;
            case ValueTask valueTask:
                await valueTask.ConfigureAwait(false);
                break;
            case ValueTask<object?> valueTaskWithResult:
                await valueTaskWithResult.ConfigureAwait(false);
                break;
        }
    }

    public static object? Invoke(object instance, string methodName, Type? declaringType = null, params object?[]? parameters)
    {
        var method = GetMethod(instance, methodName, declaringType, parameters);
        return method.Invoke(instance, parameters);
    }

    private static MethodInfo GetMethod(object instance, string methodName, Type? declaringType, object?[]? parameters)
    {
        var parameterTypes = parameters?.Select(p => p?.GetType()).ToArray() ?? Array.Empty<Type?>();
        var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var searchType = declaringType ?? instance.GetType();
        var method = searchType
            .GetMethods(bindingFlags)
            .FirstOrDefault(m => m.Name == methodName && ParametersMatch(m.GetParameters(), parameterTypes));
        if (method is null)
        {
            throw new InvalidOperationException($"Could not find method '{methodName}' on type '{searchType}'.");
        }

        return method;
    }

    private static bool ParametersMatch(ParameterInfo[] parameters, Type?[] argumentTypes)
    {
        if (parameters.Length != argumentTypes.Length)
        {
            return false;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            var argumentType = argumentTypes[i];
            if (argumentType is null)
            {
                if (!parameterType.IsClass && !parameterType.IsInterface)
                {
                    return false;
                }

                continue;
            }

            if (!parameterType.IsAssignableFrom(argumentType))
            {
                return false;
            }
        }

        return true;
    }

    private static PropertyInfo GetProperty(object instance, string propertyName, Type? declaringType)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var searchType = declaringType ?? instance.GetType();
        var property = searchType.GetProperty(propertyName, bindingFlags);
        return property ?? throw new InvalidOperationException($"Property '{propertyName}' was not found on type '{searchType}'.");
    }

    private static FieldInfo GetField(object instance, string fieldName, Type? declaringType)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var searchType = declaringType ?? instance.GetType();
        var field = searchType.GetField(fieldName, bindingFlags);
        return field ?? throw new InvalidOperationException($"Field '{fieldName}' was not found on type '{searchType}'.");
    }
}
