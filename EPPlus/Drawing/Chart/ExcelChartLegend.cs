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
 *******************************************************************************
 * Jan Källman		Added		2009-10-01
 * Jan Källman		License changed GPL-->LGPL 2011-12-16
 *******************************************************************************/

using System;
using System.Globalization;
using System.Xml;
using OfficeOpenXml.Style;

namespace OfficeOpenXml.Drawing.Chart
{
    /// <summary>
    /// Position of the legend
    /// </summary>
    public enum eLegendPosition
    {
        Top,
        Left,
        Right,
        Bottom,
        TopRight
    }

    /// <summary>
    /// Chart ledger
    /// </summary>
    public class ExcelChartLegend : XmlHelper
    {
        const string OVERLAY_PATH = "c:overlay/@val";
        const string POSITION_PATH = "c:legendPos/@val";
        ExcelDrawingBorder _border;
        readonly ExcelChart _chart;
        ExcelDrawingFill _fill;
        ExcelTextFont _font;

        internal ExcelChartLegend(XmlNamespaceManager ns, XmlNode node, ExcelChart chart)
            : base(ns, node)
        {
            _chart = chart;
            SchemaNodeOrder = new[] { "legendPos", "layout", "overlay", "txPr", "bodyPr", "lstStyle", "spPr" };
        }

        /// <summary>
        /// Position of the Legend
        /// </summary>
        public eLegendPosition Position
        {
            get
            {
                switch (GetXmlNodeString(POSITION_PATH).ToLower(CultureInfo.InvariantCulture))
                {
                    case "t":
                        return eLegendPosition.Top;
                    case "b":
                        return eLegendPosition.Bottom;
                    case "l":
                        return eLegendPosition.Left;
                    case "tr":
                        return eLegendPosition.TopRight;
                    default:
                        return eLegendPosition.Right;
                }
            }
            set
            {
                if (TopNode == null) throw new Exception("Can't set position. Chart has no legend");
                switch (value)
                {
                    case eLegendPosition.Top:
                        SetXmlNodeString(POSITION_PATH, "t");
                        break;
                    case eLegendPosition.Bottom:
                        SetXmlNodeString(POSITION_PATH, "b");
                        break;
                    case eLegendPosition.Left:
                        SetXmlNodeString(POSITION_PATH, "l");
                        break;
                    case eLegendPosition.TopRight:
                        SetXmlNodeString(POSITION_PATH, "tr");
                        break;
                    default:
                        SetXmlNodeString(POSITION_PATH, "r");
                        break;
                }
            }
        }

        /// <summary>
        /// If the legend overlays other objects
        /// </summary>
        public bool Overlay
        {
            get => GetXmlNodeBool(OVERLAY_PATH);
            set
            {
                if (TopNode == null) throw new Exception("Can't set overlay. Chart has no legend");
                SetXmlNodeBool(OVERLAY_PATH, value);
            }
        }

        /// <summary>
        /// Fill style
        /// </summary>
        public ExcelDrawingFill Fill
        {
            get
            {
                if (_fill == null)
                {
                    _fill = new ExcelDrawingFill(NameSpaceManager, TopNode, "c:spPr");
                }

                return _fill;
            }
        }

        /// <summary>
        /// Border style
        /// </summary>
        public ExcelDrawingBorder Border
        {
            get
            {
                if (_border == null)
                {
                    _border = new ExcelDrawingBorder(NameSpaceManager, TopNode, "c:spPr/a:ln");
                }

                return _border;
            }
        }

        /// <summary>
        /// Font properties
        /// </summary>
        public ExcelTextFont Font
        {
            get
            {
                if (_font == null)
                {
                    if (TopNode.SelectSingleNode("c:txPr", NameSpaceManager) == null)
                    {
                        CreateNode("c:txPr/a:bodyPr");
                        CreateNode("c:txPr/a:lstStyle");
                    }

                    _font = new ExcelTextFont(NameSpaceManager, TopNode, "c:txPr/a:p/a:pPr/a:defRPr", new[] { "legendPos", "layout", "pPr", "defRPr", "solidFill", "uFill", "latin", "cs", "r", "rPr", "t" });
                }

                return _font;
            }
        }

        /// <summary>
        /// Remove the legend
        /// </summary>
        public void Remove()
        {
            if (TopNode == null) return;
            TopNode.ParentNode.RemoveChild(TopNode);
            TopNode = null;
        }

        /// <summary>
        /// Add a legend to the chart
        /// </summary>
        public void Add()
        {
            if (TopNode != null) return;

            //XmlHelper xml = new XmlHelper(NameSpaceManager, _chart.ChartXml);
            XmlHelper xml = XmlHelperFactory.Create(NameSpaceManager, _chart.ChartXml);
            xml.SchemaNodeOrder = _chart.SchemaNodeOrder;

            xml.CreateNode("c:chartSpace/c:chart/c:legend");
            TopNode = _chart.ChartXml.SelectSingleNode("c:chartSpace/c:chart/c:legend", NameSpaceManager);
            TopNode.InnerXml = "<c:legendPos val=\"r\" /><c:layout />";
        }
    }
}