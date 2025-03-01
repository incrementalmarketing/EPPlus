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
 * Jan Källman		Initial Release		        2009-10-01
 * Jan Källman		License changed GPL-->LGPL 2011-12-27
 *******************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OfficeOpenXml.FormulaParsing;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml
{
    /// <summary>
    /// Base class containing cell address manipulating methods.
    /// </summary>
    public abstract class ExcelCellBase
    {
        #region "public functions"

        /// <summary>
        /// Get the sheet, row and column from the CellID
        /// </summary>
        /// <param name="cellID"></param>
        /// <param name="sheet"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        internal static void SplitCellID(ulong cellID, out int sheet, out int row, out int col)
        {
            sheet = (int)(cellID % 0x8000);
            col = (int)(cellID >> 15) & 0x3FF;
            row = (int)(cellID >> 29);
        }

        /// <summary>
        /// Get the cellID for the cell. 
        /// </summary>
        /// <param name="SheetID"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        internal static ulong GetCellID(int SheetID, int row, int col)
        {
            return (ulong)SheetID + ((ulong)col << 15) + ((ulong)row << 29);
        }

        #endregion

        #region "Formula Functions"

        private delegate string dlgTransl(string part, int row, int col);

        #region R1C1 Functions"

        /// <summary>
        /// Translates a R1C1 to an absolut address/Formula
        /// </summary>
        /// <param name="value">Address</param>
        /// <param name="row">Current row</param>
        /// <param name="col">Current column</param>
        /// <returns>The RC address</returns>
        public static string TranslateFromR1C1(string value, int row, int col)
        {
            return Translate(value, ToAbs, row, col);
        }

        /// <summary>
        /// Translates a absolut address to R1C1 Format
        /// </summary>
        /// <param name="value">R1C1 Address</param>
        /// <param name="row">Current row</param>
        /// <param name="col">Current column</param>
        /// <returns>The absolut address/Formula</returns>
        public static string TranslateToR1C1(string value, int row, int col)
        {
            return Translate(value, ToR1C1, row, col);
        }

        /// <summary>
        /// Translates betweein R1C1 or absolut addresses
        /// </summary>
        /// <param name="value">The addresss/function</param>
        /// <param name="addressTranslator">The translating function</param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private static string Translate(string value, dlgTransl addressTranslator, int row, int col)
        {
            if (value == "")
                return "";

            var lexer = new Lexer(SourceCodeTokenizer.R1C1, new SyntacticAnalyzer());
            IEnumerable<Token> tokens = lexer.Tokenize(value, null);
            foreach (Token token in tokens)
            {
                //Console.WriteLine($"{token.TokenType} : {token.Value}");
                if (token.TokenType is TokenType.ExcelAddress or TokenType.NameValue or TokenType.ExcelAddressR1C1)
                {
                    string part = addressTranslator(token.Value, row, col);
                    //Console.Write($"==> " + part);
                    token.Value = part;
                }
            }

            string ret = string.Join("", tokens.Select(x => x.Value).ToArray());
            return ret;
        }

        /// <summary>
        /// Translate to R1C1
        /// </summary>
        /// <param name="part">the value to be translated</param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="rowIncr"></param>
        /// <param name="colIncr"></param>
        /// <returns></returns>
        private static string ToR1C1(string part, int row, int col)
        {
            int shInd = part.IndexOf('!');
            string sh = "";
            if (shInd > 0)
            {
                sh = part.Substring(0, shInd + 1);
                part = part.Substring(shInd + 1);
            }

            int delim = part.IndexOf(':');
            if (delim > 0)
            {
                string p1 = ToR1C1_1(part.Substring(0, delim), row, col);
                string p2 = ToR1C1_1(part.Substring(delim + 1), row, col);
                if (p1.Equals(p2))
                    return p1;
                return sh + p1 + ":" + p2;
            }

            return sh + ToR1C1_1(part, row, col);
        }

        private static string ToR1C1_1(string part, int row, int col)
        {
            var sb = new StringBuilder();
            if (GetRowCol(part, out int addrRow, out int addrCol, false, out bool fixRow, out bool fixCol))
            {
                if (addrRow == 0 && addrCol == 0)
                {
                    return part;
                }

                if (addrRow > 0)
                {
                    sb.Append(fixRow ? $"R{addrRow}" : addrRow == row ? "R" : $"R[{addrRow - row}]");
                }

                if (addrCol > 0)
                {
                    sb.Append(fixCol ? $"C{addrCol}" : addrCol == col ? "C" : $"C[{addrCol - col}]");
                }

                return sb.ToString();
            }

            return part;
        }

        /// <summary>
        /// Translates to absolute address
        /// </summary>
        /// <param name="part"></param>
        /// <param name="row"></param>
        /// <param name="col"></param> 
        /// <param name="rowIncr"></param>
        /// <param name="colIncr"></param>
        /// <returns></returns>
        private static string ToAbs(string part, int row, int col)
        {
            int shInd = part.IndexOf('!');
            string sh = "";
            if (shInd > 0)
            {
                sh = part.Substring(0, shInd + 1);
                part = part.Substring(shInd + 1);
            }

            int delim = part.IndexOf(':');
            if (delim > 0)
            {
                string p1 = ToAbs_1(part.Substring(0, delim), row, col, false);
                string p2 = ToAbs_1(part.Substring(delim + 1), row, col, false);
                if (p1.Equals(p2))
                    return p1;
                return sh + p1 + ":" + p2;
            }

            return sh + ToAbs_1(part, row, col, true);
        }

        private static string ToAbs_1(string part, int row, int col, bool isSingle)
        {
            string check = ConvertUtil._invariantTextInfo.ToUpper(part);
            // Bug
            int rStart = check.IndexOf("R");
            int cStart = check.IndexOf("C");
            //if (rStart != 0)
            //    return part;
            if (rStart != 0 && cStart != 0)
                return part;
            if (part.Length == 1) //R or C
            {
                if (rStart == 0)
                {
                    //return GetAddress(row);
                    return $"{row}:{row}";
                }

                string cLetter = GetColumnLetter(col);
                return $"{cLetter}:{cLetter}";
            }

            bool absoluteRow, absoluteCol;
            if (cStart == -1)
            {
                int RNum = GetRC(part.Substring(1), row, out absoluteRow);
                if (RNum > int.MinValue)
                {
                    string r = GetAddressRow(RNum, absoluteRow);
                    if (isSingle)
                    {
                        return $"{r}:{r}";
                    }

                    return $"{r}";
                }

                return part;
            }

            if (rStart == -1)
            {
                int CNum = GetRC(part.Substring(1), col, out absoluteCol);
                if (CNum > int.MinValue)
                {
                    string c = GetAddressCol(CNum, absoluteCol);
                    if (isSingle)
                    {
                        return $"{c}:{c}";
                    }

                    return $"{c}";
                }

                return part;
            }

            {
                int RNum, CNum;
                if (1 == cStart)
                {
                    RNum = row;
                    absoluteRow = false;
                }
                else
                {
                    RNum = GetRC(part.Substring(1, cStart - 1), row, out absoluteRow);
                }

                if (part.Length - 1 == cStart)
                {
                    CNum = col;
                    absoluteCol = false;
                }
                else
                {
                    CNum = GetRC(part.Substring(cStart + 1, part.Length - cStart - 1), col, out absoluteCol);
                }


                if (RNum > int.MinValue && CNum > int.MinValue)
                {
                    return GetAddress(RNum, absoluteRow, CNum, absoluteCol);
                }

                return part;
            }
        }

        /// <summary>
        /// Adds or subtracts a row or column to an address
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="rowIncr"></param>
        /// <param name="colIncr"></param>
        /// <returns></returns>
        private static string AddToRowColumnTranslator(string Address, int row, int col, int rowIncr, int colIncr)
        {
            if (Address == "#REF!")
            {
                return Address;
            }

            if (GetRowCol(Address, out int fromRow, out int fromCol, false))
            {
                if (fromRow == 0 || fromCol == 0)
                {
                    return Address;
                }

                if (rowIncr != 0 && row != 0 && fromRow >= row && Address.IndexOf('$', 1) == -1)
                {
                    if (fromRow < row - rowIncr)
                    {
                        return "#REF!";
                    }

                    fromRow = fromRow + rowIncr;
                }

                if (colIncr != 0 && col != 0 && fromCol >= col && ConvertUtil._invariantCompareInfo.IsPrefix(Address, "$") == false)
                {
                    if (fromCol < col - colIncr)
                    {
                        return "#REF!";
                    }

                    fromCol = fromCol + colIncr;
                }

                Address = GetAddress(fromRow, Address.IndexOf('$', 1) > -1, fromCol, ConvertUtil._invariantCompareInfo.IsPrefix(Address, "$"));
            }

            return Address;
        }

        /// <summary>
        /// Returns with brackets if the value is negative
        /// </summary>
        /// <param name="v">The value</param>
        /// <returns></returns>
        private static string GetRCFmt(int v)
        {
            return v < 0 ? string.Format("[{0}]", v) : v > 0 ? v.ToString() : "";
        }

        /// <summary>
        /// Get the offset value for RC format
        /// </summary>
        /// <param name="value"></param>
        /// <param name="OffsetValue"></param>
        /// <param name="fixedAddr"></param>
        /// <returns></returns>
        private static int GetRC(string value, int OffsetValue, out bool fixedAddr)
        {
            if (value == "")
            {
                fixedAddr = false;
                return OffsetValue;
            }

            int num;
            if (value[0] == '[' && value[^1] == ']') //Offset?                
            {
                fixedAddr = false;
                if (int.TryParse(value.Substring(1, value.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                {
                    return OffsetValue + num;
                }

                return int.MinValue;
            }

            fixedAddr = true;
            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
            {
                return num;
            }

            return int.MinValue;
        }

        #endregion

        #region "Address Functions"

        #region GetColumnLetter

        /// <summary>
        /// Returns the character representation of the numbered column
        /// </summary>
        /// <param name="iColumnNumber">The number of the column</param>
        /// <returns>The letter representing the column</returns>
        protected internal static string GetColumnLetter(int iColumnNumber)
        {
            return GetColumnLetter(iColumnNumber, false);
        }

        protected internal static string GetColumnLetter(int iColumnNumber, bool fixedCol)
        {
            if (iColumnNumber < 1)
            {
                //throw new Exception("Column number is out of range");
                return "#REF!";
            }

            string sCol = "";
            do
            {
                sCol = (char)('A' + (iColumnNumber - 1) % 26) + sCol;
                iColumnNumber = (iColumnNumber - (iColumnNumber - 1) % 26) / 26;
            } while (iColumnNumber > 0);

            return fixedCol ? "$" + sCol : sCol;
        }

        #endregion

        internal static bool GetRowColFromAddress(string CellAddress, out int FromRow, out int FromColumn, out int ToRow, out int ToColumn)
        {
            bool fixedFromRow, fixedFromColumn, fixedToRow, fixedToColumn;
            return GetRowColFromAddress(CellAddress, out FromRow, out FromColumn, out ToRow, out ToColumn, out fixedFromRow, out fixedFromColumn, out fixedToRow, out fixedToColumn);
        }

        /// <summary>
        /// Get the row/columns for a Cell-address
        /// </summary>
        /// <param name="CellAddress">The address</param>
        /// <param name="FromRow">Returns the to column</param>
        /// <param name="FromColumn">Returns the from column</param>
        /// <param name="ToRow">Returns the to row</param>
        /// <param name="ToColumn">Returns the from row</param>
        /// <param name="fixedFromRow">Is the from row fixed?</param>
        /// <param name="fixedFromColumn">Is the from column fixed?</param>
        /// <param name="fixedToRow">Is the to row fixed?</param>
        /// <param name="fixedToColumn">Is the to column fixed?</param>
        /// <returns></returns>
        internal static bool GetRowColFromAddress(string CellAddress, out int FromRow, out int FromColumn, out int ToRow, out int ToColumn, out bool fixedFromRow, out bool fixedFromColumn, out bool fixedToRow, out bool fixedToColumn)
        {
            bool ret;
            if (CellAddress.IndexOf('[') > 0) //External reference or reference to Table or Pivottable.
            {
                FromRow = -1;
                FromColumn = -1;
                ToRow = -1;
                ToColumn = -1;
                fixedFromRow = false;
                fixedFromColumn = false;
                fixedToRow = false;
                fixedToColumn = false;
                return false;
            }

            CellAddress = ConvertUtil._invariantTextInfo.ToUpper(CellAddress);
            //This one can be removed when the worksheet Select format is fixed
            if (CellAddress.IndexOf(' ') > 0)
            {
                CellAddress = CellAddress.Substring(0, CellAddress.IndexOf(' '));
            }

            if (CellAddress.IndexOf(':') < 0)
            {
                ret = GetRowColFromAddress(CellAddress, out FromRow, out FromColumn, out fixedFromRow, out fixedFromColumn);
                ToColumn = FromColumn;
                ToRow = FromRow;
                fixedToRow = fixedFromRow;
                fixedToColumn = fixedFromColumn;
            }
            else
            {
                string[] cells = CellAddress.Split(':');
                ret = GetRowColFromAddress(cells[0], out FromRow, out FromColumn, out fixedFromRow, out fixedFromColumn);
                if (ret)
                    ret = GetRowColFromAddress(cells[1], out ToRow, out ToColumn, out fixedToRow, out fixedToColumn);
                else
                {
                    GetRowColFromAddress(cells[1], out ToRow, out ToColumn, out fixedToRow, out fixedToColumn);
                }

                if (FromColumn <= 0)
                    FromColumn = 1;
                if (FromRow <= 0)
                    FromRow = 1;
                if (ToColumn <= 0)
                    ToColumn = ExcelPackage.MaxColumns;
                if (ToRow <= 0)
                    ToRow = ExcelPackage.MaxRows;
            }

            return ret;
        }

        /// <summary>
        /// Get the row/column for n Cell-address
        /// </summary>
        /// <param name="CellAddress">The address</param>
        /// <param name="Row">Returns Tthe row</param>
        /// <param name="Column">Returns the column</param>
        /// <returns>true if valid</returns>
        internal static bool GetRowColFromAddress(string CellAddress, out int Row, out int Column)
        {
            return GetRowCol(CellAddress, out Row, out Column, true);
        }

        internal static bool GetRowColFromAddress(string CellAddress, out int row, out int col, out bool fixedRow, out bool fixedCol)
        {
            return GetRowCol(CellAddress, out row, out col, true, out fixedRow, out fixedCol);
        }

        internal static bool IsAlpha(char c)
        {
            return c is >= 'A' and <= 'Z';
        }

        /// <summary>
        /// Get the row/column for a Cell-address
        /// </summary>
        /// <param name="address">the address</param>
        /// <param name="row">returns the row</param>
        /// <param name="col">returns the column</param>
        /// <param name="throwException">throw exception if invalid, otherwise returns false</param>
        /// <returns></returns>
        internal static bool GetRowCol(string address, out int row, out int col, bool throwException)
        {
            bool fixedRow, fixedCol;
            return GetRowCol(address, out row, out col, throwException, out fixedRow, out fixedCol);
        }

        internal static bool GetRowCol(string address, out int row, out int col, bool throwException, out bool fixedRow, out bool fixedCol)
        {
            bool colPart = true;
            int colStartIx = 0;
            int colLength = 0;
            col = 0;
            row = 0;
            fixedRow = false;
            fixedCol = false;

            if (ConvertUtil._invariantCompareInfo.IsSuffix(address, "#REF!"))
            {
                row = 0;
                col = 0;
                return true;
            }

            int sheetMarkerIndex = address.IndexOf('!');
            if (sheetMarkerIndex >= 0)
            {
                colStartIx = sheetMarkerIndex + 1;
            }

            address = ConvertUtil._invariantTextInfo.ToUpper(address);
            for (int i = colStartIx; i < address.Length; i++)
            {
                char c = address[i];
                if (colPart && c is >= 'A' and <= 'Z' && colLength <= 3)
                {
                    col *= 26;
                    col += c - 64;
                    colLength++;
                }
                else if (c is >= '0' and <= '9')
                {
                    row *= 10;
                    row += c - 48;
                    colPart = false;
                }
                else if (c == '$')
                {
                    if (IsAlpha(address[i + 1]))
                    {
                        colStartIx++;
                        fixedCol = true;
                    }
                    else
                    {
                        colPart = false;
                        fixedRow = true;
                    }
                }
                else
                {
                    row = 0;
                    col = 0;
                    if (throwException)
                    {
                        throw new Exception(string.Format("Invalid Address format {0}", address));
                    }

                    return false;
                }
            }

            return row != 0 || col != 0;
        }

        private static int GetColumn(string sCol)
        {
            int col = 0;
            int len = sCol.Length - 1;
            for (int i = len; i >= 0; i--)
            {
                col += (sCol[i] - 64) * (int)Math.Pow(26, len - i);
            }

            return col;
        }

        #region GetAddress

        public static string GetAddressRow(int Row, bool Absolute = false)
        {
            if (Absolute)
                return $"${Row}";
            return $"{Row}";
        }

        public static string GetAddressCol(int Col, bool Absolute = false)
        {
            string colLetter = GetColumnLetter(Col);
            if (Absolute)
                return $"${colLetter}";
            return $"{colLetter}";
        }

        /// <summary>
        /// Returns the AlphaNumeric representation that Excel expects for a Cell Address
        /// </summary>
        /// <param name="Row">The number of the row</param>
        /// <param name="Column">The number of the column in the worksheet</param>
        /// <returns>The cell address in the format A1</returns>
        public static string GetAddress(int Row, int Column)
        {
            return GetAddress(Row, Column, false);
        }

        /// <summary>
        /// Returns the AlphaNumeric representation that Excel expects for a Cell Address
        /// </summary>
        /// <param name="Row">The number of the row</param>
        /// <param name="Column">The number of the column in the worksheet</param>
        /// <param name="AbsoluteRow">Absolute row</param>
        /// <param name="AbsoluteCol">Absolute column</param>
        /// <returns>The cell address in the format A1</returns>
        public static string GetAddress(int Row, bool AbsoluteRow, int Column, bool AbsoluteCol)
        {
            if (Row < 1 || Row > ExcelPackage.MaxRows || Column < 1 || Column > ExcelPackage.MaxColumns) return "#REF!";
            return (AbsoluteCol ? "$" : "") + GetColumnLetter(Column) + (AbsoluteRow ? "$" : "") + Row;
        }

        /// <summary>
        /// Returns the AlphaNumeric representation that Excel expects for a Cell Address
        /// </summary>
        /// <param name="Row">The number of the row</param>
        /// <param name="Column">The number of the column in the worksheet</param>
        /// <param name="Absolute">Get an absolute address ($A$1)</param>
        /// <returns>The cell address in the format A1</returns>
        public static string GetAddress(int Row, int Column, bool Absolute)
        {
            if (Row == 0 || Column == 0)
            {
                return "#REF!";
            }

            if (Absolute)
            {
                return "$" + GetColumnLetter(Column) + "$" + Row;
            }

            return GetColumnLetter(Column) + Row;
        }

        /// <summary>
        /// Returns the AlphaNumeric representation that Excel expects for a Cell Address
        /// </summary>
        /// <param name="FromRow">From row number</param>
        /// <param name="FromColumn">From column number</param>
        /// <param name="ToRow">To row number</param>
        /// <param name="ToColumn">From column number</param>
        /// <returns>The cell address in the format A1</returns>
        public static string GetAddress(int FromRow, int FromColumn, int ToRow, int ToColumn)
        {
            return GetAddress(FromRow, FromColumn, ToRow, ToColumn, false);
        }

        /// <summary>
        /// Returns the AlphaNumeric representation that Excel expects for a Cell Address
        /// </summary>
        /// <param name="FromRow">From row number</param>
        /// <param name="FromColumn">From column number</param>
        /// <param name="ToRow">To row number</param>
        /// <param name="ToColumn">From column number</param>
        /// <param name="Absolute">if true address is absolute (like $A$1)</param>
        /// <returns>The cell address in the format A1</returns>
        public static string GetAddress(int FromRow, int FromColumn, int ToRow, int ToColumn, bool Absolute)
        {
            if (FromRow == ToRow && FromColumn == ToColumn)
            {
                return GetAddress(FromRow, FromColumn, Absolute);
            }

            if (FromRow == 1 && ToRow == ExcelPackage.MaxRows)
            {
                string absChar = Absolute ? "$" : "";
                return absChar + GetColumnLetter(FromColumn) + ":" + absChar + GetColumnLetter(ToColumn);
            }

            if (FromColumn == 1 && ToColumn == ExcelPackage.MaxColumns)
            {
                string absChar = Absolute ? "$" : "";
                return absChar + FromRow + ":" + absChar + ToRow;
            }

            return GetAddress(FromRow, FromColumn, Absolute) + ":" + GetAddress(ToRow, ToColumn, Absolute);
        }

        /// <summary>
        /// Returns the AlphaNumeric representation that Excel expects for a Cell Address
        /// </summary>
        /// <param name="FromRow">From row number</param>
        /// <param name="FromColumn">From column number</param>
        /// <param name="ToRow">To row number</param>
        /// <param name="ToColumn">From column number</param>
        /// <param name="FixedFromColumn"></param>
        /// <param name="FixedFromRow"></param>
        /// <param name="FixedToColumn"></param>
        /// <param name="FixedToRow"></param>
        /// <returns>The cell address in the format A1</returns>
        public static string GetAddress(int FromRow, int FromColumn, int ToRow, int ToColumn, bool FixedFromRow, bool FixedFromColumn, bool FixedToRow, bool FixedToColumn)
        {
            if (FromRow == ToRow && FromColumn == ToColumn)
            {
                return GetAddress(FromRow, FixedFromRow, FromColumn, FixedFromColumn);
            }

            if (FromRow == 1 && ToRow == ExcelPackage.MaxRows)
            {
                return GetColumnLetter(FromColumn, FixedFromColumn) + ":" + GetColumnLetter(ToColumn, FixedToColumn);
            }

            if (FromColumn == 1 && ToColumn == ExcelPackage.MaxColumns)
            {
                return (FixedFromRow ? "$" : "") + FromRow + ":" + (FixedToRow ? "$" : "") + ToRow;
            }

            return GetAddress(FromRow, FixedFromRow, FromColumn, FixedFromColumn) + ":" + GetAddress(ToRow, FixedToRow, ToColumn, FixedToColumn);
        }

        /// <summary>
        /// Get the full address including the worksheet name
        /// </summary>
        /// <param name="worksheetName">The name of the worksheet</param>
        /// <param name="address">The address</param>
        /// <returns>The full address</returns>
        public static string GetFullAddress(string worksheetName, string address)
        {
            return GetFullAddress(worksheetName, address, true);
        }

        internal static string GetFullAddress(string worksheetName, string address, bool fullRowCol)
        {
            if (!string.IsNullOrEmpty(worksheetName)) worksheetName = worksheetName.Replace("'", "''"); //Makesure addresses handle single qoutes
            if (address.IndexOf("!") == -1 || address == "#REF!")
            {
                if (fullRowCol)
                {
                    string[] cells = address.Split(':');
                    if (cells.Length > 0)
                    {
                        address = string.Format("'{0}'!{1}", worksheetName, cells[0]);
                        if (cells.Length > 1)
                        {
                            address += string.Format(":{0}", cells[1]);
                        }
                    }
                }
                else
                {
                    var a = new ExcelAddressBase(address);
                    if ((a._fromRow == 1 && a._toRow == ExcelPackage.MaxRows) || (a._fromCol == 1 && a._toCol == ExcelPackage.MaxColumns))
                    {
                        address = string.Format("'{0}'!{1}{2}:{3}{4}", worksheetName, GetColumnLetter(a._fromCol), a._fromRow, GetColumnLetter(a._toCol), a._toRow);
                    }
                    else
                    {
                        address = GetFullAddress(worksheetName, address, true);
                    }
                }
            }

            return address;
        }

        #endregion

        #region IsValidCellAddress

        public static bool IsValidAddress(string address)
        {
            if (string.IsNullOrEmpty(address.Trim())) return false;
            address = ConvertUtil._invariantTextInfo.ToUpper(address);
            string[] addrs = address.Split(',');
            foreach (string a in addrs)
            {
                string r1 = "", c1 = "", r2 = "", c2 = "";
                bool isSecond = false;
                for (int i = 0; i < a.Length; i++)
                {
                    if (IsCol(a[i]))
                    {
                        if (isSecond == false)
                        {
                            if (r1 != "") return false;
                            c1 += a[i];
                            if (c1.Length > 3) return false;
                        }
                        else
                        {
                            if (r2 != "") return false;
                            c2 += a[i];
                            if (c2.Length > 3) return false;
                        }
                    }
                    else if (IsRow(a[i]))
                    {
                        if (isSecond == false)
                        {
                            r1 += a[i];
                            if (r1.Length > 7) return false;
                        }
                        else
                        {
                            r2 += a[i];
                            if (r2.Length > 7) return false;
                        }
                    }
                    else if (a[i] == ':')
                    {
                        if (isSecond || i == a.Length - 1) return false;
                        isSecond = true;
                    }
                    else if (a[i] == '$')
                    {
                        if (i == a.Length - 1 || a[i + 1] == ':' ||
                            (i > 1 && IsCol(a[i - 1]) && IsCol(a[i + 1])) ||
                            (i > 1 && IsRow(a[i - 1]) && IsRow(a[i + 1])))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                bool ret;
                if (r1 != "" && c1 != "" && r2 == "" && c2 == "") //Single Cell
                {
                    int column = GetColumn(c1);
                    int row = int.Parse(r1);
                    ret = column is >= 1 and <= ExcelPackage.MaxColumns && row is >= 1 and <= ExcelPackage.MaxRows;
                }
                else if (r1 != "" && r2 != "" && c1 != "" && c2 != "") //Range
                {
                    int iR1 = int.Parse(r1);
                    int iC1 = GetColumn(c1);
                    int iR2 = int.Parse(r2);
                    int iC2 = GetColumn(c2);

                    ret = iC1 <= iC2 && iR1 <= iR2 &&
                          iC1 >= 1 && iC2 <= ExcelPackage.MaxColumns &&
                          iR1 >= 1 && iR2 <= ExcelPackage.MaxRows;
                }
                else if (r1 == "" && r2 == "" && c1 != "" && c2 != "") //Full Column
                {
                    int iC1 = GetColumn(c1);
                    int iC2 = GetColumn(c2);
                    ret = iC1 <= iC2 &&
                          iC1 >= 1 && iC2 <= ExcelPackage.MaxColumns;
                }
                else if (r1 != "" && r2 != "" && c1 == "" && c2 == "")
                {
                    int iR1 = int.Parse(r2);
                    int iR2 = int.Parse(r2);

                    ret = int.Parse(r1) <= iR2 &&
                          iR1 >= 1 &&
                          iR2 <= ExcelPackage.MaxRows;
                }
                else
                {
                    return false;
                }

                if (ret == false) return false;
            }

            return true;
        }

        private static bool IsCol(char c)
        {
            return c is >= 'A' and <= 'Z';
        }

        private static bool IsRow(char r)
        {
            return r is >= '0' and <= '9';
        }

        /// <summary>
        /// Checks that a cell address (e.g. A5) is valid.
        /// </summary>
        /// <param name="cellAddress">The alphanumeric cell address</param>
        /// <returns>True if the cell address is valid</returns>
        public static bool IsValidCellAddress(string cellAddress)
        {
            bool result = false;
            try
            {
                if (GetRowColFromAddress(cellAddress, out int row, out int col))
                {
                    if (row > 0 && col > 0 && row <= ExcelPackage.MaxRows && col <= ExcelPackage.MaxColumns)
                        result = true;
                    else
                        result = false;
                }
            }
            catch
            {
            }

            return result;
        }

        #endregion

        #region UpdateFormulaReferences

        /// <summary>
        /// Updates the Excel formula so that all the cellAddresses are incremented by the row and column increments
        /// if they fall after the afterRow and afterColumn.
        /// Supports inserting rows and columns into existing templates.
        /// </summary>
        /// <param name="formula">The Excel formula</param>
        /// <param name="rowIncrement">The amount to increment the cell reference by</param>
        /// <param name="colIncrement">The amount to increment the cell reference by</param>
        /// <param name="afterRow">Only change rows after this row</param>
        /// <param name="afterColumn">Only change columns after this column</param>
        /// <param name="currentSheet">The sheet that contains the formula currently being processed.</param>
        /// <param name="modifiedSheet">The sheet where cells are being inserted or deleted.</param>
        /// <param name="setFixed">Fixed address</param>
        /// <returns>The updated version of the <paramref name="formula"/>.</returns>
        internal static string UpdateFormulaReferences(string formula, int rowIncrement, int colIncrement, int afterRow, int afterColumn, string currentSheet, string modifiedSheet, bool setFixed = false)
        {
            try
            {
                var sct = new SourceCodeTokenizer(FunctionNameProvider.Empty, NameValueProvider.Empty);
                IEnumerable<Token> tokens = sct.Tokenize(formula);
                string f = "";
                foreach (Token t in tokens)
                {
                    if (t.TokenType == TokenType.ExcelAddress)
                    {
                        var a = new ExcelAddressBase(t.Value);
                        bool referencesModifiedWorksheet = (string.IsNullOrEmpty(a._ws) && currentSheet.Equals(modifiedSheet, StringComparison.CurrentCultureIgnoreCase)) || modifiedSheet.Equals(a._ws, StringComparison.CurrentCultureIgnoreCase);

                        if (!setFixed && (!string.IsNullOrEmpty(a._wb) || !referencesModifiedWorksheet))
                        {
                            // This address is in a different worksheet or workbook; no update is required.
                            f += a.Address;
                            continue;
                        }

                        // Persist fully-qualified worksheet references.
                        if (!string.IsNullOrEmpty(a._ws))
                        {
                            f += $"'{a._ws}'!";
                        }

                        if (rowIncrement > 0)
                        {
                            a = a.AddRow(afterRow, rowIncrement, setFixed);
                        }
                        else if (rowIncrement < 0)
                        {
                            a = a.DeleteRow(afterRow, -rowIncrement, setFixed);
                        }

                        if (colIncrement > 0)
                        {
                            a = a.AddColumn(afterColumn, colIncrement, setFixed);
                        }
                        else if (colIncrement < 0)
                        {
                            a = a.DeleteColumn(afterColumn, -colIncrement, setFixed);
                        }

                        if (a == null || !a.IsValidRowCol())
                        {
                            f += "#REF!";
                        }
                        else
                        {
                            // If the address was not shifted, then a.Address will still have the sheet name.
                            string[] address = a.Address.Split('!');
                            if (address.Length > 1)
                                f += address[1];
                            else
                                f += a.Address;
                        }
                    }
                    else
                    {
                        f += t.Value;
                    }
                }

                return f;
            }
            catch //Invalid formula, skip updating addresses
            {
                return formula;
            }
        }

        /// <summary>
        /// Updates all the references to a renamed sheet in a formula.
        /// </summary>
        /// <param name="formula">The formula to updated.</param>
        /// <param name="oldSheetName">The old sheet name.</param>
        /// <param name="newSheetName">The new sheet name.</param>
        /// <returns>The formula with all cross-sheet references updated.</returns>
        internal static string UpdateFormulaSheetReferences(string formula, string oldSheetName, string newSheetName)
        {
            if (string.IsNullOrEmpty(oldSheetName))
                throw new ArgumentNullException(nameof(oldSheetName));
            if (string.IsNullOrEmpty(newSheetName))
                throw new ArgumentNullException(nameof(newSheetName));
            try
            {
                var sct = new SourceCodeTokenizer(FunctionNameProvider.Empty, NameValueProvider.Empty);
                IEnumerable<Token> tokens = sct.Tokenize(formula);
                string f = "";
                foreach (Token t in tokens)
                {
                    if (t.TokenType == TokenType.ExcelAddress)
                    {
                        var a = new ExcelAddressBase(t.Value);
                        if (a == null || !a.IsValidRowCol())
                        {
                            f += "#REF!";
                        }
                        else
                        {
                            a.ChangeWorksheet(oldSheetName, newSheetName);
                            f += a.Address;
                        }
                    }
                    else
                    {
                        f += t.Value;
                    }
                }

                return f;
            }
            catch //Invalid formula, skip updating addresses
            {
                return formula;
            }
        }

        #endregion

        #endregion

        #endregion
    }
}