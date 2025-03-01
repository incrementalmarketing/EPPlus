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
 * Mats Alm   		                Added       		        2013-03-01 (Prior file history on https://github.com/swmal/ExcelFormulaParser)
 *******************************************************************************/

using OfficeOpenXml.FormulaParsing.Utilities;

namespace OfficeOpenXml.FormulaParsing.ExcelUtilities
{
    public class ExcelAddressInfo
    {
        private ExcelAddressInfo(string address)
        {
            string addressOnSheet = address;
            Worksheet = string.Empty;
            if (address.Contains("!"))
            {
                string[] worksheetArr = address.Split('!');
                Worksheet = worksheetArr[0];
                addressOnSheet = worksheetArr[1];
            }

            if (addressOnSheet.Contains(":"))
            {
                string[] rangeArr = addressOnSheet.Split(':');
                StartCell = rangeArr[0];
                EndCell = rangeArr[1];
            }
            else
            {
                StartCell = addressOnSheet;
            }

            AddressOnSheet = addressOnSheet;
        }

        public string Worksheet { get; }

        public bool WorksheetIsSpecified => !string.IsNullOrEmpty(Worksheet);

        public bool IsMultipleCells => !string.IsNullOrEmpty(EndCell);

        public string StartCell { get; private set; }

        public string EndCell { get; }

        public string AddressOnSheet { get; private set; }

        public static ExcelAddressInfo Parse(string address)
        {
            Require.That(address).Named("address").IsNotNullOrEmpty();
            return new ExcelAddressInfo(address);
        }
    }
}