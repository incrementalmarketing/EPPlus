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
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.Math
{
    public class Count : HiddenValuesHandlingFunction
    {
        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            ValidateArguments(arguments, 1);
            double nItems = 0d;
            Calculate(arguments, ref nItems, context, ItemContext.SingleArg);
            return CreateResult(nItems, DataType.Integer);
        }

        private void Calculate(IEnumerable<FunctionArgument> items, ref double nItems, ParsingContext context, ItemContext itemContext)
        {
            foreach (FunctionArgument item in items)
            {
                if (item.Value is ExcelDataProvider.IRangeInfo cs)
                {
                    foreach (ExcelDataProvider.ICellInfo c in cs)
                    {
                        _CheckForAndHandleExcelError(c, context);
                        if (ShouldIgnore(c, context) == false && ShouldCount(c.Value, ItemContext.InRange))
                        {
                            nItems++;
                        }
                    }
                }
                else
                {
                    if (item.Value is IEnumerable<FunctionArgument> value)
                    {
                        Calculate(value, ref nItems, context, ItemContext.InArray);
                    }
                    else
                    {
                        _CheckForAndHandleExcelError(item, context);
                        if (ShouldIgnore(item) == false && ShouldCount(item.Value, itemContext))
                        {
                            nItems++;
                        }
                    }
                }
            }
        }

        private void _CheckForAndHandleExcelError(FunctionArgument arg, ParsingContext context)
        {
            //if (context.Scopes.Current.IsSubtotal)
            //{
            //    CheckForAndHandleExcelError(arg);
            //}
        }

        private void _CheckForAndHandleExcelError(ExcelDataProvider.ICellInfo cell, ParsingContext context)
        {
            //if (context.Scopes.Current.IsSubtotal)
            //{
            //    CheckForAndHandleExcelError(cell);
            //}
        }

        private bool ShouldCount(object value, ItemContext context)
        {
            switch (context)
            {
                case ItemContext.SingleArg:
                    return IsNumeric(value) || IsNumericString(value);
                case ItemContext.InRange:
                    return IsNumeric(value);
                case ItemContext.InArray:
                    return IsNumeric(value) || IsNumericString(value);
                default:
                    throw new ArgumentException("Unknown ItemContext:" + context);
            }
        }

        private enum ItemContext
        {
            InRange,
            InArray,
            SingleArg
        }
    }
}