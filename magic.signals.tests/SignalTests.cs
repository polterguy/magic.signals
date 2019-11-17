/*
 * Magic, Copyright(c) Thomas Hansen 2019, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
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
        public void SignalInputReturn()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Creating some arguments for our signal.
            var input = new Node("", "hello ");

            // Signaling the 'foo.bar' slot with the given arguments.
            signaler.Signal("foo.bar", input);

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
            Assert.Throws<ApplicationException>(() => signaler.Signal("foo.bar-XXX", new Node()));
        }

        [Fact]
        public void StackTest()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Pushing some string unto our stack.
            var result = new Node();
            signaler.Scope("value", "hello world", () => signaler.Signal("stack.test", result));

            // Asserts.
            Assert.Equal("hello world", result.Value);
        }

        [Fact]
        public async Task AsyncSignal()
        {
            // Creating our IServiceProvider, and retrieving our ISignaler.
            var kernel = Initialize();
            var signaler = kernel.GetService(typeof(ISignaler)) as ISignaler;

            // Pushing some string unto our stack.
            var result = new Node("", "hello ");
            await signaler.SignalAsync("foo.bar.async", result);

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
                .Where(x => !x.IsDynamic && !x.FullName.StartsWith("Microsoft", StringComparison.InvariantCulture))
                .SelectMany(s => s.GetTypes())
                .Where(p => (typeof(ISlot).IsAssignableFrom(p) || typeof(ISlotAsync).IsAssignableFrom(p)) &&
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
