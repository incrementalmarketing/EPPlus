﻿using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime.Workdays
{
    public class HolidayWeekdays
    {
        private readonly List<DayOfWeek> _holidayDays = new();

        public HolidayWeekdays()
            : this(DayOfWeek.Saturday, DayOfWeek.Sunday)
        {
        }

        public HolidayWeekdays(params DayOfWeek[] holidayDays)
        {
            foreach (DayOfWeek dayOfWeek in holidayDays)
            {
                _holidayDays.Add(dayOfWeek);
            }
        }

        public int NumberOfWorkdaysPerWeek => 7 - _holidayDays.Count;

        public bool IsHolidayWeekday(System.DateTime dateTime)
        {
            return _holidayDays.Contains(dateTime.DayOfWeek);
        }

        public System.DateTime AdjustResultWithHolidays(System.DateTime resultDate,
            IEnumerable<FunctionArgument> arguments)
        {
            if (arguments.Count() == 2) return resultDate;
            if (arguments.ElementAt(2).Value is IEnumerable<FunctionArgument> holidays)
            {
                foreach (FunctionArgument arg in holidays)
                {
                    if (ConvertUtil.IsNumeric(arg.Value))
                    {
                        double dateSerial = ConvertUtil.GetValueDouble(arg.Value);
                        System.DateTime holidayDate = System.DateTime.FromOADate(dateSerial);
                        if (!IsHolidayWeekday(holidayDate))
                        {
                            resultDate = resultDate.AddDays(1);
                        }
                    }
                }
            }
            else
            {
                if (arguments.ElementAt(2).Value is ExcelDataProvider.IRangeInfo range)
                {
                    foreach (ExcelDataProvider.ICellInfo cell in range)
                    {
                        if (ConvertUtil.IsNumeric(cell.Value))
                        {
                            double dateSerial = ConvertUtil.GetValueDouble(cell.Value);
                            System.DateTime holidayDate = System.DateTime.FromOADate(dateSerial);
                            if (!IsHolidayWeekday(holidayDate))
                            {
                                resultDate = resultDate.AddDays(1);
                            }
                        }
                    }
                }
            }

            return resultDate;
        }

        public System.DateTime GetNextWorkday(System.DateTime date, WorkdayCalculationDirection direction = WorkdayCalculationDirection.Forward)
        {
            int changeParam = (int)direction;
            System.DateTime tmpDate = date.AddDays(changeParam);
            while (IsHolidayWeekday(tmpDate))
            {
                tmpDate = tmpDate.AddDays(changeParam);
            }

            return tmpDate;
        }
    }
}