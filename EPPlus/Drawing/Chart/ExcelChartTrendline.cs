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
 * Jan Källman		Initial Release		        2011-05-25
 * Jan Källman		License changed GPL-->LGPL 2011-12-16
 *******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace OfficeOpenXml.Drawing.Chart
{
    /// <summary>
    /// A collection of trendlines.
    /// </summary>
    public class ExcelChartTrendlineCollection : IEnumerable<ExcelChartTrendline>
    {
        readonly List<ExcelChartTrendline> _list = new();
        readonly ExcelChartSerie _serie;

        internal ExcelChartTrendlineCollection(ExcelChartSerie serie)
        {
            _serie = serie;
            foreach (XmlNode node in _serie.TopNode.SelectNodes("c:trendline", _serie.NameSpaceManager))
            {
                _list.Add(new ExcelChartTrendline(_serie.NameSpaceManager, node));
            }
        }

        IEnumerator<ExcelChartTrendline> IEnumerable<ExcelChartTrendline>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>
        /// Add a new trendline
        /// </summary>
        /// <param name="Type"></param>
        /// <returns>The trendline</returns>
        public ExcelChartTrendline Add(eTrendLine Type)
        {
            if (_serie._chartSeries._chart.IsType3D() ||
                _serie._chartSeries._chart.IsTypePercentStacked() ||
                _serie._chartSeries._chart.IsTypeStacked() ||
                _serie._chartSeries._chart.IsTypePieDoughnut())
            {
                throw new ArgumentException("Trendlines don't apply to 3d-charts, stacked charts, pie charts or doughnut charts");
            }

            XmlNode insertAfter;
            if (_list.Count > 0)
            {
                insertAfter = _list[^1].TopNode;
            }
            else
            {
                insertAfter = _serie.TopNode.SelectSingleNode("c:marker", _serie.NameSpaceManager);
                if (insertAfter == null)
                {
                    insertAfter = _serie.TopNode.SelectSingleNode("c:tx", _serie.NameSpaceManager);
                    if (insertAfter == null)
                    {
                        insertAfter = _serie.TopNode.SelectSingleNode("c:order", _serie.NameSpaceManager);
                    }
                }
            }

            XmlElement node = _serie.TopNode.OwnerDocument.CreateElement("c", "trendline", ExcelPackage.schemaChart);
            _serie.TopNode.InsertAfter(node, insertAfter);

            var tl = new ExcelChartTrendline(_serie.NameSpaceManager, node);
            tl.Type = Type;
            return tl;
        }
    }

    /// <summary>
    /// A trendline object
    /// </summary>
    public class ExcelChartTrendline : XmlHelper
    {
        const string BACKWARDPATH = "c:backward/@val";
        const string DISPLAYEQUATIONPATH = "c:dispEq/@val";
        const string DISPLAYRSQUAREDVALUEPATH = "c:dispRSqr/@val";
        const string FORWARDPATH = "c:forward/@val";
        const string INTERCEPTPATH = "c:intercept/@val";
        const string NAMEPATH = "c:name";
        const string ORDERPATH = "c:order/@val";
        const string PERIODPATH = "c:period/@val";
        const string TRENDLINEPATH = "c:trendlineType/@val";

        internal ExcelChartTrendline(XmlNamespaceManager namespaceManager, XmlNode topNode) :
            base(namespaceManager, topNode)

        {
            SchemaNodeOrder = new[] { "name", "trendlineType", "order", "period", "forward", "backward", "intercept", "dispRSqr", "dispEq", "trendlineLbl" };
        }

        /// <summary>
        /// Type of Trendline
        /// </summary>
        public eTrendLine Type
        {
            get
            {
                switch (GetXmlNodeString(TRENDLINEPATH).ToLower(CultureInfo.InvariantCulture))
                {
                    case "exp":
                        return eTrendLine.Exponential;
                    case "log":
                        return eTrendLine.Logarithmic;
                    case "poly":
                        return eTrendLine.Polynomial;
                    case "movingavg":
                        return eTrendLine.MovingAvgerage;
                    case "power":
                        return eTrendLine.Power;
                    default:
                        return eTrendLine.Linear;
                }
            }
            set
            {
                switch (value)
                {
                    case eTrendLine.Exponential:
                        SetXmlNodeString(TRENDLINEPATH, "exp");
                        break;
                    case eTrendLine.Logarithmic:
                        SetXmlNodeString(TRENDLINEPATH, "log");
                        break;
                    case eTrendLine.Polynomial:
                        SetXmlNodeString(TRENDLINEPATH, "poly");
                        Order = 2;
                        break;
                    case eTrendLine.MovingAvgerage:
                        SetXmlNodeString(TRENDLINEPATH, "movingAvg");
                        Period = 2;
                        break;
                    case eTrendLine.Power:
                        SetXmlNodeString(TRENDLINEPATH, "power");
                        break;
                    default:
                        SetXmlNodeString(TRENDLINEPATH, "linear");
                        break;
                }
            }
        }

        /// <summary>
        /// Name in the legend
        /// </summary>
        public string Name
        {
            get => GetXmlNodeString(NAMEPATH);
            set => SetXmlNodeString(NAMEPATH, value, true);
        }

        /// <summary>
        /// Order for polynominal trendlines
        /// </summary>
        public decimal Order
        {
            get => GetXmlNodeDecimal(ORDERPATH);
            set
            {
                if (Type == eTrendLine.MovingAvgerage)
                {
                    throw new ArgumentException("Can't set period for trendline type MovingAvgerage");
                }

                DeleteAllNode(PERIODPATH);
                SetXmlNodeString(ORDERPATH, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Period for monthly average trendlines
        /// </summary>
        public decimal Period
        {
            get => GetXmlNodeDecimal(PERIODPATH);
            set
            {
                if (Type == eTrendLine.Polynomial)
                {
                    throw new ArgumentException("Can't set period for trendline type Polynomial");
                }

                DeleteAllNode(ORDERPATH);
                SetXmlNodeString(PERIODPATH, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Forcast forward periods
        /// </summary>
        public decimal Forward
        {
            get => GetXmlNodeDecimal(FORWARDPATH);
            set => SetXmlNodeString(FORWARDPATH, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Forcast backwards periods
        /// </summary>
        public decimal Backward
        {
            get => GetXmlNodeDecimal(BACKWARDPATH);
            set => SetXmlNodeString(BACKWARDPATH, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Specify the point where the trendline crosses the vertical axis
        /// </summary>
        public decimal Intercept
        {
            get => GetXmlNodeDecimal(INTERCEPTPATH);
            set => SetXmlNodeString(INTERCEPTPATH, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Display the R-squared value for a trendline
        /// </summary>
        public bool DisplayRSquaredValue
        {
            get => GetXmlNodeBool(DISPLAYRSQUAREDVALUEPATH, true);
            set => SetXmlNodeBool(DISPLAYRSQUAREDVALUEPATH, value, true);
        }

        /// <summary>
        /// Display the trendline equation on the chart
        /// </summary>
        public bool DisplayEquation
        {
            get => GetXmlNodeBool(DISPLAYEQUATIONPATH, true);
            set => SetXmlNodeBool(DISPLAYEQUATIONPATH, value, true);
        }
    }
}