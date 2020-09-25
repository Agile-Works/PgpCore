﻿using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace PgpCore
{
    public class EncryptionKeys : IEncryptionKeys
    {
        #region Instance Members (Public)
        public PgpPublicKey PublicKey => PublicKeys.FirstOrDefault();
        public IEnumerable<PgpPublicKey> PublicKeys { get; private set; }
        public PgpPrivateKey PrivateKey { get; private set; }
        public PgpSecretKey SecretKey { get; private set; }
        public PgpSecretKeyRingBundle SecretKeys { get; private set; }

        #endregion Instance Members (Public)

        #region Instance Members (Private)
        private readonly string _passPhrase;

        #endregion Instance Members (Private)

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the EncryptionKeys class.
        /// Two keys are required to encrypt and sign data. Your private key and the recipients public key.
        /// The data is encrypted with the recipients public key and signed with your private key.
        /// </summary>
        /// <param name="publicKeyFilePath">The key used to encrypt the data</param>
        /// <param name="privateKeyFilePath">The key used to sign the data.</param>
        /// <param name="passPhrase">The password required to access the private key</param>
        /// <exception cref="ArgumentException">Public key not found. Private key not found. Missing password</exception>
        public EncryptionKeys(string publicKeyFilePath, string privateKeyFilePath, string passPhrase)
        {
            if (String.IsNullOrEmpty(publicKeyFilePath))
                throw new ArgumentException("PublicKeyFilePath");
            if (String.IsNullOrEmpty(privateKeyFilePath))
                throw new ArgumentException("PrivateKeyFilePath");
            if (passPhrase == null)
                throw new ArgumentNullException("Invalid Pass Phrase.");

            if (!File.Exists(publicKeyFilePath))
                throw new FileNotFoundException(String.Format("Public Key file [{0}] does not exist.", publicKeyFilePath));
            if (!File.Exists(privateKeyFilePath))
                throw new FileNotFoundException(String.Format("Private Key file [{0}] does not exist.", privateKeyFilePath));

            PublicKeys = new List<PgpPublicKey>() { Utilities.ReadPublicKey(publicKeyFilePath) };
            SecretKey = ReadSecretKey(privateKeyFilePath);
            PrivateKey = ReadPrivateKey(passPhrase);
            _passPhrase = passPhrase;
        }

        /// <summary>
        /// Initializes a new instance of the EncryptionKeys class.
        /// Two or more keys are required to encrypt and sign data. Your private key and the recipients public key(s).
        /// The data is encrypted with the recipients public key(s) and signed with your private key.
        /// </summary>
        /// <param name="publicKeyFilePaths">The key(s) used to encrypt the data</param>
        /// <param name="privateKeyFilePath">The key used to sign the data.</param>
        /// <param name="passPhrase">The password required to access the private key</param>
        /// <exception cref="ArgumentException">Public key not found. Private key not found. Missing password</exception>
        public EncryptionKeys(IEnumerable<string> publicKeyFilePaths, string privateKeyFilePath, string passPhrase)
        {
            // Avoid multiple enumerations of 'publicKeyFilePaths'
            string[] publicKeys = publicKeyFilePaths.ToArray();

            if (String.IsNullOrEmpty(privateKeyFilePath))
                throw new ArgumentException("PrivateKeyFilePath");
            if (passPhrase == null)
                throw new ArgumentNullException("Invalid Pass Phrase.");

            if (!File.Exists(privateKeyFilePath))
                throw new FileNotFoundException(String.Format("Private Key file [{0}] does not exist.", privateKeyFilePath));

            foreach (string publicKeyFilePath in publicKeys)
            {
                if (String.IsNullOrEmpty(publicKeyFilePath))
                    throw new ArgumentException(nameof(publicKeyFilePath));
                if (!File.Exists(publicKeyFilePath))
                    throw new FileNotFoundException(String.Format("Input file [{0}] does not exist.", publicKeyFilePath));
            }

            PublicKeys = publicKeys.Select(x => Utilities.ReadPublicKey(x)).ToList();
            SecretKey = ReadSecretKey(privateKeyFilePath);
            PrivateKey = ReadPrivateKey(passPhrase);
            _passPhrase = passPhrase;
        }

        public EncryptionKeys(string privateKeyFilePath, string passPhrase)
        {
            if (String.IsNullOrEmpty(privateKeyFilePath))
                throw new ArgumentException("PrivateKeyFilePath");
            if (passPhrase == null)
                throw new ArgumentNullException("Invalid Pass Phrase.");

            if (!File.Exists(privateKeyFilePath))
                throw new FileNotFoundException(String.Format("Private Key file [{0}] does not exist.", privateKeyFilePath));

            PublicKeys = null;
            SecretKey = ReadSecretKey(privateKeyFilePath);
            PrivateKey = ReadPrivateKey(passPhrase);
            _passPhrase = passPhrase;
        }

        public EncryptionKeys(Stream publicKeyStream, Stream privateKeyStream, string passPhrase)
        {
            if (publicKeyStream == null)
                throw new ArgumentException("PublicKeyStream");
            if (privateKeyStream == null)
                throw new ArgumentException("PrivateKeyStream");
            if (passPhrase == null)
                throw new ArgumentNullException("Invalid Pass Phrase.");

            PublicKeys = new List<PgpPublicKey>() { Utilities.ReadPublicKey(publicKeyStream) };
            SecretKey = ReadSecretKey(privateKeyStream);
            PrivateKey = ReadPrivateKey(passPhrase);
            _passPhrase = passPhrase;
        }

        public EncryptionKeys(Stream privateKeyStream, string passPhrase)
        {
            if (privateKeyStream == null)
                throw new ArgumentException("PrivateKeyStream");
            if (passPhrase == null)
                throw new ArgumentNullException("Invalid Pass Phrase.");

            SecretKey = ReadSecretKey(privateKeyStream);
            PrivateKey = ReadPrivateKey(passPhrase);
            _passPhrase = passPhrase;
        }

        public EncryptionKeys(IEnumerable<Stream> publicKeyStreams, Stream privateKeyStream, string passPhrase)
        {
            // Avoid multiple enumerations of 'publicKeyFilePaths'
            Stream[] publicKeys = publicKeyStreams.ToArray();

            if (privateKeyStream == null)
                throw new ArgumentException("PrivateKeyStream");
            if (passPhrase == null)
                throw new ArgumentNullException("Invalid Pass Phrase.");
            foreach (Stream publicKey in publicKeys)
            {
                if (publicKey == null)
                    throw new ArgumentException("PublicKeyStream");
            }

            PublicKeys = publicKeys.Select(x => Utilities.ReadPublicKey(x)).ToList();
            SecretKey = ReadSecretKey(privateKeyStream);
            PrivateKey = ReadPrivateKey(passPhrase);
            _passPhrase = passPhrase;
        }

        /// <summary>
        /// Initializes a new instance of the EncryptionKeys class.
        /// Two keys are required to encrypt and sign data. Your private key and the recipients public key.
        /// The data is encrypted with the recipients public key and signed with your private key.
        /// </summary>
        /// <param name="publicKeyFilePath">The key used to encrypt the data</param>
        /// <exception cref="ArgumentException">Public key not found. Private key not found. Missing password</exception>
        public EncryptionKeys(string publicKeyFilePath)
        {
            if (String.IsNullOrEmpty(publicKeyFilePath))
                throw new ArgumentException("PublicKeyFilePath");

            if (!File.Exists(publicKeyFilePath))
                throw new FileNotFoundException(String.Format("Public Key file [{0}] does not exist.", publicKeyFilePath));

            PublicKeys = new List<PgpPublicKey>() { Utilities.ReadPublicKey(publicKeyFilePath) };
        }

        /// <summary>
        /// Initializes a new instance of the EncryptionKeys class.
        /// Two keys are required to encrypt and sign data. Your private key and the recipients public key.
        /// The data is encrypted with the recipients public key and signed with your private key.
        /// </summary>
        /// <param name="publicKeyFilePath">The key used to encrypt the data</param>
        /// <exception cref="ArgumentException">Public key not found. Private key not found. Missing password</exception>
        public EncryptionKeys(IEnumerable<string> publicKeyFilePaths)
        {
            // Avoid multiple enumerations of 'publicKeyFilePaths'
            string[] publicKeys = publicKeyFilePaths.ToArray();

            foreach (string publicKeyFilePath in publicKeys)
            {
                if (String.IsNullOrEmpty(publicKeyFilePath))
                    throw new ArgumentException(nameof(publicKeyFilePath));
                if (!File.Exists(publicKeyFilePath))
                    throw new FileNotFoundException(String.Format("Input file [{0}] does not exist.", publicKeyFilePath));
            }

            PublicKeys = publicKeys.Select(x => Utilities.ReadPublicKey(x)).ToList();
        }

        public EncryptionKeys(Stream publicKeyStream)
        {
            if (publicKeyStream == null)
                throw new ArgumentException("PublicKeyStream");

            PublicKeys = new List<PgpPublicKey>() { Utilities.ReadPublicKey(publicKeyStream) };
        }

        public EncryptionKeys(IEnumerable<Stream> publicKeyStreams)
        {
            Stream[] publicKeys = publicKeyStreams.ToArray();

            foreach (Stream publicKey in publicKeys)
            {
                if (publicKey == null)
                    throw new ArgumentException("PublicKeyStream");
            }

            PublicKeys = publicKeys.Select(x => Utilities.ReadPublicKey(x)).ToList();
        }

        #endregion Constructors

        #region Public Methods

        public PgpPrivateKey FindSecretKey(long keyId)
        {
            PgpSecretKey pgpSecKey = SecretKeys.GetSecretKey(keyId);

            if (pgpSecKey == null)
                return null;

            return pgpSecKey.ExtractPrivateKey(_passPhrase.ToCharArray());
        }

        #endregion Public Methods

        #region Secret Key

        private PgpSecretKey ReadSecretKey(string privateKeyPath)
        {
            using (Stream sr = File.OpenRead(privateKeyPath))
            using (Stream inputStream = PgpUtilities.GetDecoderStream(sr))
            {
                SecretKeys = new PgpSecretKeyRingBundle(inputStream);
                PgpSecretKey foundKey = GetFirstSecretKey(SecretKeys);
                if (foundKey != null)
                    return foundKey;
            }
            throw new ArgumentException("Can't find signing key in key ring.");
        }

        private PgpSecretKey ReadSecretKey(Stream privateKeyStream)
        {
            using (Stream inputStream = PgpUtilities.GetDecoderStream(privateKeyStream))
            {
                SecretKeys = new PgpSecretKeyRingBundle(inputStream);
                PgpSecretKey foundKey = GetFirstSecretKey(SecretKeys);
                if (foundKey != null)
                    return foundKey;
            }
            throw new ArgumentException("Can't find signing key in key ring.");
        }

        /// <summary>
        /// Return the first key we can use to encrypt.
        /// Note: A file can contain multiple keys (stored in "key rings")
        /// </summary>
        private PgpSecretKey GetFirstSecretKey(PgpSecretKeyRingBundle secretKeyRingBundle)
        {
            foreach (PgpSecretKeyRing kRing in secretKeyRingBundle.GetKeyRings())
            {
                PgpSecretKey key = kRing.GetSecretKeys()
                    .Cast<PgpSecretKey>()
                    .Where(k => k.IsSigningKey)
                    .FirstOrDefault();
                if (key != null)
                    return key;
            }
            return null;
        }

        #endregion Secret Key

        #region Public Key
       
        private PgpPublicKey GetFirstPublicKey(PgpPublicKeyRingBundle publicKeyRingBundle)
        {
            foreach (PgpPublicKeyRing kRing in publicKeyRingBundle.GetKeyRings())
            {
                PgpPublicKey key = kRing.GetPublicKeys()
                    .Cast<PgpPublicKey>()
                    .Where(k => k.IsEncryptionKey)
                    .FirstOrDefault();
                if (key != null)
                    return key;
            }
            return null;
        }

        #endregion Public Key

        #region Private Key

        private PgpPrivateKey ReadPrivateKey(string passPhrase)
        {
            PgpPrivateKey privateKey = SecretKey.ExtractPrivateKey(passPhrase.ToCharArray());
            if (privateKey != null)
                return privateKey;

            throw new ArgumentException("No private key found in secret key.");
        }

        #endregion Private Key
    }
}
