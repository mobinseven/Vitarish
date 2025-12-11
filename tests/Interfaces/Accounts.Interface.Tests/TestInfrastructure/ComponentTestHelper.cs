using System.Reflection;

namespace Accounts.Interface.Tests.TestInfrastructure;

internal static class ComponentTestHelper
{
    public static void SetProperty(object instance, string propertyName, object? value, Type? declaringType = null)
    {
        PropertyInfo property = GetProperty(instance, propertyName, declaringType);
        property.SetValue(instance, value);
    }

    public static void SetField(object instance, string fieldName, object? value, Type? declaringType = null)
    {
        FieldInfo field = GetField(instance, fieldName, declaringType);
        field.SetValue(instance, value);
    }

    public static T? GetFieldValue<T>(object instance, string fieldName, Type? declaringType = null)
    {
        FieldInfo field = GetField(instance, fieldName, declaringType);
        return (T?)field.GetValue(instance);
    }

    public static T? GetPropertyValue<T>(object instance, string propertyName, Type? declaringType = null)
    {
        PropertyInfo property = GetProperty(instance, propertyName, declaringType);
        return (T?)property.GetValue(instance);
    }

    public static async Task InvokeAsync(object instance, string methodName, Type? declaringType = null, params object?[]? parameters)
    {
        MethodInfo method = GetMethod(instance, methodName, declaringType, parameters);
        object? result = method.Invoke(instance, parameters);
        switch (result)
        {
            case Task task:
                await task.ConfigureAwait(false);
                break;
            case ValueTask valueTask:
                await valueTask.ConfigureAwait(false);
                break;
            case ValueTask<object?> valueTaskWithResult:
                _ = await valueTaskWithResult.ConfigureAwait(false);
                break;
        }
    }

    public static object? Invoke(object instance, string methodName, Type? declaringType = null, params object?[]? parameters)
    {
        MethodInfo method = GetMethod(instance, methodName, declaringType, parameters);
        return method.Invoke(instance, parameters);
    }

    private static MethodInfo GetMethod(object instance, string methodName, Type? declaringType, object?[]? parameters)
    {
        Type?[] parameterTypes = parameters?.Select(p => p?.GetType()).ToArray() ?? Array.Empty<Type?>();
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type searchType = declaringType ?? instance.GetType();
        MethodInfo? method = searchType
            .GetMethods(bindingFlags)
            .FirstOrDefault(m => m.Name == methodName && ParametersMatch(m.GetParameters(), parameterTypes));
        return method is null
            ? throw new InvalidOperationException($"Could not find method '{methodName}' on type '{searchType}'.")
            : method;
    }

    private static bool ParametersMatch(ParameterInfo[] parameters, Type?[] argumentTypes)
    {
        if (parameters.Length != argumentTypes.Length)
        {
            return false;
        }

        for (int i = 0; i < parameters.Length; i++)
        {
            Type parameterType = parameters[i].ParameterType;
            Type? argumentType = argumentTypes[i];
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
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type searchType = declaringType ?? instance.GetType();
        PropertyInfo? property = searchType.GetProperty(propertyName, bindingFlags);
        return property ?? throw new InvalidOperationException($"Property '{propertyName}' was not found on type '{searchType}'.");
    }

    private static FieldInfo GetField(object instance, string fieldName, Type? declaringType)
    {
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type searchType = declaringType ?? instance.GetType();
        FieldInfo? field = searchType.GetField(fieldName, bindingFlags);
        return field ?? throw new InvalidOperationException($"Field '{fieldName}' was not found on type '{searchType}'.");
    }
}
