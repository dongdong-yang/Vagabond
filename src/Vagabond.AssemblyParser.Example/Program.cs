using Mono.Reflection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            PrintReferences(typeof(TestClass));

            Console.ReadLine();
        }

        static void PrintReferences(Type type)
        {
            type.GetMethods()
             .Where(method => method.GetMethodBody() != null)
             .ToList()
             .ForEach(method =>
             {
                 var calls = method.GetInstructions()
                 .Select(x => x.Operand as MethodInfo)
                 .Where(x => x != null)
                 .ToList();

                 Console.WriteLine($"============================================");
                 Console.WriteLine(method);
                 Console.WriteLine("Calls:");
                 calls.ForEach(call =>
                 {
                     Console.WriteLine($"\t{call}");

                     call.GetGenericArguments()
                     .ToList()
                     .ForEach(arg =>
                     {
                         Console.WriteLine($"\t\t{arg.FullName}");

                         PrintReferences(arg);
                     });
                 });

                 Console.WriteLine();
                 Console.WriteLine();
             });
        }

    }

    class TestClass
    {
        public async Task TestAsync()
        {
            HelloWorld.Hello<IBar>();
            await HelloWorld.Say<IFoo>();
        }

        public void Test()
        {
            HelloWorld.Say<IFoo>().RunSynchronously();
            HelloWorld.Hello<IBar>();
        }
    }

    class HelloWorld
    {
        public static async Task Say<T>() where T : IBase
        {
            await Task.Run(() => Console.WriteLine($"Hello from {typeof(T)}.")).ConfigureAwait(false);
        }

        public static void Hello<T>() where T : IBase
        {
            Console.WriteLine($"Hello from {typeof(T)}.");
        }
    }
    interface IBase
    {
        Task Hello();
    }

    interface IFoo : IBase
    {

    }

    interface IBar : IBase
    {

    }
}
