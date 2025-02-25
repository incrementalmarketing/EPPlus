﻿using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.Math
{
    public class Rand : ExcelFunction
    {
        private static int Seed { get; set; }

        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            Seed = Seed > 50 ? 0 : Seed + 5;
            double val = new Random(System.DateTime.Now.Millisecond + Seed).NextDouble();
            return CreateResult(val, DataType.Decimal);
        }
    }
}