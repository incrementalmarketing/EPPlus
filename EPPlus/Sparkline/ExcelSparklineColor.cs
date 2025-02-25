﻿using System;
using System.Drawing;
using System.Globalization;
using System.Xml;
using OfficeOpenXml.Style;

namespace OfficeOpenXml.Sparkline
{
    /// <summary>
    /// Sparkline colors
    /// </summary>
    public class ExcelSparklineColor : XmlHelper, IColor
    {
        internal ExcelSparklineColor(XmlNamespaceManager ns, XmlNode node) : base(ns, node)
        {
        }

        /// <summary>
        /// Indexed color
        /// </summary>
        public int Indexed
        {
            get => GetXmlNodeInt("@indexed");
            set
            {
                if (value is < 0 or > 65)
                {
                    throw new ArgumentOutOfRangeException("Index out of range");
                }

                SetXmlNodeString("@indexed", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// RGB 
        /// </summary>
        public string Rgb
        {
            get => GetXmlNodeString("@rgb");
            internal set => SetXmlNodeString("@rgb", value);
        }


        public string Theme => GetXmlNodeString("@theme");

        /// <summary>
        /// The tint value
        /// </summary>
        public decimal Tint
        {
            get => GetXmlNodeDecimal("@tint");
            set
            {
                if (value is > 1 or < -1)
                {
                    throw new ArgumentOutOfRangeException("Value must be between -1 and 1");
                }

                SetXmlNodeString("@tint", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Sets a color
        /// </summary>
        /// <param name="color">The color</param>
        public void SetColor(Color color)
        {
            Rgb = color.ToArgb().ToString("X");
        }
    }
}