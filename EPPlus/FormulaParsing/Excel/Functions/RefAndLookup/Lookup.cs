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

using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.FormulaParsing.ExcelUtilities;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;
using OfficeOpenXml.FormulaParsing.Utilities;
using static OfficeOpenXml.FormulaParsing.EpplusExcelDataProvider;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup
{
    public class Lookup : LookupFunction
    {
        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            ValidateArguments(arguments, 2);
            if (HaveTwoRanges(arguments))
            {
                return HandleTwoRanges(arguments, context);
            }

            return HandleSingleRange(arguments, context);
        }

        private bool HaveTwoRanges(IEnumerable<FunctionArgument> arguments)
        {
            if (arguments.Count() < 3) return false;
            return arguments.ElementAt(2).Value is RangeInfo;
        }

        private CompileResult HandleSingleRange(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            object searchedValue = arguments.ElementAt(0).Value;
            Require.That(arguments.ElementAt(1).Value).Named("firstAddress").IsNotNull();
            string firstAddress = ArgToAddress(arguments, 1, context);
            var rangeAddressFactory = new RangeAddressFactory(context.ExcelDataProvider);
            RangeAddress address = rangeAddressFactory.Create(firstAddress);
            int nRows = address.ToRow - address.FromRow;
            int nCols = address.ToCol - address.FromCol;
            int lookupIndex = nCols + 1;
            var lookupDirection = LookupDirection.Vertical;
            if (nCols > nRows)
            {
                lookupIndex = nRows + 1;
                lookupDirection = LookupDirection.Horizontal;
            }

            var lookupArgs = new LookupArguments(searchedValue, firstAddress, lookupIndex, 0, true, arguments.ElementAt(1).ValueAsRangeInfo);
            LookupNavigator navigator = LookupNavigatorFactory.Create(lookupDirection, lookupArgs, context);
            return Lookup(navigator, lookupArgs);
        }

        private CompileResult HandleTwoRanges(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            object searchedValue = arguments.ElementAt(0).Value;
            Require.That(arguments.ElementAt(1).Value).Named("firstAddress").IsNotNull();
            Require.That(arguments.ElementAt(2).Value).Named("secondAddress").IsNotNull();
            string firstAddress = ArgToAddress(arguments, 1, context);
            string secondAddress = ArgToAddress(arguments, 2, context);
            var rangeAddressFactory = new RangeAddressFactory(context.ExcelDataProvider);
            RangeAddress address1 = rangeAddressFactory.Create(firstAddress);
            RangeAddress address2 = rangeAddressFactory.Create(secondAddress);
            int lookupIndex = address2.FromCol - address1.FromCol + 1;
            int lookupOffset = address2.FromRow - address1.FromRow;
            LookupDirection lookupDirection = GetLookupDirection(address1);
            if (lookupDirection == LookupDirection.Horizontal)
            {
                lookupIndex = address2.FromRow - address1.FromRow + 1;
                lookupOffset = address2.FromCol - address1.FromCol;
            }

            var lookupArgs = new LookupArguments(searchedValue, firstAddress, lookupIndex, lookupOffset, true, arguments.ElementAt(1).ValueAsRangeInfo);
            LookupNavigator navigator = LookupNavigatorFactory.Create(lookupDirection, lookupArgs, context);
            return Lookup(navigator, lookupArgs);
        }
    }
}