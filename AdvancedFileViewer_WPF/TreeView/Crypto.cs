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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security;
using System.Security.Cryptography;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Utilities.IO.Pem;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto.Encodings;

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

    static class RSACrypto
    {        
        static IAsymmetricBlockCipher cipher;
        static RsaKeyPairGenerator rsaKeyPairGnr;
        static RsaKeyParameters publicKey;
        static RsaKeyParameters privateKey;
        static Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair;                    

        //for OAEP
        static SHA256Managed hash = new SHA256Managed();
        static SecureRandom randomNumber = new SecureRandom();
        static byte[] encodingParam = hash.ComputeHash(Encoding.UTF8.GetBytes(randomNumber.ToString()));

        private static void btnImportKeys_Click(string privKey, string pubKey)
        {
            privateKey =
                (RsaPrivateCrtKeyParameters) PrivateKeyFactory.CreateKey(Convert.FromBase64String(privKey));
            publicKey = (RsaKeyParameters) PublicKeyFactory.CreateKey(Convert.FromBase64String(pubKey));
        }

        public static void generateKeys()
        {
            // RSAKeyPairGenerator generates the RSA Key pair based on the random number and strength of key required
            rsaKeyPairGnr = new RsaKeyPairGenerator();
            rsaKeyPairGnr.Init(new Org.BouncyCastle.Crypto.KeyGenerationParameters(new SecureRandom(), 2048));
            keyPair = rsaKeyPairGnr.GenerateKeyPair();

            publicKey = (RsaKeyParameters) keyPair.Public;
            privateKey = (RsaKeyParameters) keyPair.Private;            
        }

        public static void rsaEngine(int flag)
        {
            if (flag == 0)
            {
                // Creating the RSA algorithm object
                cipher = new RsaEngine();
            }
            else
            {
                cipher = new OaepEncoding(new RsaEngine(), new Sha256Digest(), encodingParam);
            }
        }

        public static byte[] RSAEncrypt(byte[] bytesToEncrypt,string key)
        {
            // Initializing the RSA object for Encryption with RSA public key. Remember, for encryption, public key is needed
            cipher.Init(true, publicKey);

            //Encrypting the input bytes
            var cipheredBytes = cipher.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length);
            return cipheredBytes;
        }

        public static byte[] RSADecrypt(byte[] cipheredBytes,string key)
        {
            
            cipher.Init(false, privateKey);
            var result = cipher.ProcessBlock(cipheredBytes, 0, cipheredBytes.Length);
            return result;
        }


        private static byte[] btnSign_Click(byte[] bytesToSign)
        {
            // http://stackoverflow.com/questions/8830510/c-sharp-sign-data-with-rsa-using-bouncycastle

            /* Make the key */
            //RsaKeyParameters key = MakeKey(privateModulusHexString, privateExponentHexString, true);

            /* Init alg */
            ISigner sig;
            switch (0)
            {
                //http://www.apps.ietf.org/rfc/rfc3447.html#sec-9.2
                case 0:
                    sig = SignerUtilities.GetSigner("RSA");
                    break;
                //http://www.apps.ietf.org/rfc/rfc3447.html#sec-9.1
                case 1:
                    sig = SignerUtilities.GetSigner("SHA1withRSA");
                    break;
                case 2:
                    sig = SignerUtilities.GetSigner("SHA1withRSA/ISO9796-2");
                    break;

                default:
                    sig = SignerUtilities.GetSigner("RSA");
                    break;
            }

            /* Populate key */
            sig.Init(true, privateKey);

            /* Calc the signature */
            sig.BlockUpdate(bytesToSign, 0, bytesToSign.Length);
            byte[] signature = sig.GenerateSignature();

            return signature;
        }

        private static void butVerify_Click(int signType)
        {

            /* Make the key */
            //RsaKeyParameters key = MakeKey(publicModulusHexString, publicExponentHexString, false);

            /* Init alg */
            ISigner signer;
            switch (signType)
            {
                //http://www.apps.ietf.org/rfc/rfc3447.html#sec-9.2
                case 0:
                    signer = SignerUtilities.GetSigner("RSA");
                    break;
                //http://www.apps.ietf.org/rfc/rfc3447.html#sec-9.1
                case 1:
                    signer = SignerUtilities.GetSigner("SHA1withRSA");
                    break;

                case 2:
                    signer = SignerUtilities.GetSigner("SHA1withRSA/ISO9796-2");
                    break;

                default:
                    signer = SignerUtilities.GetSigner("RSA");
                    break;
            }

            /* Populate key */
            signer.Init(false, publicKey);

            ///* Get the signature into bytes */
            //var expectedSig = Convert.FromBase64String(tbOutput.Text);

            ///* Get the bytes to be signed from the string */
            //var msgBytes = Encoding.UTF8.GetBytes(tbInput.Text);

            ///* Calculate the signature and see if it matches */
            //signer.BlockUpdate(msgBytes, 0, msgBytes.Length);

            //if (signer.VerifySignature(expectedSig))
            //{
            //    MessageBox.Show("Poprawny", "Weryfikacja");
            //}
            //else
            //{
            //    MessageBox.Show("niepoprawny", "Weryfikacja");
            //}

        }
    }
}

