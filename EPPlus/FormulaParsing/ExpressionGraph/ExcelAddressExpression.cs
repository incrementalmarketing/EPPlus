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
 * Mats Alm   		                Added       		        2013-03-01 (Prior file history on https://github.com/swmal/ExcelFormulaParser)
 *******************************************************************************/

using System;
using System.Linq;
using OfficeOpenXml.FormulaParsing.ExcelUtilities;
using OfficeOpenXml.FormulaParsing.Utilities;

namespace OfficeOpenXml.FormulaParsing.ExpressionGraph
{
    public class ExcelAddressExpression : AtomicExpression
    {
        private readonly ExcelDataProvider _excelDataProvider;
        private readonly bool _negate;
        private readonly ParsingContext _parsingContext;
        private readonly RangeAddressFactory _rangeAddressFactory;

        public ExcelAddressExpression(string expression, ExcelDataProvider excelDataProvider, ParsingContext parsingContext)
            : this(expression, excelDataProvider, parsingContext, new RangeAddressFactory(excelDataProvider), false)
        {
        }

        public ExcelAddressExpression(string expression, ExcelDataProvider excelDataProvider, ParsingContext parsingContext, bool negate)
            : this(expression, excelDataProvider, parsingContext, new RangeAddressFactory(excelDataProvider), negate)
        {
        }

        public ExcelAddressExpression(string expression, ExcelDataProvider excelDataProvider, ParsingContext parsingContext, RangeAddressFactory rangeAddressFactory, bool negate)
            : base(expression)
        {
            Require.That(excelDataProvider).Named("excelDataProvider").IsNotNull();
            Require.That(parsingContext).Named("parsingContext").IsNotNull();
            Require.That(rangeAddressFactory).Named("rangeAddressFactory").IsNotNull();
            _excelDataProvider = excelDataProvider;
            _parsingContext = parsingContext;
            _rangeAddressFactory = rangeAddressFactory;
            _negate = negate;
        }

        /// <summary>
        /// Gets or sets a value that indicates whether or not to resolve directly to an <see cref="ExcelDataProvider.IRangeInfo"/>
        /// </summary>
        public bool ResolveAsRange { get; set; }

        public override bool IsGroupedExpression => false;

        public override CompileResult Compile()
        {
            //if (ParentIsLookupFunction)
            //{
            //    return new CompileResult(ExpressionString, DataType.ExcelAddress);
            //}
            //else
            //{
            //    return CompileRangeValues();
            //}
            ExcelAddressCache cache = _parsingContext.AddressCache;
            int cacheId = cache.GetNewId();
            if (!cache.Add(cacheId, ExpressionString))
            {
                throw new InvalidOperationException("Catastropic error occurred, address caching failed");
            }

            CompileResult compileResult = CompileRangeValues();
            compileResult.ExcelAddressReferenceId = cacheId;
            return compileResult;
        }

        private CompileResult CompileRangeValues()
        {
            ParsingScope c = _parsingContext.Scopes.Current;
            ExcelDataProvider.IRangeInfo result = _excelDataProvider.GetRange(c.Address.Worksheet, c.Address.FromRow, c.Address.FromCol, ExpressionString);
            if (result == null)
            {
                return CompileResult.Empty;
            }

            if (ResolveAsRange || result.Address.Rows > 1 || result.Address.Columns > 1)
            {
                return new CompileResult(result, DataType.Enumerable);
            }

            return CompileSingleCell(result);
        }

        private CompileResult CompileSingleCell(ExcelDataProvider.IRangeInfo result)
        {
            ExcelDataProvider.ICellInfo cell = result.FirstOrDefault();
            if (cell == null)
                return CompileResult.Empty;
            var factory = new CompileResultFactory();
            CompileResult compileResult = factory.Create(cell.Value);
            if (_negate && compileResult.IsNumeric)
            {
                compileResult = new CompileResult(compileResult.ResultNumeric * -1, compileResult.DataType);
            }

            compileResult.IsHiddenCell = cell.IsHiddenRow;
            return compileResult;
        }
    }
}