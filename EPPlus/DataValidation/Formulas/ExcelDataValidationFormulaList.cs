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
 * Mats Alm   		                Added       		        2011-01-08
 * Jan Källman		    License changed GPL-->LGPL  2011-12-27
 *******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using OfficeOpenXml.DataValidation.Formulas.Contracts;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.DataValidation.Formulas
{
    internal class ExcelDataValidationFormulaList : ExcelDataValidationFormula, IExcelDataValidationFormulaList
    {
        private readonly string _formulaPath;

        public ExcelDataValidationFormulaList(XmlNamespaceManager namespaceManager, XmlNode itemNode, string formulaPath)
            : base(namespaceManager, itemNode, formulaPath)
        {
            Require.Argument(formulaPath).IsNotNullOrEmpty("formulaPath");
            _formulaPath = formulaPath;
            var values = new DataValidationList();
            values.ListChanged += values_ListChanged;
            Values = values;
            SetInitialValues();
        }

        public IList<string> Values { get; }

        private void SetInitialValues()
        {
            string value = GetXmlNodeString(_formulaPath);
            if (!string.IsNullOrEmpty(value))
            {
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.TrimStart('"').TrimEnd('"');
                    string[] items = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in items)
                    {
                        Values.Add(item);
                    }
                }
                else
                {
                    ExcelFormula = value;
                }
            }
        }

        void values_ListChanged(object sender, EventArgs e)
        {
            if (Values.Count > 0)
            {
                State = FormulaState.Value;
            }

            string valuesAsString = GetValueAsString();
            // Excel supports max 255 characters in this field.
            if (valuesAsString.Length > 255)
            {
                throw new InvalidOperationException("The total length of a DataValidation list cannot exceed 255 characters");
            }

            SetXmlNodeString(_formulaPath, valuesAsString);
        }

        protected override string GetValueAsString()
        {
            var sb = new StringBuilder();
            foreach (string val in Values)
            {
                if (sb.Length == 0)
                {
                    sb.Append("\"");
                    sb.Append(val);
                }
                else
                {
                    sb.AppendFormat(",{0}", val);
                }
            }

            sb.Append("\"");
            return sb.ToString();
        }

        internal override void ResetValue()
        {
            Values.Clear();
        }

        #region class DataValidationList

        private class DataValidationList : IList<string>, ICollection
        {
            private readonly IList<string> _items = new List<string>();
            private EventHandler<EventArgs> _listChanged;

            public void CopyTo(Array array, int index)
            {
                _items.CopyTo((string[])array, index);
            }

            int ICollection.Count => _items.Count;

            public bool IsSynchronized => ((ICollection)_items).IsSynchronized;

            public object SyncRoot => ((ICollection)_items).SyncRoot;

            public event EventHandler<EventArgs> ListChanged
            {
                add => _listChanged += value;
                remove => _listChanged -= value;
            }

            private void OnListChanged()
            {
                if (_listChanged != null)
                {
                    _listChanged(this, EventArgs.Empty);
                }
            }

            #region IList members

            int IList<string>.IndexOf(string item)
            {
                return _items.IndexOf(item);
            }

            void IList<string>.Insert(int index, string item)
            {
                _items.Insert(index, item);
                OnListChanged();
            }

            void IList<string>.RemoveAt(int index)
            {
                _items.RemoveAt(index);
                OnListChanged();
            }

            string IList<string>.this[int index]
            {
                get => _items[index];
                set
                {
                    _items[index] = value;
                    OnListChanged();
                }
            }

            void ICollection<string>.Add(string item)
            {
                _items.Add(item);
                OnListChanged();
            }

            void ICollection<string>.Clear()
            {
                _items.Clear();
                OnListChanged();
            }

            bool ICollection<string>.Contains(string item)
            {
                return _items.Contains(item);
            }

            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                _items.CopyTo(array, arrayIndex);
            }

            int ICollection<string>.Count => _items.Count;

            bool ICollection<string>.IsReadOnly => false;

            bool ICollection<string>.Remove(string item)
            {
                bool retVal = _items.Remove(item);
                OnListChanged();
                return retVal;
            }

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            #endregion
        }

        #endregion
    }
}