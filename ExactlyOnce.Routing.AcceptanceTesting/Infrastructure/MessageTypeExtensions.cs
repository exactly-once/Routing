using System;

public static class MessageTypeExtensions
{
    public static string ToHandlerTypeName(this Type handlerType)
    {
        return $"{handlerType.FullName}, {handlerType.Assembly.GetName().Name}";
    }
}