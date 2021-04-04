/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;
using magic.crypto.combinations;
using magic.node.extensions.hyperlambda;

namespace magic.signals.services
{
    /// <summary>
    /// [license.create] slot for creating a license.
    /// </summary>
    [Slot(Name = "license.create")]
    public class CreateLicense : ISlot
    {
        /// <summary>
        /// Handles the signal for the class.
        /// </summary>
        /// <param name="signaler">Signaler used to signal the slot.</param>
        /// <param name="input">Root node for invocation.</param>
        public void Signal(ISignaler signaler, Node input)
        {
            // Retrieving arguments.
            var privateKey = input.Children.FirstOrDefault(x => x.Name == "private-key")?.GetEx<string>() ??
                throw new ArgumentException("This slot requires a [private-key] argument, being ServerGardens private RSA key to generate a license");
            var fingerprint = input.Children.FirstOrDefault(x => x.Name == "fingerprint")?.GetEx<string>()?.Replace("-", "") ??
                throw new ArgumentException("This slot requires a [private-key] argument, being ServerGardens private RSA key to generate a license");

            // Injecting issued argument.
            input.Add(new Node("issued", DateTime.UtcNow));

            // Creating Hyperlambda that we should cryptographically sign, ending up being the actual license.
            var hyper = Generator.GetHyper(input.Children.Where(x => x.Name != "private-key" && x.Name != "fingerprint"));

            // Retrieving the signing key's fingerprint in byte[] format.
            int noChars = fingerprint.Length;
            byte[] fingerprintBytes = new byte[noChars / 2];
            for (int i = 0; i < noChars; i += 2)
            {
                fingerprintBytes[i / 2] = Convert.ToByte(fingerprint.Substring(i, 2), 16);
            }
            if (fingerprintBytes.Length != 32)
                throw new ArgumentException("Fingerprint is not 32 bytes long");

            // Creating our signer, and cryptographically signing the Hyperlambda created from the arguments.
            var signer = new Signer(Convert.FromBase64String(privateKey.Replace("\r", "").Replace("\n", "")), fingerprintBytes);
            var license = signer.SignToString(hyper);

            // Returning license to caller, and doing some house cleaning.
            input.Value = license;
            input.Clear();
        }
    }
}
