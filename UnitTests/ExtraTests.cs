using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    [Ignore]
    public class ExtraTests
    {
        [TestMethod]
        public async Task TestingPairOfSingletonPaternImplementations()
        {
            Console.WriteLine($"Started on: {DateTime.Now:T}");
            await Task.Delay(3000);
            var inst = Singleton2.Inst;
            inst.Hello();
        }
    }

    internal class Singleton1
    {
        private Singleton1()
        {
            Console.WriteLine($"Created on: {DateTime.Now:T}");
        }

        public void Hello()
        {
            Console.WriteLine($"Hello it's {DateTime.Now:T}");
        }

        public static Singleton1 Inst { get { return StaticHelper.inst; } }

        private class StaticHelper
        {
            static StaticHelper() { }

            internal static readonly Singleton1 inst = new Singleton1();
        }
    }

    internal class Singleton2
    {
        private Singleton2()
        {
            Console.WriteLine($"Created on: {DateTime.Now:T}");
        }

        public void Hello()
        {
            Console.WriteLine($"Hello it's {DateTime.Now:T}");
        }

        private static volatile Singleton2 _inst;
        private static readonly object _instLock = new object();

        public static Singleton2 Inst
        {
            get
            {
                if (_inst != null) return _inst;
                lock (_instLock)
                    return _inst ?? (_inst = new Singleton2());
            }
        }
    }
}
