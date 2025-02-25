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
 * Jan Källman		Initial Release		        2010-06-01
 * Jan Källman		License changed GPL-->LGPL 2011-12-16
 *******************************************************************************/

using System;
using System.Drawing;
using System.Globalization;
using System.Xml;

namespace OfficeOpenXml.Drawing.Vml
{
    /// <summary>
    /// Drawing object used for comments
    /// </summary>
    public class ExcelVmlDrawingComment : ExcelVmlDrawingBase, IRangeID
    {
        const string BACKGROUNDCOLOR_PATH = "@fillcolor";
        const string BACKGROUNDCOLOR2_PATH = "v:fill/@color2";
        const string COLUMN_PATH = "x:ClientData/x:Column";
        const string ENDCAP_PATH = "v:stroke/@endcap";
        const string HORIZONTAL_ALIGNMENT_PATH = "x:ClientData/x:TextHAlign";
        const string LINECOLOR_PATH = "@strokecolor";
        const string LINESTYLE_PATH = "v:stroke/@dashstyle";
        const string LINEWIDTH_PATH = "@strokeweight";
        const string LOCK_TEXT_PATH = "x:ClientData/x:LockText";
        const string LOCKED_PATH = "x:ClientData/x:Locked";
        const string ROW_PATH = "x:ClientData/x:Row";

        const string STYLE_PATH = "@style";

        ///// <summary>
        ///// Width of the Comment 
        ///// </summary>
        //public Single Width
        //{
        //    get
        //    {
        //        string v;
        //        GetStyle("width", out v);
        //        if(v.EndsWith("pt"))
        //        {
        //            v = v.Substring(0, v.Length - 2);
        //        }
        //        short ret;
        //        if (short.TryParse(v,System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out ret))
        //        {
        //            return ret;
        //        }
        //        else
        //        {
        //            return 0;
        //        }
        //    }
        //    set
        //    {
        //        SetStyle("width", value.ToString("N2",CultureInfo.InvariantCulture) + "pt");
        //    }
        //}
        ///// <summary>
        ///// Height of the Comment 
        ///// </summary>
        //public Single Height
        //{
        //    get
        //    {
        //        string v;
        //        GetStyle("height", out v);
        //        if (v.EndsWith("pt"))
        //        {
        //            v = v.Substring(0, v.Length - 2);
        //        }
        //        short ret;
        //        if (short.TryParse(v, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out ret))
        //        {
        //            return ret;
        //        }
        //        else
        //        {
        //            return 0;
        //        }
        //    }
        //    set
        //    {
        //        SetStyle("height", value.ToString("N2", CultureInfo.InvariantCulture) + "pt");
        //    }
        //}
        const string TEXTBOX_STYLE_PATH = "v:textbox/@style";

        const string VERTICAL_ALIGNMENT_PATH = "x:ClientData/x:TextVAlign";
        const string VISIBLE_PATH = "x:ClientData/x:Visible";
        ExcelVmlDrawingPosition _from;
        ExcelVmlDrawingPosition _to;

        internal ExcelVmlDrawingComment(XmlNode topNode, ExcelRangeBase range, XmlNamespaceManager ns) :
            base(topNode, ns)
        {
            Range = range;
            SchemaNodeOrder = new[] { "fill", "stroke", "shadow", "path", "textbox", "ClientData", "MoveWithCells", "SizeWithCells", "Anchor", "Locked", "AutoFill", "LockText", "TextHAlign", "TextVAlign", "Row", "Column", "Visible" };
        }

        internal ExcelRangeBase Range { get; set; }

        /// <summary>
        /// Address in the worksheet
        /// </summary>
        public string Address => Range.Address;

        /// <summary>
        /// Vertical alignment for text
        /// </summary>
        public eTextAlignVerticalVml VerticalAlignment
        {
            get
            {
                switch (GetXmlNodeString(VERTICAL_ALIGNMENT_PATH))
                {
                    case "Center":
                        return eTextAlignVerticalVml.Center;
                    case "Bottom":
                        return eTextAlignVerticalVml.Bottom;
                    default:
                        return eTextAlignVerticalVml.Top;
                }
            }
            set
            {
                switch (value)
                {
                    case eTextAlignVerticalVml.Center:
                        SetXmlNodeString(VERTICAL_ALIGNMENT_PATH, "Center");
                        break;
                    case eTextAlignVerticalVml.Bottom:
                        SetXmlNodeString(VERTICAL_ALIGNMENT_PATH, "Bottom");
                        break;
                    default:
                        DeleteNode(VERTICAL_ALIGNMENT_PATH);
                        break;
                }
            }
        }

        /// <summary>
        /// Horizontal alignment for text
        /// </summary>
        public eTextAlignHorizontalVml HorizontalAlignment
        {
            get
            {
                switch (GetXmlNodeString(HORIZONTAL_ALIGNMENT_PATH))
                {
                    case "Center":
                        return eTextAlignHorizontalVml.Center;
                    case "Right":
                        return eTextAlignHorizontalVml.Right;
                    default:
                        return eTextAlignHorizontalVml.Left;
                }
            }
            set
            {
                switch (value)
                {
                    case eTextAlignHorizontalVml.Center:
                        SetXmlNodeString(HORIZONTAL_ALIGNMENT_PATH, "Center");
                        break;
                    case eTextAlignHorizontalVml.Right:
                        SetXmlNodeString(HORIZONTAL_ALIGNMENT_PATH, "Right");
                        break;
                    default:
                        DeleteNode(HORIZONTAL_ALIGNMENT_PATH);
                        break;
                }
            }
        }

        /// <summary>
        /// If the drawing object is visible.
        /// </summary>
        public bool Visible
        {
            get => TopNode.SelectSingleNode(VISIBLE_PATH, NameSpaceManager) != null;
            set
            {
                if (value)
                {
                    CreateNode(VISIBLE_PATH);
                    Style = SetStyle(Style, "visibility", "visible");
                }
                else
                {
                    DeleteNode(VISIBLE_PATH);
                    Style = SetStyle(Style, "visibility", "hidden");
                }
            }
        }

        /// <summary>
        /// Background color
        /// </summary>
        public Color BackgroundColor
        {
            get
            {
                string col = GetXmlNodeString(BACKGROUNDCOLOR_PATH);
                if (col == "")
                {
                    return Color.FromArgb(0xff, 0xff, 0xe1);
                }

                if (col.StartsWith("#")) col = col.Substring(1, col.Length - 1);
                if (int.TryParse(col, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int res))
                {
                    return Color.FromArgb(res);
                }

                return Color.Empty;
            }
            set
            {
                string color = "#" + value.ToArgb().ToString("X").Substring(2, 6);
                SetXmlNodeString(BACKGROUNDCOLOR_PATH, color);
                //SetXmlNode(BACKGROUNDCOLOR2_PATH, color);
            }
        }

        /// <summary>
        /// Linestyle for border
        /// </summary>
        public eLineStyleVml LineStyle
        {
            get
            {
                string v = GetXmlNodeString(LINESTYLE_PATH);
                if (v == "")
                {
                    return eLineStyleVml.Solid;
                }

                if (v == "1 1")
                {
                    v = GetXmlNodeString(ENDCAP_PATH);
                    return (eLineStyleVml)Enum.Parse(typeof(eLineStyleVml), v, true);
                }

                return (eLineStyleVml)Enum.Parse(typeof(eLineStyleVml), v, true);
            }
            set
            {
                if (value is eLineStyleVml.Round or eLineStyleVml.Square)
                {
                    SetXmlNodeString(LINESTYLE_PATH, "1 1");
                    if (value == eLineStyleVml.Round)
                    {
                        SetXmlNodeString(ENDCAP_PATH, "round");
                    }
                    else
                    {
                        DeleteNode(ENDCAP_PATH);
                    }
                }
                else
                {
                    string v = value.ToString();
                    v = v.Substring(0, 1).ToLower(CultureInfo.InvariantCulture) + v.Substring(1, v.Length - 1);
                    SetXmlNodeString(LINESTYLE_PATH, v);
                    DeleteNode(ENDCAP_PATH);
                }
            }
        }

        /// <summary>
        /// Line color 
        /// </summary>
        public Color LineColor
        {
            get
            {
                string col = GetXmlNodeString(LINECOLOR_PATH);
                if (col == "")
                {
                    return Color.Black;
                }

                if (col.StartsWith("#")) col = col.Substring(1, col.Length - 1);
                if (int.TryParse(col, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int res))
                {
                    return Color.FromArgb(res);
                }

                return Color.Empty;
            }
            set
            {
                string color = "#" + value.ToArgb().ToString("X").Substring(2, 6);
                SetXmlNodeString(LINECOLOR_PATH, color);
            }
        }

        /// <summary>
        /// Width of the border
        /// </summary>
        public float LineWidth
        {
            get
            {
                string wt = GetXmlNodeString(LINEWIDTH_PATH);
                if (wt == "") return (float).75;
                if (wt.EndsWith("pt")) wt = wt.Substring(0, wt.Length - 2);

                if (float.TryParse(wt, NumberStyles.Any, CultureInfo.InvariantCulture, out float ret))
                {
                    return ret;
                }

                return 0;
            }
            set => SetXmlNodeString(LINEWIDTH_PATH, value.ToString(CultureInfo.InvariantCulture) + "pt");
        }

        /// <summary>
        /// Autofits the drawingobject 
        /// </summary>
        public bool AutoFit
        {
            get
            {
                GetStyle(GetXmlNodeString(TEXTBOX_STYLE_PATH), "mso-fit-shape-to-text", out string value);
                return value == "t";
            }
            set => SetXmlNodeString(TEXTBOX_STYLE_PATH, SetStyle(GetXmlNodeString(TEXTBOX_STYLE_PATH), "mso-fit-shape-to-text", value ? "t" : ""));
        }

        /// <summary>
        /// If the object is locked when the sheet is protected
        /// </summary>
        public bool Locked
        {
            get => GetXmlNodeBool(LOCKED_PATH, false);
            set => SetXmlNodeBool(LOCKED_PATH, value, false);
        }

        /// <summary>
        /// Specifies that the object's text is locked
        /// </summary>
        public bool LockText
        {
            get => GetXmlNodeBool(LOCK_TEXT_PATH, false);
            set => SetXmlNodeBool(LOCK_TEXT_PATH, value, false);
        }

        /// <summary>
        /// From position. For comments only when Visible=true.
        /// </summary>
        public ExcelVmlDrawingPosition From
        {
            get
            {
                if (_from == null)
                {
                    _from = new ExcelVmlDrawingPosition(NameSpaceManager, TopNode.SelectSingleNode("x:ClientData", NameSpaceManager), 0);
                }

                return _from;
            }
        }

        /// <summary>
        /// To position. For comments only when Visible=true.
        /// </summary>
        public ExcelVmlDrawingPosition To
        {
            get
            {
                if (_to == null)
                {
                    _to = new ExcelVmlDrawingPosition(NameSpaceManager, TopNode.SelectSingleNode("x:ClientData", NameSpaceManager), 4);
                }

                return _to;
            }
        }

        /// <summary>
        /// Row position for a comment
        /// </summary>
        internal int Row
        {
            get => GetXmlNodeInt(ROW_PATH);
            set => SetXmlNodeString(ROW_PATH, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Column position for a comment
        /// </summary>
        internal int Column
        {
            get => GetXmlNodeInt(COLUMN_PATH);
            set => SetXmlNodeString(COLUMN_PATH, value.ToString(CultureInfo.InvariantCulture));
        }

        internal string Style
        {
            get => GetXmlNodeString(STYLE_PATH);
            set => SetXmlNodeString(STYLE_PATH, value);
        }

        #region IRangeID Members

        ulong IRangeID.RangeID
        {
            get => ExcelCellBase.GetCellID(Range.Worksheet.SheetID, Range.Start.Row, Range.Start.Column);
            set { }
        }

        #endregion
    }
}