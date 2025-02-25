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
using System.Xml;
using OfficeOpenXml.Packaging;
using OfficeOpenXml.Table.PivotTable;

namespace OfficeOpenXml.Drawing.Chart
{
    /// <summary>
    /// A Surface chart
    /// </summary>
    public sealed class ExcelSurfaceChart : ExcelChart
    {
        const string WIREFRAME_PATH = "c:wireframe/@val";


        public ExcelChartSurface Floor { get; private set; }

        public ExcelChartSurface SideWall { get; private set; }

        public ExcelChartSurface BackWall { get; private set; }

        public bool Wireframe
        {
            get => _chartXmlHelper.GetXmlNodeBool(WIREFRAME_PATH);
            set => _chartXmlHelper.SetXmlNodeBool(WIREFRAME_PATH, value);
        }

        internal void SetTypeProperties()
        {
            if (ChartType is eChartType.SurfaceWireframe or eChartType.SurfaceTopViewWireframe)
            {
                Wireframe = true;
            }
            else
            {
                Wireframe = false;
            }

            if (ChartType is eChartType.SurfaceTopView or eChartType.SurfaceTopViewWireframe)
            {
                View3D.RotY = 0;
                View3D.RotX = 90;
            }
            else
            {
                View3D.RotY = 20;
                View3D.RotX = 15;
            }

            View3D.RightAngleAxes = false;
            View3D.Perspective = 0;
            Axis[1].CrossBetween = eCrossBetween.MidCat;
        }

        internal override eChartType GetChartType(string name)
        {
            if (Wireframe)
            {
                if (name == "surfaceChart")
                {
                    return eChartType.SurfaceTopViewWireframe;
                }

                return eChartType.SurfaceWireframe;
            }

            if (name == "surfaceChart")
            {
                return eChartType.SurfaceTopView;
            }

            return eChartType.Surface;
        }

        #region "Constructors"

        internal ExcelSurfaceChart(ExcelDrawings drawings, XmlNode node, eChartType type, ExcelChart topChart, ExcelPivotTable PivotTableSource) :
            base(drawings, node, type, topChart, PivotTableSource)
        {
            Init();
        }

        internal ExcelSurfaceChart(ExcelDrawings drawings, XmlNode node, Uri uriChart, ZipPackagePart part, XmlDocument chartXml, XmlNode chartNode) :
            base(drawings, node, uriChart, part, chartXml, chartNode)
        {
            Init();
        }

        internal ExcelSurfaceChart(ExcelChart topChart, XmlNode chartNode) :
            base(topChart, chartNode)
        {
            Init();
        }

        private void Init()
        {
            Floor = new ExcelChartSurface(NameSpaceManager, _chartXmlHelper.TopNode.SelectSingleNode("c:floor", NameSpaceManager));
            BackWall = new ExcelChartSurface(NameSpaceManager, _chartXmlHelper.TopNode.SelectSingleNode("c:sideWall", NameSpaceManager));
            SideWall = new ExcelChartSurface(NameSpaceManager, _chartXmlHelper.TopNode.SelectSingleNode("c:backWall", NameSpaceManager));
            SetTypeProperties();
        }

        #endregion
    }
}