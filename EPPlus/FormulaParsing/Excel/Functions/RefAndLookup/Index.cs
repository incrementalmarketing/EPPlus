﻿using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.FormulaParsing.Exceptions;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup
{
    public class Index : ExcelFunction
    {
        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            ValidateArguments(arguments, 2);
            FunctionArgument arg1 = arguments.ElementAt(0);
            var args = arg1.Value as IEnumerable<FunctionArgument>;
            var crf = new CompileResultFactory();
            if (args != null)
            {
                int index = ArgToInt(arguments, 1);
                if (index > args.Count())
                {
                    throw new ExcelErrorValueException(eErrorType.Ref);
                }

                FunctionArgument candidate = args.ElementAt(index - 1);
                //Commented JK-Can be any data type
                //if (!IsNumber(candidate.Value))
                //{
                //    throw new ExcelErrorValueException(eErrorType.Value);
                //}
                //return CreateResult(ConvertUtil.GetValueDouble(candidate.Value), DataType.Decimal);
                return crf.Create(candidate.Value);
            }

            if (arg1.IsExcelRange)
            {
                int row = ArgToInt(arguments, 1);
                int col = arguments.Count() > 2 ? ArgToInt(arguments, 2) : 1;
                ExcelDataProvider.IRangeInfo ri = arg1.ValueAsRangeInfo;
                if (row > ri.Address._toRow - ri.Address._fromRow + 1 ||
                    col > ri.Address._toCol - ri.Address._fromCol + 1)
                {
                    ThrowExcelErrorValueException(eErrorType.Ref);
                }

                object candidate = ri.GetOffset(row - 1, col - 1);
                //Commented JK-Can be any data type
                //if (!IsNumber(candidate.Value))   
                //{
                //    throw new ExcelErrorValueException(eErrorType.Value);
                //}
                return crf.Create(candidate);
            }

            throw new NotImplementedException();
        }
    }
}