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
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
using OfficeOpenXml.FormulaParsing.ExcelUtilities;
using OfficeOpenXml.FormulaParsing.Exceptions;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using OfficeOpenXml.FormulaParsing.Logging;
using OfficeOpenXml.FormulaParsing.Utilities;

namespace OfficeOpenXml.FormulaParsing
{
    public class FormulaParser : IDisposable
    {
        private readonly ExcelDataProvider _excelDataProvider;
        private readonly ParsingContext _parsingContext;
        private IExpressionCompiler _compiler;
        private IExpressionGraphBuilder _graphBuilder;

        public FormulaParser(ExcelDataProvider excelDataProvider)
            : this(excelDataProvider, ParsingContext.Create())
        {
        }

        public FormulaParser(ExcelDataProvider excelDataProvider, ParsingContext parsingContext)
        {
            parsingContext.Parser = this;
            parsingContext.ExcelDataProvider = excelDataProvider;
            parsingContext.NameValueProvider = new EpplusNameValueProvider(excelDataProvider);
            parsingContext.RangeAddressFactory = new RangeAddressFactory(excelDataProvider);
            _parsingContext = parsingContext;
            _excelDataProvider = excelDataProvider;
            Configure(configuration =>
            {
                configuration
                    .SetLexer(new Lexer(_parsingContext.Configuration.FunctionRepository, _parsingContext.NameValueProvider))
                    .SetGraphBuilder(new ExpressionGraphBuilder(excelDataProvider, _parsingContext))
                    .SetExpresionCompiler(new ExpressionCompiler())
                    .FunctionRepository.LoadModule(new BuiltInFunctions());
            });
        }

        public ILexer Lexer { get; private set; }

        public IEnumerable<string> FunctionNames => _parsingContext.Configuration.FunctionRepository.FunctionNames;

        public IFormulaParserLogger Logger => _parsingContext.Configuration.Logger;

        public void Dispose()
        {
            if (_parsingContext.Debug)
            {
                _parsingContext.Configuration.Logger.Dispose();
            }
        }

        public void Configure(Action<ParsingConfiguration> configMethod)
        {
            configMethod.Invoke(_parsingContext.Configuration);
            Lexer = _parsingContext.Configuration.Lexer ?? Lexer;
            _graphBuilder = _parsingContext.Configuration.GraphBuilder ?? _graphBuilder;
            _compiler = _parsingContext.Configuration.ExpressionCompiler ?? _compiler;
        }

        internal virtual object Parse(string formula, RangeAddress rangeAddress)
        {
            using (_parsingContext.Scopes.NewScope(rangeAddress))
            {
                IEnumerable<Token> tokens = Lexer.Tokenize(formula);
                ExpressionGraph.ExpressionGraph graph = _graphBuilder.Build(tokens);
                if (graph.Expressions.Count() == 0)
                {
                    return null;
                }

                return _compiler.Compile(graph.Expressions).Result;
            }
        }

        internal virtual object Parse(IEnumerable<Token> tokens, string worksheet, string address)
        {
            RangeAddress rangeAddress = _parsingContext.RangeAddressFactory.Create(address);
            using (_parsingContext.Scopes.NewScope(rangeAddress))
            {
                ExpressionGraph.ExpressionGraph graph = _graphBuilder.Build(tokens);
                if (graph.Expressions.Count() == 0)
                {
                    return null;
                }

                return _compiler.Compile(graph.Expressions).Result;
            }
        }

        internal virtual object ParseCell(IEnumerable<Token> tokens, string worksheet, int row, int column)
        {
            RangeAddress rangeAddress = _parsingContext.RangeAddressFactory.Create(worksheet, column, row);
            using (_parsingContext.Scopes.NewScope(rangeAddress))
            {
                //    _parsingContext.Dependencies.AddFormulaScope(scope);
                ExpressionGraph.ExpressionGraph graph = _graphBuilder.Build(tokens);
                if (graph.Expressions.Count() == 0)
                {
                    return 0d;
                }

                try
                {
                    CompileResult compileResult = _compiler.Compile(graph.Expressions);
                    // quick solution for the fact that an excelrange can be returned.
                    if (compileResult.Result is not ExcelDataProvider.IRangeInfo rangeInfo)
                    {
                        return compileResult.Result ?? 0d;
                    }

                    if (rangeInfo.IsEmpty)
                    {
                        return 0d;
                    }

                    if (!rangeInfo.IsMulti)
                    {
                        return rangeInfo.First().Value ?? 0d;
                    }

                    // ok to return multicell if it is a workbook scoped name.
                    if (string.IsNullOrEmpty(worksheet))
                    {
                        return rangeInfo;
                    }

                    if (_parsingContext.Debug)
                    {
                        string msg = string.Format("A range with multiple cell was returned at row {0}, column {1}",
                            row, column);
                        _parsingContext.Configuration.Logger.Log(_parsingContext, msg);
                    }

                    return ExcelErrorValue.Create(eErrorType.Value);
                }
                catch (ExcelErrorValueException ex)
                {
                    if (_parsingContext.Debug)
                    {
                        _parsingContext.Configuration.Logger.Log(_parsingContext, ex);
                    }

                    return ex.ErrorValue;
                }
            }
        }

        public virtual object Parse(string formula, string address)
        {
            return Parse(formula, _parsingContext.RangeAddressFactory.Create(address));
        }

        public virtual object Parse(string formula)
        {
            return Parse(formula, RangeAddress.Empty);
        }

        public virtual object ParseAt(string address)
        {
            Require.That(address).Named("address").IsNotNullOrEmpty();
            RangeAddress rangeAddress = _parsingContext.RangeAddressFactory.Create(address);
            return ParseAt(rangeAddress.Worksheet, rangeAddress.FromRow, rangeAddress.FromCol);
        }

        public virtual object ParseAt(string worksheetName, int row, int col)
        {
            string f = _excelDataProvider.GetRangeFormula(worksheetName, row, col);
            if (string.IsNullOrEmpty(f))
            {
                return _excelDataProvider.GetRangeValue(worksheetName, row, col);
            }

            return Parse(f, _parsingContext.RangeAddressFactory.Create(worksheetName, col, row));
            //var dataItem = _excelDataProvider.GetRangeValues(address).FirstOrDefault();
            //if (dataItem == null /*|| (dataItem.Value == null && dataItem.Formula == null)*/) return null;
            //if (!string.IsNullOrEmpty(dataItem.Formula))
            //{
            //    return Parse(dataItem.Formula, _parsingContext.RangeAddressFactory.Create(address));
            //}
            //return Parse(dataItem.Value.ToString(), _parsingContext.RangeAddressFactory.Create(address));
        }


        internal void InitNewCalc()
        {
            _excelDataProvider?.Reset();
        }
    }
}