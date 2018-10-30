using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;


namespace AdvancedFileViewer_WPF.TreeView
{
    static class Crypto
    {

        static byte[] data = new byte[] {0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20};

        static byte[] signed;

        static int keyLength = 2048;

        static RSACryptoServiceProvider rsaCryptoProvider;

        static CspParameters CSPParam;


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

        private static readonly byte[] SALT = new byte[]
            {0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c};

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
            var cryptoStream = new CryptoStream(memoryStream, rc2.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipher, 0, cipher.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

        public static byte[] RSAEncrypt(byte[] bytesToEncrypt, string publicKey)
        {
            RSACryptoServiceProvider rsaGenKeys = new RSACryptoServiceProvider();
            string privateXml = rsaGenKeys.ToXmlString(true);
            string publicXml = rsaGenKeys.ToXmlString(false);

            //Encode with public key
            RSACryptoServiceProvider rsaPublic = new RSACryptoServiceProvider();
            rsaPublic.FromXmlString(publicXml);
            byte[] encryptedRSA = rsaPublic.Encrypt(bytesToEncrypt, false);
            return encryptedRSA;

        }

        public static byte[] RSADecrypt(byte[] bytesToDecrypt, string Key)
        {

            CSPParam = new CspParameters(1) {Flags = CspProviderFlags.UseMachineKeyStore};
            
            RSACryptoServiceProvider rsaGenKeys = new RSACryptoServiceProvider();
            string privateXml = rsaGenKeys.ToXmlString(true);
            string publicXml = rsaGenKeys.ToXmlString(false);
            
            //Decode with private key
            var rsaPrivate = new RSACryptoServiceProvider();
            rsaPrivate.FromXmlString(privateXml);
            byte[] decryptedRSA = rsaPrivate.Decrypt(bytesToDecrypt, false);
            return decryptedRSA;
        }

        public static bool CreateRSAKeysMS(out byte[] privateKey, out byte[] publicKey)
        {
            privateKey = null;
            publicKey = null;
            rsaCryptoProvider = new RSACryptoServiceProvider(keyLength, CSPParam);
            publicKey = rsaCryptoProvider.ExportCspBlob(false);
            privateKey = rsaCryptoProvider.ExportCspBlob(true);
            return true;
        }

        public static byte[] Encrypt(byte[] publicKey, byte[] data)
        {
            rsaCryptoProvider = new RSACryptoServiceProvider(keyLength, CSPParam);
            rsaCryptoProvider.ImportCspBlob(publicKey);
            RSAParameters parameters = rsaCryptoProvider.ExportParameters(false);
            RsaKeyParameters key = DotNetUtilities.GetRsaPublicKey(parameters);
            IAsymmetricBlockCipher cipher =
                new OaepEncoding(new RsaEngine(), new Sha1Digest(), new Sha1Digest(), new byte[0]);
            cipher.Init(true, key);
            return cipher.ProcessBlock(data, 0, data.Length);
        }

        public static byte[] Decrypt(byte[] privateKey, byte[] data)
        {
            rsaCryptoProvider = new RSACryptoServiceProvider(keyLength, CSPParam);
            rsaCryptoProvider.ImportCspBlob(privateKey);
            RSAParameters parameters = rsaCryptoProvider.ExportParameters(true);
            AsymmetricCipherKeyPair keyPair = DotNetUtilities.GetRsaKeyPair(parameters);
            IAsymmetricBlockCipher cipher =
                new OaepEncoding(new RsaEngine(), new Sha1Digest(), new Sha1Digest(), new byte[0]);
            cipher.Init(false, keyPair.Private);
            return cipher.ProcessBlock(data, 0, data.Length);
        }

        public static byte[] Sign512(byte[] data, byte[] privateKey)
        {
            var enhCsp = new RSACryptoServiceProvider().CspKeyContainerInfo;
            var cspparams = new CspParameters(enhCsp.ProviderType, enhCsp.ProviderName);
            rsaCryptoProvider = new RSACryptoServiceProvider(cspparams);
            rsaCryptoProvider.ImportCspBlob(privateKey);
            return rsaCryptoProvider.SignData(data, CryptoConfig.MapNameToOID("SHA512"));
        }

        public static bool VerifySignature512(byte[] data, byte[] signature, byte[] publicKey)
        {
            var enhCsp = new RSACryptoServiceProvider().CspKeyContainerInfo;
            var cspparams = new CspParameters(enhCsp.ProviderType, enhCsp.ProviderName);
            rsaCryptoProvider = new RSACryptoServiceProvider(cspparams);
            rsaCryptoProvider.ImportCspBlob(publicKey);
            return rsaCryptoProvider.VerifyData(data, CryptoConfig.MapNameToOID("SHA512"), signature);
        }
    }
}





