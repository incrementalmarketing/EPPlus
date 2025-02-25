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
 * Jan Källman		Initial Release		        2009-10-01
 * Jan Källman		License changed GPL-->LGPL 2011-12-16
 *******************************************************************************/

using System;
using System.Globalization;
using System.Xml;

namespace OfficeOpenXml.Drawing.Chart
{
    /// <summary>
    /// A serie for a scatter chart
    /// </summary>
    public sealed class ExcelRadarChartSerie : ExcelChartSerie
    {
        const string markerPath = "c:marker/c:symbol/@val";
        const string MARKERSIZE_PATH = "c:marker/c:size/@val";
        ExcelChartSerieDataLabel _DataLabel;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="chartSeries">Parent collection</param>
        /// <param name="ns">Namespacemanager</param>
        /// <param name="node">Topnode</param>
        /// <param name="isPivot">Is pivotchart</param>
        internal ExcelRadarChartSerie(ExcelChartSeries chartSeries, XmlNamespaceManager ns, XmlNode node, bool isPivot) :
            base(chartSeries, ns, node, isPivot)
        {
            if (chartSeries.Chart.ChartType == eChartType.Radar)
            {
                Marker = eMarkerStyle.None;
            }
        }

        /// <summary>
        /// Datalabel
        /// </summary>
        public ExcelChartSerieDataLabel DataLabel
        {
            get
            {
                if (_DataLabel == null)
                {
                    _DataLabel = new ExcelChartSerieDataLabel(_ns, _node);
                }

                return _DataLabel;
            }
        }

        /// <summary>
        /// Marker symbol 
        /// </summary>
        public eMarkerStyle Marker
        {
            get
            {
                string marker = GetXmlNodeString(markerPath);
                if (marker is "" or "none")
                {
                    return eMarkerStyle.None;
                }

                return (eMarkerStyle)Enum.Parse(typeof(eMarkerStyle), marker, true);
            }
            internal set => SetXmlNodeString(markerPath, value.ToString().ToLower(CultureInfo.InvariantCulture));
        }

        public int MarkerSize
        {
            get => GetXmlNodeInt(MARKERSIZE_PATH);
            set
            {
                if (value is < 2 or > 72)
                {
                    throw new ArgumentOutOfRangeException("MarkerSize out of range. Range from 2-72 allowed.");
                }

                SetXmlNodeString(MARKERSIZE_PATH, value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}