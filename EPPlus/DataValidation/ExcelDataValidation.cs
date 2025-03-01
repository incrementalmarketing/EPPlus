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
 * Mats Alm   		                Added       		        2011-01-01
 * Jan Källman		                License changed GPL-->LGPL  2011-12-27
 *******************************************************************************/

using System;
using System.Text.RegularExpressions;
using System.Xml;
using OfficeOpenXml.DataValidation.Contracts;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.DataValidation
{
    /// <summary>
    /// Excel datavalidation
    /// </summary>
    public abstract class ExcelDataValidation : XmlHelper, IExcelDataValidation
    {
        private const string _itemElementNodeName = "d:dataValidation";
        private readonly string _allowBlankPath = "@allowBlank";
        private readonly string _errorPath = "@error";


        private readonly string _errorStylePath = "@errorStyle";
        private readonly string _errorTitlePath = "@errorTitle";
        protected readonly string _formula1Path = "d:formula1";
        protected readonly string _formula2Path = "d:formula2";
        private readonly string _operatorPath = "@operator";
        private readonly string _promptPath = "@prompt";
        private readonly string _promptTitlePath = "@promptTitle";
        private readonly string _showErrorMessagePath = "@showErrorMessage";
        private readonly string _showInputMessagePath = "@showInputMessage";
        private readonly string _sqrefPath = "@sqref";
        private readonly string _typeMessagePath = "@type";

        internal ExcelDataValidation(ExcelWorksheet worksheet, string address, ExcelDataValidationType validationType)
            : this(worksheet, address, validationType, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="worksheet">worksheet that owns the validation</param>
        /// <param name="itemElementNode">Xml top node (dataValidations)</param>
        /// <param name="validationType">Data validation type</param>
        /// <param name="address">address for data validation</param>
        internal ExcelDataValidation(ExcelWorksheet worksheet, string address, ExcelDataValidationType validationType, XmlNode itemElementNode)
            : this(worksheet, address, validationType, itemElementNode, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="worksheet">worksheet that owns the validation</param>
        /// <param name="itemElementNode">Xml top node (dataValidations) when importing xml</param>
        /// <param name="validationType">Data validation type</param>
        /// <param name="address">address for data validation</param>
        /// <param name="namespaceManager">Xml Namespace manager</param>
        internal ExcelDataValidation(ExcelWorksheet worksheet, string address, ExcelDataValidationType validationType, XmlNode itemElementNode, XmlNamespaceManager namespaceManager)
            : base(namespaceManager ?? worksheet.NameSpaceManager)
        {
            Require.Argument(address).IsNotNullOrEmpty("address");
            address = CheckAndFixRangeAddress(address);
            if (itemElementNode == null)
            {
                //var xmlDoc = worksheet.WorksheetXml;
                TopNode = worksheet.WorksheetXml.SelectSingleNode("//d:dataValidations", worksheet.NameSpaceManager);
                // did not succeed using the XmlHelper methods here... so I'm creating the new node using XmlDocument...
                string nsUri = NameSpaceManager.LookupNamespace("d");
                //itemElementNode = TopNode.OwnerDocument.CreateElement(_itemElementNodeName, nsUri);
                itemElementNode = TopNode.OwnerDocument.CreateElement(_itemElementNodeName.Split(':')[1], nsUri);
                TopNode.AppendChild(itemElementNode);
            }

            TopNode = itemElementNode;
            ValidationType = validationType;
            Address = new ExcelAddress(address);
            Init();
        }

        /// <summary>
        /// This method will validate the state of the validation
        /// </summary>
        /// <exception cref="InvalidOperationException">If the state breaks the rules of the validation</exception>
        public virtual void Validate()
        {
            string address = Address.Address;
            // validate Formula1
            if (string.IsNullOrEmpty(Formula1Internal))
            {
                throw new InvalidOperationException("Validation of " + address + " failed: Formula1 cannot be empty");
            }
        }

        private void Init()
        {
            // set schema node order
            if (ValidationType == ExcelDataValidationType.List)
            {
                SchemaNodeOrder = new[]
                {
                    "type",
                    "errorStyle",
                    "allowBlank",
                    "showInputMessage",
                    "showErrorMessage",
                    "errorTitle",
                    "error",
                    "promptTitle",
                    "prompt",
                    "sqref",
                    "formula1"
                };
            }
            else
            {
                SchemaNodeOrder = new[]
                {
                    "type",
                    "errorStyle",
                    "operator",
                    "allowBlank",
                    "showInputMessage",
                    "showErrorMessage",
                    "errorTitle",
                    "error",
                    "promptTitle",
                    "prompt",
                    "sqref",
                    "formula1",
                    "formula2"
                };
            }
        }

        private string CheckAndFixRangeAddress(string address)
        {
            if (address.Contains(','))
            {
                throw new FormatException("Multiple addresses may not be commaseparated, use space instead");
            }

            address = ConvertUtil._invariantTextInfo.ToUpper(address);
            if (Regex.IsMatch(address, @"[A-Z]+:[A-Z]+"))
            {
                address = AddressUtility.ParseEntireColumnSelections(address);
            }

            return address;
        }

        private void SetNullableBoolValue(string path, bool? val)
        {
            if (val.HasValue)
            {
                SetXmlNodeBool(path, val.Value);
            }
            else
            {
                DeleteNode(path);
            }
        }

        protected void SetValue<T>(T? val, string path)
            where T : struct
        {
            if (!val.HasValue)
            {
                DeleteNode(path);
            }

            string stringValue = val.Value.ToString().Replace(',', '.');
            SetXmlNodeString(path, stringValue);
        }

        internal void SetAddress(string address)
        {
            string dvAddress = AddressUtility.ParseEntireColumnSelections(address);
            SetXmlNodeString(_sqrefPath, dvAddress);
        }

        #region Public properties

        /// <summary>
        /// True if the validation type allows operator to be set.
        /// </summary>
        public bool AllowsOperator => ValidationType.AllowOperator;

        /// <summary>
        /// Address of data validation
        /// </summary>
        public ExcelAddress Address
        {
            get => new(GetXmlNodeString(_sqrefPath));
            private set => SetAddress(value.Address);
        }

        /// <summary>
        /// Validation type
        /// </summary>
        public ExcelDataValidationType ValidationType
        {
            get
            {
                string typeString = GetXmlNodeString(_typeMessagePath);
                return ExcelDataValidationType.GetBySchemaName(typeString);
            }
            private set => SetXmlNodeString(_typeMessagePath, value.SchemaName, true);
        }

        /// <summary>
        /// Operator for comparison between the entered value and Formula/Formulas.
        /// </summary>
        public ExcelDataValidationOperator Operator
        {
            get
            {
                string operatorString = GetXmlNodeString(_operatorPath);
                if (!string.IsNullOrEmpty(operatorString))
                {
                    return (ExcelDataValidationOperator)Enum.Parse(typeof(ExcelDataValidationOperator), operatorString);
                }

                return default;
            }
            set
            {
                if (!ValidationType.AllowOperator)
                {
                    throw new InvalidOperationException("The current validation type does not allow operator to be set");
                }

                SetXmlNodeString(_operatorPath, value.ToString());
            }
        }

        /// <summary>
        /// Warning style
        /// </summary>
        public ExcelDataValidationWarningStyle ErrorStyle
        {
            get
            {
                string errorStyleString = GetXmlNodeString(_errorStylePath);
                if (!string.IsNullOrEmpty(errorStyleString))
                {
                    return (ExcelDataValidationWarningStyle)Enum.Parse(typeof(ExcelDataValidationWarningStyle), errorStyleString);
                }

                return ExcelDataValidationWarningStyle.undefined;
            }
            set
            {
                if (value == ExcelDataValidationWarningStyle.undefined)
                {
                    DeleteNode(_errorStylePath);
                }

                SetXmlNodeString(_errorStylePath, value.ToString());
            }
        }

        /// <summary>
        /// True if blanks should be allowed
        /// </summary>
        public bool? AllowBlank
        {
            get => GetXmlNodeBoolNullable(_allowBlankPath);
            set => SetNullableBoolValue(_allowBlankPath, value);
        }

        /// <summary>
        /// True if input message should be shown
        /// </summary>
        public bool? ShowInputMessage
        {
            get => GetXmlNodeBoolNullable(_showInputMessagePath);
            set => SetNullableBoolValue(_showInputMessagePath, value);
        }

        /// <summary>
        /// True if error message should be shown
        /// </summary>
        public bool? ShowErrorMessage
        {
            get => GetXmlNodeBoolNullable(_showErrorMessagePath);
            set => SetNullableBoolValue(_showErrorMessagePath, value);
        }

        /// <summary>
        /// Title of error message box
        /// </summary>
        public string ErrorTitle
        {
            get => GetXmlNodeString(_errorTitlePath);
            set => SetXmlNodeString(_errorTitlePath, value);
        }

        /// <summary>
        /// Error message box text
        /// </summary>
        public string Error
        {
            get => GetXmlNodeString(_errorPath);
            set => SetXmlNodeString(_errorPath, value);
        }

        public string PromptTitle
        {
            get => GetXmlNodeString(_promptTitlePath);
            set => SetXmlNodeString(_promptTitlePath, value);
        }

        public string Prompt
        {
            get => GetXmlNodeString(_promptPath);
            set => SetXmlNodeString(_promptPath, value);
        }

        /// <summary>
        /// Formula 1
        /// </summary>
        protected string Formula1Internal => GetXmlNodeString(_formula1Path);

        /// <summary>
        /// Formula 2
        /// </summary>
        protected string Formula2Internal => GetXmlNodeString(_formula2Path);

        #endregion
    }
}