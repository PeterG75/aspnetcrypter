﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Web.Security.Cryptography;

namespace LowLevelDesign.AspNetCrypter
{
    internal sealed class AspNetDecryptor
    {
        private readonly CryptographicKey decryptionKey;
        private readonly CryptographicKey validationKey;
        private readonly bool isGzipped = false;

        public AspNetDecryptor(Purpose purpose, CryptographicKey decryptionKey, CryptographicKey validationKey, bool isGzipped)
        {
            this.decryptionKey = SP800_108.DeriveKey(decryptionKey, purpose);
            this.validationKey = SP800_108.DeriveKey(validationKey, purpose);
            this.isGzipped = isGzipped;
        }

        public byte[] DecryptData(byte[] data)
        {
            var cryptoService = new NetFXCryptoService(new GuessCryptoAlgorithmFactory(decryptionKey.KeyLength, 
                validationKey.KeyLength), decryptionKey, validationKey);

            var decryptedData = cryptoService.Unprotect(data);
            return isGzipped ? Decompress(decryptedData) : decryptedData;
        }


        private byte[] Decompress(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (MemoryStream decomp = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        gzip.CopyTo(decomp);
                    }
                    return decomp.ToArray();
                }
            }
        }

        private class GuessCryptoAlgorithmFactory : ICryptoAlgorithmFactory
        {
            private readonly SymmetricAlgorithm decryptionAlgorithm;
            private readonly KeyedHashAlgorithm validationAlgorigthm;

            public GuessCryptoAlgorithmFactory(int symmetricKeyLength, int validationKeyLength)
            {
                switch (symmetricKeyLength) {
                    case 64:
                        decryptionAlgorithm = DES.Create();
                        break;
                    case 192:
                        decryptionAlgorithm = TripleDES.Create();
                        break;
                    case 128:
                    case 256:
                        decryptionAlgorithm = Aes.Create();
                        break;
                    default:
                        throw new ArgumentException("Encryption algorithm could not be recognized.");
                }

                switch (validationKeyLength) {
                    case 128:
                        validationAlgorigthm = HMAC.Create("HMACMD5");
                        break;
                    case 160:
                        validationAlgorigthm = HMAC.Create("HMACSHA1");
                        break;
                    case 256:
                        validationAlgorigthm = HMAC.Create("HMACSHA256");
                        break;
                    case 384:
                        validationAlgorigthm = HMAC.Create("HMACSHA384");
                        break;
                    case 512:
                        validationAlgorigthm = HMAC.Create("HMACSHA512");
                        break;
                    default:
                        throw new ArgumentException("Validation algorithm could not be recognized.");
                }
            }

            public SymmetricAlgorithm GetEncryptionAlgorithm()
            {
                return decryptionAlgorithm;
            }

            public KeyedHashAlgorithm GetValidationAlgorithm()
            {
                return validationAlgorigthm;
            }
        }
    }
}
