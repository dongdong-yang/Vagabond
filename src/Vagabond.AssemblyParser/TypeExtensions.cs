using Mono.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Vagabond.AssemblyParser
{
    public static class TypeExtensions
    {
        public static readonly BindingFlags AllBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public static IList<Type> GetTypeReferences(this MethodBase self)
        {
            try
            {
                var methods = self.GetInstructions()
                     .Select(x => x.Operand as MethodInfo)
                     .Where(x => x != null);
                var types = methods.Select(x => x.DeclaringType)
                     .Concat(methods.SelectMany(x => x.GetParameters().Select(p => p.ParameterType)))
                     .Concat(methods.SelectMany(x => x.GetGenericArguments()))
                     .Distinct()
                     .ToList();

                return types;
            }
            catch (Exception)
            {
                return new List<Type>();
            }
        }

        public static List<Type> GetTypeReferences(this Type type)
        {
            var allBindings = AllBindings;

            var methods = type.GetMethods(allBindings);
            var variables = methods
                .Select(x => x.GetMethodBody())
                .Where(x => x != null)
                .SelectMany(x => x.LocalVariables);

            var types = type.GetProperties().Select(x => x.GetType())
                .Concat(type.GetGenericArguments())
                .Concat(type.GetNestedTypes())
                .Concat(type.GetFields(allBindings).Select(x => x.FieldType))
                .Concat(type.GetMembers(allBindings).Select(x => x.DeclaringType))
                .Concat(type.GetMembers(allBindings).Select(x => x.ReflectedType))
                .Concat(methods.SelectMany(x => x.GetGenericArguments()))
                .Concat(methods.Select(x => x.ReturnType))
                .Concat(methods.SelectMany(x => x.GetTypeReferences()))
                .Concat(variables.Select(x => x.LocalType))
                .Concat(variables.Select(x => x.GetType()))
                .Distinct()
                .ToList();

            types = types.Concat(types.SelectMany(x =>
                {
                    return x.GetGenericArguments()
                    .Concat(x.IsGenericParameter ? x.GetGenericParameterConstraints() : new List<Type>().ToArray());
                }))
                .Where(x => !x.IsGenericType)
                .Distinct()
                .ToList();

            return types;

        }

    }
}
