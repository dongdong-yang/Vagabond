//
// Disassembler.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2009 - 2010 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Mono.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Mono.Reflection
{

    public static class Extensions
    {

        public static IList<Instruction> GetInstructions(this MethodBase self)
        {
            if (self == null)
                throw new ArgumentNullException("self");

            return MethodBodyReader.GetInstructions(self).AsReadOnly();
        }

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
