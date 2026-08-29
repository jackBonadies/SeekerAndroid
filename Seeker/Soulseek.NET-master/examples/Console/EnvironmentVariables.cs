namespace Console
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public static class EnvironmentVariables
    {
        /// <summary>
        ///     Gets the DeclaringType of the first method on the stack whose name matches the specified <paramref name="caller"/>.
        /// </summary>
        /// <param name="caller">The name of the calling method for which the DeclaringType is to be fetched.</param>
        /// <returns>The DeclaringType of the first method on the stack whose name matches the specified <paramref name="caller"/>.</returns>
        internal static Type GetCallingType(string caller)
        {
            var callingMethod = new StackTrace().GetFrames()
                .Select(f => f.GetMethod())
                .FirstOrDefault(m => m.Name == caller);

            if (callingMethod == default)
            {
                throw new InvalidOperationException($"Unable to determine the containing type of the calling method '{caller}'.  Explicitly specify the originating Type.");
            }

            return callingMethod.DeclaringType;
        }

        public static void Populate(Type type)
        {
            foreach (var envar in GetEnvironmentVariableProperties(type))
            {
                var value = ChangeType(Environment.GetEnvironmentVariable(envar.Key), envar.Key, envar.Value.PropertyType);
                envar.Value.SetValue(null, value);
            }
        }

        public static void Populate([CallerMemberName] string caller = default)
        {
            Populate(GetCallingType(caller));
        }

        private static object ChangeType(object value, string name, Type toType)
        {
            try
            {
                if (toType.IsEnum)
                {
                    return Enum.Parse(toType, (string)value, true);
                }

                return Convert.ChangeType(value, toType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException || ex is ArgumentNullException)
            {
                string message = $"Specified value '{value}' for environment variable '{name}' (expected type: {toType}).  ";
                message += "See inner exception for details.";

                throw new ArgumentException(message, ex);
            }
        }

        private static Dictionary<string, PropertyInfo> GetEnvironmentVariableProperties(Type type)
        {
            Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            {
                CustomAttributeData attribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == typeof(EnvironmentVariableAttribute).Name);

                if (attribute != default(CustomAttributeData))
                {
                    string name = (string)attribute.ConstructorArguments[0].Value;

                    if (!properties.ContainsKey(name))
                    {
                        properties.Add(name, property);
                    }
                }
            }
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

            return properties;
        }
    }

    public class EnvironmentVariableAttribute : Attribute
    {
        public EnvironmentVariableAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
