using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.Extensions
{
    public static class SecurityExtensions
    {
        /// <summary>
        /// Encryptes a string using the supplied key. Encoding is done using RSA encryption.
        /// </summary>
        /// <param name="stringToEncrypt">String that must be encrypted.</param>
        /// <param name="key">Encryptionkey.</param>
        /// <returns>A string representing a byte array separated by a minus sign.</returns>
        /// <exception cref="ArgumentException">Occurs when stringToEncrypt or key is null or empty.</exception>
        public static string Encrypt(this string stringToEncrypt, string key)
        {
            if (string.IsNullOrEmpty(stringToEncrypt))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cannot encrypt using an empty key. Please supply an encryption key!");
            }

            CspParameters cspp = new CspParameters();
            cspp.KeyContainerName = key;

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;

            byte[] bytes = rsa.Encrypt(UTF8Encoding.UTF8.GetBytes(stringToEncrypt), true);

            return BitConverter.ToString(bytes);
        }

        /// <summary>
        /// Decryptes a string using the supplied key. Decoding is done using RSA encryption.
        /// </summary>
        /// <param name="stringToDecrypt">String that must be decrypted.</param>
        /// <param name="key">Decryptionkey.</param>
        /// <returns>The decrypted string or null if decryption failed.</returns>
        /// <exception cref="ArgumentException">Occurs when stringToDecrypt or key is null or empty.</exception>
        public static string Decrypt(this string stringToDecrypt, string key)
        {
            string result = null;

            if (string.IsNullOrEmpty(stringToDecrypt))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cannot decrypt using an empty key. Please supply a decryption key!");
            }

            try
            {
                CspParameters cspp = new CspParameters();
                cspp.KeyContainerName = key;

                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cspp);
                rsa.PersistKeyInCsp = true;

                var byteConverter = new UnicodeEncoding();
                
                string[] decryptArray = stringToDecrypt.Split(new string[] { "-" }, StringSplitOptions.None);
                byte[] decryptByteArray = Array.ConvertAll<string, byte>(decryptArray, (s => Convert.ToByte(byte.Parse(s, System.Globalization.NumberStyles.HexNumber))));

                byte[] bytes = rsa.Decrypt(decryptByteArray, true);

                result = UTF8Encoding.UTF8.GetString(bytes);
            }                
            catch(Exception ex)
            {
                TraktLogger.Error("Failed to decyrpt password, Reason = '{0}'", ex.Message);
            }

            return result;
        }

        public static List<TraktAuthentication> Decrypt(this List<TraktAuthentication> logins, string key)
        {
            if (logins == null || logins.Count == 0)
                return logins;

            var result = new List<TraktAuthentication>();

            foreach (var login in logins)
            {
                result.Add(new TraktAuthentication { Username = login.Username, Password = login.Password.Decrypt(key) });
            }

            return result;
        }

        public static List<TraktAuthentication> Encrypt(this List<TraktAuthentication> logins, string key)
        {
            if (logins == null || logins.Count == 0)
                return logins;

            var result = new List<TraktAuthentication>();

            foreach (var login in logins)
            {
                result.Add(new TraktAuthentication { Username = login.Username, Password = login.Password.Encrypt(key) });
            }

            return result;
        }
    }
}