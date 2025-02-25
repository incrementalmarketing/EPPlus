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
 * Jan Källman		    Added       		        2012-11-25
 *******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using OfficeOpenXml;

internal class IndexBase : IComparable<IndexBase>
{
    internal short Index;

    public int CompareTo(IndexBase other)
    {
        //return Index < other.Index ? -1 : Index > other.Index ? 1 : 0;
        return Index - other.Index;
    }
}

// to compress memory size, use struct
internal struct IndexItem : IComparable<IndexItem>
{
    internal int IndexPointer { get; set; }
    internal short Index;

    public int CompareTo(IndexItem other)
    {
        return Index - other.Index;
    }
}

internal class ColumnIndex : IndexBase, IDisposable
{
    internal PageIndex[] Pages;
    internal IndexBase SearchIx = new();
    internal int PageCount;

    public ColumnIndex()
    {
        Pages = new PageIndex[CellStore<int>.PAGES_PER_COLUMN_MIN];
        PageCount = 0;
    }

    public void Dispose()
    {
        for (int p = 0; p < PageCount; p++)
        {
            ((IDisposable)Pages[p]).Dispose();
        }

        Pages = null;
    }

    ~ColumnIndex()
    {
        Pages = null;
    }

    internal int GetPosition(int row)
    {
        short page = (short)(row >> CellStore<int>.PAGE_BITS);
        int res;
        if (page >= 0 && page < PageCount && Pages[page].Index == page)
        {
            res = page;
        }
        else
        {
            SearchIx.Index = page;
            res = Array.BinarySearch(Pages, 0, PageCount, SearchIx);
        }

        if (res >= 0)
        {
            GetPage(row, ref res);
            return res;
        }

        int p = ~res;

        if (GetPage(row, ref p))
        {
            return p;
        }

        return res;
    }

    private bool GetPage(int row, ref int res)
    {
        if (res < PageCount && Pages[res].MinIndex <= row && Pages[res].MaxIndex >= row)
        {
            return true;
        }

        if (res + 1 < PageCount && Pages[res + 1].MinIndex <= row)
        {
            do
            {
                res++;
            } while (res + 1 < PageCount && Pages[res + 1].MinIndex <= row);

            return true;
        }

        if (res - 1 >= 0 && Pages[res - 1].MaxIndex >= row)
        {
            do
            {
                res--;
            } while (res - 1 > 0 && Pages[res - 1].MaxIndex >= row);

            return true;
        }

        return false;
    }

    internal int GetNextRow(int row)
    {
        //var page = (int)((ulong)row >> CellStore<int>.pageBits);
        int p = GetPosition(row);
        if (p < 0)
        {
            p = ~p;
            if (p >= PageCount)
            {
                return -1;
            }

            if (Pages[p].IndexOffset + Pages[p].Rows[0].Index < row)
            {
                if (p + 1 >= PageCount)
                {
                    return -1;
                }

                return Pages[p + 1].IndexOffset + Pages[p].Rows[0].Index;
            }

            return Pages[p].IndexOffset + Pages[p].Rows[0].Index;
        }

        if (p < PageCount)
        {
            int r = Pages[p].GetNextRow(row);
            if (r >= 0)
            {
                return Pages[p].IndexOffset + Pages[p].Rows[r].Index;
            }

            if (++p < PageCount)
            {
                return Pages[p].IndexOffset + Pages[p].Rows[0].Index;
            }

            return -1;
        }

        return -1;
    }

    internal int FindNext(int page)
    {
        int p = GetPosition(page);
        if (p < 0)
        {
            return ~p;
        }

        return p;
    }
}

internal class PageIndex : IndexBase, IDisposable
{
    internal IndexItem SearchIx;
    internal int Offset;
    internal int RowCount;

    public PageIndex()
    {
        Rows = new IndexItem[CellStore<int>.PAGE_SIZE_MIN];
        RowCount = 0;
    }

    public PageIndex(IndexItem[] rows, int count)
    {
        Rows = rows;
        RowCount = count;
    }

    public PageIndex(PageIndex pageItem, int start, int size)
        : this(pageItem, start, size, pageItem.Index, pageItem.Offset)
    {
    }

    public PageIndex(PageIndex pageItem, int start, int size, short index, int offset)
    {
        Rows = new IndexItem[CellStore<int>.GetSize(size)];
        Array.Copy(pageItem.Rows, start, Rows, 0, size);
        RowCount = size;
        Index = index;
        Offset = offset;
    }

    internal int IndexOffset => IndexExpanded + Offset;

    internal int IndexExpanded => Index << CellStore<int>.PAGE_BITS;

    internal IndexItem[] Rows { get; set; }

    public int MinIndex
    {
        get
        {
            if (Rows.Length > 0)
            {
                return IndexOffset + Rows[0].Index;
            }

            return -1;
        }
    }

    public int MaxIndex
    {
        get
        {
            if (RowCount > 0)
            {
                return IndexOffset + Rows[RowCount - 1].Index;
            }

            return -1;
        }
    }

    public void Dispose()
    {
        Rows = null;
    }

    ~PageIndex()
    {
        Rows = null;
    }

    internal int GetPosition(int offset)
    {
        SearchIx.Index = (short)offset;
        return Array.BinarySearch(Rows, 0, RowCount, SearchIx);
    }

    internal int GetNextRow(int row)
    {
        int offset = row - IndexOffset;
        int o = GetPosition(offset);
        if (o < 0)
        {
            o = ~o;
            if (o < RowCount)
            {
                return o;
            }

            return -1;
        }

        return o;
    }

    public int GetIndex(int pos)
    {
        return IndexOffset + Rows[pos].Index;
    }
}

/// <summary>
/// This is the store for all Rows, Columns and Cells.
/// It is a Dictionary implementation that allows you to change the Key (the RowID, ColumnID or CellID )
/// </summary>
internal class CellStore<T> : IDisposable // : IEnumerable<ulong>, IEnumerator<ulong>
{
    internal const int COL_SIZE_MIN = 32;

    /**** Size constants ****/
    internal const int PAGE_BITS = 10; //13bits=8192  Note: Maximum is 13 bits since short is used (PageMax=16K)
    internal const int PAGE_SIZE = 1 << PAGE_BITS;
    internal const int PAGE_SIZE_MAX = PAGE_SIZE << 1; //Double page size
    internal const int PAGE_SIZE_MIN = 1 << 10;
    internal const int PAGES_PER_COLUMN_MIN = 32;
    int _colPos = -1, _row;
    internal ColumnIndex[] ColumnIndex;
    internal IndexItem SearchItem;
    internal IndexBase SearchIx = new();

    List<T> _values = new();
    internal int ColumnCount;

    public CellStore()
    {
        ColumnIndex = new ColumnIndex[COL_SIZE_MIN];
    }

    internal int Count
    {
        get
        {
            int count = 0;
            for (int c = 0; c < ColumnCount; c++)
            {
                for (int p = 0; p < ColumnIndex[c].PageCount; p++)
                {
                    count += ColumnIndex[c].Pages[p].RowCount;
                }
            }

            return count;
        }
    }

    public ulong Current => ((ulong)_row << 32) | (ulong)ColumnIndex[_colPos].Index;

    public void Dispose()
    {
        _values?.Clear();
        for (int c = 0; c < ColumnCount; c++)
        {
            if (ColumnIndex[c] != null)
            {
                ((IDisposable)ColumnIndex[c]).Dispose();
            }
        }

        _values = null;
        ColumnIndex = null;
    }

    ~CellStore()
    {
        if (_values != null)
        {
            _values.Clear();
            _values = null;
        }

        ColumnIndex = null;
    }

    internal int GetPosition(int column)
    {
        if (column < ColumnCount && ColumnIndex[column].Index == column) //Check if th column is lesser than
        {
            return column;
        }

        SearchIx.Index = (short)column;
        return Array.BinarySearch(ColumnIndex, 0, ColumnCount, SearchIx);
    }

    internal CellStore<T> Clone()
    {
        int row, col;
        var ret = new CellStore<T>();
        for (int c = 0; c < ColumnCount; c++)
        {
            col = ColumnIndex[c].Index;
            for (int p = 0; p < ColumnIndex[c].PageCount; p++)
            {
                for (int r = 0; r < ColumnIndex[c].Pages[p].RowCount; r++)
                {
                    row = ColumnIndex[c].Pages[p].IndexOffset + ColumnIndex[c].Pages[p].Rows[r].Index;
                    ret.SetValue(row, col, _values[ColumnIndex[c].Pages[p].Rows[r].IndexPointer]);
                }
            }
        }

        return ret;
    }

    internal bool GetDimension(out int fromRow, out int fromCol, out int toRow, out int toCol)
    {
        if (ColumnCount == 0)
        {
            fromRow = fromCol = toRow = toCol = 0;
            return false;
        }

        fromCol = ColumnIndex[0].Index;
        int fromIndex = 0;
        if (fromCol <= 0 && ColumnCount > 1)
        {
            fromCol = ColumnIndex[1].Index;
            fromIndex = 1;
        }
        else if (ColumnCount == 1 && fromCol <= 0)
        {
            fromRow = fromCol = toRow = toCol = 0;
            return false;
        }

        int col = ColumnCount - 1;
        while (col > 0)
        {
            if (ColumnIndex[col].PageCount == 0 || ColumnIndex[col].Pages[0].RowCount > 1 || ColumnIndex[col].Pages[0].Rows[0].Index > 0)
            {
                break;
            }

            col--;
        }

        toCol = ColumnIndex[col].Index;
        if (toCol == 0)
        {
            fromRow = fromCol = toRow = toCol = 0;
            return false;
        }

        fromRow = toRow = 0;

        for (int c = fromIndex; c < ColumnCount; c++)
        {
            int first, last;
            if (ColumnIndex[c].PageCount == 0) continue;
            if (ColumnIndex[c].Pages[0].RowCount > 0 && ColumnIndex[c].Pages[0].Rows[0].Index > 0)
            {
                first = ColumnIndex[c].Pages[0].IndexOffset + ColumnIndex[c].Pages[0].Rows[0].Index;
            }
            else
            {
                if (ColumnIndex[c].Pages[0].RowCount > 1)
                {
                    first = ColumnIndex[c].Pages[0].IndexOffset + ColumnIndex[c].Pages[0].Rows[1].Index;
                }
                else if (ColumnIndex[c].PageCount > 1)
                {
                    first = ColumnIndex[c].Pages[0].IndexOffset + ColumnIndex[c].Pages[1].Rows[0].Index;
                }
                else
                {
                    first = 0;
                }
            }

            int lp = ColumnIndex[c].PageCount - 1;
            while (ColumnIndex[c].Pages[lp].RowCount == 0 && lp != 0)
            {
                lp--;
            }

            PageIndex p = ColumnIndex[c].Pages[lp];
            if (p.RowCount > 0)
            {
                last = p.IndexOffset + p.Rows[p.RowCount - 1].Index;
            }
            else
            {
                last = first;
            }

            if (first > 0 && (first < fromRow || fromRow == 0))
            {
                fromRow = first;
            }

            if (first > 0 && (last > toRow || toRow == 0))
            {
                toRow = last;
            }
        }

        if (fromRow <= 0 || toRow <= 0)
        {
            fromRow = fromCol = toRow = toCol = 0;
            return false;
        }

        return true;
    }

    internal int FindNext(int column)
    {
        int c = GetPosition(column);
        if (c < 0)
        {
            return ~c;
        }

        return c;
    }

    internal T GetValue(int row, int column)
    {
        int i = GetPointer(row, column);
        if (i >= 0)
        {
            return _values[i];
        }

        return default;
        //var col = GetPosition(Column);
        //if (col >= 0)  
        //{
        //    var pos = _columnIndex[col].GetPosition(Row);
        //    if (pos >= 0) 
        //    {
        //        var pageItem = _columnIndex[col].Pages[pos];
        //        if (pageItem.MinIndex > Row)
        //        {
        //            pos--;
        //            if (pos < 0)
        //            {
        //                return default(T);
        //            }
        //            else
        //            {
        //                pageItem = _columnIndex[col].Pages[pos];
        //            }
        //        }
        //        short ix = (short)(Row - pageItem.IndexOffset);
        //        var cellPos = Array.BinarySearch(pageItem.Rows, 0, pageItem.RowCount, new IndexBase() { Index = ix });
        //        if (cellPos >= 0) 
        //        {
        //            return _values[pageItem.Rows[cellPos].IndexPointer];
        //        }
        //        else //Cell does not exist
        //        {
        //            return default(T);
        //        }
        //    }
        //    else //Page does not exist
        //    {
        //        return default(T);
        //    }
        //}
        //else //Column does not exist
        //{
        //    return default(T);
        //}
    }

    int GetPointer(int row, int column)
    {
        int col = GetPosition(column);
        if (col >= 0)
        {
            int pos = ColumnIndex[col].GetPosition(row);
            if (pos >= 0 && pos < ColumnIndex[col].PageCount)
            {
                PageIndex pageItem = ColumnIndex[col].Pages[pos];
                if (pageItem.MinIndex > row)
                {
                    pos--;
                    if (pos < 0)
                    {
                        return -1;
                    }

                    pageItem = ColumnIndex[col].Pages[pos];
                }

                short ix = (short)(row - pageItem.IndexOffset);
                SearchItem.Index = ix;
                int cellPos = Array.BinarySearch(pageItem.Rows, 0, pageItem.RowCount, SearchItem);
                if (cellPos >= 0)
                {
                    return pageItem.Rows[cellPos].IndexPointer;
                }

                //Cell does not exist
                return -1;
            }

            //Page does not exist
            return -1;
        }

        //Column does not exist
        return -1;
    }

    internal bool Exists(int row, int column)
    {
        return GetPointer(row, column) >= 0;
    }

    internal bool Exists(int row, int column, ref T value)
    {
        int p = GetPointer(row, column);
        if (p >= 0)
        {
            value = _values[p];
            return true;
        }

        return false;
    }

    internal void SetValue(int row, int column, T value)
    {
        lock (ColumnIndex)
        {
            int col = GetPosition(column); //Array.BinarySearch(_columnIndex, 0, ColumnCount, new IndexBase() { Index = (short)(Column) });
            short page = (short)(row >> PAGE_BITS);
            if (col >= 0)
            {
                //var pos = Array.BinarySearch(_columnIndex[col].Pages, 0, _columnIndex[col].Count, new IndexBase() { Index = page });
                int pos = ColumnIndex[col].GetPosition(row);
                if (pos < 0)
                {
                    pos = ~pos;
                    if (pos - 1 < 0 || ColumnIndex[col].Pages[pos - 1].IndexOffset + PAGE_SIZE - 1 < row)
                    {
                        AddPage(ColumnIndex[col], pos, page);
                    }
                    else
                    {
                        pos--;
                    }
                }

                if (pos >= ColumnIndex[col].PageCount)
                {
                    AddPage(ColumnIndex[col], pos, page);
                }

                PageIndex pageItem = ColumnIndex[col].Pages[pos];
                if (!(pageItem.MinIndex <= row && pageItem.MaxIndex >= row) && pageItem.IndexExpanded > row) //TODO: Fix issue
                {
                    pos--;
                    page--;
                    if (pos < 0)
                    {
                        throw new Exception("Unexpected error when setting value");
                    }

                    pageItem = ColumnIndex[col].Pages[pos];
                }

                short ix = (short)(row - ((pageItem.Index << PAGE_BITS) + pageItem.Offset));
                SearchItem.Index = ix;
                int cellPos = Array.BinarySearch(pageItem.Rows, 0, pageItem.RowCount, SearchItem);
                if (cellPos < 0)
                {
                    cellPos = ~cellPos;
                    AddCell(ColumnIndex[col], pos, cellPos, ix, value);
                }
                else
                {
                    _values[pageItem.Rows[cellPos].IndexPointer] = value;
                }
            }
            else //Column does not exist
            {
                col = ~col;
                AddColumn(col, column);
                AddPage(ColumnIndex[col], 0, page);
                short ix = (short)(row - (page << PAGE_BITS));
                AddCell(ColumnIndex[col], 0, 0, ix, value);
            }
        }
    }

    /// <summary>
    /// Set Value for Range
    /// </summary>
    /// <param name="fromRow"></param>
    /// <param name="fromColumn"></param>
    /// <param name="toRow"></param>
    /// <param name="toColumn"></param>
    /// <param name="updater"></param>
    /// <param name="value"></param>
    internal void SetRangeValueSpecial(int fromRow, int fromColumn, int toRow, int toColumn, SetRangeValueDelegate updater, object value)
    {
        lock (ColumnIndex)
        {
            // split row to page groups (pageIndex to RowNo List)
            var pages = new Dictionary<short, List<int>>();
            for (int rowIx = fromRow; rowIx <= toRow; rowIx++)
            {
                short pageIx = (short)(rowIx >> PAGE_BITS);
                if (!pages.ContainsKey(pageIx)) pages.Add(pageIx, new List<int>());
                pages[pageIx].Add(rowIx);
            }

            for (int colIx = fromColumn; colIx <= toColumn; colIx++)
            {
                //var col = Array.BinarySearch(_columnIndex, 0, ColumnCount, new IndexBase() { Index = (short)(colIx) });
                int col = GetPosition(colIx);

                foreach (KeyValuePair<short, List<int>> pair in pages)
                {
                    short page = pair.Key;
                    foreach (int rowIx in pair.Value)
                    {
                        if (col >= 0)
                        {
                            //var pos = Array.BinarySearch(_columnIndex[col].Pages, 0, _columnIndex[col].Count, new IndexBase() { Index = page });
                            int pos = ColumnIndex[col].GetPosition(rowIx);
                            if (pos < 0)
                            {
                                pos = ~pos;
                                if (pos - 1 < 0 || ColumnIndex[col].Pages[pos - 1].IndexOffset + PAGE_SIZE - 1 < rowIx)
                                {
                                    AddPage(ColumnIndex[col], pos, page);
                                }
                                else
                                {
                                    pos--;
                                }
                            }

                            if (pos >= ColumnIndex[col].PageCount)
                            {
                                AddPage(ColumnIndex[col], pos, page);
                            }

                            PageIndex pageItem = ColumnIndex[col].Pages[pos];
                            if (pageItem.IndexOffset > rowIx)
                            {
                                pos--;
                                page--;
                                if (pos < 0)
                                {
                                    throw new Exception("Unexpected error when setting value");
                                }

                                pageItem = ColumnIndex[col].Pages[pos];
                            }

                            short ix = (short)(rowIx - ((pageItem.Index << PAGE_BITS) + pageItem.Offset));
                            SearchItem.Index = ix;
                            int cellPos = Array.BinarySearch(pageItem.Rows, 0, pageItem.RowCount, SearchItem);
                            if (cellPos < 0)
                            {
                                cellPos = ~cellPos;
                                AddCell(ColumnIndex[col], pos, cellPos, ix, default);
                                updater(_values, pageItem.Rows[cellPos].IndexPointer, rowIx, colIx, value);
                            }
                            else
                            {
                                updater(_values, pageItem.Rows[cellPos].IndexPointer, rowIx, colIx, value);
                            }
                        }
                        else //Column does not exist
                        {
                            col = ~col;
                            AddColumn(col, colIx);
                            AddPage(ColumnIndex[col], 0, page);
                            short ix = (short)(rowIx - (page << PAGE_BITS));
                            AddCell(ColumnIndex[col], 0, 0, ix, default);
                            updater(_values, ColumnIndex[col].Pages[0].Rows[0].IndexPointer, rowIx, colIx, value);
                        }
                    }
                }
            }
        }
    }

    // Set object's property atomically
    internal void SetValueSpecial(int row, int column, SetValueDelegate updater, object value)
    {
        lock (ColumnIndex)
        {
            //var col = Array.BinarySearch(_columnIndex, 0, ColumnCount, new IndexBase() { Index = (short)(Column) });
            int col = GetPosition(column);
            short page = (short)(row >> PAGE_BITS);
            if (col >= 0)
            {
                //var pos = Array.BinarySearch(_columnIndex[col].Pages, 0, _columnIndex[col].Count, new IndexBase() { Index = page });
                int pos = ColumnIndex[col].GetPosition(row);
                if (pos < 0)
                {
                    pos = ~pos;
                    if (pos - 1 < 0 || ColumnIndex[col].Pages[pos - 1].IndexOffset + PAGE_SIZE - 1 < row)
                    {
                        AddPage(ColumnIndex[col], pos, page);
                    }
                    else
                    {
                        pos--;
                    }
                }

                if (pos >= ColumnIndex[col].PageCount)
                {
                    AddPage(ColumnIndex[col], pos, page);
                }

                PageIndex pageItem = ColumnIndex[col].Pages[pos];
                if (pageItem.IndexOffset > row)
                {
                    pos--;
                    page--;
                    if (pos < 0)
                    {
                        throw new Exception("Unexpected error when setting value");
                    }

                    pageItem = ColumnIndex[col].Pages[pos];
                }

                short ix = (short)(row - ((pageItem.Index << PAGE_BITS) + pageItem.Offset));
                SearchItem.Index = ix;
                int cellPos = Array.BinarySearch(pageItem.Rows, 0, pageItem.RowCount, SearchItem);
                if (cellPos < 0)
                {
                    cellPos = ~cellPos;
                    AddCell(ColumnIndex[col], pos, cellPos, ix, default);
                    updater(_values, pageItem.Rows[cellPos].IndexPointer, value);
                }
                else
                {
                    updater(_values, pageItem.Rows[cellPos].IndexPointer, value);
                }
            }
            else //Column does not exist
            {
                col = ~col;
                AddColumn(col, column);
                AddPage(ColumnIndex[col], 0, page);
                short ix = (short)(row - (page << PAGE_BITS));
                AddCell(ColumnIndex[col], 0, 0, ix, default);
                updater(_values, ColumnIndex[col].Pages[0].Rows[0].IndexPointer, value);
            }
        }
    }

    internal void Insert(int fromRow, int fromCol, int rows, int columns)
    {
        lock (ColumnIndex)
        {
            if (columns > 0)
            {
                int col = GetPosition(fromCol);
                if (col < 0)
                {
                    col = ~col;
                }

                for (int c = col; c < ColumnCount; c++)
                {
                    ColumnIndex[c].Index += (short)columns;
                }
            }
            else
            {
                int page = fromRow >> PAGE_BITS;
                for (int c = 0; c < ColumnCount; c++)
                {
                    ColumnIndex column = ColumnIndex[c];
                    int pagePos = column.GetPosition(fromRow);
                    if (pagePos >= 0)
                    {
                        if (IsWithinPage(fromRow, column, pagePos)) //The row is inside the page
                        {
                            int offset = fromRow - column.Pages[pagePos].IndexOffset;
                            int rowPos = column.Pages[pagePos].GetPosition(offset);
                            if (rowPos < 0)
                            {
                                rowPos = ~rowPos;
                            }

                            UpdateIndexOffset(column, pagePos, rowPos, fromRow, rows);
                        }
                        else if (pagePos > 0 && IsWithinPage(fromRow, column, pagePos - 1)) //The row is inside the previous page
                        {
                            int offset = fromRow - ((page - 1) << PAGE_BITS);
                            int rowPos = column.Pages[pagePos - 1].GetPosition(offset);
                            if (rowPos > 0 && pagePos > 0)
                            {
                                UpdateIndexOffset(column, pagePos - 1, rowPos, fromRow, rows);
                            }
                        }
                        else if (column.PageCount >= pagePos + 1)
                        {
                            int offset = fromRow - column.Pages[pagePos].IndexOffset;
                            int rowPos = column.Pages[pagePos].GetPosition(offset);
                            if (rowPos < 0)
                            {
                                rowPos = ~rowPos;
                            }

                            if (column.Pages[pagePos].RowCount > rowPos)
                            {
                                UpdateIndexOffset(column, pagePos, rowPos, fromRow, rows);
                            }
                            else
                            {
                                UpdateIndexOffset(column, pagePos + 1, 0, fromRow, rows);
                            }
                        }
                    }
                    else
                    {
                        UpdateIndexOffset(column, ~pagePos, 0, fromRow, rows);
                    }
                }
            }
        }
    }

    private static bool IsWithinPage(int row, ColumnIndex column, int pagePos)
    {
        return row >= column.Pages[pagePos].MinIndex && row <= column.Pages[pagePos].MaxIndex;
    }

    internal void Clear(int fromRow, int fromCol, int rows, int columns)
    {
        Delete(fromRow, fromCol, rows, columns, false);
    }

    internal void Delete(int fromRow, int fromCol, int rows, int columns)
    {
        Delete(fromRow, fromCol, rows, columns, true);
    }

    internal void Delete(int fromRow, int fromCol, int rows, int columns, bool shift)
    {
        lock (ColumnIndex)
        {
            if (columns > 0 && fromRow == 0 && rows >= ExcelPackage.MaxRows)
            {
                DeleteColumns(fromCol, columns, shift);
            }
            else
            {
                int toCol = fromCol + columns - 1;
                int pageFromRow = fromRow >> PAGE_BITS;
                for (int c = 0; c < ColumnCount; c++)
                {
                    ColumnIndex column = ColumnIndex[c];
                    if (column.Index >= fromCol)
                    {
                        if (column.Index > toCol) break;
                        int pagePos = column.GetPosition(fromRow);
                        if (pagePos < 0) pagePos = ~pagePos;
                        if (pagePos < column.PageCount)
                        {
                            PageIndex page = column.Pages[pagePos];
                            if (shift && page.RowCount > 0 && page.MinIndex > fromRow && page.MaxIndex >= fromRow + rows)
                            {
                                int o = page.MinIndex - fromRow;
                                if (o < rows)
                                {
                                    rows -= o;
                                    page.Offset -= o;
                                    UpdatePageOffset(column, pagePos, o);
                                }
                                else
                                {
                                    page.Offset -= rows;
                                    UpdatePageOffset(column, pagePos, rows);
                                    continue;
                                }
                            }

                            if (page.RowCount > 0 && page.MinIndex <= fromRow + rows - 1 && page.MaxIndex >= fromRow) //The row is inside the page
                            {
                                int endRow = fromRow + rows;
                                int delEndRow = DeleteCells(column.Pages[pagePos], fromRow, endRow, shift);
                                if (shift && delEndRow != fromRow) UpdatePageOffset(column, pagePos, delEndRow - fromRow);
                                if (endRow > delEndRow && pagePos < column.PageCount && column.Pages[pagePos].MinIndex < endRow)
                                {
                                    pagePos = delEndRow == fromRow ? pagePos : pagePos + 1;
                                    int rowsLeft = DeletePage(shift ? fromRow : delEndRow, endRow - delEndRow, column, pagePos, shift);
                                    //if (shift) UpdatePageOffset(column, pagePos, endRow - fromRow - rowsLeft);
                                    if (rowsLeft > 0)
                                    {
                                        int fr = shift ? fromRow : endRow - rowsLeft;
                                        pagePos = column.GetPosition(fr);
                                        delEndRow = DeleteCells(column.Pages[pagePos], fr, shift ? fr + rowsLeft : endRow, shift);
                                        if (shift) UpdatePageOffset(column, pagePos, rowsLeft);
                                    }
                                }
                            }
                            else if (pagePos > 0 && column.Pages[pagePos].IndexOffset > fromRow) //The row is on the page before.
                            {
                                int offset = fromRow + rows - 1 - ((pageFromRow - 1) << PAGE_BITS);
                                int rowPos = column.Pages[pagePos - 1].GetPosition(offset);
                                if (rowPos > 0 && pagePos > 0)
                                {
                                    if (shift) UpdateIndexOffset(column, pagePos - 1, rowPos, fromRow + rows - 1, -rows);
                                }
                            }
                            else
                            {
                                if (shift && pagePos + 1 < column.PageCount) UpdateIndexOffset(column, pagePos + 1, 0, column.Pages[pagePos + 1].MinIndex, -rows);
                            }
                        }
                    }
                }
            }
        }
    }

    private void UpdatePageOffset(ColumnIndex column, int pagePos, int rows)
    {
        //Update Pageoffset

        if (++pagePos < column.PageCount)
        {
            for (int p = pagePos; p < column.PageCount; p++)
            {
                if (column.Pages[p].Offset - rows <= -PAGE_SIZE)
                {
                    column.Pages[p].Index--;
                    column.Pages[p].Offset -= rows - PAGE_SIZE;
                }
                else
                {
                    column.Pages[p].Offset -= rows;
                }
            }

            if (Math.Abs(column.Pages[pagePos].Offset) > PAGE_SIZE ||
                Math.Abs(column.Pages[pagePos].Rows[column.Pages[pagePos].RowCount - 1].Index) > PAGE_SIZE_MAX) //Split or Merge???
            {
                rows = ResetPageOffset(column, pagePos, rows);
                ////MergePages
                //if (column.Pages[pagePos - 1].Index + 1 == column.Pages[pagePos].Index)
                //{
                //    if (column.Pages[pagePos].IndexOffset + column.Pages[pagePos].Rows[column.Pages[pagePos].RowCount - 1].Index + rows -
                //        column.Pages[pagePos - 1].IndexOffset + column.Pages[pagePos - 1].Rows[0].Index <= PageSize)
                //    {
                //        //Merge
                //        MergePage(column, pagePos - 1, -rows);
                //    }
                //    else
                //    {
                //        //Split
                //    }
                //}
                //rows -= PageSize;
                //for (int p = pagePos; p < column.PageCount; p++)
                //{                            
                //    column.Pages[p].Index -= 1;
                //}
            }
        }
    }

    private int ResetPageOffset(ColumnIndex column, int pagePos, int rows)
    {
        PageIndex fromPage = column.Pages[pagePos];
        PageIndex toPage;
        short pageAdd = 0;
        if (fromPage.Offset < -PAGE_SIZE)
        {
            toPage = column.Pages[pagePos - 1];
            pageAdd = -1;
            if (fromPage.Index - 1 == toPage.Index)
            {
                if (fromPage.IndexOffset + fromPage.Rows[fromPage.RowCount - 1].Index -
                    toPage.IndexOffset + toPage.Rows[0].Index <= PAGE_SIZE_MAX)
                {
                    MergePage(column, pagePos - 1);
                    //var newPage = new PageIndex(toPage, 0, GetSize(fromPage.RowCount + toPage.RowCount));
                    //newPage.RowCount = fromPage.RowCount + fromPage.RowCount;
                    //Array.Copy(toPage.Rows, 0, newPage.Rows, 0, toPage.RowCount);
                    //Array.Copy(fromPage.Rows, 0, newPage.Rows, toPage.RowCount, fromPage.RowCount);
                    //for (int r = toPage.RowCount; r < newPage.RowCount; r++)
                    //{
                    //    newPage.Rows[r].Index += (short)(fromPage.IndexOffset - toPage.IndexOffset);
                    //}
                }
            }
            else //No page after 
            {
                fromPage.Index -= pageAdd;
                fromPage.Offset += PAGE_SIZE;
            }
        }
        else if (fromPage.Offset > PAGE_SIZE)
        {
            toPage = column.Pages[pagePos + 1];
            pageAdd = 1;
            if (fromPage.Index + 1 == toPage.Index)
            {
            }
            else
            {
                fromPage.Index += pageAdd;
                fromPage.Offset += PAGE_SIZE;
            }
        }

        return rows;
    }

    private int DeletePage(int fromRow, int rows, ColumnIndex column, int pagePos, bool shift)
    {
        PageIndex page = column.Pages[pagePos];
        int startRows = rows;
        while (page != null && page.MinIndex >= fromRow && ((shift && page.MaxIndex < fromRow + rows) || (!shift && page.MaxIndex < fromRow + startRows)))
        {
            //Delete entire page.
            int delSize = page.MaxIndex - page.MinIndex + 1;
            rows -= delSize;
            Array.Copy(column.Pages, pagePos + 1, column.Pages, pagePos, column.PageCount - pagePos - 1);
            column.PageCount--;
            if (column.PageCount == 0)
            {
                return 0;
            }

            if (shift)
            {
                for (int i = pagePos; i < column.PageCount; i++)
                {
                    column.Pages[i].Offset -= delSize;
                    if (column.Pages[i].Offset <= -PAGE_SIZE)
                    {
                        column.Pages[i].Index--;
                        column.Pages[i].Offset += PAGE_SIZE;
                    }
                }
            }

            if (column.PageCount > pagePos)
            {
                page = column.Pages[pagePos];
                //page.Offset = pagePos == 0 ? 1 : prevOffset;  //First page can only reference to rows starting from Index == 1
            }
            else
            {
                //No more pages, return 0
                return 0;
            }
        }

        return rows;
    }

    ///
    private int DeleteCells(PageIndex page, int fromRow, int toRow, bool shift)
    {
        int fromPos = page.GetPosition(fromRow - page.IndexOffset);
        if (fromPos < 0)
        {
            fromPos = ~fromPos;
        }

        int maxRow = page.MaxIndex;
        int offset = toRow - page.IndexOffset;
        if (offset > PAGE_SIZE_MAX) offset = PAGE_SIZE_MAX;
        int toPos = page.GetPosition(offset);
        if (toPos < 0)
        {
            toPos = ~toPos;
        }

        if (fromPos <= toPos && fromPos < page.RowCount && page.GetIndex(fromPos) < toRow)
        {
            if (toRow > page.MaxIndex)
            {
                if (fromRow == page.MinIndex) //Delete entire page, late in the page delete method
                {
                    return fromRow;
                }

                int r = page.MaxIndex;
                int deletedRow = page.RowCount - fromPos;
                page.RowCount -= deletedRow;
                return r + 1;
            }

            int rows = toRow - fromRow;
            if (shift) UpdateRowIndex(page, toPos, rows);
            Array.Copy(page.Rows, toPos, page.Rows, fromPos, page.RowCount - toPos);
            page.RowCount -= toPos - fromPos;

            return toRow;
        }

        if (shift)
        {
            UpdateRowIndex(page, toPos, toRow - fromRow);
        }

        return toRow < maxRow ? toRow : maxRow;
    }

    private static void UpdateRowIndex(PageIndex page, int toPos, int rows)
    {
        for (int r = toPos; r < page.RowCount; r++)
        {
            page.Rows[r].Index -= (short)rows;
        }
    }

    private void DeleteColumns(int fromCol, int columns, bool shift)
    {
        int fPos = GetPosition(fromCol);
        if (fPos < 0)
        {
            fPos = ~fPos;
        }

        int tPos = fPos;
        for (int c = fPos; c <= ColumnCount; c++)
        {
            tPos = c;
            if (tPos == ColumnCount || ColumnIndex[c].Index >= fromCol + columns)
            {
                break;
            }
        }

        if (ColumnCount <= fPos)
        {
            return;
        }

        if (ColumnIndex[fPos].Index >= fromCol && ColumnIndex[fPos].Index <= fromCol + columns)
        {
            //if (_columnIndex[fPos].Index < ColumnCount)
            //{
            if (tPos < ColumnCount)
            {
                Array.Copy(ColumnIndex, tPos, ColumnIndex, fPos, ColumnCount - tPos);
            }

            ColumnCount -= tPos - fPos;
            //}
        }

        if (shift)
        {
            for (int c = fPos; c < ColumnCount; c++)
            {
                ColumnIndex[c].Index -= (short)columns;
            }
        }
    }

    private void UpdateIndexOffset(ColumnIndex column, int pagePos, int rowPos, int row, int rows)
    {
        if (pagePos >= column.PageCount) return; //A page after last cell.
        PageIndex page = column.Pages[pagePos];
        if (rows > PAGE_SIZE)
        {
            short addPages = (short)(rows >> PAGE_BITS);
            int offset = +(rows - PAGE_SIZE * addPages);
            for (int p = pagePos + 1; p < column.PageCount; p++)
            {
                if (column.Pages[p].Offset + offset > PAGE_SIZE)
                {
                    column.Pages[p].Index += (short)(addPages + 1);
                    column.Pages[p].Offset += offset - PAGE_SIZE;
                }
                else
                {
                    column.Pages[p].Index += addPages;
                    column.Pages[p].Offset += offset;
                }
            }

            int size = page.RowCount - rowPos;
            if (page.RowCount > rowPos)
            {
                if (column.PageCount - 1 == pagePos) //No page after, create a new one.
                {
                    //Copy rows to next page.
                    PageIndex newPage = CopyNew(page, rowPos, size);
                    newPage.Index = (short)((row + rows) >> PAGE_BITS);
                    newPage.Offset = row + rows - newPage.Index * PAGE_SIZE - newPage.Rows[0].Index;
                    if (newPage.Offset > PAGE_SIZE)
                    {
                        newPage.Index++;
                        newPage.Offset -= PAGE_SIZE;
                    }

                    AddPage(column, pagePos + 1, newPage);
                    page.RowCount = rowPos;
                }
                else
                {
                    if (column.Pages[pagePos + 1].RowCount + size > PAGE_SIZE_MAX) //Split Page
                    {
                        SplitPageInsert(column, pagePos, rowPos, rows, size, addPages);
                    }
                    else //Copy Page.
                    {
                        CopyMergePage(page, rowPos, rows, size, column.Pages[pagePos + 1]);
                    }
                }
            }
        }
        else
        {
            //Add to Pages.
            for (int r = rowPos; r < page.RowCount; r++)
            {
                page.Rows[r].Index += (short)rows;
            }

            if (page.Offset + page.Rows[page.RowCount - 1].Index >= PAGE_SIZE_MAX) //Can not be larger than the max size of the page.
            {
                AdjustIndex(column, pagePos);
                if (page.Offset + page.Rows[page.RowCount - 1].Index >= PAGE_SIZE_MAX)
                {
                    pagePos = SplitPage(column, pagePos);
                }
                //IndexItem[] newRows = new IndexItem[GetSize(page.RowCount - page.Rows[r].Index)];
                //var newPage = new PageIndex(newRows, r);
                //newPage.Index = (short)(pagePos + 1);
                //TODO: MoveRows to next page.
            }

            for (int p = pagePos + 1; p < column.PageCount; p++)
            {
                if (column.Pages[p].Offset + rows < PAGE_SIZE)
                {
                    column.Pages[p].Offset += rows;
                }
                else
                {
                    column.Pages[p].Index++;
                    column.Pages[p].Offset = (column.Pages[p].Offset + rows) % PAGE_SIZE;
                }
            }
        }
    }

    private void SplitPageInsert(ColumnIndex column, int pagePos, int rowPos, int rows, int size, int addPages)
    {
        _ = new IndexItem[GetSize(size)];
        PageIndex page = column.Pages[pagePos];

        int rStart = -1;
        for (int r = rowPos; r < page.RowCount; r++)
        {
            if (page.IndexExpanded - (page.Rows[r].Index + rows) > PAGE_SIZE)
            {
                rStart = r;
                break;
            }

            page.Rows[r].Index += (short)rows;
        }

        int rc = page.RowCount - rStart;
        page.RowCount = rStart;
        if (rc > 0)
        {
            //Copy to a new page
            PageIndex newPage = CopyNew(page, rStart, rc);
            short ix = (short)(page.Index + addPages);
            int offset = page.IndexOffset + rows - ix * PAGE_SIZE;
            if (offset > PAGE_SIZE)
            {
                ix += (short)(offset / PAGE_SIZE);
                offset %= PAGE_SIZE;
            }

            newPage.Index = ix;
            newPage.Offset = offset;
            AddPage(column, pagePos + 1, newPage);
        }

        //Copy from next Row
    }

    private void CopyMergePage(PageIndex page, int rowPos, int rows, int size, PageIndex toPage)
    {
        var newRows = new IndexItem[GetSize(toPage.RowCount + size)];
        page.RowCount -= size;
        Array.Copy(page.Rows, rowPos, newRows, 0, size);
        for (int r = 0; r < size; r++)
        {
            newRows[r].Index += (short)(page.IndexOffset + rows - toPage.IndexOffset);
        }

        Array.Copy(toPage.Rows, 0, newRows, size, toPage.RowCount);
        toPage.Rows = newRows;
        toPage.RowCount += size;
    }

    private void MergePage(ColumnIndex column, int pagePos)
    {
        PageIndex page1 = column.Pages[pagePos];
        PageIndex page2 = column.Pages[pagePos + 1];

        var newPage = new PageIndex(page1, 0, page1.RowCount + page2.RowCount);
        newPage.RowCount = page1.RowCount + page2.RowCount;
        Array.Copy(page1.Rows, 0, newPage.Rows, 0, page1.RowCount);
        Array.Copy(page2.Rows, 0, newPage.Rows, page1.RowCount, page2.RowCount);
        for (int r = page1.RowCount; r < newPage.RowCount; r++)
        {
            newPage.Rows[r].Index += (short)(page2.IndexOffset - page1.IndexOffset);
        }

        column.Pages[pagePos] = newPage;
        column.PageCount--;

        if (column.PageCount > pagePos + 1)
        {
            Array.Copy(column.Pages, pagePos + 2, column.Pages, pagePos + 1, column.PageCount - (pagePos + 1));
            for (int p = pagePos + 1; p < column.PageCount; p++)
            {
                column.Pages[p].Index--;
                column.Pages[p].Offset += PAGE_SIZE;
            }
        }
    }

    private PageIndex CopyNew(PageIndex pageFrom, int rowPos, int size)
    {
        var newRows = new IndexItem[GetSize(size)];
        Array.Copy(pageFrom.Rows, rowPos, newRows, 0, size);
        return new PageIndex(newRows, size);
    }

    internal static int GetSize(int size)
    {
        int newSize = 256;
        while (newSize < size)
        {
            newSize <<= 1;
        }

        return newSize;
    }

    private void AddCell(ColumnIndex columnIndex, int pagePos, int pos, short ix, T value)
    {
        PageIndex pageItem = columnIndex.Pages[pagePos];
        if (pageItem.RowCount == pageItem.Rows.Length)
        {
            if (pageItem.RowCount == PAGE_SIZE_MAX) //Max size-->Split
            {
                pagePos = SplitPage(columnIndex, pagePos);
                if (columnIndex.Pages[pagePos - 1].RowCount > pos)
                {
                    pagePos--;
                }
                else
                {
                    pos -= columnIndex.Pages[pagePos - 1].RowCount;
                }

                pageItem = columnIndex.Pages[pagePos];
            }
            else //Expand to double size.
            {
                var rowsTmp = new IndexItem[pageItem.Rows.Length << 1];
                Array.Copy(pageItem.Rows, 0, rowsTmp, 0, pageItem.RowCount);
                pageItem.Rows = rowsTmp;
            }
        }

        if (pos < pageItem.RowCount)
        {
            Array.Copy(pageItem.Rows, pos, pageItem.Rows, pos + 1, pageItem.RowCount - pos);
        }

        pageItem.Rows[pos] = new IndexItem { Index = ix, IndexPointer = _values.Count };
        _values.Add(value);
        pageItem.RowCount++;
    }

    private int SplitPage(ColumnIndex columnIndex, int pagePos)
    {
        PageIndex page = columnIndex.Pages[pagePos];
        if (page.Offset != 0)
        {
            int offset = page.Offset;
            page.Offset = 0;
            for (int r = 0; r < page.RowCount; r++)
            {
                page.Rows[r].Index -= (short)offset;
            }
        }

        //Find Split pos
        int splitPos = 0;
        for (int r = 0; r < page.RowCount; r++)
        {
            if (page.Rows[r].Index > PAGE_SIZE)
            {
                splitPos = r;
                break;
            }
        }

        var newPage = new PageIndex(page, 0, splitPos);
        var nextPage = new PageIndex(page, splitPos, page.RowCount - splitPos, (short)(page.Index + 1), page.Offset);

        for (int r = 0; r < nextPage.RowCount; r++)
        {
            nextPage.Rows[r].Index = (short)(nextPage.Rows[r].Index - PAGE_SIZE);
        }

        columnIndex.Pages[pagePos] = newPage;
        if (columnIndex.PageCount + 1 > columnIndex.Pages.Length)
        {
            var pageTmp = new PageIndex[columnIndex.Pages.Length << 1];
            Array.Copy(columnIndex.Pages, 0, pageTmp, 0, columnIndex.PageCount);
            columnIndex.Pages = pageTmp;
        }

        Array.Copy(columnIndex.Pages, pagePos + 1, columnIndex.Pages, pagePos + 2, columnIndex.PageCount - pagePos - 1);
        columnIndex.Pages[pagePos + 1] = nextPage;
        page = nextPage;
        //pos -= PageSize;
        columnIndex.PageCount++;
        return pagePos + 1;
    }

    private PageIndex AdjustIndex(ColumnIndex columnIndex, int pagePos)
    {
        PageIndex page = columnIndex.Pages[pagePos];
        //First Adjust indexes
        if (page.Offset + page.Rows[0].Index >= PAGE_SIZE ||
            page.Offset >= PAGE_SIZE ||
            page.Rows[0].Index >= PAGE_SIZE)
        {
            page.Index++;
            page.Offset -= PAGE_SIZE;
        }
        else if (page.Offset + page.Rows[0].Index <= -PAGE_SIZE ||
                 page.Offset <= -PAGE_SIZE ||
                 page.Rows[0].Index <= -PAGE_SIZE)
        {
            page.Index--;
            page.Offset += PAGE_SIZE;
        }

        //else if (page.Rows[0].Index >= PageSize) //Delete
        //{
        //    page.Index++;
        //    AddPageRowOffset(page, -PageSize);
        //}
        //else if (page.Rows[0].Index <= -PageSize)   //Delete
        //{
        //    page.Index--;
        //    AddPageRowOffset(page, PageSize);
        //}
        return page;
    }

    private void AddPageRowOffset(PageIndex page, short offset)
    {
        for (int r = 0; r < page.RowCount; r++)
        {
            page.Rows[r].Index += offset;
        }
    }

    private void AddPage(ColumnIndex column, int pos, short index)
    {
        AddPage(column, pos);
        column.Pages[pos] = new PageIndex { Index = index };
        if (pos > 0)
        {
            PageIndex pp = column.Pages[pos - 1];
            if (pp.RowCount > 0 && pp.Rows[pp.RowCount - 1].Index > PAGE_SIZE)
            {
                column.Pages[pos].Offset = pp.Rows[pp.RowCount - 1].Index - PAGE_SIZE;
            }
        }
    }

    /// <summary>
    /// Add a new page to the collection
    /// </summary>
    /// <param name="column">The column</param>
    /// <param name="pos">Position</param>
    /// <param name="page">The new page object to add</param>
    private void AddPage(ColumnIndex column, int pos, PageIndex page)
    {
        AddPage(column, pos);
        column.Pages[pos] = page;
    }

    /// <summary>
    /// Add a new page to the collection
    /// </summary>
    /// <param name="column">The column</param>
    /// <param name="pos">Position</param>
    private void AddPage(ColumnIndex column, int pos)
    {
        if (column.PageCount == column.Pages.Length)
        {
            var pageTmp = new PageIndex[column.Pages.Length * 2];
            Array.Copy(column.Pages, 0, pageTmp, 0, column.PageCount);
            column.Pages = pageTmp;
        }

        if (pos < column.PageCount)
        {
            Array.Copy(column.Pages, pos, column.Pages, pos + 1, column.PageCount - pos);
        }

        column.PageCount++;
    }

    private void AddColumn(int pos, int column)
    {
        if (ColumnCount == ColumnIndex.Length)
        {
            var colTmp = new ColumnIndex[ColumnIndex.Length * 2];
            Array.Copy(ColumnIndex, 0, colTmp, 0, ColumnCount);
            ColumnIndex = colTmp;
        }

        if (pos < ColumnCount)
        {
            Array.Copy(ColumnIndex, pos, ColumnIndex, pos + 1, ColumnCount - pos);
        }

        ColumnIndex[pos] = new ColumnIndex { Index = (short)column };
        ColumnCount++;
    }

    //object IEnumerator.Current
    //{
    //    get 
    //    {
    //        return GetValue(_row+1, _columnIndex[_colPos].Index);
    //    }
    //}
    public bool MoveNext()
    {
        return GetNextCell(ref _row, ref _colPos, 0, ExcelPackage.MaxRows, ExcelPackage.MaxColumns);
    }

    internal bool NextCell(ref int row, ref int col)
    {
        return NextCell(ref row, ref col, 0, 0, ExcelPackage.MaxRows, ExcelPackage.MaxColumns);
    }

    internal bool NextCell(ref int row, ref int col, int minRow, int minColPos, int maxRow, int maxColPos)
    {
        if (minColPos >= ColumnCount)
        {
            return false;
        }

        if (maxColPos >= ColumnCount)
        {
            maxColPos = ColumnCount - 1;
        }

        int c = GetPosition(col);
        if (c >= 0)
        {
            if (c > maxColPos)
            {
                if (col <= minColPos)
                {
                    return false;
                }

                col = minColPos;
                return NextCell(ref row, ref col);
            }

            bool r = GetNextCell(ref row, ref c, minColPos, maxRow, maxColPos);
            col = ColumnIndex[c].Index;
            return r;
        }

        c = ~c;
        if (c >= ColumnCount) c = ColumnCount - 1;
        if (col > ColumnIndex[c].Index)
        {
            if (col <= minColPos)
            {
                return false;
            }

            col = minColPos;
            return NextCell(ref row, ref col, minRow, minColPos, maxRow, maxColPos);
        }

        {
            bool r = GetNextCell(ref row, ref c, minColPos, maxRow, maxColPos);
            col = ColumnIndex[c].Index;
            return r;
        }
    }

    internal bool GetNextCell(ref int row, ref int colPos, int startColPos, int endRow, int endColPos)
    {
        if (ColumnCount == 0)
        {
            return false;
        }

        if (++colPos < ColumnCount && colPos <= endColPos)
        {
            int r = ColumnIndex[colPos].GetNextRow(row);
            if (r == row) //Exists next Row
            {
                return true;
            }

            int minRow, minCol;
            if (r > row)
            {
                minRow = r;
                minCol = colPos;
            }
            else
            {
                minRow = int.MaxValue;
                minCol = 0;
            }

            int c = colPos + 1;
            while (c < ColumnCount && c <= endColPos)
            {
                r = ColumnIndex[c].GetNextRow(row);
                if (r == row) //Exists next Row
                {
                    colPos = c;
                    return true;
                }

                if (r > row && r < minRow)
                {
                    minRow = r;
                    minCol = c;
                }

                c++;
            }

            c = startColPos;
            if (row < endRow)
            {
                row++;
                while (c < colPos)
                {
                    r = ColumnIndex[c].GetNextRow(row);
                    if (r == row) //Exists next Row
                    {
                        colPos = c;
                        return true;
                    }

                    if (r > row && (r < minRow || (r == minRow && c < minCol)) && r <= endRow)
                    {
                        minRow = r;
                        minCol = c;
                    }

                    c++;
                }
            }

            if (minRow == int.MaxValue || minRow > endRow)
            {
                return false;
            }

            row = minRow;
            colPos = minCol;
            return true;
        }

        if (colPos <= startColPos || row >= endRow)
        {
            return false;
        }

        colPos = startColPos - 1;
        row++;
        return GetNextCell(ref row, ref colPos, startColPos, endRow, endColPos);
    }

    internal bool GetNextCell(ref int row, ref int colPos, int startColPos, int endRow, int endColPos, ref int[] pagePos, ref int[] cellPos)
    {
        if (colPos == endColPos)
        {
            colPos = startColPos;
            row++;
        }
        else
        {
            colPos++;
        }

        if (pagePos[colPos] < 0)
        {
            if (pagePos[colPos] == -1)
            {
                pagePos[colPos] = ColumnIndex[colPos].GetPosition(row);
            }
        }
        else if (ColumnIndex[colPos].Pages[pagePos[colPos]].RowCount <= row)
        {
            if (ColumnIndex[colPos].PageCount > pagePos[colPos])
                pagePos[colPos]++;
            else
            {
                pagePos[colPos] = -2;
            }
        }

        int r = ColumnIndex[colPos].Pages[pagePos[colPos]].IndexOffset + ColumnIndex[colPos].Pages[pagePos[colPos]].Rows[cellPos[colPos]].Index;
        if (r == row)
        {
            row = r;
        }

        return true;
    }

    internal bool PrevCell(ref int row, ref int col)
    {
        return PrevCell(ref row, ref col, 0, 0, ExcelPackage.MaxRows, ExcelPackage.MaxColumns);
    }

    internal bool PrevCell(ref int row, ref int col, int minRow, int minColPos, int maxRow, int maxColPos)
    {
        if (minColPos >= ColumnCount)
        {
            return false;
        }

        if (maxColPos >= ColumnCount)
        {
            maxColPos = ColumnCount - 1;
        }

        int c = GetPosition(col);
        if (c >= 0)
        {
            if (c == 0)
            {
                if (col >= maxColPos)
                {
                    return false;
                }

                if (row == minRow)
                {
                    return false;
                }

                row--;
                col = maxColPos;
                return PrevCell(ref row, ref col, minRow, minColPos, maxRow, maxColPos);
            }

            bool ret = GetPrevCell(ref row, ref c, minRow, minColPos, maxColPos);
            if (ret)
            {
                col = ColumnIndex[c].Index;
            }

            return ret;
        }

        c = ~c;
        if (c == 0)
        {
            if (col >= maxColPos || row <= 0)
            {
                return false;
            }

            col = maxColPos;
            row--;
            return PrevCell(ref row, ref col, minRow, minColPos, maxRow, maxColPos);
        }

        {
            bool ret = GetPrevCell(ref row, ref c, minRow, minColPos, maxColPos);
            if (ret)
            {
                col = ColumnIndex[c].Index;
            }

            return ret;
        }
    }

    internal bool GetPrevCell(ref int row, ref int colPos, int startRow, int startColPos, int endColPos)
    {
        if (ColumnCount == 0)
        {
            return false;
        }

        if (--colPos >= startColPos)
//                if (++colPos < ColumnCount && colPos <= endColPos)
        {
            int r = ColumnIndex[colPos].GetNextRow(row);
            if (r == row) //Exists next Row
            {
                return true;
            }

            int minRow, minCol;
            if (r > row && r >= startRow)
            {
                minRow = r;
                minCol = colPos;
            }
            else
            {
                minRow = int.MaxValue;
                minCol = 0;
            }

            int c = colPos - 1;
            if (c >= startColPos)
            {
                while (c >= startColPos)
                {
                    r = ColumnIndex[c].GetNextRow(row);
                    if (r == row) //Exists next Row
                    {
                        colPos = c;
                        return true;
                    }

                    if (r > row && r < minRow && r >= startRow)
                    {
                        minRow = r;
                        minCol = c;
                    }

                    c--;
                }
            }

            if (row > startRow)
            {
                c = endColPos;
                row--;
                while (c > colPos)
                {
                    r = ColumnIndex[c].GetNextRow(row);
                    if (r == row) //Exists next Row
                    {
                        colPos = c;
                        return true;
                    }

                    if (r > row && r < minRow && r >= startRow)
                    {
                        minRow = r;
                        minCol = c;
                    }

                    c--;
                }
            }

            if (minRow == int.MaxValue || startRow < minRow)
            {
                return false;
            }

            row = minRow;
            colPos = minCol;
            return true;
        }

        colPos = ColumnCount;
        row--;
        if (row < startRow)
        {
            Reset();
            return false;
        }

        return GetPrevCell(ref colPos, ref row, startRow, startColPos, endColPos);
    }

    public void Reset()
    {
        _colPos = -1;
        _row = 0;
    }

    internal delegate void SetRangeValueDelegate(List<T> list, int index, int row, int column, object value);

    internal delegate void SetValueDelegate(List<T> list, int index, object value);
    //public IEnumerator<ulong> GetEnumerator()
    //{
    //    this.Reset();
    //    return this;
    //}

    //IEnumerator IEnumerable.GetEnumerator()
    //{
    //    this.Reset();
    //    return this;
    //}
}

internal class CellsStoreEnumerator<T> : IEnumerable<T>, IEnumerator<T>
{
    readonly CellStore<T> _cellStore;
    readonly int _startRow;
    readonly int _startCol;
    readonly int _endRow;
    readonly int _endCol;
    int _minRow, _minColPos, _maxRow, _maxColPos;
    int[] _pagePos, _cellPos;
    int _row, _colPos;

    public CellsStoreEnumerator(CellStore<T> cellStore) :
        this(cellStore, 0, 0, ExcelPackage.MaxRows, ExcelPackage.MaxColumns)
    {
    }

    public CellsStoreEnumerator(CellStore<T> cellStore, int startRow, int startCol, int endRow, int endCol)
    {
        _cellStore = cellStore;

        _startRow = startRow;
        _startCol = startCol;
        _endRow = endRow;
        _endCol = endCol;

        Init();
    }

    internal int Row => _row;

    internal int Column
    {
        get
        {
            if (_colPos == -1) MoveNext();
            if (_colPos == -1) return 0;
            return _cellStore.ColumnIndex[_colPos].Index;
        }
    }

    internal T Value
    {
        get
        {
            lock (_cellStore)
            {
                return _cellStore.GetValue(_row, Column);
            }
        }
        set
        {
            lock (_cellStore)
            {
                _cellStore.SetValue(_row, Column, value);
            }
        }
    }

    public string CellAddress => ExcelCellBase.GetAddress(Row, Column);

    public IEnumerator<T> GetEnumerator()
    {
        Reset();
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        Reset();
        return this;
    }

    public T Current => Value;

    public void Dispose()
    {
        //_cellStore=null;
    }

    object IEnumerator.Current
    {
        get
        {
            Reset();
            return this;
        }
    }

    public bool MoveNext()
    {
        return Next();
    }

    public void Reset()
    {
        Init();
    }

    internal void Init()
    {
        _minRow = _startRow;
        _maxRow = _endRow;

        _minColPos = _cellStore.GetPosition(_startCol);
        if (_minColPos < 0) _minColPos = ~_minColPos;
        _maxColPos = _cellStore.GetPosition(_endCol);
        if (_maxColPos < 0) _maxColPos = ~_maxColPos - 1;
        _row = _minRow;
        _colPos = _minColPos - 1;

        int cols = _maxColPos - _minColPos + 1;
        _pagePos = new int[cols];
        _cellPos = new int[cols];
        for (int i = 0; i < cols; i++)
        {
            _pagePos[i] = -1;
            _cellPos[i] = -1;
        }
    }

    internal bool Next()
    {
        //return _cellStore.GetNextCell(ref row, ref colPos, minColPos, maxRow, maxColPos);
        return _cellStore.GetNextCell(ref _row, ref _colPos, _minColPos, _maxRow, _maxColPos);
    }

    internal bool Previous()
    {
        lock (_cellStore)
        {
            return _cellStore.GetPrevCell(ref _row, ref _colPos, _minRow, _minColPos, _maxColPos);
        }
    }
}

internal class FlagCellStore : CellStore<byte>
{
    internal void SetFlagValue(int row, int col, bool value, CellFlags cellFlags)
    {
        var currentValue = (CellFlags)GetValue(row, col);
        if (value)
        {
            SetValue(row, col, (byte)(currentValue | cellFlags)); // add the CellFlag bit
        }
        else
        {
            SetValue(row, col, (byte)(currentValue & ~cellFlags)); // remove the CellFlag bit
        }
    }

    internal bool GetFlagValue(int row, int col, CellFlags cellFlags)
    {
        return ((byte)cellFlags & GetValue(row, col)) != 0;
    }
}