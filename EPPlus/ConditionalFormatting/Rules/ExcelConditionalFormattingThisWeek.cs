﻿/*******************************************************************************
 * You may amend and distribute as you like, but don't remove this header!
 *
 * EPPlus provides server-side generation of Excel 2007/2010 spreadsheets.
 * See https://github.com/JanKallman/EPPlus for details.
 *
 * Copyright (C) 2011  Jan Källman
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
 * ******************************************************************************
 * Eyal Seagull        Added       		  2012-04-03
 *******************************************************************************/

using System.Xml;

namespace OfficeOpenXml.ConditionalFormatting
{
    /// <summary>
    /// ExcelConditionalFormattingThisWeek
    /// </summary>
    public class ExcelConditionalFormattingThisWeek
        : ExcelConditionalFormattingTimePeriodGroup
    {
        /****************************************************************************************/

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="address"></param>
        /// <param name="worksheet"></param>
        /// <param name="itemElementNode"></param>
        /// <param name="namespaceManager"></param>
        internal ExcelConditionalFormattingThisWeek(
            ExcelAddress address,
            int priority,
            ExcelWorksheet worksheet,
            XmlNode itemElementNode,
            XmlNamespaceManager namespaceManager)
            : base(
                eExcelConditionalFormattingRuleType.ThisWeek,
                address,
                priority,
                worksheet,
                itemElementNode,
                namespaceManager ?? worksheet.NameSpaceManager)
        {
            if (itemElementNode == null) //Set default values and create attributes if needed
            {
                TimePeriod = eExcelConditionalFormattingTimePeriodType.ThisWeek;
                Formula = string.Format(
                    "AND(TODAY()-ROUNDDOWN({0},0)<=WEEKDAY(TODAY())-1,ROUNDDOWN({0},0)-TODAY()<=7-WEEKDAY(TODAY()))",
                    Address.Start.Address);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="address"></param>
        /// <param name="worksheet"></param>
        /// <param name="itemElementNode"></param>
        internal ExcelConditionalFormattingThisWeek(
            ExcelAddress address,
            int priority,
            ExcelWorksheet worksheet,
            XmlNode itemElementNode)
            : this(
                address,
                priority,
                worksheet,
                itemElementNode,
                null)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="address"></param>
        /// <param name="worksheet"></param>
        internal ExcelConditionalFormattingThisWeek(
            ExcelAddress address,
            int priority,
            ExcelWorksheet worksheet)
            : this(
                address,
                priority,
                worksheet,
                null,
                null)
        {
        }

        #endregion Constructors

        /****************************************************************************************/
    }
}