/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using magic.node;
using magic.common.contracts;
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

        #region [ -- Private helper methods -- ]

        /*
         * Helper method to wire up and create our IServiceProvider correctly.
         */
        IServiceProvider Initialize()
        {
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            services.AddTransient<IConfiguration>((svc) => configuration);
            foreach (var idx in InstantiateAllTypes<IConfigureServices>())
            {
                idx.Configure(services, configuration);
            }
            var provider = services.BuildServiceProvider();
            return provider;
        }

        /*
         * Helper for above, that simply loops through all types in AppDomain, and yield returns
         * each type as an instance back to caller for each type being of specified type.
         */
        static IEnumerable<T> InstantiateAllTypes<T>() where T : class
        {
            var type = typeof(T);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach (var idx in types)
            {
                yield return Activator.CreateInstance(idx) as T;
            }
        }

        #endregion
    }
}
