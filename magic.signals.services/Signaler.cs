/*
 * Magic, Copyright(c) Thomas Hansen 2019, thomas@gaiasoul.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using magic.node;
using magic.signals.contracts;

namespace magic.signals.services
{
    /// <summary>
    /// Default implementation service class for the ISignaler contract/interface.
    /// </summary>
    public class Signaler : ISignaler
    {
        static bool? _canRaiseSignals;
        static readonly object _locker = new object();
        static readonly DateTime _startUpTime = DateTime.Now;

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

        /// <summary>
        /// Sets your license key for current installation.
        /// </summary>
        static public string LicenseKey { get; set; }

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
                throw new ApplicationException("You seem to be missing a valid licence, please obtain one at https://gaiasoul.com/license-magic/ if you wish to continue using Magic.");

            var type = _signals.GetSlot(name) ?? throw new ApplicationException($"No slot exists for [{name}]");
            var raw = _provider.GetService(type);

            // Basic sanity checking.
            if (!(raw is ISlot instance))
            {
                if (raw is ISlotAsync)
                    throw new ApplicationException($"The [{name}] slot is an async slot, and you tried to invoke it synchronously. Please invoke it async.");
                throw new ApplicationException($"I couldn't find the [{name}] slot, have you registered it?");
            }

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
        /// <returns>An awaitable task.</returns>
        public async Task SignalAsync(string name, Node input)
        {
            if (!CanRaiseSignals())
                throw new ApplicationException("You seem to be missing a valid licence, please obtain one at https://gaiasoul.com/license-magic/ if you wish to continue using Magic.");

            var type = _signals.GetSlot(name) ?? throw new ApplicationException($"No slot exists for [{name}]");
            var raw = _provider.GetService(type);

            // Basic sanity checking.
            if (!(raw is ISlotAsync instance))
            {
                if (raw is ISlot)
                    throw new ApplicationException($"The [{name}] slot is not an async slot, and you tried to invoke it as such. Please invoke it synchronously.");
                throw new ApplicationException($"I couldn't find the [{name}] slot, have you registered it?");
            }

            await instance.SignalAsync(this, input);
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
        /// Pushes the specified object unto the stack with the given key name,
        /// for then to evaluate the given functor. Useful for evaluating some piece of code
        /// making sure the evaluation has access to some stack object during its evaluation process.
        /// </summary>
        /// <param name="name">Name to push value unto the stack as.</param>
        /// <param name="value">Actual object to push unto the stack. Notice, object will be automatically disposed at
        /// the end of the scope if the object implements IDisposable.</param>
        /// <param name="functor">Callback evaluated while stack object is on the stack.</param>
        /// <returns>An awaitable task.</returns>
        public async Task ScopeAsync(string name, object value, Func<Task> functor)
        {
            _stack.Add(new Tuple<string, object>(name, value));
            try
            {
                await functor();
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
            return _canRaiseSignals ?? HasValidLicense();
        }

        /*
         * This is our license checker logic, that will check to see if caller
         * has a valid license or not.
         */
        bool HasValidLicense()
        {
            /*
             * We allow the user to use the software for 5 hours
             * of "trial period".
             */
            if (string.IsNullOrEmpty(LicenseKey))
            {
                if (_startUpTime.AddHours(5) > DateTime.Now)
                    return true; // Trial period is still running ...

                // Trial period is over,and there's no license key.
                _canRaiseSignals = false;
                return false;
            }

            /*
             * Synchronizing access to static shared fields while we check for
             * a valid license key.
             */
            lock (_locker)
            {
                // Checking if license key is valid.
                var licenseEntities = LicenseKey.Split(':');
                if (licenseEntities.Length != 2)
                    throw new ApplicationException("Your license must contain your domain (hostname/DNS entry) and your actual key, separated by ':', e.g. 'api.some-website.com:xxxxxxx'.");

                /*
                 * Salting hostname parts of license key, hashing it, and
                 * comparing it to the license key of the license key.
                 */
                using (var sha = SHA256.Create())
                {
                    var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(licenseEntities[0] + "thomas hansen is cool"));
                    var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    if (hash == licenseEntities[1])
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
