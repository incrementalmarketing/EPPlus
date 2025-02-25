// ZipConstants.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2006, 2007, 2008, 2009 Dino Chiesa and Microsoft Corporation.  
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License. 
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs): 
// Time-stamp: <2009-August-27 23:22:32>
//
// ------------------------------------------------------------------
//
// This module defines a few constants that are used in the project. 
//
// ------------------------------------------------------------------

using System;

namespace OfficeOpenXml.Packaging.Ionic.Zip
{
    static class ZipConstants
    {
        public const ushort AesAlgId128 = 0x660E;
        public const ushort AesAlgId192 = 0x660F;
        public const ushort AesAlgId256 = 0x6610;
        public const int AesBlockSize = 128; // ???


        // These are dictated by the Zip Spec.See APPNOTE.txt
        public const int AesKeySize = 192; // 128, 192, 256
        public const uint EndOfCentralDirectorySignature = 0x06054b50;
        public const uint PackedToRemovableMedia = 0x30304b50;
        public const int SplitArchiveSignature = 0x08074b50;
        public const uint Zip64EndOfCentralDirectoryLocatorSignature = 0x07064b50;
        public const uint Zip64EndOfCentralDirectoryRecordSignature = 0x06064b50;
        public const int ZipDirEntrySignature = 0x02014b50;
        public const int ZipEntryDataDescriptorSignature = 0x08074b50;
        public const int ZipEntrySignature = 0x04034b50;
    }
}