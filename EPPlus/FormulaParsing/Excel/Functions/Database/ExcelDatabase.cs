﻿/* Copyright (C) 2011  Jan Källman
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
 * Mats Alm   		                Added		                2015-04-06
 *******************************************************************************/

using System.Collections.Generic;
using System.Globalization;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.Database
{
    public class ExcelDatabase
    {
        private readonly ExcelDataProvider _dataProvider;
        private readonly int _endRow;
        private readonly int _fieldRow;
        private readonly List<ExcelDatabaseField> _fields = new();
        private readonly int _fromCol;
        private readonly int _toCol;
        private readonly string _worksheet;
        private int _rowIndex;

        public ExcelDatabase(ExcelDataProvider dataProvider, string range)
        {
            _dataProvider = dataProvider;
            var address = new ExcelAddressBase(range);
            _fromCol = address._fromCol;
            _toCol = address._toCol;
            _fieldRow = address._fromRow;
            _endRow = address._toRow;
            _worksheet = address.WorkSheet;
            _rowIndex = _fieldRow;
            Initialize();
        }

        public IEnumerable<ExcelDatabaseField> Fields => _fields;

        public bool HasMoreRows => _rowIndex < _endRow;

        private void Initialize()
        {
            int fieldIx = 0;
            for (int colIndex = _fromCol; colIndex <= _toCol; colIndex++)
            {
                object nameObj = GetCellValue(_fieldRow, colIndex);
                string name = nameObj != null ? nameObj.ToString().ToLower(CultureInfo.InvariantCulture) : string.Empty;
                _fields.Add(new ExcelDatabaseField(name, fieldIx++));
            }
        }

        private object GetCellValue(int row, int col)
        {
            return _dataProvider.GetRangeValue(_worksheet, row, col);
        }

        public ExcelDatabaseRow Read()
        {
            var retVal = new ExcelDatabaseRow();
            _rowIndex++;
            foreach (ExcelDatabaseField field in Fields)
            {
                int colIndex = _fromCol + field.ColIndex;
                object val = GetCellValue(_rowIndex, colIndex);
                retVal[field.FieldName] = val;
            }

            return retVal;
        }
    }
}