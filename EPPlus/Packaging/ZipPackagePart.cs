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
using System.IO;
using OfficeOpenXml.Packaging.Ionic.Zip;

namespace OfficeOpenXml.Packaging
{
    internal class ZipPackagePart : ZipPackageRelationshipBase, IDisposable
    {
        string _contentType = "";
        internal CompressionLevel CompressionLevel;

        internal ZipPackagePart(ZipPackage package, ZipEntry entry)
        {
            Package = package;
            Entry = entry;
            SaveHandler = null;
            Uri = new Uri(package.GetUriKey(entry.FileName), UriKind.Relative);
        }

        internal ZipPackagePart(ZipPackage package, Uri partUri, string contentType, CompressionLevel compressionLevel)
        {
            Package = package;
            //Entry = new ZipEntry();
            //Entry.FileName = partUri.OriginalString.Replace('/','\\');
            Uri = partUri;
            ContentType = contentType;
            CompressionLevel = compressionLevel;
        }

        internal ZipPackage Package { get; set; }
        internal ZipEntry Entry { get; set; }

        internal MemoryStream Stream { get; set; }

        public string ContentType
        {
            get => _contentType;
            internal set
            {
                if (!string.IsNullOrEmpty(_contentType))
                {
                    if (Package._contentTypes.ContainsKey(Package.GetUriKey(Uri.OriginalString)))
                    {
                        Package._contentTypes.Remove(Package.GetUriKey(Uri.OriginalString));
                        Package._contentTypes.Add(Package.GetUriKey(Uri.OriginalString), new ZipPackage.ContentType(value, false, Uri.OriginalString));
                    }
                }

                _contentType = value;
            }
        }

        public Uri Uri { get; }

        internal SaveHandlerDelegate SaveHandler { get; set; }


        public void Dispose()
        {
            Stream.Close();
            Stream.Dispose();
        }

        internal override ZipPackageRelationship CreateRelationship(Uri targetUri, TargetMode targetMode, string relationshipType)
        {
            ZipPackageRelationship rel = base.CreateRelationship(targetUri, targetMode, relationshipType);
            rel.SourceUri = Uri;
            return rel;
        }

        internal MemoryStream GetStream()
        {
            return GetStream(FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        internal MemoryStream GetStream(FileMode fileMode)
        {
            return GetStream(FileMode.Create, FileAccess.ReadWrite);
        }

        internal MemoryStream GetStream(FileMode fileMode, FileAccess fileAccess)
        {
            if (Stream == null || fileMode == FileMode.CreateNew || fileMode == FileMode.Create)
            {
                Stream = new MemoryStream();
            }
            else
            {
                Stream.Seek(0, SeekOrigin.Begin);
            }

            return Stream;
        }

        public Stream GetZipStream()
        {
            var ms = new MemoryStream();
            var os = new ZipOutputStream(ms);
            return os;
        }

        internal void WriteZip(ZipOutputStream os)
        {
            byte[] b;
            if (SaveHandler == null)
            {
                b = GetStream().ToArray();
                if (b.Length == 0) //Make sure the file isn't empty. DotNetZip streams does not seems to handle zero sized files.
                {
                    return;
                }

                os.CompressionLevel = (Ionic.Zlib.CompressionLevel)CompressionLevel;
                os.PutNextEntry(Uri.OriginalString);
                os.Write(b, 0, b.Length);
            }
            else
            {
                SaveHandler(os, CompressionLevel, Uri.OriginalString);
            }

            if (_rels.Count > 0)
            {
                string f = Uri.OriginalString;
                string name = Path.GetFileName(f);
                _rels.WriteZip(os, string.Format("{0}_rels/{1}.rels", f.Substring(0, f.Length - name.Length), name));
            }

            b = null;
        }

        internal delegate void SaveHandlerDelegate(ZipOutputStream stream, CompressionLevel compressionLevel, string fileName);
    }
}