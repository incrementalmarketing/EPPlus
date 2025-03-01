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
    public class Ceiling : ExcelFunction
    {
        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            ValidateArguments(arguments, 2);
            double number = ArgToDecimal(arguments, 0);
            double significance = ArgToDecimal(arguments, 1);
            ValidateNumberAndSign(number, significance);
            if (significance is < 1 and > 0)
            {
                double floor = System.Math.Floor(number);
                double rest = number - floor;
                int nSign = (int)(rest / significance) + 1;
                return CreateResult(floor + nSign * significance, DataType.Decimal);
            }

            if (significance == 1)
            {
                return CreateResult(System.Math.Ceiling(number), DataType.Decimal);
            }

            if (number % significance == 0)
            {
                return CreateResult(number, DataType.Decimal);
            }

            double result = number - number % significance + significance;
            return CreateResult(result, DataType.Decimal);
        }

        private void ValidateNumberAndSign(double number, double sign)
        {
            if (number > 0d && sign < 0)
            {
                string values = string.Format("num: {0}, sign: {1}", number, sign);
                throw new InvalidOperationException("Ceiling cannot handle a negative significance when the number is positive" + values);
            }
        }
    }
}