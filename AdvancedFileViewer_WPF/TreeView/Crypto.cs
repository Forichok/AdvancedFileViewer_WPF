using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;

namespace AdvancedFileViewer_WPF.TreeView
{
    static class Crypto
    {

        public static string Decryptkey { get; set; } = "777";
        public static byte[] TripleDESEncrypt(byte[] toEncryptArray, string key)
        {
            byte[] keyArray;

            System.Configuration.AppSettingsReader settingsReader =
                new AppSettingsReader();

            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
       

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();


            tdes.Key = keyArray;

            tdes.Mode = CipherMode.ECB;


            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();

            byte[] resultArray =
                cTransform.TransformFinalBlock(toEncryptArray, 0,
                    toEncryptArray.Length);

            tdes.Clear();

            return resultArray;
        }

        public static byte[] TripleDESDecrypt(byte[] toEncryptArray, string key)
        {
            byte[] keyArray;
            System.Configuration.AppSettingsReader settingsReader =
                new AppSettingsReader();

            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            tdes.Key = keyArray;

            tdes.Mode = CipherMode.ECB;

            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(
                toEncryptArray, 0, toEncryptArray.Length);           
            tdes.Clear();
        
            return resultArray;
        }

        private static readonly byte[] SALT = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c };

        public static byte[] RijndaelEncrypt(byte[] plain, string password)
        {
            MemoryStream memoryStream;
            CryptoStream cryptoStream;
            Rijndael rijndael = Rijndael.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, SALT);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            memoryStream = new MemoryStream();
            cryptoStream = new CryptoStream(memoryStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(plain, 0, plain.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

        public static byte[] RijndaelDecrypt(byte[] cipher, string password)
        {
            MemoryStream memoryStream;
            CryptoStream cryptoStream;
            Rijndael rijndael = Rijndael.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, SALT);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            memoryStream = new MemoryStream();
            cryptoStream = new CryptoStream(memoryStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipher, 0, cipher.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

        public static byte[] RC2Encrypt(byte[] plain, string password)
        {
            RC2CryptoServiceProvider rc2 = new RC2CryptoServiceProvider();
            SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
            byte[] shahash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            byte[] keybytes = new byte[16];
            Array.Copy(shahash, keybytes, 16);

            rc2.Key = keybytes;
            rc2.IV = new byte[rc2.BlockSize / 8];

            var memoryStream = new MemoryStream();
            var cryptoStream = new CryptoStream(memoryStream, rc2.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(plain, 0, plain.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

       public static byte[] RC2Decrypt(byte[] cipher, string password)
        {
            RC2CryptoServiceProvider rc2 = new RC2CryptoServiceProvider();
            SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
            byte[] shahash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            byte[] keybytes = new byte[16];
            Array.Copy(shahash, keybytes, 16);

            rc2.Key = keybytes;
            rc2.IV = new byte[rc2.BlockSize / 8];

           var memoryStream = new MemoryStream();
           var  cryptoStream = new CryptoStream(memoryStream, rc2.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipher, 0, cipher.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

        public static byte[] RSAEncrypt(byte[] bytesToEncrypt, string publicKey)
        {
            publicKey = $"-----BEGIN PRIVATE KEY----- \n {publicKey} \n -----END PRIVATE KEY-----";
            var encryptEngine = new Pkcs1Encoding(new RsaEngine());

            using (var txtreader = new StringReader(publicKey))
            {
                var keyParameter = (AsymmetricKeyParameter)new PemReader(txtreader).ReadObject();

                encryptEngine.Init(true, keyParameter);
            }

            var encrypted = encryptEngine.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length);
            return encrypted;
        }




        // Decryption:

        public static byte[] RSADecrypt(byte[] bytesToDecrypt, string Key)
        {
            AsymmetricCipherKeyPair keyPair;
            Key = $"-----BEGIN PRIVATE KEY----- \n {Key} \n -----END PRIVATE KEY-----";
            var decryptEngine = new Pkcs1Encoding(new RsaEngine());

            using (var txtreader = new StringReader(Key))
            {
                keyPair = (AsymmetricCipherKeyPair)new PemReader(txtreader).ReadObject();

                decryptEngine.Init(false, keyPair.Private);
            }

            var decrypted = decryptEngine.ProcessBlock(bytesToDecrypt, 0, bytesToDecrypt.Length);
            return decrypted;
        }



    }
}
