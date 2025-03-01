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

using System;
using System.Drawing;
using System.Globalization;
using System.Xml;
using OfficeOpenXml.ConditionalFormatting.Contracts;

namespace OfficeOpenXml.ConditionalFormatting
{
    /// <summary>
    /// Databar
    /// </summary>
    public class ExcelConditionalFormattingDataBar
        : ExcelConditionalFormattingRule,
            IExcelConditionalFormattingDataBarGroup
    {
        private const string _colorPath = "d:dataBar/d:color/@rgb";
        private const string _showValuePath = "d:dataBar/@showValue";

        public bool ShowValue
        {
            get => GetXmlNodeBool(_showValuePath, true);
            set => SetXmlNodeBool(_showValuePath, value);
        }


        public ExcelConditionalFormattingIconDataBarValue LowValue { get; internal set; }

        public ExcelConditionalFormattingIconDataBarValue HighValue { get; internal set; }

        public Color Color
        {
            get
            {
                string rgb = GetXmlNodeString(_colorPath);
                if (!string.IsNullOrEmpty(rgb))
                {
                    return Color.FromArgb(int.Parse(rgb, NumberStyles.HexNumber));
                }

                return Color.White;
            }
            set => SetXmlNodeString(_colorPath, value.ToArgb().ToString("X"));
        }
        /****************************************************************************************/

        /****************************************************************************************/

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="priority"></param>
        /// <param name="address"></param>
        /// <param name="worksheet"></param>
        /// <param name="itemElementNode"></param>
        /// <param name="namespaceManager"></param>
        internal ExcelConditionalFormattingDataBar(
            eExcelConditionalFormattingRuleType type,
            ExcelAddress address,
            int priority,
            ExcelWorksheet worksheet,
            XmlNode itemElementNode,
            XmlNamespaceManager namespaceManager)
            : base(
                type,
                address,
                priority,
                worksheet,
                itemElementNode,
                namespaceManager ?? worksheet.NameSpaceManager)
        {
            string[] s = SchemaNodeOrder;
            Array.Resize(ref s, s.Length + 2); //Fixes issue 15429. Append node order instead om overwriting it.
            s[^2] = "cfvo";
            s[^1] = "color";
            SchemaNodeOrder = s;

            //Create the <dataBar> node inside the <cfRule> node
            if (itemElementNode is { HasChildNodes: true })
            {
                bool high = false;
                foreach (XmlNode node in itemElementNode.SelectNodes("d:dataBar/d:cfvo", NameSpaceManager))
                {
                    if (high == false)
                    {
                        LowValue = new ExcelConditionalFormattingIconDataBarValue(
                            type,
                            address,
                            worksheet,
                            node,
                            namespaceManager);
                        high = true;
                    }
                    else
                    {
                        HighValue = new ExcelConditionalFormattingIconDataBarValue(
                            type,
                            address,
                            worksheet,
                            node,
                            namespaceManager);
                    }
                }
            }
            else
            {
                XmlNode iconSetNode = CreateComplexNode(
                    Node,
                    ExcelConditionalFormattingConstants.Paths.DataBar);

                XmlElement lowNode = iconSetNode.OwnerDocument.CreateElement(ExcelConditionalFormattingConstants.Paths.Cfvo, ExcelPackage.schemaMain);
                iconSetNode.AppendChild(lowNode);
                LowValue = new ExcelConditionalFormattingIconDataBarValue(eExcelConditionalFormattingValueObjectType.Min,
                    0,
                    "",
                    eExcelConditionalFormattingRuleType.DataBar,
                    address,
                    priority,
                    worksheet,
                    lowNode,
                    namespaceManager);

                XmlElement highNode = iconSetNode.OwnerDocument.CreateElement(ExcelConditionalFormattingConstants.Paths.Cfvo, ExcelPackage.schemaMain);
                iconSetNode.AppendChild(highNode);
                HighValue = new ExcelConditionalFormattingIconDataBarValue(eExcelConditionalFormattingValueObjectType.Max,
                    0,
                    "",
                    eExcelConditionalFormattingRuleType.DataBar,
                    address,
                    priority,
                    worksheet,
                    highNode,
                    namespaceManager);
            }

            Type = type;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="priority"></param>
        /// <param name="address"></param>
        /// <param name="worksheet"></param>
        /// <param name="itemElementNode"></param>
        internal ExcelConditionalFormattingDataBar(
            eExcelConditionalFormattingRuleType type,
            ExcelAddress address,
            int priority,
            ExcelWorksheet worksheet,
            XmlNode itemElementNode)
            : this(
                type,
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
        /// <param name="type"></param>
        /// <param name="priority"></param>
        /// <param name="address"></param>
        /// <param name="worksheet"></param>
        internal ExcelConditionalFormattingDataBar(
            eExcelConditionalFormattingRuleType type,
            ExcelAddress address,
            int priority,
            ExcelWorksheet worksheet)
            : this(
                type,
                address,
                priority,
                worksheet,
                null,
                null)
        {
        }

        #endregion Constructors
    }
}