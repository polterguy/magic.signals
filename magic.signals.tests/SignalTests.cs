/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.signals.tests
{
    /*
     * Unit tests for signals and slots implementation.
     */
    public class SignalTests
    {
        [Fact]
        public void Signal()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Creating some arguments for our signal.
            var input = new Node();
            input.Add(new Node("bar", "Jo!"));

            // Signaling the 'foo.bar' slot with the given arguments.
            signaler.Signal("foo.bar", input);

            // Asserts.
            Assert.Equal("Jo!Yup!", input.Children.First().Get<string>());
        }

        [Fact]
        public void SignalNoExisting_Throws()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Assuming this one will choke, since there are no 'foo.bar-XXX' slots registered.
            Assert.Throws<ApplicationException>(() => signaler.Signal("foo.bar-XXX", new Node()));
        }

        [Fact]
        public void StackTest_01()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Pushing some string unto our stack.
            signaler.Push("its-name", "its-value");

            // Asserts.
            Assert.Equal("its-value", signaler.Peek<string>("its-name"));
        }

        [Fact]
        public void StackTest_02_Throws()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Asserts.
            Assert.Throws<ArgumentException>(() => signaler.Peek<string>("its-name"));
        }

        [Fact]
        public void StackTest_03_Throws()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Pushing some string unto our stack.
            signaler.Push("its-name", "its-value");

            // Popping it off again.
            signaler.Pop();

            // Asserts.
            Assert.Throws<ArgumentException>(() => signaler.Peek<string>("its-name"));
        }

        [Fact]
        public void StackTest_04()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Pushing some string unto our stack.
            signaler.Push("value", "hello world");

            var result = new Node();
            signaler.Signal("stack.test", result);

            // Asserts.
            Assert.Equal("hello world", result.Value);
        }

        #region [ -- Private helper methods -- ]

        /*
         * Helper method to wire up and create our IServiceProvider correctly.
         */
        IServiceProvider Initialize()
        {
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            services.AddTransient<IConfiguration>((svc) => configuration);
            var type = typeof(ISlot);
            var slots = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) &&
                    !p.IsInterface &&
                    !p.IsAbstract &&
                    p.CustomAttributes.Any(x => x.AttributeType == typeof(SlotAttribute)));
            foreach (var idx in slots)
            {
                services.AddTransient(idx);
            }
            services.AddSingleton<ISignalsProvider>((svc) => new services.SignalsProvider(slots));
            services.AddTransient<ISignaler, services.Signaler>();
            var provider = services.BuildServiceProvider();
            return provider;
        }

        #endregion
    }
}
