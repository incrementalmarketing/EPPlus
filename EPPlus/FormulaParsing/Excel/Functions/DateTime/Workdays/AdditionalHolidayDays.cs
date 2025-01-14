﻿using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime.Workdays
{
    public class AdditionalHolidayDays
    {
        private readonly FunctionArgument _holidayArg;
        private readonly List<System.DateTime> _holidayDates = new();

        public AdditionalHolidayDays(FunctionArgument holidayArg)
        {
            _holidayArg = holidayArg;
            Initialize();
        }

        public IEnumerable<System.DateTime> AdditionalDates => _holidayDates;

        private void Initialize()
        {
            if (_holidayArg.Value is IEnumerable<FunctionArgument> holidays)
            {
                foreach (System.DateTime holidayDate in from arg in holidays where ConvertUtil.IsNumeric(arg.Value) select ConvertUtil.GetValueDouble(arg.Value) into dateSerial select System.DateTime.FromOADate(dateSerial))
                {
                    _holidayDates.Add(holidayDate);
                }
            }

            if (_holidayArg.Value is ExcelDataProvider.IRangeInfo range)
            {
                foreach (System.DateTime holidayDate in from cell in range where ConvertUtil.IsNumeric(cell.Value) select ConvertUtil.GetValueDouble(cell.Value) into dateSerial select System.DateTime.FromOADate(dateSerial))
                {
                    _holidayDates.Add(holidayDate);
                }
            }

            if (ConvertUtil.IsNumeric(_holidayArg.Value))
            {
                _holidayDates.Add(System.DateTime.FromOADate(ConvertUtil.GetValueDouble(_holidayArg.Value)));
            }
        }
    }
}