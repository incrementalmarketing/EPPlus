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

using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.FormulaParsing.ExpressionGraph.CompileStrategy;

namespace OfficeOpenXml.FormulaParsing.ExpressionGraph
{
    public class ExpressionCompiler : IExpressionCompiler
    {
        private readonly ICompileStrategyFactory _compileStrategyFactory;
        private readonly IExpressionConverter _expressionConverter;
        private IEnumerable<Expression> _expressions;

        public ExpressionCompiler()
            : this(new ExpressionConverter(), new CompileStrategyFactory())
        {
        }

        public ExpressionCompiler(IExpressionConverter expressionConverter, ICompileStrategyFactory compileStrategyFactory)
        {
            _expressionConverter = expressionConverter;
            _compileStrategyFactory = compileStrategyFactory;
        }

        public CompileResult Compile(IEnumerable<Expression> expressions)
        {
            _expressions = expressions;
            return PerformCompilation();
        }

        public CompileResult Compile(string worksheet, int row, int column, IEnumerable<Expression> expressions)
        {
            _expressions = expressions;
            return PerformCompilation(worksheet, row, column);
        }

        private CompileResult PerformCompilation(string worksheet = "", int row = -1, int column = -1)
        {
            IEnumerable<Expression> compiledExpressions = HandleGroupedExpressions();
            while (compiledExpressions.Any(x => x.Operator != null))
            {
                int prec = FindLowestPrecedence();
                compiledExpressions = HandlePrecedenceLevel(prec);
            }

            if (_expressions.Any())
            {
                return compiledExpressions.First().Compile();
            }

            return CompileResult.Empty;
        }

        private IEnumerable<Expression> HandleGroupedExpressions()
        {
            if (!_expressions.Any()) return Enumerable.Empty<Expression>();
            Expression first = _expressions.First();
            IEnumerable<Expression> groupedExpressions = _expressions.Where(x => x.IsGroupedExpression);
            foreach (Expression groupedExpression in groupedExpressions)
            {
                CompileResult result = groupedExpression.Compile();
                if (result == CompileResult.Empty) continue;
                Expression newExp = _expressionConverter.FromCompileResult(result);
                newExp.Operator = groupedExpression.Operator;
                newExp.Prev = groupedExpression.Prev;
                newExp.Next = groupedExpression.Next;
                if (groupedExpression.Prev != null)
                {
                    groupedExpression.Prev.Next = newExp;
                }

                if (groupedExpression.Next != null)
                {
                    groupedExpression.Next.Prev = newExp;
                }

                if (groupedExpression == first)
                {
                    first = newExp;
                }
            }

            return RefreshList(first);
        }

        private IEnumerable<Expression> HandlePrecedenceLevel(int precedence)
        {
            Expression first = _expressions.First();
            IEnumerable<Expression> expressionsToHandle = _expressions.Where(x => x.Operator != null && x.Operator.Precedence == precedence);
            Expression expression = expressionsToHandle.First();
            do
            {
                CompileStrategy.CompileStrategy strategy = _compileStrategyFactory.Create(expression);
                Expression compiledExpression = strategy.Compile();
                if (compiledExpression is ExcelErrorExpression)
                {
                    return RefreshList(compiledExpression);
                }

                if (expression == first)
                {
                    first = compiledExpression;
                }

                expression = compiledExpression;
            } while (expression is { Operator: not null } && expression.Operator.Precedence == precedence);

            return RefreshList(first);
        }

        private int FindLowestPrecedence()
        {
            return _expressions.Where(x => x.Operator != null).Min(x => x.Operator.Precedence);
        }

        private IEnumerable<Expression> RefreshList(Expression first)
        {
            var resultList = new List<Expression>();
            Expression exp = first;
            resultList.Add(exp);
            while (exp.Next != null)
            {
                resultList.Add(exp.Next);
                exp = exp.Next;
            }

            _expressions = resultList;
            return resultList;
        }
    }
}