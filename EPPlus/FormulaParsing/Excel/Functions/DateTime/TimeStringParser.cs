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
using System.Text.RegularExpressions;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime
{
    public class TimeStringParser
    {
        private const string RegEx12 = @"^[0-9]{1,2}(\:[0-9]{1,2}){0,2}( PM| AM)$";
        private const string RegEx24 = @"^[0-9]{1,2}(\:[0-9]{1,2}){0,2}$";

        private double GetSerialNumber(int hour, int minute, int second)
        {
            double secondsInADay = 24d * 60d * 60d;
            return ((double)hour * 60 * 60 + (double)minute * 60 + second) / secondsInADay;
        }

        private void ValidateValues(int hour, int minute, int second)
        {
            if (second is < 0 or > 59)
            {
                throw new FormatException("Illegal value for second: " + second);
            }

            if (minute is < 0 or > 59)
            {
                throw new FormatException("Illegal value for minute: " + minute);
            }
        }

        public virtual double Parse(string input)
        {
            return InternalParse(input);
        }

        public virtual bool CanParse(string input)
        {
            System.DateTime dt;
            return Regex.IsMatch(input, RegEx24) || Regex.IsMatch(input, RegEx12) || System.DateTime.TryParse(input, out dt);
        }

        private double InternalParse(string input)
        {
            if (Regex.IsMatch(input, RegEx24))
            {
                return Parse24HourTimeString(input);
            }

            if (Regex.IsMatch(input, RegEx12))
            {
                return Parse12HourTimeString(input);
            }

            if (System.DateTime.TryParse(input, out System.DateTime dateTime))
            {
                return GetSerialNumber(dateTime.Hour, dateTime.Minute, dateTime.Second);
            }

            return -1;
        }

        private double Parse12HourTimeString(string input)
        {
            string dayPart = string.Empty;
            dayPart = input.Substring(input.Length - 2, 2);
            GetValuesFromString(input, out int hour, out int minute, out int second);
            if (dayPart == "PM") hour += 12;
            ValidateValues(hour, minute, second);
            return GetSerialNumber(hour, minute, second);
        }

        private double Parse24HourTimeString(string input)
        {
            GetValuesFromString(input, out int hour, out int minute, out int second);
            ValidateValues(hour, minute, second);
            return GetSerialNumber(hour, minute, second);
        }

        private static void GetValuesFromString(string input, out int hour, out int minute, out int second)
        {
            hour = 0;
            minute = 0;
            second = 0;

            string[] items = input.Split(':');
            hour = int.Parse(items[0]);
            if (items.Length > 1)
            {
                minute = int.Parse(items[1]);
            }

            if (items.Length > 2)
            {
                string val = items[2];
                val = Regex.Replace(val, "[^0-9]+$", string.Empty);
                second = int.Parse(val);
            }
        }
    }
}