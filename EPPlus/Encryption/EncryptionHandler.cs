﻿/*******************************************************************************
 * You may amend and distribute as you like, but don't remove this header!
 *
 * EPPlus provides server-side generation of Excel 2007/2010 spreadsheets.
 * See https://github.com/JanKallman/EPPlus for details.
 *
 * Copyright (C) 2011  Jan Källman
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.

 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU Lesser General Public License for more details.
 *
 * The GNU Lesser General Public License can be viewed at http://www.opensource.org/licenses/lgpl-license.php
 * If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
 *
 * All code and executables are provided "as is" with no warranty either express or implied.
 * The author accepts no liability for any damage or loss of business that this product may cause.
 *
 * Code change notes:
 *
 * Author							Change						Date
 * ******************************************************************************
 * Jan Källman		    Added       		        2013-01-05
 *******************************************************************************/

using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using OfficeOpenXml.Utils.CompundDocument;

namespace OfficeOpenXml.Encryption
{
    /// <summary>
    /// Handels encrypted Excel documents 
    /// </summary>
    internal class EncryptedPackageHandler
    {
        /// <summary>
        /// Read the package from the OLE document and decrypt it using the supplied password
        /// </summary>
        /// <param name="fi">The file</param>
        /// <param name="encryption"></param>
        /// <returns></returns>
        internal MemoryStream DecryptPackage(FileInfo fi, ExcelEncryption encryption)
        {
            if (CompoundDocument.IsCompoundDocument(fi))
            {
                var doc = new CompoundDocument(fi);

                MemoryStream ret = null;
                ret = GetStreamFromPackage(doc, encryption);
                return ret;
            }

            throw new InvalidDataException(string.Format("File {0} is not an encrypted package", fi.FullName));
        }

        //Helpmethod to output the streams in the storage
        //private void WriteDoc(CompoundDocument.StoragePart storagePart, string p)
        //{
        //    foreach (var store in storagePart.SubStorage)
        //    {
        //        string sdir=p + store.Key.Replace((char)6,'x') + "\\";
        //        Directory.CreateDirectory(sdir);
        //        WriteDoc(store.Value, sdir);
        //    }
        //    foreach (var str in storagePart.DataStreams)
        //    {
        //        File.WriteAllBytes(p + str.Key.Replace((char)6, 'x') + ".bin", str.Value);
        //    }
        //}
        /// <summary>
        /// Read the package from the OLE document and decrypt it using the supplied password
        /// </summary>
        /// <param name="stream">The memory stream. </param>
        /// <param name="encryption">The encryption object from the Package</param>
        /// <returns></returns>
        internal MemoryStream DecryptPackage(MemoryStream stream, ExcelEncryption encryption)
        {
            if (CompoundDocument.IsCompoundDocument(stream))
            {
                var doc = new CompoundDocument(stream);
                return GetStreamFromPackage(doc, encryption);
            }

            throw new InvalidDataException("The stream is not an valid/supported encrypted document.");
        }

        /// <summary>
        /// Encrypts a package
        /// </summary>
        /// <param name="package">The package as a byte array</param>
        /// <param name="encryption">The encryption info from the workbook</param>
        /// <returns></returns>
        internal MemoryStream EncryptPackage(byte[] package, ExcelEncryption encryption)
        {
            if (encryption.Version == EncryptionVersion.Standard) //Standard encryption
            {
                return EncryptPackageBinary(package, encryption);
            }

            if (encryption.Version == EncryptionVersion.Agile) //Agile encryption
            {
                return EncryptPackageAgile(package, encryption);
            }

            throw new ArgumentException("Unsupported encryption version.");
        }

        private MemoryStream EncryptPackageAgile(byte[] package, ExcelEncryption encryption)
        {
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n";
            xml += "<encryption xmlns=\"http://schemas.microsoft.com/office/2006/encryption\" xmlns:p=\"http://schemas.microsoft.com/office/2006/keyEncryptor/password\" xmlns:c=\"http://schemas.microsoft.com/office/2006/keyEncryptor/certificate\">";
            xml += "<keyData saltSize=\"16\" blockSize=\"16\" keyBits=\"256\" hashSize=\"64\" cipherAlgorithm=\"AES\" cipherChaining=\"ChainingModeCBC\" hashAlgorithm=\"SHA512\" saltValue=\"\"/>";
            xml += "<dataIntegrity encryptedHmacKey=\"\" encryptedHmacValue=\"\"/>";
            xml += "<keyEncryptors>";
            xml += "<keyEncryptor uri=\"http://schemas.microsoft.com/office/2006/keyEncryptor/password\">";
            xml += "<p:encryptedKey spinCount=\"100000\" saltSize=\"16\" blockSize=\"16\" keyBits=\"256\" hashSize=\"64\" cipherAlgorithm=\"AES\" cipherChaining=\"ChainingModeCBC\" hashAlgorithm=\"SHA512\" saltValue=\"\" encryptedVerifierHashInput=\"\" encryptedVerifierHashValue=\"\" encryptedKeyValue=\"\" />";
            xml += "</keyEncryptor></keyEncryptors></encryption>";

            var encryptionInfo = new EncryptionInfoAgile();
            encryptionInfo.ReadFromXml(xml);
            EncryptionInfoAgile.EncryptionKeyEncryptor encr = encryptionInfo.KeyEncryptors[0];
            var rnd = RandomNumberGenerator.Create();

            byte[] s = new byte[16];
            rnd.GetBytes(s);
            encryptionInfo.KeyData.SaltValue = s;

            rnd.GetBytes(s);
            encr.SaltValue = s;

            encr.KeyValue = new byte[encr.KeyBits / 8];
            rnd.GetBytes(encr.KeyValue);

            //Get the password key.
            HashAlgorithm hashProvider = GetHashProvider(encryptionInfo.KeyEncryptors[0]);
            byte[] baseHash = GetPasswordHash(hashProvider, encr.SaltValue, encryption.Password, encr.SpinCount, encr.HashSize);
            byte[] hashFinal = GetFinalHash(hashProvider, BlockKey_KeyValue, baseHash);
            hashFinal = FixHashSize(hashFinal, encr.KeyBits / 8);

            byte[] encrData = EncryptDataAgile(package, encryptionInfo, hashProvider);

            /**** Data Integrity ****/
            byte[] saltHMAC = new byte[64];
            rnd.GetBytes(saltHMAC);

            SetHMAC(encryptionInfo, hashProvider, saltHMAC, encrData);

            /**** Verifier ****/
            encr.VerifierHashInput = new byte[16];
            rnd.GetBytes(encr.VerifierHashInput);

            encr.VerifierHash = hashProvider.ComputeHash(encr.VerifierHashInput);

            byte[] VerifierInputKey = GetFinalHash(hashProvider, BlockKey_HashInput, baseHash);
            byte[] VerifierHashKey = GetFinalHash(hashProvider, BlockKey_HashValue, baseHash);
            byte[] KeyValueKey = GetFinalHash(hashProvider, BlockKey_KeyValue, baseHash);

            var ms = new MemoryStream();
            EncryptAgileFromKey(encr, VerifierInputKey, encr.VerifierHashInput, 0, encr.VerifierHashInput.Length, encr.SaltValue, ms);
            encr.EncryptedVerifierHashInput = ms.ToArray();

            ms = new MemoryStream();
            EncryptAgileFromKey(encr, VerifierHashKey, encr.VerifierHash, 0, encr.VerifierHash.Length, encr.SaltValue, ms);
            encr.EncryptedVerifierHash = ms.ToArray();

            ms = new MemoryStream();
            EncryptAgileFromKey(encr, KeyValueKey, encr.KeyValue, 0, encr.KeyValue.Length, encr.SaltValue, ms);
            encr.EncryptedKeyValue = ms.ToArray();

            xml = encryptionInfo.Xml.OuterXml;

            byte[] byXml = Encoding.UTF8.GetBytes(xml);

            ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes((ushort)4), 0, 2); //Major Version
            ms.Write(BitConverter.GetBytes((ushort)4), 0, 2); //Minor Version
            ms.Write(BitConverter.GetBytes((uint)0x40), 0, 4); //Reserved
            ms.Write(byXml, 0, byXml.Length);

            var doc = new CompoundDocument();

            //Add the dataspace streams
            CreateDataSpaces(doc);
            //EncryptionInfo...
            doc.Storage.DataStreams.Add("EncryptionInfo", ms.ToArray());
            //...and the encrypted package
            doc.Storage.DataStreams.Add("EncryptedPackage", encrData);

            ms = new MemoryStream();
            doc.Save(ms);
            //ms.Write(e,0,e.Length);
            return ms;
        }

        private byte[] EncryptDataAgile(byte[] data, EncryptionInfoAgile encryptionInfo, HashAlgorithm hashProvider)
        {
            EncryptionInfoAgile.EncryptionKeyEncryptor ke = encryptionInfo.KeyEncryptors[0];
#if Core
            var aes = Aes.Create();
#else
            RijndaelManaged aes = new RijndaelManaged();
#endif
            aes.KeySize = ke.KeyBits;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.Zeros;

            int pos = 0;
            int segment = 0;

            //Encrypt the data
            var ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes((ulong)data.Length), 0, 8);
            while (pos < data.Length)
            {
                int segmentSize = data.Length - pos > 4096 ? 4096 : data.Length - pos;

                byte[] ivTmp = new byte[4 + encryptionInfo.KeyData.SaltSize];
                Array.Copy(encryptionInfo.KeyData.SaltValue, 0, ivTmp, 0, encryptionInfo.KeyData.SaltSize);
                Array.Copy(BitConverter.GetBytes(segment), 0, ivTmp, encryptionInfo.KeyData.SaltSize, 4);
                byte[] iv = hashProvider.ComputeHash(ivTmp);

                EncryptAgileFromKey(ke, ke.KeyValue, data, pos, segmentSize, iv, ms);
                pos += segmentSize;
                segment++;
            }

            ms.Flush();
            return ms.ToArray();
        }

        // Set the dataintegrity
        private void SetHMAC(EncryptionInfoAgile ei, HashAlgorithm hashProvider, byte[] salt, byte[] data)
        {
            byte[] iv = GetFinalHash(hashProvider, BlockKey_HmacKey, ei.KeyData.SaltValue);
            var ms = new MemoryStream();
            EncryptAgileFromKey(ei.KeyEncryptors[0], ei.KeyEncryptors[0].KeyValue, salt, 0L, salt.Length, iv, ms);
            ei.DataIntegrity.EncryptedHmacKey = ms.ToArray();

            HMAC h = GetHmacProvider(ei.KeyEncryptors[0], salt);
            byte[] hmacValue = h.ComputeHash(data);

            ms = new MemoryStream();
            iv = GetFinalHash(hashProvider, BlockKey_HmacValue, ei.KeyData.SaltValue);
            EncryptAgileFromKey(ei.KeyEncryptors[0], ei.KeyEncryptors[0].KeyValue, hmacValue, 0L, hmacValue.Length, iv, ms);
            ei.DataIntegrity.EncryptedHmacValue = ms.ToArray();
        }

        private HMAC GetHmacProvider(EncryptionInfoAgile.EncryptionKeyData ei, byte[] salt)
        {
            switch (ei.HashAlgorithm)
            {
#if (!Core)
                case eHashAlogorithm.RIPEMD160:
                    return new HMACRIPEMD160(salt);
#endif
                case eHashAlogorithm.MD5:
                    return new HMACMD5(salt);
                case eHashAlogorithm.SHA1:
                    return new HMACSHA1(salt);
                case eHashAlogorithm.SHA256:
                    return new HMACSHA256(salt);
                case eHashAlogorithm.SHA384:
                    return new HMACSHA384(salt);
                case eHashAlogorithm.SHA512:
                    return new HMACSHA512(salt);
                default:
                    throw new NotSupportedException(string.Format("Hash method {0} not supported.", ei.HashAlgorithm));
            }
        }

        private MemoryStream EncryptPackageBinary(byte[] package, ExcelEncryption encryption)
        {
            //Create the Encryption Info. This also returns the Encryptionkey
            EncryptionInfoBinary encryptionInfo = CreateEncryptionInfo(encryption.Password,
                encryption.Algorithm == EncryptionAlgorithm.AES128 ? AlgorithmID.AES128 :
                encryption.Algorithm == EncryptionAlgorithm.AES192 ? AlgorithmID.AES192 :
                AlgorithmID.AES256, out byte[] encryptionKey);

            //ILockBytes lb;
            //var iret = CreateILockBytesOnHGlobal(IntPtr.Zero, true, out lb);

            //IStorage storage = null;
            //MemoryStream ret = null;

            var doc = new CompoundDocument();
            CreateDataSpaces(doc);

            doc.Storage.DataStreams.Add("EncryptionInfo", encryptionInfo.WriteBinary());

            //Encrypt the package
            byte[] encryptedPackage = EncryptData(encryptionKey, package, false);
            var ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes((ulong)package.Length), 0, 8);
            ms.Write(encryptedPackage, 0, encryptedPackage.Length);
            doc.Storage.DataStreams.Add("EncryptedPackage", ms.ToArray());

            var ret = new MemoryStream();
            doc.Save(ret);

            return ret;
        }

        #region "Dataspaces Stream methods"

        private void CreateDataSpaces(CompoundDocument doc)
        {
            var ds = new CompoundDocument.StoragePart();
            doc.Storage.SubStorage.Add("\x06" + "DataSpaces", ds);
            ds.DataStreams.Add("Version", CreateVersionStream());
            ds.DataStreams.Add("DataSpaceMap", CreateDataSpaceMap());

            var dsInfo = new CompoundDocument.StoragePart();
            ds.SubStorage.Add("DataSpaceInfo", dsInfo);
            dsInfo.DataStreams.Add("StrongEncryptionDataSpace", CreateStrongEncryptionDataSpaceStream());

            var transInfo = new CompoundDocument.StoragePart();
            ds.SubStorage.Add("TransformInfo", transInfo);

            var strEncTrans = new CompoundDocument.StoragePart();
            transInfo.SubStorage.Add("StrongEncryptionTransform", strEncTrans);

            strEncTrans.DataStreams.Add("\x06Primary", CreateTransformInfoPrimary());
        }

        private byte[] CreateStrongEncryptionDataSpaceStream()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write(8); //HeaderLength
            bw.Write(1); //EntryCount

            string tr = "StrongEncryptionTransform";
            bw.Write(tr.Length * 2);
            bw.Write(Encoding.Unicode.GetBytes(tr + "\0")); // end \0 is for padding

            bw.Flush();
            return ms.ToArray();
        }

        private byte[] CreateVersionStream()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((short)0x3C); //Major
            bw.Write((short)0); //Minor
            bw.Write(Encoding.Unicode.GetBytes("Microsoft.Container.DataSpaces"));
            bw.Write(1); //ReaderVersion
            bw.Write(1); //UpdaterVersion
            bw.Write(1); //WriterVersion

            bw.Flush();
            return ms.ToArray();
        }

        private byte[] CreateDataSpaceMap()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write(8); //HeaderLength
            bw.Write(1); //EntryCount
            string s1 = "EncryptedPackage";
            string s2 = "StrongEncryptionDataSpace";
            bw.Write((s1.Length + s2.Length) * 2 + 0x16);
            bw.Write(1); //ReferenceComponentCount
            bw.Write(0); //Stream=0
            bw.Write(s1.Length * 2); //Length s1
            bw.Write(Encoding.Unicode.GetBytes(s1));
            bw.Write(s2.Length * 2); //Length s2
            bw.Write(Encoding.Unicode.GetBytes(s2 + "\0")); // end \0 is for padding

            bw.Flush();
            return ms.ToArray();
        }

        private byte[] CreateTransformInfoPrimary()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            string TransformID = "{FF9A3F03-56EF-4613-BDD5-5A41C1D07246}";
            string TransformName = "Microsoft.Container.EncryptionTransform";
            bw.Write(TransformID.Length * 2 + 12);
            bw.Write(1);
            bw.Write(TransformID.Length * 2);
            bw.Write(Encoding.Unicode.GetBytes(TransformID));
            bw.Write(TransformName.Length * 2);
            bw.Write(Encoding.Unicode.GetBytes(TransformName + "\0"));
            bw.Write(1); //ReaderVersion
            bw.Write(1); //UpdaterVersion
            bw.Write(1); //WriterVersion

            bw.Write(0);
            bw.Write(0);
            bw.Write(0); //CipherMode
            bw.Write(4); //Reserved

            bw.Flush();
            return ms.ToArray();
        }

        #endregion

        /// <summary>
        /// Create an EncryptionInfo object to encrypt a workbook
        /// </summary>
        /// <param name="password">The password</param>
        /// <param name="algID"></param>
        /// <param name="key">The Encryption key</param>
        /// <returns></returns>
        private EncryptionInfoBinary CreateEncryptionInfo(string password, AlgorithmID algID, out byte[] key)
        {
            if (algID is AlgorithmID.Flags or AlgorithmID.RC4)
            {
                throw new ArgumentException("algID must be AES128, AES192 or AES256");
            }

            var encryptionInfo = new EncryptionInfoBinary();
            encryptionInfo.MajorVersion = 4;
            encryptionInfo.MinorVersion = 2;
            encryptionInfo.Flags = Flags.fAES | Flags.fCryptoAPI;

            //Header
            encryptionInfo.Header = new EncryptionHeader();
            encryptionInfo.Header.AlgID = algID;
            encryptionInfo.Header.AlgIDHash = AlgorithmHashID.SHA1;
            encryptionInfo.Header.Flags = encryptionInfo.Flags;
            encryptionInfo.Header.KeySize =
                algID == AlgorithmID.AES128 ? 0x80 : algID == AlgorithmID.AES192 ? 0xC0 : 0x100;
            encryptionInfo.Header.ProviderType = ProviderType.AES;
            encryptionInfo.Header.CSPName = "Microsoft Enhanced RSA and AES Cryptographic Provider\0";
            encryptionInfo.Header.Reserved1 = 0;
            encryptionInfo.Header.Reserved2 = 0;
            encryptionInfo.Header.SizeExtra = 0;

            //Verifier
            encryptionInfo.Verifier = new EncryptionVerifier();
            encryptionInfo.Verifier.Salt = new byte[16];

            var rnd = RandomNumberGenerator.Create();
            rnd.GetBytes(encryptionInfo.Verifier.Salt);
            encryptionInfo.Verifier.SaltSize = 0x10;

            key = GetPasswordHashBinary(password, encryptionInfo);

            byte[] verifier = new byte[16];
            rnd.GetBytes(verifier);
            encryptionInfo.Verifier.EncryptedVerifier = EncryptData(key, verifier, true);

            //AES = 32 Bits
            encryptionInfo.Verifier.VerifierHashSize = 0x20;
#if (Core)
            var sha = SHA1.Create();
#else
            var sha = new SHA1Managed();
#endif
            byte[] verifierHash = sha.ComputeHash(verifier);

            encryptionInfo.Verifier.EncryptedVerifierHash = EncryptData(key, verifierHash, false);

            return encryptionInfo;
        }

        private byte[] EncryptData(byte[] key, byte[] data, bool useDataSize)
        {
#if (Core)
            var aes = Aes.Create();
#else
            RijndaelManaged aes = new RijndaelManaged();
#endif
            aes.KeySize = key.Length * 8;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.Zeros;

            //Encrypt the data
            ICryptoTransform crypt = aes.CreateEncryptor(key, null);
            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, crypt, CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);

            cs.FlushFinalBlock();

            byte[] ret;
            if (useDataSize)
            {
                ret = new byte[data.Length];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(ret, 0, data.Length); //Truncate any padded Zeros
                return ret;
            }

            return ms.ToArray();
        }

        private MemoryStream GetStreamFromPackage(CompoundDocument doc, ExcelEncryption encryption)
        {
            if (doc.Storage.DataStreams.ContainsKey("EncryptionInfo") ||
                doc.Storage.DataStreams.ContainsKey("EncryptedPackage"))
            {
                EncryptionInfo encryptionInfo = EncryptionInfo.ReadBinary(doc.Storage.DataStreams["EncryptionInfo"]);

                return DecryptDocument(doc.Storage.DataStreams["EncryptedPackage"], encryptionInfo, encryption.Password);
            }

            throw new InvalidDataException("Invalid document. EncryptionInfo or EncryptedPackage stream is missing");
        }

        /// <summary>
        /// Decrypt a document
        /// </summary>
        /// <param name="data">The Encrypted data</param>
        /// <param name="encryptionInfo">Encryption Info object</param>
        /// <param name="password">The password</param>
        /// <returns></returns>
        private MemoryStream DecryptDocument(byte[] data, EncryptionInfo encryptionInfo, string password)
        {
            long size = BitConverter.ToInt64(data, 0);

            byte[] encryptedData = new byte[data.Length - 8];
            Array.Copy(data, 8, encryptedData, 0, encryptedData.Length);

            if (encryptionInfo is EncryptionInfoBinary)
            {
                return DecryptBinary((EncryptionInfoBinary)encryptionInfo, password, size, encryptedData);
            }

            return DecryptAgile((EncryptionInfoAgile)encryptionInfo, password, size, encryptedData, data);
        }

        readonly byte[] BlockKey_HashInput = { 0xfe, 0xa7, 0xd2, 0x76, 0x3b, 0x4b, 0x9e, 0x79 };
        readonly byte[] BlockKey_HashValue = { 0xd7, 0xaa, 0x0f, 0x6d, 0x30, 0x61, 0x34, 0x4e };
        readonly byte[] BlockKey_KeyValue = { 0x14, 0x6e, 0x0b, 0xe7, 0xab, 0xac, 0xd0, 0xd6 };
        readonly byte[] BlockKey_HmacKey = { 0x5f, 0xb2, 0xad, 0x01, 0x0c, 0xb9, 0xe1, 0xf6 }; //MSOFFCRYPTO 2.3.4.14 section 3
        readonly byte[] BlockKey_HmacValue = { 0xa0, 0x67, 0x7f, 0x02, 0xb2, 0x2c, 0x84, 0x33 }; //MSOFFCRYPTO 2.3.4.14 section 5

        private MemoryStream DecryptAgile(EncryptionInfoAgile encryptionInfo, string password, long size, byte[] encryptedData, byte[] data)
        {
            var doc = new MemoryStream();

            if (encryptionInfo.KeyData.CipherAlgorithm == eCipherAlgorithm.AES)
            {
                EncryptionInfoAgile.EncryptionKeyEncryptor encr = encryptionInfo.KeyEncryptors[0];
                HashAlgorithm hashProvider = GetHashProvider(encr);
                HashAlgorithm hashProviderDataKey = GetHashProvider(encryptionInfo.KeyData);

                byte[] baseHash = GetPasswordHash(hashProvider, encr.SaltValue, password, encr.SpinCount, encr.HashSize);

                //Get the keys for the verifiers and the key value
                byte[] valInputKey = GetFinalHash(hashProvider, BlockKey_HashInput, baseHash);
                byte[] valHashKey = GetFinalHash(hashProvider, BlockKey_HashValue, baseHash);
                byte[] valKeySizeKey = GetFinalHash(hashProvider, BlockKey_KeyValue, baseHash);

                //Decrypt
                encr.VerifierHashInput = DecryptAgileFromKey(encr, valInputKey, encr.EncryptedVerifierHashInput, encr.SaltSize, encr.SaltValue);
                encr.VerifierHash = DecryptAgileFromKey(encr, valHashKey, encr.EncryptedVerifierHash, encr.HashSize, encr.SaltValue);
                encr.KeyValue = DecryptAgileFromKey(encr, valKeySizeKey, encr.EncryptedKeyValue, encryptionInfo.KeyData.KeyBits / 8, encr.SaltValue);

                if (IsPasswordValid(hashProvider, encr))
                {
                    byte[] ivhmac = GetFinalHash(hashProviderDataKey, BlockKey_HmacKey, encryptionInfo.KeyData.SaltValue);
                    byte[] key = DecryptAgileFromKey(encryptionInfo.KeyData, encr.KeyValue, encryptionInfo.DataIntegrity.EncryptedHmacKey, encryptionInfo.KeyData.HashSize, ivhmac);

                    ivhmac = GetFinalHash(hashProviderDataKey, BlockKey_HmacValue, encryptionInfo.KeyData.SaltValue);
                    byte[] value = DecryptAgileFromKey(encryptionInfo.KeyData, encr.KeyValue, encryptionInfo.DataIntegrity.EncryptedHmacValue, encryptionInfo.KeyData.HashSize, ivhmac);

                    HMAC hmca = GetHmacProvider(encryptionInfo.KeyData, key);
                    byte[] v2 = hmca.ComputeHash(data);

                    for (int i = 0; i < v2.Length; i++)
                    {
                        if (value[i] != v2[i])
                        {
                            throw new Exception("Dataintegrity key mismatch");
                        }
                    }

                    int pos = 0;
                    uint segment = 0;
                    while (pos < size)
                    {
                        int segmentSize = (int)(size - pos > 4096 ? 4096 : size - pos);
                        int bufferSize = encryptedData.Length - pos > 4096 ? 4096 : encryptedData.Length - pos;
                        byte[] ivTmp = new byte[4 + encryptionInfo.KeyData.SaltSize];
                        Array.Copy(encryptionInfo.KeyData.SaltValue, 0, ivTmp, 0, encryptionInfo.KeyData.SaltSize);
                        Array.Copy(BitConverter.GetBytes(segment), 0, ivTmp, encryptionInfo.KeyData.SaltSize, 4);
                        byte[] iv = hashProviderDataKey.ComputeHash(ivTmp);
                        byte[] buffer = new byte[bufferSize];
                        Array.Copy(encryptedData, pos, buffer, 0, bufferSize);

                        byte[] b = DecryptAgileFromKey(encryptionInfo.KeyData, encr.KeyValue, buffer, segmentSize, iv);
                        doc.Write(b, 0, b.Length);
                        pos += segmentSize;
                        segment++;
                    }

                    doc.Flush();
                    return doc;
                }

                throw new SecurityException("Invalid password");
            }

            return null;
        }
#if Core
        private HashAlgorithm GetHashProvider(EncryptionInfoAgile.EncryptionKeyData encr)
        {
            switch (encr.HashAlgorithm)
            {
                case eHashAlogorithm.MD5:
                    return MD5.Create();
                //case eHashAlogorithm.RIPEMD160:
                //    return new RIPEMD160Managed();                    
                case eHashAlogorithm.SHA1:
                    return SHA1.Create();
                case eHashAlogorithm.SHA256:
                    return SHA256.Create();
                case eHashAlogorithm.SHA384:
                    return SHA384.Create();
                case eHashAlogorithm.SHA512:
                    return SHA512.Create();
                default:
                    throw new NotSupportedException(string.Format("Hash provider is unsupported. {0}", encr.HashAlgorithm));
            }
        }
#else
        private HashAlgorithm GetHashProvider(EncryptionInfoAgile.EncryptionKeyData encr)
        {
            switch (encr.HashAlgorithm)
            {
                case eHashAlogorithm.MD5:
                        return new MD5CryptoServiceProvider();
                case eHashAlogorithm.RIPEMD160:
                        return new RIPEMD160Managed();
                case eHashAlogorithm.SHA1:
                        return new SHA1CryptoServiceProvider();
                case eHashAlogorithm.SHA256:
                        return  new SHA256CryptoServiceProvider();
                case eHashAlogorithm.SHA384:
                        return new SHA384CryptoServiceProvider();
                case eHashAlogorithm.SHA512:
                        return new SHA512CryptoServiceProvider();
                default:
                        throw new NotSupportedException(string.Format("Hash provider is unsupported. {0}", encr.HashAlgorithm));
            }
        }
#endif
        private MemoryStream DecryptBinary(EncryptionInfoBinary encryptionInfo, string password, long size, byte[] encryptedData)
        {
            var doc = new MemoryStream();

            if (encryptionInfo.Header.AlgID == AlgorithmID.AES128 || (encryptionInfo.Header.AlgID == AlgorithmID.Flags && (encryptionInfo.Flags & (Flags.fAES | Flags.fExternal | Flags.fCryptoAPI)) == (Flags.fAES | Flags.fCryptoAPI))
                                                                  ||
                                                                  encryptionInfo.Header.AlgID == AlgorithmID.AES192
                                                                  ||
                                                                  encryptionInfo.Header.AlgID == AlgorithmID.AES256
               )
            {
#if (Core)
                var decryptKey = Aes.Create();
#else
                RijndaelManaged decryptKey = new RijndaelManaged();
#endif
                decryptKey.KeySize = encryptionInfo.Header.KeySize;
                decryptKey.Mode = CipherMode.ECB;
                decryptKey.Padding = PaddingMode.None;

                byte[] key = GetPasswordHashBinary(password, encryptionInfo);
                if (IsPasswordValid(key, encryptionInfo))
                {
                    ICryptoTransform decryptor = decryptKey.CreateDecryptor(
                        key,
                        null);

                    var dataStream = new MemoryStream(encryptedData);
                    var cryptoStream = new CryptoStream(dataStream,
                        decryptor,
                        CryptoStreamMode.Read);

                    byte[] decryptedData = new byte[size];

                    cryptoStream.Read(decryptedData, 0, (int)size);
                    doc.Write(decryptedData, 0, (int)size);
                }
                else
                {
                    throw new UnauthorizedAccessException("Invalid password");
                }
            }

            return doc;
        }

        /// <summary>
        /// Validate the password
        /// </summary>
        /// <param name="key">The encryption key</param>
        /// <param name="encryptionInfo">The encryption info extracted from the ENCRYPTIOINFO stream inside the OLE document</param>
        /// <returns></returns>
        private bool IsPasswordValid(byte[] key, EncryptionInfoBinary encryptionInfo)
        {
#if (Core)
            var decryptKey = Aes.Create();
#else
                RijndaelManaged decryptKey = new RijndaelManaged();
#endif
            decryptKey.KeySize = encryptionInfo.Header.KeySize;
            decryptKey.Mode = CipherMode.ECB;
            decryptKey.Padding = PaddingMode.None;

            ICryptoTransform decryptor = decryptKey.CreateDecryptor(
                key,
                null);


            //Decrypt the verifier
            var dataStream = new MemoryStream(encryptionInfo.Verifier.EncryptedVerifier);
            var cryptoStream = new CryptoStream(dataStream,
                decryptor,
                CryptoStreamMode.Read);
            byte[] decryptedVerifier = new byte[16];
            cryptoStream.Read(decryptedVerifier, 0, 16);

            dataStream = new MemoryStream(encryptionInfo.Verifier.EncryptedVerifierHash);

            cryptoStream = new CryptoStream(dataStream,
                decryptor,
                CryptoStreamMode.Read);

            //Decrypt the verifier hash
            byte[] decryptedVerifierHash = new byte[16];
            cryptoStream.Read(decryptedVerifierHash, 0, 16);

            //Get the hash for the decrypted verifier
#if (Core)
            var sha = SHA1.Create();
#else
            var sha = new SHA1Managed();
#endif
            byte[] hash = sha.ComputeHash(decryptedVerifier);

            //Equal?
            for (int i = 0; i < 16; i++)
            {
                if (hash[i] != decryptedVerifierHash[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate the password
        /// </summary>
        /// <param name="sha">The hash algorithm</param>
        /// <param name="encr">The encryption info extracted from the ENCRYPTIOINFO stream inside the OLE document</param>
        /// <returns></returns>
        private bool IsPasswordValid(HashAlgorithm sha, EncryptionInfoAgile.EncryptionKeyEncryptor encr)
        {
            byte[] valHash = sha.ComputeHash(encr.VerifierHashInput);

            //Equal?
            for (int i = 0; i < valHash.Length; i++)
            {
                if (encr.VerifierHash[i] != valHash[i])
                {
                    return false;
                }
            }

            return true;
        }

        private byte[] DecryptAgileFromKey(EncryptionInfoAgile.EncryptionKeyData encr, byte[] key, byte[] encryptedData, long size, byte[] iv)
        {
            SymmetricAlgorithm decryptKey = GetEncryptionAlgorithm(encr);
            decryptKey.BlockSize = encr.BlockSize << 3;
            decryptKey.KeySize = encr.KeyBits;
#if (Core)
            decryptKey.Mode = CipherMode.CBC;
#else
            decryptKey.Mode = encr.CipherChaining == eChainingMode.ChainingModeCBC ? CipherMode.CBC : CipherMode.CFB;
#endif
            decryptKey.Padding = PaddingMode.Zeros;

            ICryptoTransform decryptor = decryptKey.CreateDecryptor(
                FixHashSize(key, encr.KeyBits / 8),
                FixHashSize(iv, encr.BlockSize, 0x36));


            var dataStream = new MemoryStream(encryptedData);

            var cryptoStream = new CryptoStream(dataStream,
                decryptor,
                CryptoStreamMode.Read);

            byte[] decryptedData = new byte[size];

            cryptoStream.Read(decryptedData, 0, (int)size);
            return decryptedData;
        }

#if (Core)
        private SymmetricAlgorithm GetEncryptionAlgorithm(EncryptionInfoAgile.EncryptionKeyData encr)
        {
            switch (encr.CipherAlgorithm)
            {
                case eCipherAlgorithm.AES:
                    return Aes.Create();
                //case eCipherAlgorithm.DES:
                //    return new DESCryptoServiceProvider();
                case eCipherAlgorithm.TRIPLE_DES:
                case eCipherAlgorithm.TRIPLE_DES_112:
                    return TripleDES.Create();
                //case eCipherAlgorithm.RC2:
                //    return new RC2CryptoServiceProvider();                    
                default:
                    throw new NotSupportedException(string.Format("Unsupported Cipher Algorithm: {0}", encr.CipherAlgorithm.ToString()));
            }
        }
#else
        private SymmetricAlgorithm GetEncryptionAlgorithm(EncryptionInfoAgile.EncryptionKeyData encr)
        {
            switch (encr.CipherAlgorithm)
            {
                case eCipherAlgorithm.AES:
                    return new RijndaelManaged();
                case eCipherAlgorithm.DES:
                    return new DESCryptoServiceProvider();
                case eCipherAlgorithm.TRIPLE_DES:
                case eCipherAlgorithm.TRIPLE_DES_112:
                    return new TripleDESCryptoServiceProvider();
                case eCipherAlgorithm.RC2:
                    return new RC2CryptoServiceProvider();
                default:
                    throw(new NotSupportedException(string.Format("Unsupported Cipher Algorithm: {0}", encr.CipherAlgorithm.ToString())));
            }
        }
#endif
        private void EncryptAgileFromKey(EncryptionInfoAgile.EncryptionKeyEncryptor encr, byte[] key, byte[] data, long pos, long size, byte[] iv, MemoryStream ms)
        {
            SymmetricAlgorithm encryptKey = GetEncryptionAlgorithm(encr);
            encryptKey.BlockSize = encr.BlockSize << 3;
            encryptKey.KeySize = encr.KeyBits;
#if (Core)
            encryptKey.Mode = CipherMode.CBC;
#else
            encryptKey.Mode = encr.CipherChaining==eChainingMode.ChainingModeCBC ? CipherMode.CBC : CipherMode.CFB;
#endif
            encryptKey.Padding = PaddingMode.Zeros;

            ICryptoTransform encryptor = encryptKey.CreateEncryptor(
                FixHashSize(key, encr.KeyBits / 8),
                FixHashSize(iv, 16, 0x36));


            var cryptoStream = new CryptoStream(ms,
                encryptor,
                CryptoStreamMode.Write);

            byte[] buffer = new byte[size];
            Array.Copy(data, (int)pos, buffer, 0, (int)size);
            cryptoStream.Write(buffer, 0, (int)size);
            while (size % encr.BlockSize != 0)
            {
                cryptoStream.WriteByte(0);
                size++;
            }
        }

        /// <summary>
        /// Create the hash.
        /// This method is written with the help of Lyquidity library, many thanks for this nice sample
        /// </summary>
        /// <param name="password">The password</param>
        /// <param name="encryptionInfo">The encryption info extracted from the ENCRYPTIOINFO stream inside the OLE document</param>
        /// <returns>The hash to encrypt the document</returns>
        private byte[] GetPasswordHashBinary(string password, EncryptionInfoBinary encryptionInfo)
        {
            byte[] hash = null;
            byte[] tempHash = new byte[4 + 20]; //Iterator + prev. hash
            try
            {
                HashAlgorithm hashProvider;
                if (encryptionInfo.Header.AlgIDHash == AlgorithmHashID.SHA1 || encryptionInfo.Header.AlgIDHash == AlgorithmHashID.App && (encryptionInfo.Flags & Flags.fExternal) == 0)
                {
#if (Core)
                    hashProvider = SHA1.Create();
#else
                    hashProvider = new SHA1CryptoServiceProvider();
#endif
                }
                else if (encryptionInfo.Header.KeySize is > 0 and < 80)
                {
                    throw new NotSupportedException("RC4 Hash provider is not supported. Must be SHA1(AlgIDHash == 0x8004)");
                }
                else
                {
                    throw new NotSupportedException("Hash provider is invalid. Must be SHA1(AlgIDHash == 0x8004)");
                }

                hash = GetPasswordHash(hashProvider, encryptionInfo.Verifier.Salt, password, 50000, 20);

                // Append "block" (0)
                Array.Copy(hash, tempHash, hash.Length);
                Array.Copy(BitConverter.GetBytes(0), 0, tempHash, hash.Length, 4);
                hash = hashProvider.ComputeHash(tempHash);

                /***** Now use the derived key algorithm *****/
                byte[] derivedKey = new byte[64];
                int keySizeBytes = encryptionInfo.Header.KeySize / 8;

                //First XOR hash bytes with 0x36 and fill the rest with 0x36
                for (int i = 0; i < derivedKey.Length; i++)
                    derivedKey[i] = (byte)(i < hash.Length ? 0x36 ^ hash[i] : 0x36);


                byte[] X1 = hashProvider.ComputeHash(derivedKey);

                //if verifier size is bigger than the key size we can return X1
                if ((int)encryptionInfo.Verifier.VerifierHashSize > keySizeBytes)
                    return FixHashSize(X1, keySizeBytes);

                //Else XOR hash bytes with 0x5C and fill the rest with 0x5C
                for (int i = 0; i < derivedKey.Length; i++)
                    derivedKey[i] = (byte)(i < hash.Length ? 0x5C ^ hash[i] : 0x5C);

                byte[] X2 = hashProvider.ComputeHash(derivedKey);

                //Join the two and return 
                byte[] join = new byte[X1.Length + X2.Length];

                Array.Copy(X1, 0, join, 0, X1.Length);
                Array.Copy(X2, 0, join, X1.Length, X2.Length);


                return FixHashSize(join, keySizeBytes);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occured when the encryptionkey was created", ex);
            }
        }

        /// <summary>
        /// Create the hash.
        /// This method is written with the help of Lyquidity library, many thanks for this nice sample
        /// </summary>
        /// <param name="password">The password</param>
        /// <param name="encr">The encryption info extracted from the ENCRYPTIOINFO stream inside the OLE document</param>
        /// <param name="blockKey">The block key appended to the hash to obtain the final hash</param>
        /// <returns>The hash to encrypt the document</returns>
        private byte[] GetPasswordHashAgile(string password, EncryptionInfoAgile.EncryptionKeyEncryptor encr, byte[] blockKey)
        {
            try
            {
                HashAlgorithm hashProvider = GetHashProvider(encr);
                byte[] hash = GetPasswordHash(hashProvider, encr.SaltValue, password, encr.SpinCount, encr.HashSize);
                byte[] hashFinal = GetFinalHash(hashProvider, blockKey, hash);

                return FixHashSize(hashFinal, encr.KeyBits / 8);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occured when the encryptionkey was created", ex);
            }
        }

        private byte[] GetFinalHash(HashAlgorithm hashProvider, byte[] blockKey, byte[] hash)
        {
            //2.3.4.13 MS-OFFCRYPTO
            byte[] tempHash = new byte[hash.Length + blockKey.Length];
            Array.Copy(hash, tempHash, hash.Length);
            Array.Copy(blockKey, 0, tempHash, hash.Length, blockKey.Length);
            byte[] hashFinal = hashProvider.ComputeHash(tempHash);
            return hashFinal;
        }

        private byte[] GetPasswordHash(HashAlgorithm hashProvider, byte[] salt, string password, int spinCount, int hashSize)
        {
            byte[] hash = null;
            byte[] tempHash = new byte[4 + hashSize]; //Iterator + prev. hash
            hash = hashProvider.ComputeHash(CombinePassword(salt, password));

            //Iterate "spinCount" times, inserting i in first 4 bytes and then the prev. hash in byte 5-24
            for (int i = 0; i < spinCount; i++)
            {
                Array.Copy(BitConverter.GetBytes(i), tempHash, 4);
                Array.Copy(hash, 0, tempHash, 4, hash.Length);

                hash = hashProvider.ComputeHash(tempHash);
            }

            return hash;
        }

        private byte[] FixHashSize(byte[] hash, int size, byte fill = 0)
        {
            if (hash.Length == size)
                return hash;
            if (hash.Length < size)
            {
                byte[] buff = new byte[size];
                Array.Copy(hash, buff, hash.Length);
                for (int i = hash.Length; i < size; i++)
                {
                    buff[i] = fill;
                }

                return buff;
            }
            else
            {
                byte[] buff = new byte[size];
                Array.Copy(hash, buff, size);
                return buff;
            }
        }

        private byte[] CombinePassword(byte[] salt, string password)
        {
            if (password == "")
            {
                password = "VelvetSweatshop"; //Used if Password is blank
            }

            // Convert password to unicode...
            byte[] passwordBuf = Encoding.Unicode.GetBytes(password);

            byte[] inputBuf = new byte[salt.Length + passwordBuf.Length];
            Array.Copy(salt, inputBuf, salt.Length);
            Array.Copy(passwordBuf, 0, inputBuf, salt.Length, passwordBuf.Length);
            return inputBuf;
        }

        internal static ushort CalculatePasswordHash(string Password)
        {
            //Calculate the hash
            //Thanks to Kohei Yoshida for the sample http://kohei.us/2008/01/18/excel-sheet-protection-password-hash/
            ushort hash = 0;
            for (int i = Password.Length - 1; i >= 0; i--)
            {
                hash ^= Password[i];
                hash = (ushort)((ushort)((hash >> 14) & 0x01)
                                |
                                (ushort)((hash << 1) & 0x7FFF)); //Shift 1 to the left. Overflowing bit 15 goes into bit 0
            }

            hash ^= 0x8000 | ('N' << 8) | 'K'; //Xor NK with high bit set(0xCE4B)
            hash ^= (ushort)Password.Length;

            return hash;
        }
    }
}