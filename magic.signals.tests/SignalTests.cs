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
using magic.signals.services;
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
            var input = new Node("foo.bar", "hello ");

            // Signaling the 'foo.bar' slot with the given arguments.
            signaler.Signal(input);

            // Asserts.
            Assert.Equal("hello world", input.Get<string>());
        }

        [Fact]
        public void SignalNoExisting_Throws()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Assuming this one will choke, since there are no 'foo.bar-XXX' slots registered.
            Assert.Throws<ApplicationException>(() => signaler.Signal(new Node("foo.bar-XXX")));
        }

        [Fact]
        public void StackTest()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Pushing some string unto our stack.
            var result = new Node("stack.test");
            signaler.Scope("value", "hello world", () => signaler.Signal(result));

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

            // Initializing slots, first by making sure we retrieve all classes implementin ISlot, and having 
            // the SlotAttribute declared as an attribute.
            var slots = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ISlot).IsAssignableFrom(p) &&
                    !p.IsInterface &&
                    !p.IsAbstract &&
                    p.CustomAttributes.Any(x => x.AttributeType == typeof(SlotAttribute)));

            // Adding each slot type as a transient service.
            foreach (var idx in slots)
            {
                services.AddTransient(idx);
            }

            // Making sure we use the default ISignalsProvider and ISignaler services.
            services.AddSingleton<ISignalsProvider>((svc) => new SignalsProvider(slots));
            services.AddTransient<ISignaler, Signaler>();

            // Building and returning service provider to caller.
            return services.BuildServiceProvider();
        }

        #endregion
    }
}
