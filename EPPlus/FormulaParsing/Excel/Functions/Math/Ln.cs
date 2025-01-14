﻿using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.Math
{
    public class Ln : ExcelFunction
    {
        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            ValidateArguments(arguments, 1);
            double arg = ArgToDecimal(arguments, 0);
            return CreateResult(System.Math.Log(arg, System.Math.E), DataType.Decimal);
        }
    }
}