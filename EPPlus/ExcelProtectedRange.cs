﻿using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml
{
    /// <summary>
    /// Algorithm for password hash
    /// </summary>
    internal enum eProtectedRangeAlgorithm
    {
        /// <summary>
        /// Specifies that the MD2 algorithm, as defined by RFC 1319, shall be used.
        /// </summary>
        MD2,

        /// <summary>
        /// Specifies that the MD4 algorithm, as defined by RFC 1319, shall be used.
        /// </summary>
        MD4,

        /// <summary>
        /// Specifies that the MD5 algorithm, as defined by RFC 1319, shall be used.
        /// </summary>
        MD5,

        /// <summary>
        /// Specifies that the RIPEMD-128 algorithm, as defined by RFC 1319, shall be used.
        /// </summary>
        RIPEMD128,

        /// <summary>
        /// Specifies that the RIPEMD-160 algorithm, as defined by ISO/IEC10118-3:2004 shall be used.
        /// </summary>
        RIPEMD160,

        /// <summary>
        /// Specifies that the SHA-1 algorithm, as defined by ISO/IEC 10118-3:2004 shall be used.
        /// </summary>
        SHA1,

        /// <summary>
        /// Specifies that the SHA-256 algorithm, as defined by ISO/IEC10118-3:2004 shall be used.
        /// </summary>
        SHA256,

        /// <summary>
        /// Specifies that the SHA-384 algorithm, as defined by ISO/IEC 10118-3:2004 shall be used.
        /// </summary>
        SHA384,

        /// <summary>
        /// Specifies that the SHA-512 algorithm, as defined by ISO/IEC10118-3:2004 shall be used.
        /// </summary>
        SHA512,

        /// <summary>
        /// Specifies that the WHIRLPOOL algorithm, as defined by ISO/IEC 10118-3:2004 shall be used.
        /// </summary>
        WHIRLPOOL
    }

    public class ExcelProtectedRange : XmlHelper
    {
        ExcelAddress _address;

        internal ExcelProtectedRange(string name, ExcelAddress address, XmlNamespaceManager ns, XmlNode topNode) :
            base(ns, topNode)
        {
            Name = name;
            Address = address;
        }

        public string Name
        {
            get => GetXmlNodeString("@name");
            set => SetXmlNodeString("@name", value);
        }

        public ExcelAddress Address
        {
            get
            {
                if (_address == null)
                {
                    _address = new ExcelAddress(GetXmlNodeString("@sqref"));
                }

                return _address;
            }
            set
            {
                SetXmlNodeString("@sqref", SqRefUtility.ToSqRefAddress(value.Address));
                _address = value;
            }
        }

        public string SecurityDescriptor
        {
            get => GetXmlNodeString("@securityDescriptor");
            set => SetXmlNodeString("@securityDescriptor", value);
        }

        internal int SpinCount
        {
            get => GetXmlNodeInt("@spinCount");
            set => SetXmlNodeString("@spinCount", value.ToString(CultureInfo.InvariantCulture));
        }

        internal string Salt
        {
            get => GetXmlNodeString("@saltValue");
            set => SetXmlNodeString("@saltValue", value);
        }

        internal string Hash
        {
            get => GetXmlNodeString("@hashValue");
            set => SetXmlNodeString("@hashValue", value);
        }

        internal eProtectedRangeAlgorithm Algorithm
        {
            get
            {
                string v = GetXmlNodeString("@algorithmName");
                return (eProtectedRangeAlgorithm)Enum.Parse(typeof(eProtectedRangeAlgorithm), v.Replace("-", ""));
            }
            set
            {
                string v = value.ToString();
                if (v.StartsWith("SHA"))
                {
                    v = v.Insert(3, "-");
                }
                else if (v.StartsWith("RIPEMD"))
                {
                    v = v.Insert(6, "-");
                }

                SetXmlNodeString("@algorithmName", v);
            }
        }

        /// <summary>
        /// Sets the password for the range
        /// </summary>
        /// <param name="password"></param>
        public void SetPassword(string password)
        {
            byte[] byPwd = Encoding.Unicode.GetBytes(password);
            var rnd = RandomNumberGenerator.Create();
            byte[] bySalt = new byte[16];
            rnd.GetBytes(bySalt);

            //Default SHA512 and 10000 spins
            Algorithm = eProtectedRangeAlgorithm.SHA512;
            SpinCount = SpinCount < 100000 ? 100000 : SpinCount;

            //Combine salt and password and calculate the initial hash
#if Core
            var hp = SHA512.Create();
#else
            var hp = new SHA512CryptoServiceProvider();
#endif
            byte[] buffer = new byte[byPwd.Length + bySalt.Length];
            Array.Copy(bySalt, buffer, bySalt.Length);
            Array.Copy(byPwd, 0, buffer, 16, byPwd.Length);
            byte[] hash = hp.ComputeHash(buffer);

            //Now iterate the number of spinns.
            for (int i = 0; i < SpinCount; i++)
            {
                buffer = new byte[hash.Length + 4];
                Array.Copy(hash, buffer, hash.Length);
                Array.Copy(BitConverter.GetBytes(i), 0, buffer, hash.Length, 4);
                hash = hp.ComputeHash(buffer);
            }

            Salt = Convert.ToBase64String(bySalt);
            Hash = Convert.ToBase64String(hash);
        }
    }
}