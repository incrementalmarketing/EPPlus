﻿using System;
using System.Globalization;
using System.Xml;

namespace OfficeOpenXml.Sparkline
{
    /// <summary>
    /// Represents a group of sparklines
    /// </summary>
    public class ExcelSparklineGroup : XmlHelper
    {
        const string _dispBlanksAsPath = "@displayEmptyCellsAs";
        const string _typePath = "@type";

        const string lineWidthPath = "x14:sparklineGroup/@lineWidth";
        // Schema here... https://msdn.microsoft.com/en-us/library/hh656506(v=office.12).aspx

        /*****
        <ext xmlns:x14="http://schemas.microsoft.com/office/spreadsheetml/2009/9/main" uri="{05C60535-1F16-4fd2-B633-F4F36F0B64E0}">
            <x14:sparklineGroups xmlns:xm="http://schemas.microsoft.com/office/excel/2006/main">
                <x14:sparklineGroup xr2:uid="{C83E6921-40D4-4DE5-9D8A-DC5D29881A86}" negative="1" displayEmptyCellsAs="gap" type="stacked">
                    <x14:colorSeries rgb="FF376092"/>
                    <x14:colorNegative rgb="FFD00000"/>
                    <x14:colorAxis rgb="FF000000"/>
                    <x14:colorMarkers rgb="FFD00000"/>
                    <x14:colorFirst rgb="FFD00000"/>
                    <x14:colorLast rgb="FFD00000"/>
                    <x14:colorHigh rgb="FFD00000"/>
                    <x14:colorLow rgb="FFD00000"/>
                    <x14:sparklines>
                        <x14:sparkline>
                            <xm:f>Sheet1!A1:A4</xm:f>
                            <xm:sqref>A7</xm:sqref>
                        </x14:sparkline>
                    </x14:sparklines>
                </x14:sparklineGroup>
            </x14:sparklineGroups>
        </ext>
          ****/
        /* Schema here...https://msdn.microsoft.com/en-us/library/hh656506(v=office.12).aspx
     <xsd:complexType name="CT_SparklineGroup">
<xsd:sequence>
<xsd:element name="colorSeries" minOccurs="0" maxOccurs="1" type="x:CT_Color"/>
<xsd:element name="colorNegative" minOccurs="0" maxOccurs="1" type="x:CT_Color"/>
<xsd:element name="colorAxis" minOccurs="0" maxOccurs="1" type="x:CT_Color"/>
<xsd:element name="colorMarkers" minOccurs="0" maxOccurs="1" type="x:CT_Color"/>
<xsd:element name="colorFirst" minOccurs="0" maxOccurs="1" type="x:CT_Color"/>
<xsd:element name="colorLast" minOccurs="0" maxOccurs="1" type="x:CT_Color"/>
<xsd:element name="colorHigh" minOccurs="0" maxOccurs="1" type="x:CT_Color"/>
<xsd:element name="colorLow" minOccurs="0" maxOccurs="1" type="x:CT_Color"/>
<xsd:element ref="xm:f" minOccurs="0" maxOccurs="1"/>
<xsd:element name="sparklines" type="CT_Sparklines" minOccurs="1" maxOccurs="1"/>
</xsd:sequence>
<xsd:attribute name="manualMax" type="xsd:double" use="optional"/>
<xsd:attribute name="manualMin" type="xsd:double" use="optional"/>
<xsd:attribute name="lineWeight" type="xsd:double" use="optional" default="0.75"/>
<xsd:attribute name="type" type="ST_SparklineType" use="optional" default="line"/>
<xsd:attribute name="dateAxis" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute name="displayEmptyCellsAs" type="ST_DispBlanksAs" use="optional" default="zero"/>
<xsd:attribute name="markers" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute name="high" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute name="low" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute name="first" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute name="last" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute name="negative" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute name="displayXAxis" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute name="displayHidden" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute name="minAxisType" type="ST_SparklineAxisMinMax" use="optional" default="individual"/>
<xsd:attribute name="maxAxisType" type="ST_SparklineAxisMinMax" use="optional" default="individual"/>
<xsd:attribute name="rightToLeft" type="xsd:boolean" use="optional" default="false"/>
<xsd:attribute ref="xr2:uid"/>
</xsd:complexType>
   */
        readonly ExcelWorksheet _ws;

        internal ExcelSparklineGroup(XmlNamespaceManager ns, XmlElement topNode, ExcelWorksheet ws) : base(ns, topNode)
        {
            SchemaNodeOrder = new[] { "colorSeries", "colorNegative", "colorAxis", "colorMarkers", "colorFirst", "colorLast", "colorHigh", "colorLow", "f", "sparklines" };
            Sparklines = new ExcelSparklineCollection(this);
            _ws = ws;
        }

        /// <summary>
        /// The range containing the dateaxis from the sparklines.
        /// Set to Null to remove the dateaxis.
        /// </summary>
        public ExcelRangeBase DateAxisRange
        {
            get
            {
                string f = GetXmlNodeString("xm:f");
                if (string.IsNullOrEmpty(f)) return null;
                var a = new ExcelAddressBase(f);
                if (a.WorkSheet.Equals(_ws.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return _ws.Cells[a.Address];
                }

                ExcelWorksheet ws = _ws.Workbook.Worksheets[a.WorkSheet];
                return ws.Cells[a.Address];
            }
            set
            {
                if (value == null)
                {
                    RemoveDateAxis();
                    return;
                }

                if (value.Worksheet.Workbook != _ws.Workbook)
                {
                    throw new ArgumentException("Range must be in the same package");
                }

                if (value.Rows != 1 && value.Columns != 1)
                {
                    throw new ArgumentException("Range must only be 1 row or column");
                }

                DateAxis = true;
                SetXmlNodeString("xm:f", value.FullAddress);
            }
        }

        /// <summary>
        /// The range containing the data from the sparklines
        /// </summary>
        public ExcelRangeBase DataRange
        {
            get
            {
                if (Sparklines.Count == 0)
                {
                    return null;
                }

                return _ws.Cells[Sparklines[0].RangeAddress._fromRow, Sparklines[0].RangeAddress._fromCol, Sparklines[^1].RangeAddress._toRow, Sparklines[^1].RangeAddress._toCol];
            }
        }

        /// <summary>
        /// The range containing the sparklines
        /// </summary>
        public ExcelRangeBase LocationRange
        {
            get
            {
                if (Sparklines.Count == 0)
                {
                    return null;
                }

                return _ws.Cells[Sparklines[0].Cell.Row, Sparklines[0].Cell.Column, Sparklines[^1].Cell.Row, Sparklines[^1].Cell.Column];
            }
        }

        /// <summary>
        /// The Sparklines for the sparklinegroup
        /// </summary>
        public ExcelSparklineCollection Sparklines { get; internal set; }

        public double LineWidth
        {
            get => GetXmlNodeDoubleNull(lineWidthPath) ?? 0.75;
            set => SetXmlNodeString(lineWidthPath, value.ToString(CultureInfo.InvariantCulture));
        }

        public eDispBlanksAs DisplayEmptyCellsAs
        {
            get
            {
                string s = GetXmlNodeString(_dispBlanksAsPath);
                if (string.IsNullOrEmpty(s))
                {
                    return eDispBlanksAs.Zero;
                }

                return (eDispBlanksAs)Enum.Parse(typeof(eDispBlanksAs), s, true);
            }
            set => SetXmlNodeString(_dispBlanksAsPath, value.ToString().ToLower());
        }

        /// <summary>
        /// Type of sparkline
        /// </summary>
        public eSparklineType Type
        {
            get
            {
                string type = GetXmlNodeString(_typePath);
                if (string.IsNullOrEmpty(type))
                {
                    return eSparklineType.Line;
                }

                return (eSparklineType)Enum.Parse(typeof(eSparklineType), type, true);
            }
            set => SetXmlNodeString(_typePath, value.ToString().ToLower());
        }

        private void RemoveDateAxis()
        {
            DeleteNode("xm:f");
            DateAxis = false;
        }

        #region Boolean settings

        const string _dateAxisPath = "@dateAxis";

        internal bool DateAxis
        {
            get => GetXmlNodeBool(_dateAxisPath, false);
            set => SetXmlNodeBool(_dateAxisPath, value);
        }

        const string _markersPath = "@markers";

        /// <summary>
        /// Highlight each point in each sparkline in the sparkline group.
        /// Applies to line sparklines only
        /// </summary>
        public bool Markers
        {
            get => GetXmlNodeBool(_markersPath, false);
            set => SetXmlNodeBool(_markersPath, value);
        }

        const string _highPath = "@high";

        /// <summary>
        /// Highlight the highest point of data in the sparkline group
        /// </summary>
        public bool High
        {
            get => GetXmlNodeBool(_highPath, false);
            set => SetXmlNodeBool(_highPath, value);
        }

        const string _lowPath = "@low";

        /// <summary>
        /// Highlight the lowest point of data in the sparkline group
        /// </summary>
        public bool Low
        {
            get => GetXmlNodeBool(_lowPath, false);
            set => SetXmlNodeBool(_lowPath, value);
        }

        const string _firstPath = "@first";

        /// <summary>
        /// Highlight the first point of data in the sparkline group
        /// </summary>
        public bool First
        {
            get => GetXmlNodeBool(_firstPath, false);
            set => SetXmlNodeBool(_firstPath, value);
        }

        const string _lastPath = "@last";

        /// <summary>
        /// Highlight the last point of data in the sparkline group
        /// </summary>
        public bool Last
        {
            get => GetXmlNodeBool(_lastPath, false);
            set => SetXmlNodeBool(_lastPath, value);
        }

        const string _negativePath = "@negative";

        /// <summary>
        /// Highlight negative points of data in the sparkline group with a different color or marker
        /// </summary>
        public bool Negative
        {
            get => GetXmlNodeBool(_negativePath);
            set => SetXmlNodeBool(_negativePath, value);
        }


        const string _displayXAxisPath = "@displayXAxis";

        public bool DisplayXAxis
        {
            get => GetXmlNodeBool(_displayXAxisPath);
            set => SetXmlNodeBool(_displayXAxisPath, value);
        }

        const string _displayHiddenPath = "@displayHidden";

        public bool DisplayHidden
        {
            get => GetXmlNodeBool(_displayHiddenPath);
            set => SetXmlNodeBool(_displayHiddenPath, value);
        }

        #endregion

        #region Colors

        const string _colorSeriesPath = "x14:colorSeries";

        /// <summary>
        /// Sparkline color
        /// </summary>
        public ExcelSparklineColor ColorSeries
        {
            get
            {
                CreateNode(_colorSeriesPath);
                return new ExcelSparklineColor(NameSpaceManager, TopNode.SelectSingleNode(_colorSeriesPath, NameSpaceManager));
            }
        }

        const string _colorNegativePath = "x14:colorNegative";

        /// <summary>
        /// Markercolor for the lowest negative point
        /// </summary>  
        public ExcelSparklineColor ColorNegative
        {
            get
            {
                CreateNode(_colorNegativePath);
                return new ExcelSparklineColor(NameSpaceManager, TopNode.SelectSingleNode(_colorNegativePath, NameSpaceManager));
            }
        }

        const string _colorAxisPath = "x14:colorAxis";

        /// <summary>
        /// Markercolor for the lowest negative point
        /// </summary>
        public ExcelSparklineColor ColorAxis
        {
            get
            {
                CreateNode(_colorAxisPath);
                return new ExcelSparklineColor(NameSpaceManager, TopNode.SelectSingleNode(_colorAxisPath, NameSpaceManager));
            }
        }

        const string _colorMarkersPath = "x14:colorMarkers";

        /// <summary>
        /// Default marker color 
        /// </summary> 
        public ExcelSparklineColor ColorMarkers
        {
            get
            {
                CreateNode(_colorMarkersPath);
                return new ExcelSparklineColor(NameSpaceManager, TopNode.SelectSingleNode(_colorMarkersPath, NameSpaceManager));
            }
        }

        const string _colorFirstPath = "x14:colorFirst";

        public ExcelSparklineColor ColorFirst
        {
            get
            {
                CreateNode(_colorFirstPath);
                return new ExcelSparklineColor(NameSpaceManager, TopNode.SelectSingleNode(_colorFirstPath, NameSpaceManager));
            }
        }

        const string _colorLastPath = "x14:colorLast";

        public ExcelSparklineColor ColorLast
        {
            get
            {
                CreateNode(_colorLastPath);
                return new ExcelSparklineColor(NameSpaceManager, TopNode.SelectSingleNode(_colorLastPath, NameSpaceManager));
            }
        }

        const string _colorHighPath = "x14:colorHigh";

        public ExcelSparklineColor ColorHigh
        {
            get
            {
                CreateNode(_colorHighPath);
                return new ExcelSparklineColor(NameSpaceManager, TopNode.SelectSingleNode(_colorHighPath, NameSpaceManager));
            }
        }

        const string _colorLowPath = "x14:colorLow";

        public ExcelSparklineColor ColorLow
        {
            get
            {
                CreateNode(_colorLowPath);
                return new ExcelSparklineColor(NameSpaceManager, TopNode.SelectSingleNode(_colorLowPath, NameSpaceManager));
            }
        }

        const string _manualMinPath = "@manualMin";

        public double ManualMin
        {
            get => GetXmlNodeDouble(_manualMinPath);
            set
            {
                SetXmlNodeString(_minAxisTypePath, "custom");
                SetXmlNodeString(_manualMinPath, value.ToString("F", CultureInfo.InvariantCulture));
            }
        }

        const string _manualMaxPath = "@manualMax";

        public double ManualMax
        {
            get => GetXmlNodeDouble(_manualMaxPath);
            set
            {
                SetXmlNodeString(_maxAxisTypePath, "custom");
                SetXmlNodeString(_manualMaxPath, value.ToString("F", CultureInfo.InvariantCulture));
            }
        }

        const string _minAxisTypePath = "@minAxisType";

        public eSparklineAxisMinMax MinAxisType
        {
            get
            {
                string s = GetXmlNodeString(_minAxisTypePath);
                if (string.IsNullOrEmpty(s))
                {
                    return eSparklineAxisMinMax.Individual;
                }

                return (eSparklineAxisMinMax)Enum.Parse(typeof(eSparklineAxisMinMax), s, true);
            }
            set
            {
                if (value == eSparklineAxisMinMax.Custom)
                {
                    ManualMin = 0;
                }
                else
                {
                    SetXmlNodeString(_minAxisTypePath, value.ToString());
                    DeleteNode(_manualMinPath);
                }
            }
        }

        const string _maxAxisTypePath = "@maxAxisType";

        public eSparklineAxisMinMax MaxAxisType
        {
            get
            {
                string s = GetXmlNodeString(_maxAxisTypePath);
                if (string.IsNullOrEmpty(s))
                {
                    return eSparklineAxisMinMax.Individual;
                }

                return (eSparklineAxisMinMax)Enum.Parse(typeof(eSparklineAxisMinMax), s, true);
            }
            set
            {
                if (value == eSparklineAxisMinMax.Custom)
                {
                    ManualMax = 0;
                }
                else
                {
                    SetXmlNodeString(_maxAxisTypePath, value.ToString());
                    DeleteNode(_manualMaxPath);
                }
            }
        }

        const string _rightToLeftPath = "@rightToLeft";

        public bool RightToLeft
        {
            get => GetXmlNodeBool(_rightToLeftPath, false);
            set => SetXmlNodeBool(_rightToLeftPath, value);
        }

        #endregion
    }
}