﻿using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.Text
{
    public class Value : ExcelFunction
    {
        private readonly DateValue _dateValueFunc = new();
        private readonly string _decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        private readonly string _groupSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
        private readonly string _shortTimePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
        private readonly string _timeSeparator = CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator;
        private readonly TimeValue _timeValueFunc = new();

        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            ValidateArguments(arguments, 1);
            string val = ArgToString(arguments, 0);
            double result = 0d;
            if (string.IsNullOrEmpty(val)) return CreateResult(result, DataType.Integer);
            val = val.TrimEnd(' ');
            if (Regex.IsMatch(val, $"^[\\d]*({Regex.Escape(_groupSeparator)}?[\\d]*)?({Regex.Escape(_decimalSeparator)}[\\d]*)*?[ ?% ?]?$"))
            {
                if (val.EndsWith("%"))
                {
                    val = val.TrimEnd('%');
                    result = double.Parse(val) / 100;
                }
                else
                {
                    result = double.Parse(val);
                }

                return CreateResult(result, DataType.Decimal);
            }

            if (double.TryParse(val, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
            {
                return CreateResult(result, DataType.Decimal);
            }

            string timeSeparator = Regex.Escape(_timeSeparator);
            if (Regex.IsMatch(val, @"^[\d]{1,2}" + timeSeparator + @"[\d]{2}(" + timeSeparator + @"[\d]{2})?$"))
            {
                CompileResult timeResult = _timeValueFunc.Execute(val);
                if (timeResult.DataType == DataType.Date)
                {
                    return timeResult;
                }
            }

            CompileResult dateResult = _dateValueFunc.Execute(val);
            if (dateResult.DataType == DataType.Date)
            {
                return dateResult;
            }

            return CreateResult(ExcelErrorValue.Create(eErrorType.Value), DataType.ExcelError);
        }
    }
}