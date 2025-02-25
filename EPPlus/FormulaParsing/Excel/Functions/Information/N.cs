﻿using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Exceptions;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.Information
{
    public class N : ExcelFunction
    {
        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            ValidateArguments(arguments, 1);
            object arg = GetFirstValue(arguments);

            if (arg is bool)
            {
                double val = (bool)arg ? 1d : 0d;
                return CreateResult(val, DataType.Decimal);
            }

            if (IsNumeric(arg))
            {
                double val = ConvertUtil.GetValueDouble(arg);
                return CreateResult(val, DataType.Decimal);
            }

            if (arg is string)
            {
                return CreateResult(0d, DataType.Decimal);
            }

            if (arg is ExcelErrorValue)
            {
                return CreateResult(arg, DataType.ExcelError);
            }

            throw new ExcelErrorValueException(eErrorType.Value);
        }
    }
}