﻿/* Copyright (C) 2011  Jan Källman
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
 * Mats Alm   		                Added		                2015-04-19
 *******************************************************************************/

using System.Collections.Generic;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.Database
{
    public class ExcelDatabaseRow
    {
        private readonly Dictionary<string, object> _items = new();
        private int _colIndex = 1;
        private readonly Dictionary<int, string> _fieldIndexes = new();

        public object this[string field]
        {
            get => _items[field];

            set
            {
                _items[field] = value;
                _fieldIndexes[_colIndex++] = field;
            }
        }

        public object this[int index]
        {
            get
            {
                string field = _fieldIndexes[index];
                return _items[field];
            }
        }
    }
}