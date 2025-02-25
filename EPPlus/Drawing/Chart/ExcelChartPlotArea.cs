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
 * Jan Källman		Added		2009-12-30
 * Jan Källman		License changed GPL-->LGPL 2011-12-16
 *******************************************************************************/

using System;
using System.Xml;

namespace OfficeOpenXml.Drawing.Chart
{
    /// <summary>
    /// A charts plot area
    /// </summary>
    public sealed class ExcelChartPlotArea : XmlHelper
    {
        ExcelDrawingBorder _border;

        ExcelChartCollection _chartTypes;
        ExcelDrawingFill _fill;
        readonly ExcelChart _firstChart;

        internal ExcelChartPlotArea(XmlNamespaceManager ns, XmlNode node, ExcelChart firstChart)
            : base(ns, node)
        {
            _firstChart = firstChart;
            if (TopNode.SelectSingleNode("c:dTable", NameSpaceManager) != null)
            {
                DataTable = new ExcelChartDataTable(NameSpaceManager, TopNode);
            }
        }

        public ExcelChartCollection ChartTypes
        {
            get
            {
                if (_chartTypes == null)
                {
                    _chartTypes = new ExcelChartCollection(_firstChart);
                }

                return _chartTypes;
            }
        }

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

        #region Data table

        /// <summary>
        /// Creates a data table in the plotarea
        /// The datatable can also be accessed via the DataTable propery
        /// <see cref="DataTable"/>
        /// </summary>
        public ExcelChartDataTable CreateDataTable()
        {
            if (DataTable != null)
            {
                throw new InvalidOperationException("Data table already exists");
            }

            DataTable = new ExcelChartDataTable(NameSpaceManager, TopNode);
            return DataTable;
        }

        /// <summary>
        /// Remove the data table if it's created in the plotarea
        /// </summary>
        public void RemoveDataTable()
        {
            DeleteAllNode("c:dTable");
            DataTable = null;
        }

        /// <summary>
        /// The data table object.
        /// Use the CreateDataTable method to create a datatable if it does not exist.
        /// <see cref="CreateDataTable"/>
        /// <see cref="RemoveDataTable"/>
        /// </summary>
        public ExcelChartDataTable DataTable { get; private set; }

        #endregion
    }
}