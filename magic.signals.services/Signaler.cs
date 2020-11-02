/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using magic.node;
using magic.signals.contracts;
using magic.crypto.combinations;
using magic.node.extensions.hyperlambda;
using magic.node.extensions;

namespace magic.signals.services
{
    /// <summary>
    /// Default implementation service class for the ISignaler contract/interface.
    /// </summary>
    public class Signaler : ISignaler
    {
        static bool _validLicense;
        static DateTime _stopTime = DateTime.UtcNow.AddHours(47);
        const int MAJOR_VERSION = 8;
        const string PUBLIC_KEY = @"
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAm6ZleQuIJiD3DaI/1EUdS8WQgQYc6RTKXNu/7VJwgxd
3jUirQeGmcAP105RjTG46Htr8Ne7TomNyLLtv+XGG+GwQ7UO8gcXULSwj/N1AeHhc9xH/0HdCfiPUOHyZhZlbLt
veHXaxMTTKVYS0CO7teQJc6EwK17YzSn6q5h8Dp/K6oTdOXe2gmR4asIDwjBdNWF+KdalhQF+piGpZvaUS8+QDZ
TIBYKPHO4DC6UmHJDk20mxPSzEFytFaaVM2dj6DTtq8RHkoQ4lfwn5zEauTjT8iuzfcYwckkEVBehyOGOOooSlF
ra0iTkZcZgIC6V2n7wluxNS/VyV5SlnF56qVaQIDAQAB";
        internal static Node _licenseData = null;
        readonly IServiceProvider _provider;
        readonly ISignalsProvider _signals;
        readonly List<Tuple<string, object>> _stack = new List<Tuple<string, object>>();

        /// <summary>
        /// Creates a new instance of the default ISignaler service class.
        /// </summary>
        /// <param name="provider">Service provider to use for retrieving services.</param>
        /// <param name="signals">Implementation class to use for retrieving
        /// types from their string representations.</param>
        public Signaler(IServiceProvider provider, ISignalsProvider signals)
        {
            _provider = provider;
            _signals = signals;
        }

        /// <summary>
        /// Sets your license for current installation.
        ///
        /// Notice, without a valid license, Magic will stop working
        /// after 47 hours.
        /// </summary>
        /// <param name="license">Your license, as obtained from Server Gardens.</param>
        static public void SetLicenseKey(string license)
        {
            // Sanity checking invocation.
            if (_licenseData != null)
                throw new Exception("You have already applied your license, please avoid doing it twice.");

            // This will throw an exception if the signature, and/or data has been tampered with.
            var verifier = new Verifier(
                Convert.FromBase64String(PUBLIC_KEY.Replace("\r", "").Replace("\n", "")));
            var licenseData = Encoding.UTF8.GetString(
                verifier.Verify(Convert.FromBase64String(license.Replace("\r", "").Replace("\n", ""))));

            // Parsing license, and keeping meta data around.
            _licenseData = new Parser(licenseData).Lambda();

            // Checking if user is using a version that's not included in the license.
            var untilVersion = _licenseData.Children.FirstOrDefault(x => x.Name == "valid-version")?.GetEx<int>() ?? -1;
            if (untilVersion != -1 && untilVersion < MAJOR_VERSION)
                throw new Exception("Your license is not valid for the current version, please contact license@servergardens.com or visit https://servergardens.com/buy for a new license, or downgrade Magic to an older version");

            // Checking if it's a temporary license key, with an absolute expiration date.
            var expiration = _licenseData.Children.FirstOrDefault(x => x.Name == "expires")?.GetEx<DateTime>() ?? DateTime.MinValue;
            if (expiration != DateTime.MinValue)
            {
                // License has an absolute expiration date.
                _stopTime = expiration;

                // Checking if license has expired.
                if (expiration <= DateTime.UtcNow)
                    throw new Exception("Your license has expired, please contact license@servergardens.com or visit https://servergardens.com/buy for a new license");
            }
            else
            {
                // Not a license with an absolute expiration date.
                _validLicense = true;
            }
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
            if (!_validLicense && DateTime.UtcNow > _stopTime)
                throw new ArgumentException("You seem to be missing a valid licence, please obtain one at https://servergardens.com/buy/ if you wish to continue using Magic.");

            var type = _signals.GetSlot(name) ?? throw new ArgumentException($"No slot exists for [{name}]");
            var raw = _provider.GetService(type);

            // Basic sanity checking.
            if (raw is ISlot slot)
                slot.Signal(this, input);
            else
                throw new ArgumentException($"I couldn't find a synchronous version of the [{name}] slot?");
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
        public Task SignalAsync(string name, Node input)
        {
            if (!_validLicense && DateTime.UtcNow > _stopTime)
                throw new ArgumentException("You seem to be missing a valid licence, please obtain one at https://servergardens.com/buy/ if you wish to continue using Magic.");

            var type = _signals.GetSlot(name) ?? throw new ArgumentException($"No slot exists for [{name}]");
            var raw = _provider.GetService(type);

            // Basic sanity checking.
            if (raw is ISlotAsync asyncSlot)
                return asyncSlot.SignalAsync(this, input);

            if (raw is ISlot syncSlot)
            {
                syncSlot.Signal(this, input);
                return Task.CompletedTask;
            }
            throw new ArgumentException($"I couldn't find the [{name}] slot, have you registered it?");
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
                var obj = _stack.Last();
                _stack.RemoveAt(_stack.Count - 1);
                if (obj is IDisposable disp)
                    disp.Dispose();
            }
        }

        /// <summary>
        /// Pushes the specified object unto the stack with the given key name,
        /// for then to evaluate the given functor. Useful for evaluating some
        /// piece of code making sure the evaluation has access to some stack
        /// object during its evaluation process.
        /// </summary>
        /// <param name="name">Name to push value unto the stack as.</param>
        /// <param name="value">Actual object to push unto the stack. Notice,
        /// object will be automatically disposed at the end of the scope if
        /// the object implements IDisposable.</param>
        /// <param name="functor">Callback evaluated while stack object is on
        /// the stack.</param>
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
                var obj = _stack.Last();
                _stack.RemoveAt(_stack.Count - 1);
                if (obj is IDisposable disp)
                    disp.Dispose();
            }
        }

        /// <summary>
        /// Retrieves the last stack object pushed unto the stack with the
        /// specified name.
        /// </summary>
        /// <typeparam name="T">Type to return stack object as. Notice, no
        /// conversion will be attempted. Make sure you use the correct type
        /// when retrieving your stack object.</typeparam>
        /// <param name="name">Name stack object was pushed as.</param>
        /// <returns>The first stack object with the specified name, or null if
        /// none are found.</returns>
        public T Peek<T>(string name) where T : class
        {
            return _stack.LastOrDefault(x => x.Item1 == name)?.Item2 as T;
        }

        #endregion
    }
}
