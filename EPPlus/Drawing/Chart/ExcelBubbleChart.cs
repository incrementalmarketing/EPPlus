﻿using System;
using System.Globalization;
using System.Xml;
using OfficeOpenXml.Packaging;
using OfficeOpenXml.Table.PivotTable;

namespace OfficeOpenXml.Drawing.Chart
{
    /// <summary>
    /// Provides access to bubble chart specific properties
    /// </summary>
    public sealed class ExcelBubbleChart : ExcelChart
    {
        readonly string BUBBLE3D_PATH = "c:bubble3D/@val";
        readonly string BUBBLESCALE_PATH = "c:bubbleScale/@val";
        readonly string SHOWNEGBUBBLES_PATH = "c:showNegBubbles/@val";
        readonly string SIZEREPRESENTS_PATH = "c:sizeRepresents/@val";

        internal ExcelBubbleChart(ExcelDrawings drawings, XmlNode node, eChartType type, ExcelChart topChart, ExcelPivotTable PivotTableSource) :
            base(drawings, node, type, topChart, PivotTableSource)
        {
            ShowNegativeBubbles = false;
            BubbleScale = 100;
            _chartSeries = new ExcelBubbleChartSeries(this, drawings.NameSpaceManager, _chartNode, PivotTableSource != null);
            //SetTypeProperties();
        }

        internal ExcelBubbleChart(ExcelDrawings drawings, XmlNode node, eChartType type, bool isPivot) :
            base(drawings, node, type, isPivot)
        {
            _chartSeries = new ExcelBubbleChartSeries(this, drawings.NameSpaceManager, _chartNode, isPivot);
            //SetTypeProperties();
        }

        internal ExcelBubbleChart(ExcelDrawings drawings, XmlNode node, Uri uriChart, ZipPackagePart part, XmlDocument chartXml, XmlNode chartNode) :
            base(drawings, node, uriChart, part, chartXml, chartNode)
        {
            _chartSeries = new ExcelBubbleChartSeries(this, _drawings.NameSpaceManager, _chartNode, false);
            //SetTypeProperties();
        }

        internal ExcelBubbleChart(ExcelChart topChart, XmlNode chartNode) :
            base(topChart, chartNode)
        {
            _chartSeries = new ExcelBubbleChartSeries(this, _drawings.NameSpaceManager, _chartNode, false);
        }

        /// <summary>
        /// Specifies the scale factor for the bubble chart. Can range from 0 to 300, corresponding to a percentage of the default size,
        /// </summary>
        public int BubbleScale
        {
            get => _chartXmlHelper.GetXmlNodeInt(BUBBLESCALE_PATH);
            set
            {
                if (value is < 0 or > 300)
                {
                    throw new ArgumentOutOfRangeException("Bubblescale out of range. 0-300 allowed");
                }

                _chartXmlHelper.SetXmlNodeString(BUBBLESCALE_PATH, value.ToString());
            }
        }

        /// <summary>
        /// Specifies negative sized bubbles shall be shown on a bubble chart
        /// </summary>
        public bool ShowNegativeBubbles
        {
            get => _chartXmlHelper.GetXmlNodeBool(SHOWNEGBUBBLES_PATH);
            set => _chartXmlHelper.SetXmlNodeBool(BUBBLESCALE_PATH, value, true);
        }

        /// <summary>
        /// Specifies if the bubblechart is three dimensional
        /// </summary>
        public bool Bubble3D
        {
            get => _chartXmlHelper.GetXmlNodeBool(BUBBLE3D_PATH);
            set
            {
                _chartXmlHelper.SetXmlNodeBool(BUBBLE3D_PATH, value);
                ChartType = value ? eChartType.Bubble3DEffect : eChartType.Bubble;
            }
        }

        /// <summary>
        /// Specifies the scale factor for the bubble chart. Can range from 0 to 300, corresponding to a percentage of the default size,
        /// </summary>
        public eSizeRepresents SizeRepresents
        {
            get
            {
                string v = _chartXmlHelper.GetXmlNodeString(SIZEREPRESENTS_PATH).ToLower(CultureInfo.InvariantCulture);
                if (v == "w")
                {
                    return eSizeRepresents.Width;
                }

                return eSizeRepresents.Area;
            }
            set => _chartXmlHelper.SetXmlNodeString(SIZEREPRESENTS_PATH, value == eSizeRepresents.Width ? "w" : "area");
        }

        public new ExcelBubbleChartSeries Series => (ExcelBubbleChartSeries)_chartSeries;

        internal override eChartType GetChartType(string name)
        {
            if (Bubble3D)
            {
                return eChartType.Bubble3DEffect;
            }

            return eChartType.Bubble;
        }
    }
}