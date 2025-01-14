﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime.Workdays;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime
{
    public class NetworkdaysIntl : ExcelFunction
    {
        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            FunctionArgument[] functionArguments = arguments as FunctionArgument[] ?? arguments.ToArray();
            ValidateArguments(functionArguments, 2);
            System.DateTime startDate = System.DateTime.FromOADate(ArgToInt(functionArguments, 0));
            System.DateTime endDate = System.DateTime.FromOADate(ArgToInt(functionArguments, 1));
            var calculator = new WorkdayCalculator();
            var weekdayFactory = new HolidayWeekdaysFactory();
            if (functionArguments.Length > 2)
            {
                object holidayArg = functionArguments[2].Value;
                if (Regex.IsMatch(holidayArg.ToString(), "^[01]{7}"))
                {
                    calculator = new WorkdayCalculator(weekdayFactory.Create(holidayArg.ToString()));
                }
                else if (IsNumeric(holidayArg))
                {
                    int holidayCode = Convert.ToInt32(holidayArg);
                    calculator = new WorkdayCalculator(weekdayFactory.Create(holidayCode));
                }
                else
                {
                    return new CompileResult(eErrorType.Value);
                }
            }

            WorkdayCalculatorResult result = calculator.CalculateNumberOfWorkdays(startDate, endDate);
            if (functionArguments.Length > 3)
            {
                result = calculator.ReduceWorkdaysWithHolidays(result, functionArguments[3]);
            }

            return new CompileResult(result.NumberOfWorkdays, DataType.Integer);
        }
    }
}