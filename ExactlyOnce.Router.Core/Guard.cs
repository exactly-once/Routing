﻿using System;
using System.Linq;
using System.Reflection;
using NServiceBus.Raw.Properties;

namespace ExactlyOnce.Router.Core
{
    static class Guard
    {
// ReSharper disable UnusedParameter.Global
        public static void TypeHasDefaultConstructor(Type type, [InvokerParameterName] string argumentName)
        {
            if (type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .All(ctor => ctor.GetParameters().Length != 0))
            {
                var error = $"Type '{type.FullName}' must have a default constructor.";
                throw new ArgumentException(error, argumentName);
            }
        }

        [ContractAnnotation("value: null => halt")]
        public static void AgainstNull([InvokerParameterName] string argumentName, [NotNull] object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        [ContractAnnotation("value: null => halt")]
        public static void AgainstNullAndEmpty([InvokerParameterName] string argumentName, [NotNull] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        public static void AgainstNegativeAndZero([InvokerParameterName] string argumentName, int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        public static void AgainstNegative([InvokerParameterName] string argumentName, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

    }
}