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
 * Mats Alm   		                Added		                2013-12-03
 *******************************************************************************/

using System;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions
{
    public class FunctionArgument
    {
        private ExcelCellState _excelCellState;

        public FunctionArgument(object val)
        {
            Value = val;
            DataType = DataType.Unknown;
        }

        public FunctionArgument(object val, DataType dataType)
            : this(val)
        {
            DataType = dataType;
        }

        public object Value { get; private set; }

        public DataType DataType { get; }

        public Type Type => Value?.GetType();

        public int ExcelAddressReferenceId { get; set; }

        public bool IsExcelRange => Value is ExcelDataProvider.IRangeInfo;

        public bool ValueIsExcelError => ExcelErrorValue.Values.IsErrorValue(Value);

        public ExcelErrorValue ValueAsExcelErrorValue => ExcelErrorValue.Parse(Value.ToString());

        public ExcelDataProvider.IRangeInfo ValueAsRangeInfo => Value as ExcelDataProvider.IRangeInfo;

        public object ValueFirst
        {
            get
            {
                if (Value is ExcelDataProvider.INameInfo)
                {
                    Value = ((ExcelDataProvider.INameInfo)Value).Value;
                }

                if (Value is not ExcelDataProvider.IRangeInfo v)
                {
                    return Value;
                }

                return v.GetValue(v.Address._fromRow, v.Address._fromCol);
            }
        }

        public void SetExcelStateFlag(ExcelCellState state)
        {
            _excelCellState |= state;
        }

        public bool ExcelStateFlagIsSet(ExcelCellState state)
        {
            return (_excelCellState & state) != 0;
        }
    }
}