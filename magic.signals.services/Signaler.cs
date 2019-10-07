/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using magic.node;
using magic.signals.contracts;

namespace magic.signals.services
{
    /// <summary>
    /// Default implementation service class for the ISignaler contract/interface.
    /// </summary>
    public class Signaler : ISignaler
    {
        static readonly object _locker = new object();
        static bool? _canRaiseSignals;
        readonly IServiceProvider _provider;
        readonly ISignalsProvider _signals;
        readonly List<Tuple<string, object>> _stack = new List<Tuple<string, object>>();

        /// <summary>
        /// Creates a new instance of the default Signaler ISignaler service class.
        /// </summary>
        /// <param name="provider">Service provider to use for retrieving services.</param>
        /// <param name="signals">Implementation class to use for retrieving types from their string representations.</param>
        public Signaler(IServiceProvider provider, ISignalsProvider signals)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
        }

        #region [ -- Interface implementation -- ]

        /// <summary>
        /// Invokes the slot with the specified name,
        /// passing in the node itself as arguments to the slot.
        /// </summary>
        /// <param name="name">Name of slot to invoke.</param>
        /// <param name="input">Arguments being passed in to slot.</param>
        public void Signal(string name, Node input)
        {
            if (!CanRaiseSignals())
                throw new ApplicationException("You seem to be missing a valid licence, please obtain one at https://polterguy.github.io if you wish to continue using Magic.");

            var type = _signals.GetSlot(name) ?? throw new ApplicationException($"No slot exists for [{name}]");
            var raw = _provider.GetService(type);

            // Basic sanity checking.
            if (!(raw is ISlot instance))
                throw new ApplicationException($"The [{name}] slot is an async slot, and you tried to invoke it synchronously. Please invoke it async.");

            instance.Signal(this, input);
        }

        /// <summary>
        /// Invokes the slot with the specified name,
        /// passing in the node itself as arguments to the slot.
        /// Notice, the ISlotAsync interface must have been implemented on your type
        /// to signal it using the async Signal method.
        /// </summary>
        /// <param name="name">Name of slot to invoke.</param>
        /// <param name="input">Arguments being passed in to slot.</param>
        public Task SignalAsync(string name, Node input)
        {
            if (!CanRaiseSignals())
                throw new ApplicationException("You seem to be missing a valid licence, please obtain one at https://polterguy.github.io if you wish to continue using Magic.");

            var type = _signals.GetSlot(name) ?? throw new ApplicationException($"No slot exists for [{name}]");
            var raw = _provider.GetService(type);

            // Basic sanity checking.
            if (!(raw is ISlotAsync instance))
                throw new ApplicationException($"The [{name}] slot is not an async slot, and you tried to invoke it as such. Please invoke it synchronously.");

            return instance.SignalAsync(this, input);
        }

        /// <summary>
        /// Pushes the specified object unto the stack with the given key name,
        /// for then to evaluate the given functor. Useful for evaluating some piece of code
        /// making sure the evaluation has access to some stack object during its evaluation process.
        /// </summary>
        /// <param name="name">Name to push value unto the stack as.</param>
        /// <param name="value">Actual object to push unto the stack. Notice, object will be automatically disposed at
        /// the end of the scope if the object implements IDisposable.</param>
        /// <param name="functor">Callback evaluated while stack object is on the stack.</param>
        public void Scope(string name, object value, Action functor)
        {
            _stack.Add(new Tuple<string, object>(name, value));
            try
            {
                functor();
            }
            finally
            {
                var obj = _stack[_stack.Count - 1];
                _stack.Remove(obj);
                if (obj is IDisposable disp)
                    disp.Dispose();
            }
        }

        /// <summary>
        /// Retrieves the last stack object pushed unto the stack with the specified name.
        /// </summary>
        /// <typeparam name="T">Type to return stack object as. Notice, no conversion will be attempted.
        /// Make sure you use the correct type when retrieving your stack object.</typeparam>
        /// <param name="name">Name stack object was pushed as.</param>
        /// <returns>The first stack object with the specified name, or null if none are found.</returns>
        public T Peek<T>(string name) where T : class
        {
            return _stack.AsEnumerable().Reverse().FirstOrDefault(x => x.Item1 == name)?.Item2 as T;
        }

        #endregion

        #region [ -- Private helper methods -- ]

        /*
         * Returns a boolean indicating if caller is allowed to raise signals
         * or not.
         */
        bool CanRaiseSignals()
        {
            return _canRaiseSignals.HasValue ? _canRaiseSignals.Value : HasValidLicense();
        }

        /*
         * This is our license checker logic, that will check to see if caller
         * has a valid license or not.
         */
        bool HasValidLicense()
        {
            // Checking if this instance has an HTTP context.
            var contextAccessor = _provider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
            if (contextAccessor?.HttpContext?.Request == null)
            {
                /*
                 * This is not an HTTP context, hence we allow all usage.
                 *
                 * Notice, we still don't set the _canRaiseSignals boolean field,
                 * since it might just be a thread outside of the HTTP context
                 * doing the current invocation, and other HTTP context signals
                 * might be raised later.
                 */
                return true;
            }

            // Retrieving the Host HTTP header of the current request.
            var host = contextAccessor.HttpContext.Request.Host.Host;

            /*
             * Checking if the Host header is a localhost domain of some sort,
             * at which point we allow for fallthrough, raising all signals,
             * to avoid having to create a license file for localhost development.
             */
            if (host == "localhost")
            {
                /*
                 * This is a localhost type of access.
                 *
                 * Notice, we still don't set the _canRaiseSignals boolean field,
                 * since it might just be one invocation from the localhost
                 * computer, and other types of requests might come up later,
                 * with an "real" hostname.
                 */
                return true;
            }

            /*
             * Now we now we have an HTTP context, with a Host
             * header, that is not "localhost" - Hence, we can check if the
             * caller has a valid license file, and if not, we turn off all
             * signals from now on an onwards.
             *
             * But as we do, we have to synchronize access to our shared resource.
             */
            lock(_locker)
            {
                // To avoid multiple threads executing the same logic.
                if (_canRaiseSignals.HasValue)
                    return _canRaiseSignals.Value;

                // Checking if there even exists a configuration setting.
                var configuration = _provider.GetService(typeof(IConfiguration)) as IConfiguration;
                var license = configuration["magic:license"];
                if (license == null)
                {
                    // No license settings in configuration file.
                    _canRaiseSignals = false;
                    return false;
                }

                // Checking if current domain has a valid license.
                var sec = host + "thomas hansen is cool";
                using (var sha = SHA256.Create())
                {
                    var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sec));
                    var hash = BitConverter.ToString(hashBytes).Replace("-","").ToLowerInvariant();
                    var domains = license.Split(',');
                    if (domains.Any(x => x.Split(':')[1].Trim() == sec))
                    {
                        // Yup, we have a valid license!
                        _canRaiseSignals = true;
                        return true;
                    }
                    return false; // No valid license!
                }
            }
        }

        #endregion
    }
}
