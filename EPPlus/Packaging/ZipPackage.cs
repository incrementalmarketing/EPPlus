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
 *******************************************************************************
 * Jan Källman		Added		25-Oct-2012
 *******************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Ionic.Zip;
using OfficeOpenXml.Packaging.Ionic.Zip;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.Packaging
{
    /// <summary>
    /// Specifies whether the target is inside or outside the System.IO.Packaging.Package.
    /// </summary>
    public enum TargetMode
    {
        /// <summary>
        /// The relationship references a part that is inside the package.
        /// </summary>
        Internal = 0,

        /// <summary>
        /// The relationship references a resource that is external to the package.
        /// </summary>
        External = 1,
    }

    /// <summary>
    /// Represent an OOXML Zip package.
    /// </summary>
    public class ZipPackage : ZipPackageRelationshipBase
    {
        CompressionLevel _compression = CompressionLevel.Default;
        internal Dictionary<string, ContentType> _contentTypes = new(StringComparer.OrdinalIgnoreCase);
        internal char _dirSeparator = '/';
        readonly Dictionary<string, ZipPackagePart> Parts = new(StringComparer.OrdinalIgnoreCase);

        internal ZipPackage()
        {
            AddNew();
        }

        internal ZipPackage(Stream stream)
        {
            bool hasContentTypeXml = false;
            if (stream == null || stream.Length == 0)
            {
                AddNew();
            }
            else
            {
                var rels = new Dictionary<string, string>();
                stream.Seek(0, SeekOrigin.Begin);
                using var zip = new ZipInputStream(stream);
                ZipEntry e = zip.GetNextEntry();
                if (e == null)
                {
                    throw new InvalidDataException("The file is not an valid Package file. If the file is encrypted, please supply the password in the constructor.");
                }

                if (e.FileName.Contains("\\"))
                {
                    _dirSeparator = '\\';
                }
                else
                {
                    _dirSeparator = '/';
                }

                while (e != null)
                {
                    if (e.UncompressedSize > 0)
                    {
                        byte[] b = new byte[e.UncompressedSize];
                        zip.Read(b, 0, (int)e.UncompressedSize);
                        if (e.FileName.Equals("[content_types].xml", StringComparison.OrdinalIgnoreCase))
                        {
                            AddContentTypes(Encoding.UTF8.GetString(b));
                            hasContentTypeXml = true;
                        }
                        else if (e.FileName.Equals($"_rels{_dirSeparator}.rels", StringComparison.OrdinalIgnoreCase))
                        {
                            ReadRelation(Encoding.UTF8.GetString(b), "");
                        }
                        else
                        {
                            if (e.FileName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
                            {
                                rels.Add(GetUriKey(e.FileName), Encoding.UTF8.GetString(b));
                            }
                            else
                            {
                                var part = new ZipPackagePart(this, e);
                                part.Stream = new MemoryStream();
                                part.Stream.Write(b, 0, b.Length);
                                Parts.Add(GetUriKey(e.FileName), part);
                            }
                        }
                    }

                    e = zip.GetNextEntry();
                }

                foreach (KeyValuePair<string, ZipPackagePart> p in Parts)
                {
                    string name = Path.GetFileName(p.Key);
                    string extension = Path.GetExtension(p.Key);
                    string relFile = string.Format("{0}_rels/{1}.rels", p.Key.Substring(0, p.Key.Length - name.Length), name);
                    if (rels.ContainsKey(relFile))
                    {
                        p.Value.ReadRelation(rels[relFile], p.Value.Uri.OriginalString);
                    }

                    if (_contentTypes.ContainsKey(p.Key))
                    {
                        p.Value.ContentType = _contentTypes[p.Key].Name;
                    }
                    else if (extension.Length > 1 && _contentTypes.ContainsKey(extension.Substring(1)))
                    {
                        p.Value.ContentType = _contentTypes[extension.Substring(1)].Name;
                    }
                }

                if (!hasContentTypeXml)
                {
                    throw new InvalidDataException("The file is not an valid Package file. If the file is encrypted, please supply the password in the constructor.");
                }

                if (!hasContentTypeXml)
                {
                    throw new InvalidDataException("The file is not an valid Package file. If the file is encrypted, please supply the password in the constructor.");
                }

                zip.Close();
                zip.Dispose();
            }
        }

        public CompressionLevel Compression
        {
            get => _compression;
            set
            {
                foreach (ZipPackagePart part in Parts.Values)
                {
                    if (part.CompressionLevel == _compression)
                    {
                        part.CompressionLevel = value;
                    }
                }

                _compression = value;
            }
        }

        private void AddNew()
        {
            _contentTypes.Add("xml", new ContentType(ExcelPackage.schemaXmlExtension, true, "xml"));
            _contentTypes.Add("rels", new ContentType(ExcelPackage.schemaRelsExtension, true, "rels"));
        }

        private void AddContentTypes(string xml)
        {
            var doc = new XmlDocument();
            XmlHelper.LoadXmlSafe(doc, xml, Encoding.UTF8);

            foreach (XmlElement c in doc.DocumentElement.ChildNodes)
            {
                ContentType ct;
                if (string.IsNullOrEmpty(c.GetAttribute("Extension")))
                {
                    ct = new ContentType(c.GetAttribute("ContentType"), false, c.GetAttribute("PartName"));
                }
                else
                {
                    ct = new ContentType(c.GetAttribute("ContentType"), true, c.GetAttribute("Extension"));
                }

                _contentTypes.Add(GetUriKey(ct.Match), ct);
            }
        }

        internal void DeletePart(Uri Uri)
        {
            var delList = new List<object[]>();
            foreach (ZipPackagePart p in Parts.Values)
            {
                foreach (ZipPackageRelationship r in p.GetRelationships())
                {
                    if (UriHelper.ResolvePartUri(p.Uri, r.TargetUri).OriginalString.Equals(Uri.OriginalString, StringComparison.OrdinalIgnoreCase))
                    {
                        delList.Add(new object[] { r.Id, p });
                    }
                }
            }

            foreach (object[] o in delList)
            {
                ((ZipPackagePart)o[1]).DeleteRelationship(o[0].ToString());
            }

            ZipPackageRelationshipCollection rels = GetPart(Uri).GetRelationships();
            while (rels.Count > 0)
            {
                rels.Remove(rels.First().Id);
            }

            rels = null;
            _contentTypes.Remove(GetUriKey(Uri.OriginalString));
            //remove all relations
            Parts.Remove(GetUriKey(Uri.OriginalString));
        }

        internal void Save(Stream stream)
        {
            Encoding enc = Encoding.UTF8;
            var os = new ZipOutputStream(stream, true);
            os.CompressionLevel = (Ionic.Zlib.CompressionLevel)_compression;
            /**** ContentType****/
            os.PutNextEntry("[Content_Types].xml");
            byte[] b = enc.GetBytes(GetContentTypeXml());
            os.Write(b, 0, b.Length);
            /**** Top Rels ****/
            _rels.WriteZip(os, "_rels/.rels");
            ZipPackagePart ssPart = null;
            foreach (ZipPackagePart part in Parts.Values)
            {
                if (part.ContentType != ExcelPackage.contentTypeSharedString)
                {
                    part.WriteZip(os);
                }
                else
                {
                    ssPart = part;
                }
            }

            //Shared strings must be saved after all worksheets. The ss dictionary is populated when that workheets are saved (to get the best performance).
            ssPart?.WriteZip(os);

            os.Flush();

            os.Close();
            os.Dispose();

            //return ms;
        }

        private string GetContentTypeXml()
        {
            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            foreach (ContentType ct in _contentTypes.Values)
            {
                if (ct.IsExtension)
                {
                    xml.AppendFormat("<Default ContentType=\"{0}\" Extension=\"{1}\"/>", ct.Name, ct.Match);
                }
                else
                {
                    xml.AppendFormat("<Override ContentType=\"{0}\" PartName=\"{1}\" />", ct.Name, GetUriKey(ct.Match));
                }
            }

            xml.Append("</Types>");
            return xml.ToString();
        }

        internal void Flush()
        {
        }

        internal void Close()
        {
        }

        internal class ContentType
        {
            internal bool IsExtension;
            internal string Match;
            internal string Name;

            public ContentType(string name, bool isExtension, string match)
            {
                Name = name;
                IsExtension = isExtension;
                Match = match;
            }
        }

        #region Methods

        internal ZipPackagePart CreatePart(Uri partUri, string contentType)
        {
            return CreatePart(partUri, contentType, CompressionLevel.Default);
        }

        internal ZipPackagePart CreatePart(Uri partUri, string contentType, CompressionLevel compressionLevel)
        {
            if (PartExists(partUri))
            {
                throw new InvalidOperationException("Part already exist");
            }

            var part = new ZipPackagePart(this, partUri, contentType, compressionLevel);
            _contentTypes.Add(GetUriKey(part.Uri.OriginalString), new ContentType(contentType, false, part.Uri.OriginalString));
            Parts.Add(GetUriKey(part.Uri.OriginalString), part);
            return part;
        }

        internal ZipPackagePart GetPart(Uri partUri)
        {
            if (PartExists(partUri))
            {
                return Parts.Single(x => x.Key.Equals(GetUriKey(partUri.OriginalString), StringComparison.OrdinalIgnoreCase)).Value;
            }

            throw new InvalidOperationException("Part does not exist.");
        }

        internal string GetUriKey(string uri)
        {
            string ret = uri.Replace('\\', '/');
            if (ret[0] != '/')
            {
                ret = '/' + ret;
            }

            return ret;
        }

        internal bool PartExists(Uri partUri)
        {
            string uriKey = GetUriKey(partUri.OriginalString.ToLowerInvariant());
            return Parts.ContainsKey(uriKey);
            //return Parts.Keys.Any(x => x.Equals(uriKey, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}