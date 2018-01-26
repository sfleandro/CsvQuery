﻿// NPP plugin platform for .Net v0.93.96 by Kasper B. Graversen etc.
using System;
using System.Text;
using System.Threading.Tasks;
using CsvQuery.Tools;

namespace CsvQuery.PluginInfrastructure
{
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// This it the plugin-writers primary interface to Notepad++/Scintilla.
    /// It takes away all the complexity with command numbers and Int-pointer casting.
    ///
    /// See http://www.scintilla.org/ScintillaDoc.html for further details.
    /// </summary>
    public class ScintillaGateway : IScintillaGateway
    {
        private const int Unused = 0;

        private readonly IntPtr scintilla;

        public static readonly int LengthZeroTerminator = "\0".Length;


        public ScintillaGateway(IntPtr scintilla)
        {
            this.scintilla = scintilla;
        }

        public int GetSelectionLength()
        {
            var selectionLength = (int) Win32.SendMessage(scintilla, SciMsg.SCI_GETSELTEXT, Unused, Unused) - LengthZeroTerminator;
            return selectionLength;
        }

        public void AppendTextAndMoveCursor(string text)
        {
            AppendText(text.Length, text);
            GotoPos(new Position(GetCurrentPos().Value + text.Length));
        }

        public void InsertTextAndMoveCursor(string text)
        {
            var currentPos = GetCurrentPos();
            InsertText(currentPos, text);
            GotoPos(new Position(currentPos.Value + text.Length));
        }

        public void SelectCurrentLine()
        {
            int line = GetCurrentLineNumber();
            SetSelection(PositionFromLine(line).Value, PositionFromLine(line + 1).Value);
        }

        /// <summary>
        /// clears the selection without changing the position of the cursor
        /// </summary>
        public void ClearSelectionToCursor()
        {
            var pos = GetCurrentPos().Value;
            SetSelection(pos, pos);
        }

        /// <summary>
        /// Get the current line from the current position
        /// </summary>
        public int GetCurrentLineNumber()
        {
            return LineFromPosition(GetCurrentPos()); 
        }

        public string GetTextRange(int start, int end)
        {
            var codepage = GetCodePage();
            using (var tr = new TextRange(start, end))
            {
                GetTextRange(tr);
                if (codepage == (int) SciMsg.SC_CP_UTF8)
                    return tr.GetFromUtf8();
                return tr.lpstrText;
            }
        }

        public string GetAllText()
        {
            var length = GetLength();
            // return GetTextRange(0, length);

            var chunkSize = 1000000;
            var sb = new StringBuilder();
            for (int pos = 0; pos < length; pos += Math.Min(chunkSize, length - pos))
            {
                sb.Append( GetTextRange(pos, Math.Min(pos+chunkSize,length)));
            }

            return sb.ToString();
        }

        protected string DecodeStringresult(byte[] textBuffer)
        {
            var codePage = GetCodePage();
            var encoding = codePage == (int) SciMsg.SC_CP_UTF8 ? Encoding.UTF8 : Encoding.Default;
            return encoding.GetString(textBuffer).TrimEnd('\0');
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string FastAllocateString(int length);

        public unsafe void AddText(Stream utf8TextStream)
        {
            byte[] buffer = new byte[1024 * 8 * 2];
            int blockCount = 0, totalRead = 0;
            var startTime = Stopwatch.GetTimestamp();
            var beganRead = startTime;
            long readWait = 0, sendWait = 0, beganSend;
            try
            {
                int read;
                while ((read = utf8TextStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    beganSend = Stopwatch.GetTimestamp();
                    readWait += beganSend - beganRead;
                    fixed (byte* textPtr = buffer)
                    {
                        //Trace.TraceInformation("Scintilla.AddText block read. Size=" + read);
                        IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ADDTEXT, read, (IntPtr) textPtr);
                        //Trace.TraceInformation("Scintilla.AddText SCI_ADDTEXT sent. return=" + res);
                    }
                    beganRead = Stopwatch.GetTimestamp();
                    sendWait += beganRead - beganSend;
                    blockCount++;
                    totalRead += read;
                }
                Trace.TraceInformation(
                    $"Scintilla.AddText leaving. Blocks={blockCount}, bytes={totalRead}, avg={(blockCount == 0 ? 0 : totalRead / (double) blockCount):#.#}");
                Trace.TraceInformation($"Scintilla.AddText WAITS: read={readWait}, send={sendWait}");
            }
            catch (Exception e)
            {
                Trace.TraceInformation("Scintilla.AddText Exception: " + e.Message);
                throw;
            }
        }

        /* ++Autogenerated -- start of section automatically generated from Scintilla.iface */
        /// <summary>Add text to the document at current position. (Scintilla feature 2001)</summary>
        public unsafe void AddText(int length, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ADDTEXT, length, (IntPtr) textPtr);
            }
        }

        /// <summary>Add array of cells to document. (Scintilla feature 2002)</summary>
        public unsafe void AddStyledText(int length, Cells c)
        {
            fixed (char* cPtr = c.Value)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ADDSTYLEDTEXT, length, (IntPtr) cPtr);
            }
        }

        /// <summary>Insert string at a position. (Scintilla feature 2003)</summary>
        public unsafe void InsertText(Position pos, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INSERTTEXT, pos.Value, (IntPtr) textPtr);
            }
        }

        /// <summary>Change the text that is being inserted in response to SC_MOD_INSERTCHECK (Scintilla feature 2672)</summary>
        public unsafe void ChangeInsertion(int length, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHANGEINSERTION, length, (IntPtr) textPtr);
            }
        }

        /// <summary>Delete all text in the document. (Scintilla feature 2004)</summary>
        public void ClearAll()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CLEARALL, Unused, Unused);
        }

        /// <summary>Delete a range of text in the document. (Scintilla feature 2645)</summary>
        public void DeleteRange(Position pos, int deleteLength)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DELETERANGE, pos.Value, deleteLength);
        }

        /// <summary>Set all style bytes to 0, remove all folding information. (Scintilla feature 2005)</summary>
        public void ClearDocumentStyle()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CLEARDOCUMENTSTYLE, Unused, Unused);
        }

        /// <summary>Returns the number of bytes in the document. (Scintilla feature 2006)</summary>
        public int GetLength()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLENGTH, Unused, Unused);
            return (int) res;
        }

        /// <summary>Returns the character byte at the position. (Scintilla feature 2007)</summary>
        public int GetCharAt(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCHARAT, pos.Value, Unused);
            return (int) res;
        }

        /// <summary>Returns the position of the caret. (Scintilla feature 2008)</summary>
        public Position GetCurrentPos()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCURRENTPOS, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Returns the position of the opposite end of the selection to the caret. (Scintilla feature 2009)</summary>
        public Position GetAnchor()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETANCHOR, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Returns the style byte at the position. (Scintilla feature 2010)</summary>
        public int GetStyleAt(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSTYLEAT, pos.Value, Unused);
            return (int) res;
        }

        /// <summary>Redoes the next action on the undo history. (Scintilla feature 2011)</summary>
        public void Redo()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_REDO, Unused, Unused);
        }

        /// <summary>
        /// Choose between collecting actions into the undo
        /// history and discarding them.
        /// (Scintilla feature 2012)
        /// </summary>
        public void SetUndoCollection(bool collectUndo)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETUNDOCOLLECTION, collectUndo ? 1 : 0, Unused);
        }

        /// <summary>Select all the text in the document. (Scintilla feature 2013)</summary>
        public void SelectAll()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SELECTALL, Unused, Unused);
        }

        /// <summary>
        /// Remember the current position in the undo history as the position
        /// at which the document was saved.
        /// (Scintilla feature 2014)
        /// </summary>
        public void SetSavePoint()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSAVEPOINT, Unused, Unused);
        }

        /// <summary>
        /// Retrieve a buffer of cells.
        /// Returns the number of bytes in the buffer not including terminating NULs.
        /// (Scintilla feature 2015)
        /// </summary>
        public int GetStyledText(TextRange tr)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSTYLEDTEXT, Unused, tr.NativePointer);
            return (int) res;
        }

        /// <summary>Are there any redoable actions in the undo history? (Scintilla feature 2016)</summary>
        public bool CanRedo()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CANREDO, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Retrieve the line number at which a particular marker is located. (Scintilla feature 2017)</summary>
        public int MarkerLineFromHandle(int handle)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERLINEFROMHANDLE, handle, Unused);
            return (int) res;
        }

        /// <summary>Delete a marker. (Scintilla feature 2018)</summary>
        public void MarkerDeleteHandle(int handle)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERDELETEHANDLE, handle, Unused);
        }

        /// <summary>Is undo history being collected? (Scintilla feature 2019)</summary>
        public bool GetUndoCollection()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETUNDOCOLLECTION, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>
        /// Are white space characters currently visible?
        /// Returns one of SCWS_* constants.
        /// (Scintilla feature 2020)
        /// </summary>
        public int GetViewWS()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETVIEWWS, Unused, Unused);
            return (int) res;
        }

        /// <summary>Make white space characters invisible, always visible or visible outside indentation. (Scintilla feature 2021)</summary>
        public void SetViewWS(int viewWS)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETVIEWWS, viewWS, Unused);
        }

        /// <summary>Find the position from a point within the window. (Scintilla feature 2022)</summary>
        public Position PositionFromPoint(int x, int y)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_POSITIONFROMPOINT, x, y);
            return new Position((int) res);
        }

        /// <summary>
        /// Find the position from a point within the window but return
        /// INVALID_POSITION if not close to text.
        /// (Scintilla feature 2023)
        /// </summary>
        public Position PositionFromPointClose(int x, int y)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_POSITIONFROMPOINTCLOSE, x, y);
            return new Position((int) res);
        }

        /// <summary>Set caret to start of a line and ensure it is visible. (Scintilla feature 2024)</summary>
        public void GotoLine(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GOTOLINE, line, Unused);
        }

        /// <summary>Set caret to a position and ensure it is visible. (Scintilla feature 2025)</summary>
        public void GotoPos(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GOTOPOS, pos.Value, Unused);
        }

        /// <summary>
        /// Set the selection anchor to a position. The anchor is the opposite
        /// end of the selection from the caret.
        /// (Scintilla feature 2026)
        /// </summary>
        public void SetAnchor(Position posAnchor)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETANCHOR, posAnchor.Value, Unused);
        }

        /// <summary>
        /// Retrieve the text of the line containing the caret.
        /// Returns the index of the caret on the line.
        /// Result is NUL-terminated.
        /// (Scintilla feature 2027)
        /// </summary>
        public unsafe string GetCurLine(int length)
        {
            var givenLength = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETCURLINE, 0, IntPtr.Zero);
            byte[] textBuffer = new byte[givenLength];
            fixed (byte* textPtr = textBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCURLINE, givenLength, (IntPtr) textPtr);
                return DecodeStringresult(textBuffer);
            }
        }

        /// <summary>Retrieve the position of the last correctly styled character. (Scintilla feature 2028)</summary>
        public Position GetEndStyled()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETENDSTYLED, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Convert all line endings in the document to one mode. (Scintilla feature 2029)</summary>
        public void ConvertEOLs(int eolMode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CONVERTEOLS, eolMode, Unused);
        }

        /// <summary>Retrieve the current end of line mode - one of CRLF, CR, or LF. (Scintilla feature 2030)</summary>
        public int GetEOLMode()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETEOLMODE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the current end of line mode. (Scintilla feature 2031)</summary>
        public void SetEOLMode(int eolMode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETEOLMODE, eolMode, Unused);
        }

        /// <summary>
        /// Set the current styling position to pos and the styling mask to mask.
        /// The styling mask can be used to protect some bits in each styling byte from modification.
        /// (Scintilla feature 2032)
        /// </summary>
        public void StartStyling(Position pos, int mask)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STARTSTYLING, pos.Value, mask);
        }

        /// <summary>
        /// Change style from current styling position for length characters to a style
        /// and move the current styling position to after this newly styled segment.
        /// (Scintilla feature 2033)
        /// </summary>
        public void SetStyling(int length, int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSTYLING, length, style);
        }

        /// <summary>Is drawing done first into a buffer or direct to the screen? (Scintilla feature 2034)</summary>
        public bool GetBufferedDraw()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETBUFFEREDDRAW, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>
        /// If drawing is buffered then each line of text is drawn into a bitmap buffer
        /// before drawing it to the screen to avoid flicker.
        /// (Scintilla feature 2035)
        /// </summary>
        public void SetBufferedDraw(bool buffered)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETBUFFEREDDRAW, buffered ? 1 : 0, Unused);
        }

        /// <summary>Change the visible size of a tab to be a multiple of the width of a space character. (Scintilla feature 2036)</summary>
        public void SetTabWidth(int tabWidth)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETTABWIDTH, tabWidth, Unused);
        }

        /// <summary>Retrieve the visible size of a tab. (Scintilla feature 2121)</summary>
        public int GetTabWidth()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTABWIDTH, Unused, Unused);
            return (int) res;
        }

        /// <summary>Clear explicit tabstops on a line. (Scintilla feature 2675)</summary>
        public void ClearTabStops(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CLEARTABSTOPS, line, Unused);
        }

        /// <summary>Add an explicit tab stop for a line. (Scintilla feature 2676)</summary>
        public void AddTabStop(int line, int x)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ADDTABSTOP, line, x);
        }

        /// <summary>Find the next explicit tab stop position on a line after a position. (Scintilla feature 2677)</summary>
        public int GetNextTabStop(int line, int x)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETNEXTTABSTOP, line, x);
            return (int) res;
        }

        /// <summary>
        /// Set the code page used to interpret the bytes of the document as characters.
        /// The SC_CP_UTF8 value can be used to enter Unicode mode.
        /// (Scintilla feature 2037)
        /// </summary>
        public void SetCodePage(int codePage)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCODEPAGE, codePage, Unused);
        }

        /// <summary>Is the IME displayed in a winow or inline? (Scintilla feature 2678)</summary>
        public int GetIMEInteraction()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETIMEINTERACTION, Unused, Unused);
            return (int) res;
        }

        /// <summary>Choose to display the the IME in a winow or inline. (Scintilla feature 2679)</summary>
        public void SetIMEInteraction(int imeInteraction)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETIMEINTERACTION, imeInteraction, Unused);
        }

        /// <summary>Set the symbol used for a particular marker number. (Scintilla feature 2040)</summary>
        public void MarkerDefine(int markerNumber, int markerSymbol)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERDEFINE, markerNumber, markerSymbol);
        }

        /// <summary>Set the foreground colour used for a particular marker number. (Scintilla feature 2041)</summary>
        public void MarkerSetFore(int markerNumber, Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERSETFORE, markerNumber, fore.Value);
        }

        /// <summary>Set the background colour used for a particular marker number. (Scintilla feature 2042)</summary>
        public void MarkerSetBack(int markerNumber, Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERSETBACK, markerNumber, back.Value);
        }

        /// <summary>Set the background colour used for a particular marker number when its folding block is selected. (Scintilla feature 2292)</summary>
        public void MarkerSetBackSelected(int markerNumber, Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERSETBACKSELECTED, markerNumber, back.Value);
        }

        /// <summary>Enable/disable highlight for current folding bloc (smallest one that contains the caret) (Scintilla feature 2293)</summary>
        public void MarkerEnableHighlight(bool enabled)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERENABLEHIGHLIGHT, enabled ? 1 : 0, Unused);
        }

        /// <summary>Add a marker to a line, returning an ID which can be used to find or delete the marker. (Scintilla feature 2043)</summary>
        public int MarkerAdd(int line, int markerNumber)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERADD, line, markerNumber);
            return (int) res;
        }

        /// <summary>Delete a marker from a line. (Scintilla feature 2044)</summary>
        public void MarkerDelete(int line, int markerNumber)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERDELETE, line, markerNumber);
        }

        /// <summary>Delete all markers with a particular number from all lines. (Scintilla feature 2045)</summary>
        public void MarkerDeleteAll(int markerNumber)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERDELETEALL, markerNumber, Unused);
        }

        /// <summary>Get a bit mask of all the markers set on a line. (Scintilla feature 2046)</summary>
        public int MarkerGet(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERGET, line, Unused);
            return (int) res;
        }

        /// <summary>
        /// Find the next line at or after lineStart that includes a marker in mask.
        /// Return -1 when no more lines.
        /// (Scintilla feature 2047)
        /// </summary>
        public int MarkerNext(int lineStart, int markerMask)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERNEXT, lineStart, markerMask);
            return (int) res;
        }

        /// <summary>Find the previous line before lineStart that includes a marker in mask. (Scintilla feature 2048)</summary>
        public int MarkerPrevious(int lineStart, int markerMask)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERPREVIOUS, lineStart, markerMask);
            return (int) res;
        }

        /// <summary>Define a marker from a pixmap. (Scintilla feature 2049)</summary>
        public unsafe void MarkerDefinePixmap(int markerNumber, string pixmap)
        {
            fixed (byte* pixmapPtr = Encoding.UTF8.GetBytes(pixmap))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERDEFINEPIXMAP, markerNumber, (IntPtr) pixmapPtr);
            }
        }

        /// <summary>Add a set of markers to a line. (Scintilla feature 2466)</summary>
        public void MarkerAddSet(int line, int set)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERADDSET, line, set);
        }

        /// <summary>Set the alpha used for a marker that is drawn in the text area, not the margin. (Scintilla feature 2476)</summary>
        public void MarkerSetAlpha(int markerNumber, int alpha)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERSETALPHA, markerNumber, alpha);
        }

        /// <summary>Set a margin to be either numeric or symbolic. (Scintilla feature 2240)</summary>
        public void SetMarginTypeN(int margin, int marginType)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMARGINTYPEN, margin, marginType);
        }

        /// <summary>Retrieve the type of a margin. (Scintilla feature 2241)</summary>
        public int GetMarginTypeN(int margin)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMARGINTYPEN, margin, Unused);
            return (int) res;
        }

        /// <summary>Set the width of a margin to a width expressed in pixels. (Scintilla feature 2242)</summary>
        public void SetMarginWidthN(int margin, int pixelWidth)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMARGINWIDTHN, margin, pixelWidth);
        }

        /// <summary>Retrieve the width of a margin in pixels. (Scintilla feature 2243)</summary>
        public int GetMarginWidthN(int margin)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMARGINWIDTHN, margin, Unused);
            return (int) res;
        }

        /// <summary>Set a mask that determines which markers are displayed in a margin. (Scintilla feature 2244)</summary>
        public void SetMarginMaskN(int margin, int mask)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMARGINMASKN, margin, mask);
        }

        /// <summary>Retrieve the marker mask of a margin. (Scintilla feature 2245)</summary>
        public int GetMarginMaskN(int margin)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMARGINMASKN, margin, Unused);
            return (int) res;
        }

        /// <summary>Make a margin sensitive or insensitive to mouse clicks. (Scintilla feature 2246)</summary>
        public void SetMarginSensitiveN(int margin, bool sensitive)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMARGINSENSITIVEN, margin, sensitive ? 1 : 0);
        }

        /// <summary>Retrieve the mouse click sensitivity of a margin. (Scintilla feature 2247)</summary>
        public bool GetMarginSensitiveN(int margin)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMARGINSENSITIVEN, margin, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set the cursor shown when the mouse is inside a margin. (Scintilla feature 2248)</summary>
        public void SetMarginCursorN(int margin, int cursor)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMARGINCURSORN, margin, cursor);
        }

        /// <summary>Retrieve the cursor shown in a margin. (Scintilla feature 2249)</summary>
        public int GetMarginCursorN(int margin)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMARGINCURSORN, margin, Unused);
            return (int) res;
        }

        /// <summary>Clear all the styles and make equivalent to the global default style. (Scintilla feature 2050)</summary>
        public void StyleClearAll()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLECLEARALL, Unused, Unused);
        }

        /// <summary>Set the foreground colour of a style. (Scintilla feature 2051)</summary>
        public void StyleSetFore(int style, Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETFORE, style, fore.Value);
        }

        /// <summary>Set the background colour of a style. (Scintilla feature 2052)</summary>
        public void StyleSetBack(int style, Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETBACK, style, back.Value);
        }

        /// <summary>Set a style to be bold or not. (Scintilla feature 2053)</summary>
        public void StyleSetBold(int style, bool bold)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETBOLD, style, bold ? 1 : 0);
        }

        /// <summary>Set a style to be italic or not. (Scintilla feature 2054)</summary>
        public void StyleSetItalic(int style, bool italic)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETITALIC, style, italic ? 1 : 0);
        }

        /// <summary>Set the size of characters of a style. (Scintilla feature 2055)</summary>
        public void StyleSetSize(int style, int sizePoints)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETSIZE, style, sizePoints);
        }

        /// <summary>Set the font of a style. (Scintilla feature 2056)</summary>
        public unsafe void StyleSetFont(int style, string fontName)
        {
            fixed (byte* fontNamePtr = Encoding.UTF8.GetBytes(fontName))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETFONT, style, (IntPtr) fontNamePtr);
            }
        }

        /// <summary>Set a style to have its end of line filled or not. (Scintilla feature 2057)</summary>
        public void StyleSetEOLFilled(int style, bool filled)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETEOLFILLED, style, filled ? 1 : 0);
        }

        /// <summary>Reset the default style to its state at startup (Scintilla feature 2058)</summary>
        public void StyleResetDefault()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLERESETDEFAULT, Unused, Unused);
        }

        /// <summary>Set a style to be underlined or not. (Scintilla feature 2059)</summary>
        public void StyleSetUnderline(int style, bool underline)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETUNDERLINE, style, underline ? 1 : 0);
        }

        /// <summary>Get the foreground colour of a style. (Scintilla feature 2481)</summary>
        public Colour StyleGetFore(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETFORE, style, Unused);
            return new Colour((int) res);
        }

        /// <summary>Get the background colour of a style. (Scintilla feature 2482)</summary>
        public Colour StyleGetBack(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETBACK, style, Unused);
            return new Colour((int) res);
        }

        /// <summary>Get is a style bold or not. (Scintilla feature 2483)</summary>
        public bool StyleGetBold(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETBOLD, style, Unused);
            return 1 == (int) res;
        }

        /// <summary>Get is a style italic or not. (Scintilla feature 2484)</summary>
        public bool StyleGetItalic(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETITALIC, style, Unused);
            return 1 == (int) res;
        }

        /// <summary>Get the size of characters of a style. (Scintilla feature 2485)</summary>
        public int StyleGetSize(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETSIZE, style, Unused);
            return (int) res;
        }

        /// <summary>
        /// Get the font of a style.
        /// Returns the length of the fontName
        /// Result is NUL-terminated.
        /// (Scintilla feature 2486)
        /// </summary>
        public unsafe string StyleGetFont(int style)
        {
            byte[] fontNameBuffer = new byte[10000];
            fixed (byte* fontNamePtr = fontNameBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETFONT, style, (IntPtr) fontNamePtr);
                return Encoding.UTF8.GetString(fontNameBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Get is a style to have its end of line filled or not. (Scintilla feature 2487)</summary>
        public bool StyleGetEOLFilled(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETEOLFILLED, style, Unused);
            return 1 == (int) res;
        }

        /// <summary>Get is a style underlined or not. (Scintilla feature 2488)</summary>
        public bool StyleGetUnderline(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETUNDERLINE, style, Unused);
            return 1 == (int) res;
        }

        /// <summary>Get is a style mixed case, or to force upper or lower case. (Scintilla feature 2489)</summary>
        public int StyleGetCase(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETCASE, style, Unused);
            return (int) res;
        }

        /// <summary>Get the character get of the font in a style. (Scintilla feature 2490)</summary>
        public int StyleGetCharacterSet(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETCHARACTERSET, style, Unused);
            return (int) res;
        }

        /// <summary>Get is a style visible or not. (Scintilla feature 2491)</summary>
        public bool StyleGetVisible(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETVISIBLE, style, Unused);
            return 1 == (int) res;
        }

        /// <summary>
        /// Get is a style changeable or not (read only).
        /// Experimental feature, currently buggy.
        /// (Scintilla feature 2492)
        /// </summary>
        public bool StyleGetChangeable(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETCHANGEABLE, style, Unused);
            return 1 == (int) res;
        }

        /// <summary>Get is a style a hotspot or not. (Scintilla feature 2493)</summary>
        public bool StyleGetHotSpot(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETHOTSPOT, style, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set a style to be mixed case, or to force upper or lower case. (Scintilla feature 2060)</summary>
        public void StyleSetCase(int style, int caseForce)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETCASE, style, caseForce);
        }

        /// <summary>Set the size of characters of a style. Size is in points multiplied by 100. (Scintilla feature 2061)</summary>
        public void StyleSetSizeFractional(int style, int caseForce)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETSIZEFRACTIONAL, style, caseForce);
        }

        /// <summary>Get the size of characters of a style in points multiplied by 100 (Scintilla feature 2062)</summary>
        public int StyleGetSizeFractional(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETSIZEFRACTIONAL, style, Unused);
            return (int) res;
        }

        /// <summary>Set the weight of characters of a style. (Scintilla feature 2063)</summary>
        public void StyleSetWeight(int style, int weight)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETWEIGHT, style, weight);
        }

        /// <summary>Get the weight of characters of a style. (Scintilla feature 2064)</summary>
        public int StyleGetWeight(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLEGETWEIGHT, style, Unused);
            return (int) res;
        }

        /// <summary>Set the character set of the font in a style. (Scintilla feature 2066)</summary>
        public void StyleSetCharacterSet(int style, int characterSet)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETCHARACTERSET, style, characterSet);
        }

        /// <summary>Set a style to be a hotspot or not. (Scintilla feature 2409)</summary>
        public void StyleSetHotSpot(int style, bool hotspot)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETHOTSPOT, style, hotspot ? 1 : 0);
        }

        /// <summary>Set the foreground colour of the main and additional selections and whether to use this setting. (Scintilla feature 2067)</summary>
        public void SetSelFore(bool useSetting, Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELFORE, useSetting ? 1 : 0, fore.Value);
        }

        /// <summary>Set the background colour of the main and additional selections and whether to use this setting. (Scintilla feature 2068)</summary>
        public void SetSelBack(bool useSetting, Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELBACK, useSetting ? 1 : 0, back.Value);
        }

        /// <summary>Get the alpha of the selection. (Scintilla feature 2477)</summary>
        public int GetSelAlpha()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELALPHA, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the alpha of the selection. (Scintilla feature 2478)</summary>
        public void SetSelAlpha(int alpha)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELALPHA, alpha, Unused);
        }

        /// <summary>Is the selection end of line filled? (Scintilla feature 2479)</summary>
        public bool GetSelEOLFilled()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELEOLFILLED, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set the selection to have its end of line filled or not. (Scintilla feature 2480)</summary>
        public void SetSelEOLFilled(bool filled)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELEOLFILLED, filled ? 1 : 0, Unused);
        }

        /// <summary>Set the foreground colour of the caret. (Scintilla feature 2069)</summary>
        public void SetCaretFore(Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCARETFORE, fore.Value, Unused);
        }

        /// <summary>When key+modifier combination km is pressed perform msg. (Scintilla feature 2070)</summary>
        public void AssignCmdKey(KeyModifier km, int msg)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ASSIGNCMDKEY, km.Value, msg);
        }

        /// <summary>When key+modifier combination km is pressed do nothing. (Scintilla feature 2071)</summary>
        public void ClearCmdKey(KeyModifier km)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CLEARCMDKEY, km.Value, Unused);
        }

        /// <summary>Drop all key mappings. (Scintilla feature 2072)</summary>
        public void ClearAllCmdKeys()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CLEARALLCMDKEYS, Unused, Unused);
        }

        /// <summary>Set the styles for a segment of the document. (Scintilla feature 2073)</summary>
        public unsafe void SetStylingEx(int length, string styles)
        {
            fixed (byte* stylesPtr = Encoding.UTF8.GetBytes(styles))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSTYLINGEX, length, (IntPtr) stylesPtr);
            }
        }

        /// <summary>Set a style to be visible or not. (Scintilla feature 2074)</summary>
        public void StyleSetVisible(int style, bool visible)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETVISIBLE, style, visible ? 1 : 0);
        }

        /// <summary>Get the time in milliseconds that the caret is on and off. (Scintilla feature 2075)</summary>
        public int GetCaretPeriod()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCARETPERIOD, Unused, Unused);
            return (int) res;
        }

        /// <summary>Get the time in milliseconds that the caret is on and off. 0 = steady on. (Scintilla feature 2076)</summary>
        public void SetCaretPeriod(int periodMilliseconds)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCARETPERIOD, periodMilliseconds, Unused);
        }

        /// <summary>
        /// Set the set of characters making up words for when moving or selecting by word.
        /// First sets defaults like SetCharsDefault.
        /// (Scintilla feature 2077)
        /// </summary>
        public unsafe void SetWordChars(string characters)
        {
            fixed (byte* charactersPtr = Encoding.UTF8.GetBytes(characters))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWORDCHARS, Unused, (IntPtr) charactersPtr);
            }
        }

        /// <summary>
        /// Get the set of characters making up words for when moving or selecting by word.
        /// Returns the number of characters
        /// (Scintilla feature 2646)
        /// </summary>
        public unsafe string GetWordChars()
        {
            byte[] charactersBuffer = new byte[10000];
            fixed (byte* charactersPtr = charactersBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETWORDCHARS, Unused, (IntPtr) charactersPtr);
                return Encoding.UTF8.GetString(charactersBuffer).TrimEnd('\0');
            }
        }

        /// <summary>
        /// Start a sequence of actions that is undone and redone as a unit.
        /// May be nested.
        /// (Scintilla feature 2078)
        /// </summary>
        public void BeginUndoAction()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_BEGINUNDOACTION, Unused, Unused);
        }

        /// <summary>End a sequence of actions that is undone and redone as a unit. (Scintilla feature 2079)</summary>
        public void EndUndoAction()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ENDUNDOACTION, Unused, Unused);
        }

        /// <summary>Set an indicator to plain, squiggle or TT. (Scintilla feature 2080)</summary>
        public void IndicSetStyle(int indic, int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETSTYLE, indic, style);
        }

        /// <summary>Retrieve the style of an indicator. (Scintilla feature 2081)</summary>
        public int IndicGetStyle(int indic)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICGETSTYLE, indic, Unused);
            return (int) res;
        }

        /// <summary>Set the foreground colour of an indicator. (Scintilla feature 2082)</summary>
        public void IndicSetFore(int indic, Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETFORE, indic, fore.Value);
        }

        /// <summary>Retrieve the foreground colour of an indicator. (Scintilla feature 2083)</summary>
        public Colour IndicGetFore(int indic)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICGETFORE, indic, Unused);
            return new Colour((int) res);
        }

        /// <summary>Set an indicator to draw under text or over(default). (Scintilla feature 2510)</summary>
        public void IndicSetUnder(int indic, bool under)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETUNDER, indic, under ? 1 : 0);
        }

        /// <summary>Retrieve whether indicator drawn under or over text. (Scintilla feature 2511)</summary>
        public bool IndicGetUnder(int indic)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICGETUNDER, indic, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set a hover indicator to plain, squiggle or TT. (Scintilla feature 2680)</summary>
        public void IndicSetHoverStyle(int indic, int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETHOVERSTYLE, indic, style);
        }

        /// <summary>Retrieve the hover style of an indicator. (Scintilla feature 2681)</summary>
        public int IndicGetHoverStyle(int indic)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICGETHOVERSTYLE, indic, Unused);
            return (int) res;
        }

        /// <summary>Set the foreground hover colour of an indicator. (Scintilla feature 2682)</summary>
        public void IndicSetHoverFore(int indic, Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETHOVERFORE, indic, fore.Value);
        }

        /// <summary>Retrieve the foreground hover colour of an indicator. (Scintilla feature 2683)</summary>
        public Colour IndicGetHoverFore(int indic)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICGETHOVERFORE, indic, Unused);
            return new Colour((int) res);
        }

        /// <summary>Set the attributes of an indicator. (Scintilla feature 2684)</summary>
        public void IndicSetFlags(int indic, int flags)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETFLAGS, indic, flags);
        }

        /// <summary>Retrieve the attributes of an indicator. (Scintilla feature 2685)</summary>
        public int IndicGetFlags(int indic)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICGETFLAGS, indic, Unused);
            return (int) res;
        }

        /// <summary>Set the foreground colour of all whitespace and whether to use this setting. (Scintilla feature 2084)</summary>
        public void SetWhitespaceFore(bool useSetting, Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWHITESPACEFORE, useSetting ? 1 : 0, fore.Value);
        }

        /// <summary>Set the background colour of all whitespace and whether to use this setting. (Scintilla feature 2085)</summary>
        public void SetWhitespaceBack(bool useSetting, Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWHITESPACEBACK, useSetting ? 1 : 0, back.Value);
        }

        /// <summary>Set the size of the dots used to mark space characters. (Scintilla feature 2086)</summary>
        public void SetWhitespaceSize(int size)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWHITESPACESIZE, size, Unused);
        }

        /// <summary>Get the size of the dots used to mark space characters. (Scintilla feature 2087)</summary>
        public int GetWhitespaceSize()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETWHITESPACESIZE, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Divide each styling byte into lexical class bits (default: 5) and indicator
        /// bits (default: 3). If a lexer requires more than 32 lexical states, then this
        /// is used to expand the possible states.
        /// (Scintilla feature 2090)
        /// </summary>
        public void SetStyleBits(int bits)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSTYLEBITS, bits, Unused);
        }

        /// <summary>Retrieve number of bits in style bytes used to hold the lexical state. (Scintilla feature 2091)</summary>
        public int GetStyleBits()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSTYLEBITS, Unused, Unused);
            return (int) res;
        }

        /// <summary>Used to hold extra styling information for each line. (Scintilla feature 2092)</summary>
        public void SetLineState(int line, int state)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETLINESTATE, line, state);
        }

        /// <summary>Retrieve the extra styling information for a line. (Scintilla feature 2093)</summary>
        public int GetLineState(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINESTATE, line, Unused);
            return (int) res;
        }

        /// <summary>Retrieve the last line number that has line state. (Scintilla feature 2094)</summary>
        public int GetMaxLineState()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMAXLINESTATE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Is the background of the line containing the caret in a different colour? (Scintilla feature 2095)</summary>
        public bool GetCaretLineVisible()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCARETLINEVISIBLE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Display the background of the line containing the caret in a different colour. (Scintilla feature 2096)</summary>
        public void SetCaretLineVisible(bool show)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCARETLINEVISIBLE, show ? 1 : 0, Unused);
        }

        /// <summary>Get the colour of the background of the line containing the caret. (Scintilla feature 2097)</summary>
        public Colour GetCaretLineBack()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCARETLINEBACK, Unused, Unused);
            return new Colour((int) res);
        }

        /// <summary>Set the colour of the background of the line containing the caret. (Scintilla feature 2098)</summary>
        public void SetCaretLineBack(Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCARETLINEBACK, back.Value, Unused);
        }

        /// <summary>
        /// Set a style to be changeable or not (read only).
        /// Experimental feature, currently buggy.
        /// (Scintilla feature 2099)
        /// </summary>
        public void StyleSetChangeable(int style, bool changeable)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STYLESETCHANGEABLE, style, changeable ? 1 : 0);
        }

        /// <summary>
        /// Display a auto-completion list.
        /// The lenEntered parameter indicates how many characters before
        /// the caret should be used to provide context.
        /// (Scintilla feature 2100)
        /// </summary>
        public unsafe void AutoCShow(int lenEntered, string itemList)
        {
            fixed (byte* itemListPtr = Encoding.UTF8.GetBytes(itemList))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSHOW, lenEntered, (IntPtr) itemListPtr);
            }
        }

        /// <summary>Remove the auto-completion list from the screen. (Scintilla feature 2101)</summary>
        public void AutoCCancel()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCCANCEL, Unused, Unused);
        }

        /// <summary>Is there an auto-completion list visible? (Scintilla feature 2102)</summary>
        public bool AutoCActive()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCACTIVE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Retrieve the position of the caret when the auto-completion list was displayed. (Scintilla feature 2103)</summary>
        public Position AutoCPosStart()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCPOSSTART, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>User has selected an item so remove the list and insert the selection. (Scintilla feature 2104)</summary>
        public void AutoCComplete()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCCOMPLETE, Unused, Unused);
        }

        /// <summary>Define a set of character that when typed cancel the auto-completion list. (Scintilla feature 2105)</summary>
        public unsafe void AutoCStops(string characterSet)
        {
            fixed (byte* characterSetPtr = Encoding.UTF8.GetBytes(characterSet))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSTOPS, Unused, (IntPtr) characterSetPtr);
            }
        }

        /// <summary>
        /// Change the separator character in the string setting up an auto-completion list.
        /// Default is space but can be changed if items contain space.
        /// (Scintilla feature 2106)
        /// </summary>
        public void AutoCSetSeparator(int separatorCharacter)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETSEPARATOR, separatorCharacter, Unused);
        }

        /// <summary>Retrieve the auto-completion list separator character. (Scintilla feature 2107)</summary>
        public int AutoCGetSeparator()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETSEPARATOR, Unused, Unused);
            return (int) res;
        }

        /// <summary>Select the item in the auto-completion list that starts with a string. (Scintilla feature 2108)</summary>
        public unsafe void AutoCSelect(string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSELECT, Unused, (IntPtr) textPtr);
            }
        }

        /// <summary>
        /// Should the auto-completion list be cancelled if the user backspaces to a
        /// position before where the box was created.
        /// (Scintilla feature 2110)
        /// </summary>
        public void AutoCSetCancelAtStart(bool cancel)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETCANCELATSTART, cancel ? 1 : 0, Unused);
        }

        /// <summary>Retrieve whether auto-completion cancelled by backspacing before start. (Scintilla feature 2111)</summary>
        public bool AutoCGetCancelAtStart()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETCANCELATSTART, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>
        /// Define a set of characters that when typed will cause the autocompletion to
        /// choose the selected item.
        /// (Scintilla feature 2112)
        /// </summary>
        public unsafe void AutoCSetFillUps(string characterSet)
        {
            fixed (byte* characterSetPtr = Encoding.UTF8.GetBytes(characterSet))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETFILLUPS, Unused, (IntPtr) characterSetPtr);
            }
        }

        /// <summary>Should a single item auto-completion list automatically choose the item. (Scintilla feature 2113)</summary>
        public void AutoCSetChooseSingle(bool chooseSingle)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETCHOOSESINGLE, chooseSingle ? 1 : 0, Unused);
        }

        /// <summary>Retrieve whether a single item auto-completion list automatically choose the item. (Scintilla feature 2114)</summary>
        public bool AutoCGetChooseSingle()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETCHOOSESINGLE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set whether case is significant when performing auto-completion searches. (Scintilla feature 2115)</summary>
        public void AutoCSetIgnoreCase(bool ignoreCase)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETIGNORECASE, ignoreCase ? 1 : 0, Unused);
        }

        /// <summary>Retrieve state of ignore case flag. (Scintilla feature 2116)</summary>
        public bool AutoCGetIgnoreCase()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETIGNORECASE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Display a list of strings and send notification when user chooses one. (Scintilla feature 2117)</summary>
        public unsafe void UserListShow(int listType, string itemList)
        {
            fixed (byte* itemListPtr = Encoding.UTF8.GetBytes(itemList))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_USERLISTSHOW, listType, (IntPtr) itemListPtr);
            }
        }

        /// <summary>Set whether or not autocompletion is hidden automatically when nothing matches. (Scintilla feature 2118)</summary>
        public void AutoCSetAutoHide(bool autoHide)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETAUTOHIDE, autoHide ? 1 : 0, Unused);
        }

        /// <summary>Retrieve whether or not autocompletion is hidden automatically when nothing matches. (Scintilla feature 2119)</summary>
        public bool AutoCGetAutoHide()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETAUTOHIDE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>
        /// Set whether or not autocompletion deletes any word characters
        /// after the inserted text upon completion.
        /// (Scintilla feature 2270)
        /// </summary>
        public void AutoCSetDropRestOfWord(bool dropRestOfWord)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETDROPRESTOFWORD, dropRestOfWord ? 1 : 0, Unused);
        }

        /// <summary>
        /// Retrieve whether or not autocompletion deletes any word characters
        /// after the inserted text upon completion.
        /// (Scintilla feature 2271)
        /// </summary>
        public bool AutoCGetDropRestOfWord()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETDROPRESTOFWORD, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Register an XPM image for use in autocompletion lists. (Scintilla feature 2405)</summary>
        public unsafe void RegisterImage(int type, string xpmData)
        {
            fixed (byte* xpmDataPtr = Encoding.UTF8.GetBytes(xpmData))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERIMAGE, type, (IntPtr) xpmDataPtr);
            }
        }

        /// <summary>Clear all the registered XPM images. (Scintilla feature 2408)</summary>
        public void ClearRegisteredImages()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CLEARREGISTEREDIMAGES, Unused, Unused);
        }

        /// <summary>Retrieve the auto-completion list type-separator character. (Scintilla feature 2285)</summary>
        public int AutoCGetTypeSeparator()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETTYPESEPARATOR, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Change the type-separator character in the string setting up an auto-completion list.
        /// Default is '?' but can be changed if items contain '?'.
        /// (Scintilla feature 2286)
        /// </summary>
        public void AutoCSetTypeSeparator(int separatorCharacter)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETTYPESEPARATOR, separatorCharacter, Unused);
        }

        /// <summary>
        /// Set the maximum width, in characters, of auto-completion and user lists.
        /// Set to 0 to autosize to fit longest item, which is the default.
        /// (Scintilla feature 2208)
        /// </summary>
        public void AutoCSetMaxWidth(int characterCount)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETMAXWIDTH, characterCount, Unused);
        }

        /// <summary>Get the maximum width, in characters, of auto-completion and user lists. (Scintilla feature 2209)</summary>
        public int AutoCGetMaxWidth()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETMAXWIDTH, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Set the maximum height, in rows, of auto-completion and user lists.
        /// The default is 5 rows.
        /// (Scintilla feature 2210)
        /// </summary>
        public void AutoCSetMaxHeight(int rowCount)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETMAXHEIGHT, rowCount, Unused);
        }

        /// <summary>Set the maximum height, in rows, of auto-completion and user lists. (Scintilla feature 2211)</summary>
        public int AutoCGetMaxHeight()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETMAXHEIGHT, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the number of spaces used for one level of indentation. (Scintilla feature 2122)</summary>
        public void SetIndent(int indentSize)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETINDENT, indentSize, Unused);
        }

        /// <summary>Retrieve indentation size. (Scintilla feature 2123)</summary>
        public int GetIndent()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETINDENT, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Indentation will only use space characters if useTabs is false, otherwise
        /// it will use a combination of tabs and spaces.
        /// (Scintilla feature 2124)
        /// </summary>
        public void SetUseTabs(bool useTabs)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETUSETABS, useTabs ? 1 : 0, Unused);
        }

        /// <summary>Retrieve whether tabs will be used in indentation. (Scintilla feature 2125)</summary>
        public bool GetUseTabs()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETUSETABS, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Change the indentation of a line to a number of columns. (Scintilla feature 2126)</summary>
        public void SetLineIndentation(int line, int indentSize)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETLINEINDENTATION, line, indentSize);
        }

        /// <summary>Retrieve the number of columns that a line is indented. (Scintilla feature 2127)</summary>
        public int GetLineIndentation(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINEINDENTATION, line, Unused);
            return (int) res;
        }

        /// <summary>Retrieve the position before the first non indentation character on a line. (Scintilla feature 2128)</summary>
        public Position GetLineIndentPosition(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINEINDENTPOSITION, line, Unused);
            return new Position((int) res);
        }

        /// <summary>Retrieve the column number of a position, taking tab width into account. (Scintilla feature 2129)</summary>
        public int GetColumn(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCOLUMN, pos.Value, Unused);
            return (int) res;
        }

        /// <summary>Count characters between two positions. (Scintilla feature 2633)</summary>
        public int CountCharacters(int startPos, int endPos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_COUNTCHARACTERS, startPos, endPos);
            return (int) res;
        }

        /// <summary>Show or hide the horizontal scroll bar. (Scintilla feature 2130)</summary>
        public void SetHScrollBar(bool show)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETHSCROLLBAR, show ? 1 : 0, Unused);
        }

        /// <summary>Is the horizontal scroll bar visible? (Scintilla feature 2131)</summary>
        public bool GetHScrollBar()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETHSCROLLBAR, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Show or hide indentation guides. (Scintilla feature 2132)</summary>
        public void SetIndentationGuides(int indentView)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETINDENTATIONGUIDES, indentView, Unused);
        }

        /// <summary>Are the indentation guides visible? (Scintilla feature 2133)</summary>
        public int GetIndentationGuides()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETINDENTATIONGUIDES, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Set the highlighted indentation guide column.
        /// 0 = no highlighted guide.
        /// (Scintilla feature 2134)
        /// </summary>
        public void SetHighlightGuide(int column)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETHIGHLIGHTGUIDE, column, Unused);
        }

        /// <summary>Get the highlighted indentation guide column. (Scintilla feature 2135)</summary>
        public int GetHighlightGuide()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETHIGHLIGHTGUIDE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Get the position after the last visible characters on a line. (Scintilla feature 2136)</summary>
        public Position GetLineEndPosition(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINEENDPOSITION, line, Unused);
            return new Position((int) res);
        }

        /// <summary>Get the code page used to interpret the bytes of the document as characters. (Scintilla feature 2137)</summary>
        public int GetCodePage()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCODEPAGE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Get the foreground colour of the caret. (Scintilla feature 2138)</summary>
        public Colour GetCaretFore()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCARETFORE, Unused, Unused);
            return new Colour((int) res);
        }

        /// <summary>In read-only mode? (Scintilla feature 2140)</summary>
        public bool GetReadOnly()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETREADONLY, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Sets the position of the caret. (Scintilla feature 2141)</summary>
        public void SetCurrentPos(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCURRENTPOS, pos.Value, Unused);
        }

        /// <summary>Sets the position that starts the selection - this becomes the anchor. (Scintilla feature 2142)</summary>
        public void SetSelectionStart(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTIONSTART, pos.Value, Unused);
        }

        /// <summary>Returns the position at the start of the selection. (Scintilla feature 2143)</summary>
        public Position GetSelectionStart()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONSTART, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Sets the position that ends the selection - this becomes the currentPosition. (Scintilla feature 2144)</summary>
        public void SetSelectionEnd(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTIONEND, pos.Value, Unused);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2145)</summary>
        public Position GetSelectionEnd()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONEND, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Set caret to a position, while removing any existing selection. (Scintilla feature 2556)</summary>
        public void SetEmptySelection(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETEMPTYSELECTION, pos.Value, Unused);
        }

        /// <summary>Sets the print magnification added to the point size of each style for printing. (Scintilla feature 2146)</summary>
        public void SetPrintMagnification(int magnification)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETPRINTMAGNIFICATION, magnification, Unused);
        }

        /// <summary>Returns the print magnification. (Scintilla feature 2147)</summary>
        public int GetPrintMagnification()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPRINTMAGNIFICATION, Unused, Unused);
            return (int) res;
        }

        /// <summary>Modify colours when printing for clearer printed text. (Scintilla feature 2148)</summary>
        public void SetPrintColourMode(int mode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETPRINTCOLOURMODE, mode, Unused);
        }

        /// <summary>Returns the print colour mode. (Scintilla feature 2149)</summary>
        public int GetPrintColourMode()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPRINTCOLOURMODE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Find some text in the document. (Scintilla feature 2150)</summary>
        public Position FindText(int flags, TextToFind ft)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FINDTEXT, flags, ft.NativePointer);
            return new Position((int) res);
        }

        /// <summary>Retrieve the display line at the top of the display. (Scintilla feature 2152)</summary>
        public int GetFirstVisibleLine()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETFIRSTVISIBLELINE, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Retrieve the contents of a line.
        /// Returns the length of the line.
        /// (Scintilla feature 2153)
        /// </summary>
        public unsafe string GetLine(int line)
        {
            byte[] textBuffer = new byte[10000];
            fixed (byte* textPtr = textBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINE, line, (IntPtr) textPtr);
                return Encoding.UTF8.GetString(textBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Returns the number of lines in the document. There is always at least one. (Scintilla feature 2154)</summary>
        public int GetLineCount()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINECOUNT, Unused, Unused);
            return (int) res;
        }

        /// <summary>Sets the size in pixels of the left margin. (Scintilla feature 2155)</summary>
        public void SetMarginLeft(int pixelWidth)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMARGINLEFT, Unused, pixelWidth);
        }

        /// <summary>Returns the size in pixels of the left margin. (Scintilla feature 2156)</summary>
        public int GetMarginLeft()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMARGINLEFT, Unused, Unused);
            return (int) res;
        }

        /// <summary>Sets the size in pixels of the right margin. (Scintilla feature 2157)</summary>
        public void SetMarginRight(int pixelWidth)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMARGINRIGHT, Unused, pixelWidth);
        }

        /// <summary>Returns the size in pixels of the right margin. (Scintilla feature 2158)</summary>
        public int GetMarginRight()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMARGINRIGHT, Unused, Unused);
            return (int) res;
        }

        /// <summary>Is the document different from when it was last saved? (Scintilla feature 2159)</summary>
        public bool GetModify()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMODIFY, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Select a range of text. (Scintilla feature 2160)</summary>
        public void SetSel(Position start, Position end)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSEL, start.Value, end.Value);
        }

        /// <summary>
        /// Retrieve the selected text.
        /// Return the length of the text.
        /// Result is NUL-terminated.
        /// (Scintilla feature 2161)
        /// </summary>
        public unsafe string GetSelText()
        {
            byte[] textBuffer = new byte[10000];
            fixed (byte* textPtr = textBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELTEXT, Unused, (IntPtr) textPtr);
                return Encoding.UTF8.GetString(textBuffer).TrimEnd('\0');
            }
        }

        /// <summary>
        /// Retrieve a range of text.
        /// Return the length of the text.
        /// (Scintilla feature 2162)
        /// </summary>
        public int GetTextRange(TextRange tr)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTEXTRANGE, Unused, tr.NativePointer);
            return (int) res;
        }

        /// <summary>Draw the selection in normal style or with selection highlighted. (Scintilla feature 2163)</summary>
        public void HideSelection(bool normal)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_HIDESELECTION, normal ? 1 : 0, Unused);
        }

        /// <summary>Retrieve the x value of the point in the window where a position is displayed. (Scintilla feature 2164)</summary>
        public int PointXFromPosition(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_POINTXFROMPOSITION, Unused, pos.Value);
            return (int) res;
        }

        /// <summary>Retrieve the y value of the point in the window where a position is displayed. (Scintilla feature 2165)</summary>
        public int PointYFromPosition(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_POINTYFROMPOSITION, Unused, pos.Value);
            return (int) res;
        }

        /// <summary>Retrieve the line containing a position. (Scintilla feature 2166)</summary>
        public int LineFromPosition(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEFROMPOSITION, pos.Value, Unused);
            return (int) res;
        }

        /// <summary>Retrieve the position at the start of a line. (Scintilla feature 2167)</summary>
        public Position PositionFromLine(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_POSITIONFROMLINE, line, Unused);
            return new Position((int) res);
        }

        /// <summary>Scroll horizontally and vertically. (Scintilla feature 2168)</summary>
        public void LineScroll(int columns, int lines)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINESCROLL, columns, lines);
        }

        /// <summary>Ensure the caret is visible. (Scintilla feature 2169)</summary>
        public void ScrollCaret()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SCROLLCARET, Unused, Unused);
        }

        /// <summary>
        /// Scroll the argument positions and the range between them into view giving
        /// priority to the primary position then the secondary position.
        /// This may be used to make a search match visible.
        /// (Scintilla feature 2569)
        /// </summary>
        public void ScrollRange(Position secondary, Position primary)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SCROLLRANGE, secondary.Value, primary.Value);
        }

        /// <summary>Replace the selected text with the argument text. (Scintilla feature 2170)</summary>
        public unsafe void ReplaceSel(string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_REPLACESEL, Unused, (IntPtr) textPtr);
            }
        }

        /// <summary>Set to read only or read write. (Scintilla feature 2171)</summary>
        public void SetReadOnly(bool readOnly)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETREADONLY, readOnly ? 1 : 0, Unused);
        }

        /// <summary>Null operation. (Scintilla feature 2172)</summary>
        public void Null()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_NULL, Unused, Unused);
        }

        /// <summary>Will a paste succeed? (Scintilla feature 2173)</summary>
        public bool CanPaste()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CANPASTE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Are there any undoable actions in the undo history? (Scintilla feature 2174)</summary>
        public bool CanUndo()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CANUNDO, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Delete the undo history. (Scintilla feature 2175)</summary>
        public void EmptyUndoBuffer()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_EMPTYUNDOBUFFER, Unused, Unused);
        }

        /// <summary>Undo one action in the undo history. (Scintilla feature 2176)</summary>
        public void Undo()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_UNDO, Unused, Unused);
        }

        /// <summary>Cut the selection to the clipboard. (Scintilla feature 2177)</summary>
        public void Cut()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CUT, Unused, Unused);
        }

        /// <summary>Copy the selection to the clipboard. (Scintilla feature 2178)</summary>
        public void Copy()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_COPY, Unused, Unused);
        }

        /// <summary>Paste the contents of the clipboard into the document replacing the selection. (Scintilla feature 2179)</summary>
        public void Paste()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PASTE, Unused, Unused);
        }

        /// <summary>Clear the selection. (Scintilla feature 2180)</summary>
        public void Clear()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CLEAR, Unused, Unused);
        }

        /// <summary>Replace the contents of the document with the argument text. (Scintilla feature 2181)</summary>
        public unsafe void SetText(string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETTEXT, Unused, (IntPtr) textPtr);
            }
        }

        /// <summary>
        /// Retrieve all the text in the document.
        /// Returns number of characters retrieved.
        /// Result is NUL-terminated.
        /// (Scintilla feature 2182)
        /// </summary>
        public unsafe string GetText(int length)
        {
            byte[] textBuffer = new byte[10000];
            fixed (byte* textPtr = textBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTEXT, length, (IntPtr) textPtr);
                return Encoding.UTF8.GetString(textBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Retrieve the number of characters in the document. (Scintilla feature 2183)</summary>
        public int GetTextLength()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTEXTLENGTH, Unused, Unused);
            return (int) res;
        }

        /// <summary>Retrieve a pointer to a function that processes messages for this Scintilla. (Scintilla feature 2184)</summary>
        public IntPtr GetDirectFunction()
        {
            return Win32.SendMessage(scintilla, SciMsg.SCI_GETDIRECTFUNCTION, Unused, Unused);
        }

        /// <summary>
        /// Retrieve a pointer value to use as the first argument when calling
        /// the function returned by GetDirectFunction.
        /// (Scintilla feature 2185)
        /// </summary>
        public IntPtr GetDirectPointer()
        {
            return Win32.SendMessage(scintilla, SciMsg.SCI_GETDIRECTPOINTER, Unused, Unused);
        }

        /// <summary>Set to overtype (true) or insert mode. (Scintilla feature 2186)</summary>
        public void SetOvertype(bool overtype)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETOVERTYPE, overtype ? 1 : 0, Unused);
        }

        /// <summary>Returns true if overtype mode is active otherwise false is returned. (Scintilla feature 2187)</summary>
        public bool GetOvertype()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETOVERTYPE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set the width of the insert mode caret. (Scintilla feature 2188)</summary>
        public void SetCaretWidth(int pixelWidth)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCARETWIDTH, pixelWidth, Unused);
        }

        /// <summary>Returns the width of the insert mode caret. (Scintilla feature 2189)</summary>
        public int GetCaretWidth()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCARETWIDTH, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Sets the position that starts the target which is used for updating the
        /// document without affecting the scroll position.
        /// (Scintilla feature 2190)
        /// </summary>
        public void SetTargetStart(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETTARGETSTART, pos.Value, Unused);
        }

        /// <summary>Get the position that starts the target. (Scintilla feature 2191)</summary>
        public Position GetTargetStart()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTARGETSTART, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>
        /// Sets the position that ends the target which is used for updating the
        /// document without affecting the scroll position.
        /// (Scintilla feature 2192)
        /// </summary>
        public void SetTargetEnd(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETTARGETEND, pos.Value, Unused);
        }

        /// <summary>Get the position that ends the target. (Scintilla feature 2193)</summary>
        public Position GetTargetEnd()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTARGETEND, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Sets both the start and end of the target in one call. (Scintilla feature 2686)</summary>
        public void SetTargetRange(Position start, Position end)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETTARGETRANGE, start.Value, end.Value);
        }

        /// <summary>Retrieve the text in the target. (Scintilla feature 2687)</summary>
        public unsafe string GetTargetText()
        {
            byte[] charactersBuffer = new byte[10000];
            fixed (byte* charactersPtr = charactersBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTARGETTEXT, Unused, (IntPtr) charactersPtr);
                return Encoding.UTF8.GetString(charactersBuffer).TrimEnd('\0');
            }
        }

        /// <summary>
        /// Replace the target text with the argument text.
        /// Text is counted so it can contain NULs.
        /// Returns the length of the replacement text.
        /// (Scintilla feature 2194)
        /// </summary>
        public unsafe int ReplaceTarget(int length, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_REPLACETARGET, length, (IntPtr) textPtr);
                return (int) res;
            }
        }

        /// <summary>
        /// Replace the target text with the argument text after \d processing.
        /// Text is counted so it can contain NULs.
        /// Looks for \d where d is between 1 and 9 and replaces these with the strings
        /// matched in the last search operation which were surrounded by \( and \).
        /// Returns the length of the replacement text including any change
        /// caused by processing the \d patterns.
        /// (Scintilla feature 2195)
        /// </summary>
        public unsafe int ReplaceTargetRE(int length, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_REPLACETARGETRE, length, (IntPtr) textPtr);
                return (int) res;
            }
        }

        /// <summary>
        /// Search for a counted string in the target and set the target to the found
        /// range. Text is counted so it can contain NULs.
        /// Returns length of range or -1 for failure in which case target is not moved.
        /// (Scintilla feature 2197)
        /// </summary>
        public unsafe int SearchInTarget(int length, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SEARCHINTARGET, length, (IntPtr) textPtr);
                return (int) res;
            }
        }

        /// <summary>Set the search flags used by SearchInTarget. (Scintilla feature 2198)</summary>
        public void SetSearchFlags(int flags)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSEARCHFLAGS, flags, Unused);
        }

        /// <summary>Get the search flags used by SearchInTarget. (Scintilla feature 2199)</summary>
        public int GetSearchFlags()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSEARCHFLAGS, Unused, Unused);
            return (int) res;
        }

        /// <summary>Show a call tip containing a definition near position pos. (Scintilla feature 2200)</summary>
        public unsafe void CallTipShow(Position pos, string definition)
        {
            fixed (byte* definitionPtr = Encoding.UTF8.GetBytes(definition))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSHOW, pos.Value, (IntPtr) definitionPtr);
            }
        }

        /// <summary>Remove the call tip from the screen. (Scintilla feature 2201)</summary>
        public void CallTipCancel()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPCANCEL, Unused, Unused);
        }

        /// <summary>Is there an active call tip? (Scintilla feature 2202)</summary>
        public bool CallTipActive()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPACTIVE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Retrieve the position where the caret was before displaying the call tip. (Scintilla feature 2203)</summary>
        public Position CallTipPosStart()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPPOSSTART, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Set the start position in order to change when backspacing removes the calltip. (Scintilla feature 2214)</summary>
        public void CallTipSetPosStart(int posStart)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSETPOSSTART, posStart, Unused);
        }

        /// <summary>Highlight a segment of the definition. (Scintilla feature 2204)</summary>
        public void CallTipSetHlt(int start, int end)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSETHLT, start, end);
        }

        /// <summary>Set the background colour for the call tip. (Scintilla feature 2205)</summary>
        public void CallTipSetBack(Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSETBACK, back.Value, Unused);
        }

        /// <summary>Set the foreground colour for the call tip. (Scintilla feature 2206)</summary>
        public void CallTipSetFore(Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSETFORE, fore.Value, Unused);
        }

        /// <summary>Set the foreground colour for the highlighted part of the call tip. (Scintilla feature 2207)</summary>
        public void CallTipSetForeHlt(Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSETFOREHLT, fore.Value, Unused);
        }

        /// <summary>Enable use of STYLE_CALLTIP and set call tip tab size in pixels. (Scintilla feature 2212)</summary>
        public void CallTipUseStyle(int tabSize)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPUSESTYLE, tabSize, Unused);
        }

        /// <summary>Set position of calltip, above or below text. (Scintilla feature 2213)</summary>
        public void CallTipSetPosition(bool above)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSETPOSITION, above ? 1 : 0, Unused);
        }

        /// <summary>Find the display line of a document line taking hidden lines into account. (Scintilla feature 2220)</summary>
        public int VisibleFromDocLine(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_VISIBLEFROMDOCLINE, line, Unused);
            return (int) res;
        }

        /// <summary>Find the document line of a display line taking hidden lines into account. (Scintilla feature 2221)</summary>
        public int DocLineFromVisible(int lineDisplay)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DOCLINEFROMVISIBLE, lineDisplay, Unused);
            return (int) res;
        }

        /// <summary>The number of display lines needed to wrap a document line (Scintilla feature 2235)</summary>
        public int WrapCount(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WRAPCOUNT, line, Unused);
            return (int) res;
        }

        /// <summary>
        /// Set the fold level of a line.
        /// This encodes an integer level along with flags indicating whether the
        /// line is a header and whether it is effectively white space.
        /// (Scintilla feature 2222)
        /// </summary>
        public void SetFoldLevel(int line, int level)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETFOLDLEVEL, line, level);
        }

        /// <summary>Retrieve the fold level of a line. (Scintilla feature 2223)</summary>
        public int GetFoldLevel(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETFOLDLEVEL, line, Unused);
            return (int) res;
        }

        /// <summary>Find the last child line of a header line. (Scintilla feature 2224)</summary>
        public int GetLastChild(int line, int level)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLASTCHILD, line, level);
            return (int) res;
        }

        /// <summary>Find the parent line of a child line. (Scintilla feature 2225)</summary>
        public int GetFoldParent(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETFOLDPARENT, line, Unused);
            return (int) res;
        }

        /// <summary>Make a range of lines visible. (Scintilla feature 2226)</summary>
        public void ShowLines(int lineStart, int lineEnd)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SHOWLINES, lineStart, lineEnd);
        }

        /// <summary>Make a range of lines invisible. (Scintilla feature 2227)</summary>
        public void HideLines(int lineStart, int lineEnd)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_HIDELINES, lineStart, lineEnd);
        }

        /// <summary>Is a line visible? (Scintilla feature 2228)</summary>
        public bool GetLineVisible(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINEVISIBLE, line, Unused);
            return 1 == (int) res;
        }

        /// <summary>Are all lines visible? (Scintilla feature 2236)</summary>
        public bool GetAllLinesVisible()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETALLLINESVISIBLE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Show the children of a header line. (Scintilla feature 2229)</summary>
        public void SetFoldExpanded(int line, bool expanded)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETFOLDEXPANDED, line, expanded ? 1 : 0);
        }

        /// <summary>Is a header line expanded? (Scintilla feature 2230)</summary>
        public bool GetFoldExpanded(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETFOLDEXPANDED, line, Unused);
            return 1 == (int) res;
        }

        /// <summary>Switch a header line between expanded and contracted. (Scintilla feature 2231)</summary>
        public void ToggleFold(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_TOGGLEFOLD, line, Unused);
        }

        /// <summary>Expand or contract a fold header. (Scintilla feature 2237)</summary>
        public void FoldLine(int line, int action)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FOLDLINE, line, action);
        }

        /// <summary>Expand or contract a fold header and its children. (Scintilla feature 2238)</summary>
        public void FoldChildren(int line, int action)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FOLDCHILDREN, line, action);
        }

        /// <summary>Expand a fold header and all children. Use the level argument instead of the line's current level. (Scintilla feature 2239)</summary>
        public void ExpandChildren(int line, int level)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_EXPANDCHILDREN, line, level);
        }

        /// <summary>Expand or contract all fold headers. (Scintilla feature 2662)</summary>
        public void FoldAll(int action)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FOLDALL, action, Unused);
        }

        /// <summary>Ensure a particular line is visible by expanding any header line hiding it. (Scintilla feature 2232)</summary>
        public void EnsureVisible(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ENSUREVISIBLE, line, Unused);
        }

        /// <summary>Set automatic folding behaviours. (Scintilla feature 2663)</summary>
        public void SetAutomaticFold(int automaticFold)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETAUTOMATICFOLD, automaticFold, Unused);
        }

        /// <summary>Get automatic folding behaviours. (Scintilla feature 2664)</summary>
        public int GetAutomaticFold()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETAUTOMATICFOLD, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set some style options for folding. (Scintilla feature 2233)</summary>
        public void SetFoldFlags(int flags)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETFOLDFLAGS, flags, Unused);
        }

        /// <summary>
        /// Ensure a particular line is visible by expanding any header line hiding it.
        /// Use the currently set visibility policy to determine which range to display.
        /// (Scintilla feature 2234)
        /// </summary>
        public void EnsureVisibleEnforcePolicy(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ENSUREVISIBLEENFORCEPOLICY, line, Unused);
        }

        /// <summary>Sets whether a tab pressed when caret is within indentation indents. (Scintilla feature 2260)</summary>
        public void SetTabIndents(bool tabIndents)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETTABINDENTS, tabIndents ? 1 : 0, Unused);
        }

        /// <summary>Does a tab pressed when caret is within indentation indent? (Scintilla feature 2261)</summary>
        public bool GetTabIndents()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTABINDENTS, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Sets whether a backspace pressed when caret is within indentation unindents. (Scintilla feature 2262)</summary>
        public void SetBackSpaceUnIndents(bool bsUnIndents)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETBACKSPACEUNINDENTS, bsUnIndents ? 1 : 0, Unused);
        }

        /// <summary>Does a backspace pressed when caret is within indentation unindent? (Scintilla feature 2263)</summary>
        public bool GetBackSpaceUnIndents()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETBACKSPACEUNINDENTS, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Sets the time the mouse must sit still to generate a mouse dwell event. (Scintilla feature 2264)</summary>
        public void SetMouseDwellTime(int periodMilliseconds)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMOUSEDWELLTIME, periodMilliseconds, Unused);
        }

        /// <summary>Retrieve the time the mouse must sit still to generate a mouse dwell event. (Scintilla feature 2265)</summary>
        public int GetMouseDwellTime()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMOUSEDWELLTIME, Unused, Unused);
            return (int) res;
        }

        /// <summary>Get position of start of word. (Scintilla feature 2266)</summary>
        public int WordStartPosition(Position pos, bool onlyWordCharacters)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDSTARTPOSITION, pos.Value, onlyWordCharacters ? 1 : 0);
            return (int) res;
        }

        /// <summary>Get position of end of word. (Scintilla feature 2267)</summary>
        public int WordEndPosition(Position pos, bool onlyWordCharacters)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDENDPOSITION, pos.Value, onlyWordCharacters ? 1 : 0);
            return (int) res;
        }

        /// <summary>Sets whether text is word wrapped. (Scintilla feature 2268)</summary>
        public void SetWrapMode(int mode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWRAPMODE, mode, Unused);
        }

        /// <summary>Retrieve whether text is word wrapped. (Scintilla feature 2269)</summary>
        public int GetWrapMode()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETWRAPMODE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the display mode of visual flags for wrapped lines. (Scintilla feature 2460)</summary>
        public void SetWrapVisualFlags(int wrapVisualFlags)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWRAPVISUALFLAGS, wrapVisualFlags, Unused);
        }

        /// <summary>Retrive the display mode of visual flags for wrapped lines. (Scintilla feature 2461)</summary>
        public int GetWrapVisualFlags()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETWRAPVISUALFLAGS, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the location of visual flags for wrapped lines. (Scintilla feature 2462)</summary>
        public void SetWrapVisualFlagsLocation(int wrapVisualFlagsLocation)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWRAPVISUALFLAGSLOCATION, wrapVisualFlagsLocation, Unused);
        }

        /// <summary>Retrive the location of visual flags for wrapped lines. (Scintilla feature 2463)</summary>
        public int GetWrapVisualFlagsLocation()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETWRAPVISUALFLAGSLOCATION, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the start indent for wrapped lines. (Scintilla feature 2464)</summary>
        public void SetWrapStartIndent(int indent)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWRAPSTARTINDENT, indent, Unused);
        }

        /// <summary>Retrive the start indent for wrapped lines. (Scintilla feature 2465)</summary>
        public int GetWrapStartIndent()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETWRAPSTARTINDENT, Unused, Unused);
            return (int) res;
        }

        /// <summary>Sets how wrapped sublines are placed. Default is fixed. (Scintilla feature 2472)</summary>
        public void SetWrapIndentMode(int mode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWRAPINDENTMODE, mode, Unused);
        }

        /// <summary>Retrieve how wrapped sublines are placed. Default is fixed. (Scintilla feature 2473)</summary>
        public int GetWrapIndentMode()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETWRAPINDENTMODE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Sets the degree of caching of layout information. (Scintilla feature 2272)</summary>
        public void SetLayoutCache(int mode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETLAYOUTCACHE, mode, Unused);
        }

        /// <summary>Retrieve the degree of caching of layout information. (Scintilla feature 2273)</summary>
        public int GetLayoutCache()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLAYOUTCACHE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Sets the document width assumed for scrolling. (Scintilla feature 2274)</summary>
        public void SetScrollWidth(int pixelWidth)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSCROLLWIDTH, pixelWidth, Unused);
        }

        /// <summary>Retrieve the document width assumed for scrolling. (Scintilla feature 2275)</summary>
        public int GetScrollWidth()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSCROLLWIDTH, Unused, Unused);
            return (int) res;
        }

        /// <summary>Sets whether the maximum width line displayed is used to set scroll width. (Scintilla feature 2516)</summary>
        public void SetScrollWidthTracking(bool tracking)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSCROLLWIDTHTRACKING, tracking ? 1 : 0, Unused);
        }

        /// <summary>Retrieve whether the scroll width tracks wide lines. (Scintilla feature 2517)</summary>
        public bool GetScrollWidthTracking()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSCROLLWIDTHTRACKING, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>
        /// Measure the pixel width of some text in a particular style.
        /// NUL terminated text argument.
        /// Does not handle tab or control characters.
        /// (Scintilla feature 2276)
        /// </summary>
        public unsafe int TextWidth(int style, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_TEXTWIDTH, style, (IntPtr) textPtr);
                return (int) res;
            }
        }

        /// <summary>
        /// Sets the scroll range so that maximum scroll position has
        /// the last line at the bottom of the view (default).
        /// Setting this to false allows scrolling one page below the last line.
        /// (Scintilla feature 2277)
        /// </summary>
        public void SetEndAtLastLine(bool endAtLastLine)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETENDATLASTLINE, endAtLastLine ? 1 : 0, Unused);
        }

        /// <summary>
        /// Retrieve whether the maximum scroll position has the last
        /// line at the bottom of the view.
        /// (Scintilla feature 2278)
        /// </summary>
        public bool GetEndAtLastLine()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETENDATLASTLINE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Retrieve the height of a particular line of text in pixels. (Scintilla feature 2279)</summary>
        public int TextHeight(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_TEXTHEIGHT, line, Unused);
            return (int) res;
        }

        /// <summary>Show or hide the vertical scroll bar. (Scintilla feature 2280)</summary>
        public void SetVScrollBar(bool show)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETVSCROLLBAR, show ? 1 : 0, Unused);
        }

        /// <summary>Is the vertical scroll bar visible? (Scintilla feature 2281)</summary>
        public bool GetVScrollBar()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETVSCROLLBAR, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Append a string to the end of the document without changing the selection. (Scintilla feature 2282)</summary>
        public unsafe void AppendText(int length, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_APPENDTEXT, length, (IntPtr) textPtr);
            }
        }

        /// <summary>Is drawing done in two phases with backgrounds drawn before foregrounds? (Scintilla feature 2283)</summary>
        public bool GetTwoPhaseDraw()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTWOPHASEDRAW, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>
        /// In twoPhaseDraw mode, drawing is performed in two phases, first the background
        /// and then the foreground. This avoids chopping off characters that overlap the next run.
        /// (Scintilla feature 2284)
        /// </summary>
        public void SetTwoPhaseDraw(bool twoPhase)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETTWOPHASEDRAW, twoPhase ? 1 : 0, Unused);
        }

        /// <summary>How many phases is drawing done in? (Scintilla feature 2673)</summary>
        public int GetPhasesDraw()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPHASESDRAW, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// In one phase draw, text is drawn in a series of rectangular blocks with no overlap.
        /// In two phase draw, text is drawn in a series of lines allowing runs to overlap horizontally.
        /// In multiple phase draw, each element is drawn over the whole drawing area, allowing text
        /// to overlap from one line to the next.
        /// (Scintilla feature 2674)
        /// </summary>
        public void SetPhasesDraw(int phases)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETPHASESDRAW, phases, Unused);
        }

        /// <summary>Choose the quality level for text from the FontQuality enumeration. (Scintilla feature 2611)</summary>
        public void SetFontQuality(int fontQuality)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETFONTQUALITY, fontQuality, Unused);
        }

        /// <summary>Retrieve the quality level for text. (Scintilla feature 2612)</summary>
        public int GetFontQuality()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETFONTQUALITY, Unused, Unused);
            return (int) res;
        }

        /// <summary>Scroll so that a display line is at the top of the display. (Scintilla feature 2613)</summary>
        public void SetFirstVisibleLine(int lineDisplay)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETFIRSTVISIBLELINE, lineDisplay, Unused);
        }

        /// <summary>Change the effect of pasting when there are multiple selections. (Scintilla feature 2614)</summary>
        public void SetMultiPaste(int multiPaste)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMULTIPASTE, multiPaste, Unused);
        }

        /// <summary>Retrieve the effect of pasting when there are multiple selections.. (Scintilla feature 2615)</summary>
        public int GetMultiPaste()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMULTIPASTE, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Retrieve the value of a tag from a regular expression search.
        /// Result is NUL-terminated.
        /// (Scintilla feature 2616)
        /// </summary>
        public unsafe string GetTag(int tagNumber)
        {
            byte[] tagValueBuffer = new byte[10000];
            fixed (byte* tagValuePtr = tagValueBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTAG, tagNumber, (IntPtr) tagValuePtr);
                return Encoding.UTF8.GetString(tagValueBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Make the target range start and end be the same as the selection range start and end. (Scintilla feature 2287)</summary>
        public void TargetFromSelection()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_TARGETFROMSELECTION, Unused, Unused);
        }

        /// <summary>Join the lines in the target. (Scintilla feature 2288)</summary>
        public void LinesJoin()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINESJOIN, Unused, Unused);
        }

        /// <summary>
        /// Split the lines in the target into lines that are less wide than pixelWidth
        /// where possible.
        /// (Scintilla feature 2289)
        /// </summary>
        public void LinesSplit(int pixelWidth)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINESSPLIT, pixelWidth, Unused);
        }

        /// <summary>Set the colours used as a chequerboard pattern in the fold margin (Scintilla feature 2290)</summary>
        public void SetFoldMarginColour(bool useSetting, Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETFOLDMARGINCOLOUR, useSetting ? 1 : 0, back.Value);
        }

        /// <summary>Set the colours used as a chequerboard pattern in the fold margin (Scintilla feature 2291)</summary>
        public void SetFoldMarginHiColour(bool useSetting, Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETFOLDMARGINHICOLOUR, useSetting ? 1 : 0, fore.Value);
        }

        /// <summary>Move caret down one line. (Scintilla feature 2300)</summary>
        public void LineDown()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEDOWN, Unused, Unused);
        }

        /// <summary>Move caret down one line extending selection to new caret position. (Scintilla feature 2301)</summary>
        public void LineDownExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEDOWNEXTEND, Unused, Unused);
        }

        /// <summary>Move caret up one line. (Scintilla feature 2302)</summary>
        public void LineUp()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEUP, Unused, Unused);
        }

        /// <summary>Move caret up one line extending selection to new caret position. (Scintilla feature 2303)</summary>
        public void LineUpExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEUPEXTEND, Unused, Unused);
        }

        /// <summary>Move caret left one character. (Scintilla feature 2304)</summary>
        public void CharLeft()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHARLEFT, Unused, Unused);
        }

        /// <summary>Move caret left one character extending selection to new caret position. (Scintilla feature 2305)</summary>
        public void CharLeftExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHARLEFTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret right one character. (Scintilla feature 2306)</summary>
        public void CharRight()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHARRIGHT, Unused, Unused);
        }

        /// <summary>Move caret right one character extending selection to new caret position. (Scintilla feature 2307)</summary>
        public void CharRightExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHARRIGHTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret left one word. (Scintilla feature 2308)</summary>
        public void WordLeft()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDLEFT, Unused, Unused);
        }

        /// <summary>Move caret left one word extending selection to new caret position. (Scintilla feature 2309)</summary>
        public void WordLeftExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDLEFTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret right one word. (Scintilla feature 2310)</summary>
        public void WordRight()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDRIGHT, Unused, Unused);
        }

        /// <summary>Move caret right one word extending selection to new caret position. (Scintilla feature 2311)</summary>
        public void WordRightExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDRIGHTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret to first position on line. (Scintilla feature 2312)</summary>
        public void Home()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_HOME, Unused, Unused);
        }

        /// <summary>Move caret to first position on line extending selection to new caret position. (Scintilla feature 2313)</summary>
        public void HomeExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_HOMEEXTEND, Unused, Unused);
        }

        /// <summary>Move caret to last position on line. (Scintilla feature 2314)</summary>
        public void LineEnd()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEEND, Unused, Unused);
        }

        /// <summary>Move caret to last position on line extending selection to new caret position. (Scintilla feature 2315)</summary>
        public void LineEndExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEENDEXTEND, Unused, Unused);
        }

        /// <summary>Move caret to first position in document. (Scintilla feature 2316)</summary>
        public void DocumentStart()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DOCUMENTSTART, Unused, Unused);
        }

        /// <summary>Move caret to first position in document extending selection to new caret position. (Scintilla feature 2317)</summary>
        public void DocumentStartExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DOCUMENTSTARTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret to last position in document. (Scintilla feature 2318)</summary>
        public void DocumentEnd()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DOCUMENTEND, Unused, Unused);
        }

        /// <summary>Move caret to last position in document extending selection to new caret position. (Scintilla feature 2319)</summary>
        public void DocumentEndExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DOCUMENTENDEXTEND, Unused, Unused);
        }

        /// <summary>Move caret one page up. (Scintilla feature 2320)</summary>
        public void PageUp()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PAGEUP, Unused, Unused);
        }

        /// <summary>Move caret one page up extending selection to new caret position. (Scintilla feature 2321)</summary>
        public void PageUpExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PAGEUPEXTEND, Unused, Unused);
        }

        /// <summary>Move caret one page down. (Scintilla feature 2322)</summary>
        public void PageDown()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PAGEDOWN, Unused, Unused);
        }

        /// <summary>Move caret one page down extending selection to new caret position. (Scintilla feature 2323)</summary>
        public void PageDownExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PAGEDOWNEXTEND, Unused, Unused);
        }

        /// <summary>Switch from insert to overtype mode or the reverse. (Scintilla feature 2324)</summary>
        public void EditToggleOvertype()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_EDITTOGGLEOVERTYPE, Unused, Unused);
        }

        /// <summary>Cancel any modes such as call tip or auto-completion list display. (Scintilla feature 2325)</summary>
        public void Cancel()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CANCEL, Unused, Unused);
        }

        /// <summary>Delete the selection or if no selection, the character before the caret. (Scintilla feature 2326)</summary>
        public void DeleteBack()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DELETEBACK, Unused, Unused);
        }

        /// <summary>
        /// If selection is empty or all on one line replace the selection with a tab character.
        /// If more than one line selected, indent the lines.
        /// (Scintilla feature 2327)
        /// </summary>
        public void Tab()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_TAB, Unused, Unused);
        }

        /// <summary>Dedent the selected lines. (Scintilla feature 2328)</summary>
        public void BackTab()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_BACKTAB, Unused, Unused);
        }

        /// <summary>Insert a new line, may use a CRLF, CR or LF depending on EOL mode. (Scintilla feature 2329)</summary>
        public void NewLine()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_NEWLINE, Unused, Unused);
        }

        /// <summary>Insert a Form Feed character. (Scintilla feature 2330)</summary>
        public void FormFeed()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FORMFEED, Unused, Unused);
        }

        /// <summary>
        /// Move caret to before first visible character on line.
        /// If already there move to first character on line.
        /// (Scintilla feature 2331)
        /// </summary>
        public void VCHome()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_VCHOME, Unused, Unused);
        }

        /// <summary>Like VCHome but extending selection to new caret position. (Scintilla feature 2332)</summary>
        public void VCHomeExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_VCHOMEEXTEND, Unused, Unused);
        }

        /// <summary>Magnify the displayed text by increasing the sizes by 1 point. (Scintilla feature 2333)</summary>
        public void ZoomIn()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ZOOMIN, Unused, Unused);
        }

        /// <summary>Make the displayed text smaller by decreasing the sizes by 1 point. (Scintilla feature 2334)</summary>
        public void ZoomOut()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ZOOMOUT, Unused, Unused);
        }

        /// <summary>Delete the word to the left of the caret. (Scintilla feature 2335)</summary>
        public void DelWordLeft()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DELWORDLEFT, Unused, Unused);
        }

        /// <summary>Delete the word to the right of the caret. (Scintilla feature 2336)</summary>
        public void DelWordRight()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DELWORDRIGHT, Unused, Unused);
        }

        /// <summary>Delete the word to the right of the caret, but not the trailing non-word characters. (Scintilla feature 2518)</summary>
        public void DelWordRightEnd()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DELWORDRIGHTEND, Unused, Unused);
        }

        /// <summary>Cut the line containing the caret. (Scintilla feature 2337)</summary>
        public void LineCut()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINECUT, Unused, Unused);
        }

        /// <summary>Delete the line containing the caret. (Scintilla feature 2338)</summary>
        public void LineDelete()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEDELETE, Unused, Unused);
        }

        /// <summary>Switch the current line with the previous. (Scintilla feature 2339)</summary>
        public void LineTranspose()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINETRANSPOSE, Unused, Unused);
        }

        /// <summary>Duplicate the current line. (Scintilla feature 2404)</summary>
        public void LineDuplicate()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEDUPLICATE, Unused, Unused);
        }

        /// <summary>Transform the selection to lower case. (Scintilla feature 2340)</summary>
        public void LowerCase()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LOWERCASE, Unused, Unused);
        }

        /// <summary>Transform the selection to upper case. (Scintilla feature 2341)</summary>
        public void UpperCase()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_UPPERCASE, Unused, Unused);
        }

        /// <summary>Scroll the document down, keeping the caret visible. (Scintilla feature 2342)</summary>
        public void LineScrollDown()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINESCROLLDOWN, Unused, Unused);
        }

        /// <summary>Scroll the document up, keeping the caret visible. (Scintilla feature 2343)</summary>
        public void LineScrollUp()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINESCROLLUP, Unused, Unused);
        }

        /// <summary>
        /// Delete the selection or if no selection, the character before the caret.
        /// Will not delete the character before at the start of a line.
        /// (Scintilla feature 2344)
        /// </summary>
        public void DeleteBackNotLine()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DELETEBACKNOTLINE, Unused, Unused);
        }

        /// <summary>Move caret to first position on display line. (Scintilla feature 2345)</summary>
        public void HomeDisplay()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_HOMEDISPLAY, Unused, Unused);
        }

        /// <summary>
        /// Move caret to first position on display line extending selection to
        /// new caret position.
        /// (Scintilla feature 2346)
        /// </summary>
        public void HomeDisplayExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_HOMEDISPLAYEXTEND, Unused, Unused);
        }

        /// <summary>Move caret to last position on display line. (Scintilla feature 2347)</summary>
        public void LineEndDisplay()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEENDDISPLAY, Unused, Unused);
        }

        /// <summary>
        /// Move caret to last position on display line extending selection to new
        /// caret position.
        /// (Scintilla feature 2348)
        /// </summary>
        public void LineEndDisplayExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEENDDISPLAYEXTEND, Unused, Unused);
        }

        /// <summary>
        /// These are like their namesakes Home(Extend)?, LineEnd(Extend)?, VCHome(Extend)?
        /// except they behave differently when word-wrap is enabled:
        /// They go first to the start / end of the display line, like (Home|LineEnd)Display
        /// The difference is that, the cursor is already at the point, it goes on to the start
        /// or end of the document line, as appropriate for (Home|LineEnd|VCHome)(Extend)?.
        /// (Scintilla feature 2349)
        /// </summary>
        public void HomeWrap()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_HOMEWRAP, Unused, Unused);
        }

        /// <summary>
        /// These are like their namesakes Home(Extend)?, LineEnd(Extend)?, VCHome(Extend)?
        /// except they behave differently when word-wrap is enabled:
        /// They go first to the start / end of the display line, like (Home|LineEnd)Display
        /// The difference is that, the cursor is already at the point, it goes on to the start
        /// or end of the document line, as appropriate for (Home|LineEnd|VCHome)(Extend)?.
        /// (Scintilla feature 2450)
        /// </summary>
        public void HomeWrapExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_HOMEWRAPEXTEND, Unused, Unused);
        }

        /// <summary>
        /// These are like their namesakes Home(Extend)?, LineEnd(Extend)?, VCHome(Extend)?
        /// except they behave differently when word-wrap is enabled:
        /// They go first to the start / end of the display line, like (Home|LineEnd)Display
        /// The difference is that, the cursor is already at the point, it goes on to the start
        /// or end of the document line, as appropriate for (Home|LineEnd|VCHome)(Extend)?.
        /// (Scintilla feature 2451)
        /// </summary>
        public void LineEndWrap()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEENDWRAP, Unused, Unused);
        }

        /// <summary>
        /// These are like their namesakes Home(Extend)?, LineEnd(Extend)?, VCHome(Extend)?
        /// except they behave differently when word-wrap is enabled:
        /// They go first to the start / end of the display line, like (Home|LineEnd)Display
        /// The difference is that, the cursor is already at the point, it goes on to the start
        /// or end of the document line, as appropriate for (Home|LineEnd|VCHome)(Extend)?.
        /// (Scintilla feature 2452)
        /// </summary>
        public void LineEndWrapExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEENDWRAPEXTEND, Unused, Unused);
        }

        /// <summary>
        /// These are like their namesakes Home(Extend)?, LineEnd(Extend)?, VCHome(Extend)?
        /// except they behave differently when word-wrap is enabled:
        /// They go first to the start / end of the display line, like (Home|LineEnd)Display
        /// The difference is that, the cursor is already at the point, it goes on to the start
        /// or end of the document line, as appropriate for (Home|LineEnd|VCHome)(Extend)?.
        /// (Scintilla feature 2453)
        /// </summary>
        public void VCHomeWrap()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_VCHOMEWRAP, Unused, Unused);
        }

        /// <summary>
        /// These are like their namesakes Home(Extend)?, LineEnd(Extend)?, VCHome(Extend)?
        /// except they behave differently when word-wrap is enabled:
        /// They go first to the start / end of the display line, like (Home|LineEnd)Display
        /// The difference is that, the cursor is already at the point, it goes on to the start
        /// or end of the document line, as appropriate for (Home|LineEnd|VCHome)(Extend)?.
        /// (Scintilla feature 2454)
        /// </summary>
        public void VCHomeWrapExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_VCHOMEWRAPEXTEND, Unused, Unused);
        }

        /// <summary>Copy the line containing the caret. (Scintilla feature 2455)</summary>
        public void LineCopy()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINECOPY, Unused, Unused);
        }

        /// <summary>Move the caret inside current view if it's not there already. (Scintilla feature 2401)</summary>
        public void MoveCaretInsideView()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MOVECARETINSIDEVIEW, Unused, Unused);
        }

        /// <summary>How many characters are on a line, including end of line characters? (Scintilla feature 2350)</summary>
        public int LineLength(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINELENGTH, line, Unused);
            return (int) res;
        }

        /// <summary>Highlight the characters at two positions. (Scintilla feature 2351)</summary>
        public void BraceHighlight(Position pos1, Position pos2)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_BRACEHIGHLIGHT, pos1.Value, pos2.Value);
        }

        /// <summary>Use specified indicator to highlight matching braces instead of changing their style. (Scintilla feature 2498)</summary>
        public void BraceHighlightIndicator(bool useBraceHighlightIndicator, int indicator)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_BRACEHIGHLIGHTINDICATOR, useBraceHighlightIndicator ? 1 : 0, indicator);
        }

        /// <summary>Highlight the character at a position indicating there is no matching brace. (Scintilla feature 2352)</summary>
        public void BraceBadLight(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_BRACEBADLIGHT, pos.Value, Unused);
        }

        /// <summary>Use specified indicator to highlight non matching brace instead of changing its style. (Scintilla feature 2499)</summary>
        public void BraceBadLightIndicator(bool useBraceBadLightIndicator, int indicator)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_BRACEBADLIGHTINDICATOR, useBraceBadLightIndicator ? 1 : 0, indicator);
        }

        /// <summary>Find the position of a matching brace or INVALID_POSITION if no match. (Scintilla feature 2353)</summary>
        public Position BraceMatch(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_BRACEMATCH, pos.Value, Unused);
            return new Position((int) res);
        }

        /// <summary>Are the end of line characters visible? (Scintilla feature 2355)</summary>
        public bool GetViewEOL()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETVIEWEOL, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Make the end of line characters visible or invisible. (Scintilla feature 2356)</summary>
        public void SetViewEOL(bool visible)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETVIEWEOL, visible ? 1 : 0, Unused);
        }

        /// <summary>Retrieve a pointer to the document object. (Scintilla feature 2357)</summary>
        public IntPtr GetDocPointer()
        {
            return Win32.SendMessage(scintilla, SciMsg.SCI_GETDOCPOINTER, Unused, Unused);
        }

        /// <summary>Change the document object used. (Scintilla feature 2358)</summary>
        public void SetDocPointer(IntPtr pointer)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETDOCPOINTER, Unused, pointer);
        }

        /// <summary>Set which document modification events are sent to the container. (Scintilla feature 2359)</summary>
        public void SetModEventMask(int mask)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMODEVENTMASK, mask, Unused);
        }

        /// <summary>Retrieve the column number which text should be kept within. (Scintilla feature 2360)</summary>
        public int GetEdgeColumn()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETEDGECOLUMN, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Set the column number of the edge.
        /// If text goes past the edge then it is highlighted.
        /// (Scintilla feature 2361)
        /// </summary>
        public void SetEdgeColumn(int column)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETEDGECOLUMN, column, Unused);
        }

        /// <summary>Retrieve the edge highlight mode. (Scintilla feature 2362)</summary>
        public int GetEdgeMode()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETEDGEMODE, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// The edge may be displayed by a line (EDGE_LINE) or by highlighting text that
        /// goes beyond it (EDGE_BACKGROUND) or not displayed at all (EDGE_NONE).
        /// (Scintilla feature 2363)
        /// </summary>
        public void SetEdgeMode(int mode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETEDGEMODE, mode, Unused);
        }

        /// <summary>Retrieve the colour used in edge indication. (Scintilla feature 2364)</summary>
        public Colour GetEdgeColour()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETEDGECOLOUR, Unused, Unused);
            return new Colour((int) res);
        }

        /// <summary>Change the colour used in edge indication. (Scintilla feature 2365)</summary>
        public void SetEdgeColour(Colour edgeColour)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETEDGECOLOUR, edgeColour.Value, Unused);
        }

        /// <summary>Sets the current caret position to be the search anchor. (Scintilla feature 2366)</summary>
        public void SearchAnchor()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SEARCHANCHOR, Unused, Unused);
        }

        /// <summary>
        /// Find some text starting at the search anchor.
        /// Does not ensure the selection is visible.
        /// (Scintilla feature 2367)
        /// </summary>
        public unsafe int SearchNext(int flags, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SEARCHNEXT, flags, (IntPtr) textPtr);
                return (int) res;
            }
        }

        /// <summary>
        /// Find some text starting at the search anchor and moving backwards.
        /// Does not ensure the selection is visible.
        /// (Scintilla feature 2368)
        /// </summary>
        public unsafe int SearchPrev(int flags, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SEARCHPREV, flags, (IntPtr) textPtr);
                return (int) res;
            }
        }

        /// <summary>Retrieves the number of lines completely visible. (Scintilla feature 2370)</summary>
        public int LinesOnScreen()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINESONSCREEN, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Set whether a pop up menu is displayed automatically when the user presses
        /// the wrong mouse button.
        /// (Scintilla feature 2371)
        /// </summary>
        public void UsePopUp(bool allowPopUp)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_USEPOPUP, allowPopUp ? 1 : 0, Unused);
        }

        /// <summary>Is the selection rectangular? The alternative is the more common stream selection. (Scintilla feature 2372)</summary>
        public bool SelectionIsRectangle()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SELECTIONISRECTANGLE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>
        /// Set the zoom level. This number of points is added to the size of all fonts.
        /// It may be positive to magnify or negative to reduce.
        /// (Scintilla feature 2373)
        /// </summary>
        public void SetZoom(int zoom)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETZOOM, zoom, Unused);
        }

        /// <summary>Retrieve the zoom level. (Scintilla feature 2374)</summary>
        public int GetZoom()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETZOOM, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Create a new document object.
        /// Starts with reference count of 1 and not selected into editor.
        /// (Scintilla feature 2375)
        /// </summary>
        public int CreateDocument()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CREATEDOCUMENT, Unused, Unused);
            return (int) res;
        }

        /// <summary>Extend life of document. (Scintilla feature 2376)</summary>
        public void AddRefDocument(int doc)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ADDREFDOCUMENT, Unused, doc);
        }

        /// <summary>Release a reference to the document, deleting document if it fades to black. (Scintilla feature 2377)</summary>
        public void ReleaseDocument(int doc)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_RELEASEDOCUMENT, Unused, doc);
        }

        /// <summary>Get which document modification events are sent to the container. (Scintilla feature 2378)</summary>
        public int GetModEventMask()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMODEVENTMASK, Unused, Unused);
            return (int) res;
        }

        /// <summary>Change internal focus flag. (Scintilla feature 2380)</summary>
        public void SetFocus(bool focus)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETFOCUS, focus ? 1 : 0, Unused);
        }

        /// <summary>Get internal focus flag. (Scintilla feature 2381)</summary>
        public bool GetFocus()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETFOCUS, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Change error status - 0 = OK. (Scintilla feature 2382)</summary>
        public void SetStatus(int statusCode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSTATUS, statusCode, Unused);
        }

        /// <summary>Get error status. (Scintilla feature 2383)</summary>
        public int GetStatus()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSTATUS, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set whether the mouse is captured when its button is pressed. (Scintilla feature 2384)</summary>
        public void SetMouseDownCaptures(bool captures)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMOUSEDOWNCAPTURES, captures ? 1 : 0, Unused);
        }

        /// <summary>Get whether mouse gets captured. (Scintilla feature 2385)</summary>
        public bool GetMouseDownCaptures()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMOUSEDOWNCAPTURES, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Sets the cursor to one of the SC_CURSOR* values. (Scintilla feature 2386)</summary>
        public void SetCursor(int cursorType)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCURSOR, cursorType, Unused);
        }

        /// <summary>Get cursor type. (Scintilla feature 2387)</summary>
        public int GetCursor()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCURSOR, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Change the way control characters are displayed:
        /// If symbol is < 32, keep the drawn way, else, use the given character.
        /// (Scintilla feature 2388)
        /// </summary>
        public void SetControlCharSymbol(int symbol)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCONTROLCHARSYMBOL, symbol, Unused);
        }

        /// <summary>Get the way control characters are displayed. (Scintilla feature 2389)</summary>
        public int GetControlCharSymbol()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCONTROLCHARSYMBOL, Unused, Unused);
            return (int) res;
        }

        /// <summary>Move to the previous change in capitalisation. (Scintilla feature 2390)</summary>
        public void WordPartLeft()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDPARTLEFT, Unused, Unused);
        }

        /// <summary>
        /// Move to the previous change in capitalisation extending selection
        /// to new caret position.
        /// (Scintilla feature 2391)
        /// </summary>
        public void WordPartLeftExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDPARTLEFTEXTEND, Unused, Unused);
        }

        /// <summary>Move to the change next in capitalisation. (Scintilla feature 2392)</summary>
        public void WordPartRight()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDPARTRIGHT, Unused, Unused);
        }

        /// <summary>
        /// Move to the next change in capitalisation extending selection
        /// to new caret position.
        /// (Scintilla feature 2393)
        /// </summary>
        public void WordPartRightExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDPARTRIGHTEXTEND, Unused, Unused);
        }

        /// <summary>
        /// Set the way the display area is determined when a particular line
        /// is to be moved to by Find, FindNext, GotoLine, etc.
        /// (Scintilla feature 2394)
        /// </summary>
        public void SetVisiblePolicy(int visiblePolicy, int visibleSlop)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETVISIBLEPOLICY, visiblePolicy, visibleSlop);
        }

        /// <summary>Delete back from the current position to the start of the line. (Scintilla feature 2395)</summary>
        public void DelLineLeft()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DELLINELEFT, Unused, Unused);
        }

        /// <summary>Delete forwards from the current position to the end of the line. (Scintilla feature 2396)</summary>
        public void DelLineRight()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DELLINERIGHT, Unused, Unused);
        }

        /// <summary>Get and Set the xOffset (ie, horizontal scroll position). (Scintilla feature 2397)</summary>
        public void SetXOffset(int newOffset)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETXOFFSET, newOffset, Unused);
        }

        /// <summary>Get and Set the xOffset (ie, horizontal scroll position). (Scintilla feature 2398)</summary>
        public int GetXOffset()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETXOFFSET, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the last x chosen value to be the caret x position. (Scintilla feature 2399)</summary>
        public void ChooseCaretX()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHOOSECARETX, Unused, Unused);
        }

        /// <summary>Set the focus to this Scintilla widget. (Scintilla feature 2400)</summary>
        public void GrabFocus()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GRABFOCUS, Unused, Unused);
        }

        /// <summary>
        /// Set the way the caret is kept visible when going sideways.
        /// The exclusion zone is given in pixels.
        /// (Scintilla feature 2402)
        /// </summary>
        public void SetXCaretPolicy(int caretPolicy, int caretSlop)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETXCARETPOLICY, caretPolicy, caretSlop);
        }

        /// <summary>
        /// Set the way the line the caret is on is kept visible.
        /// The exclusion zone is given in lines.
        /// (Scintilla feature 2403)
        /// </summary>
        public void SetYCaretPolicy(int caretPolicy, int caretSlop)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETYCARETPOLICY, caretPolicy, caretSlop);
        }

        /// <summary>Set printing to line wrapped (SC_WRAP_WORD) or not line wrapped (SC_WRAP_NONE). (Scintilla feature 2406)</summary>
        public void SetPrintWrapMode(int mode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETPRINTWRAPMODE, mode, Unused);
        }

        /// <summary>Is printing line wrapped? (Scintilla feature 2407)</summary>
        public int GetPrintWrapMode()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPRINTWRAPMODE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set a fore colour for active hotspots. (Scintilla feature 2410)</summary>
        public void SetHotspotActiveFore(bool useSetting, Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETHOTSPOTACTIVEFORE, useSetting ? 1 : 0, fore.Value);
        }

        /// <summary>Get the fore colour for active hotspots. (Scintilla feature 2494)</summary>
        public Colour GetHotspotActiveFore()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETHOTSPOTACTIVEFORE, Unused, Unused);
            return new Colour((int) res);
        }

        /// <summary>Set a back colour for active hotspots. (Scintilla feature 2411)</summary>
        public void SetHotspotActiveBack(bool useSetting, Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETHOTSPOTACTIVEBACK, useSetting ? 1 : 0, back.Value);
        }

        /// <summary>Get the back colour for active hotspots. (Scintilla feature 2495)</summary>
        public Colour GetHotspotActiveBack()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETHOTSPOTACTIVEBACK, Unused, Unused);
            return new Colour((int) res);
        }

        /// <summary>Enable / Disable underlining active hotspots. (Scintilla feature 2412)</summary>
        public void SetHotspotActiveUnderline(bool underline)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, underline ? 1 : 0, Unused);
        }

        /// <summary>Get whether underlining for active hotspots. (Scintilla feature 2496)</summary>
        public bool GetHotspotActiveUnderline()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETHOTSPOTACTIVEUNDERLINE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Limit hotspots to single line so hotspots on two lines don't merge. (Scintilla feature 2421)</summary>
        public void SetHotspotSingleLine(bool singleLine)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETHOTSPOTSINGLELINE, singleLine ? 1 : 0, Unused);
        }

        /// <summary>Get the HotspotSingleLine property (Scintilla feature 2497)</summary>
        public bool GetHotspotSingleLine()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETHOTSPOTSINGLELINE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Move caret between paragraphs (delimited by empty lines). (Scintilla feature 2413)</summary>
        public void ParaDown()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PARADOWN, Unused, Unused);
        }

        /// <summary>Move caret between paragraphs (delimited by empty lines). (Scintilla feature 2414)</summary>
        public void ParaDownExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PARADOWNEXTEND, Unused, Unused);
        }

        /// <summary>Move caret between paragraphs (delimited by empty lines). (Scintilla feature 2415)</summary>
        public void ParaUp()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PARAUP, Unused, Unused);
        }

        /// <summary>Move caret between paragraphs (delimited by empty lines). (Scintilla feature 2416)</summary>
        public void ParaUpExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PARAUPEXTEND, Unused, Unused);
        }

        /// <summary>
        /// Given a valid document position, return the previous position taking code
        /// page into account. Returns 0 if passed 0.
        /// (Scintilla feature 2417)
        /// </summary>
        public Position PositionBefore(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_POSITIONBEFORE, pos.Value, Unused);
            return new Position((int) res);
        }

        /// <summary>
        /// Given a valid document position, return the next position taking code
        /// page into account. Maximum value returned is the last position in the document.
        /// (Scintilla feature 2418)
        /// </summary>
        public Position PositionAfter(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_POSITIONAFTER, pos.Value, Unused);
            return new Position((int) res);
        }

        /// <summary>
        /// Given a valid document position, return a position that differs in a number
        /// of characters. Returned value is always between 0 and last position in document.
        /// (Scintilla feature 2670)
        /// </summary>
        public Position PositionRelative(Position pos, int relative)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_POSITIONRELATIVE, pos.Value, relative);
            return new Position((int) res);
        }

        /// <summary>Copy a range of text to the clipboard. Positions are clipped into the document. (Scintilla feature 2419)</summary>
        public void CopyRange(Position start, Position end)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_COPYRANGE, start.Value, end.Value);
        }

        /// <summary>Copy argument text to the clipboard. (Scintilla feature 2420)</summary>
        public unsafe void CopyText(int length, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_COPYTEXT, length, (IntPtr) textPtr);
            }
        }

        /// <summary>
        /// Set the selection mode to stream (SC_SEL_STREAM) or rectangular (SC_SEL_RECTANGLE/SC_SEL_THIN) or
        /// by lines (SC_SEL_LINES).
        /// (Scintilla feature 2422)
        /// </summary>
        public void SetSelectionMode(int mode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTIONMODE, mode, Unused);
        }

        /// <summary>Get the mode of the current selection. (Scintilla feature 2423)</summary>
        public int GetSelectionMode()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONMODE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Retrieve the position of the start of the selection at the given line (INVALID_POSITION if no selection on this line). (Scintilla feature 2424)</summary>
        public Position GetLineSelStartPosition(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINESELSTARTPOSITION, line, Unused);
            return new Position((int) res);
        }

        /// <summary>Retrieve the position of the end of the selection at the given line (INVALID_POSITION if no selection on this line). (Scintilla feature 2425)</summary>
        public Position GetLineSelEndPosition(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINESELENDPOSITION, line, Unused);
            return new Position((int) res);
        }

        /// <summary>Move caret down one line, extending rectangular selection to new caret position. (Scintilla feature 2426)</summary>
        public void LineDownRectExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEDOWNRECTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret up one line, extending rectangular selection to new caret position. (Scintilla feature 2427)</summary>
        public void LineUpRectExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEUPRECTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret left one character, extending rectangular selection to new caret position. (Scintilla feature 2428)</summary>
        public void CharLeftRectExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHARLEFTRECTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret right one character, extending rectangular selection to new caret position. (Scintilla feature 2429)</summary>
        public void CharRightRectExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHARRIGHTRECTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret to first position on line, extending rectangular selection to new caret position. (Scintilla feature 2430)</summary>
        public void HomeRectExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_HOMERECTEXTEND, Unused, Unused);
        }

        /// <summary>
        /// Move caret to before first visible character on line.
        /// If already there move to first character on line.
        /// In either case, extend rectangular selection to new caret position.
        /// (Scintilla feature 2431)
        /// </summary>
        public void VCHomeRectExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_VCHOMERECTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret to last position on line, extending rectangular selection to new caret position. (Scintilla feature 2432)</summary>
        public void LineEndRectExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LINEENDRECTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret one page up, extending rectangular selection to new caret position. (Scintilla feature 2433)</summary>
        public void PageUpRectExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PAGEUPRECTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret one page down, extending rectangular selection to new caret position. (Scintilla feature 2434)</summary>
        public void PageDownRectExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PAGEDOWNRECTEXTEND, Unused, Unused);
        }

        /// <summary>Move caret to top of page, or one page up if already at top of page. (Scintilla feature 2435)</summary>
        public void StutteredPageUp()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STUTTEREDPAGEUP, Unused, Unused);
        }

        /// <summary>Move caret to top of page, or one page up if already at top of page, extending selection to new caret position. (Scintilla feature 2436)</summary>
        public void StutteredPageUpExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STUTTEREDPAGEUPEXTEND, Unused, Unused);
        }

        /// <summary>Move caret to bottom of page, or one page down if already at bottom of page. (Scintilla feature 2437)</summary>
        public void StutteredPageDown()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STUTTEREDPAGEDOWN, Unused, Unused);
        }

        /// <summary>Move caret to bottom of page, or one page down if already at bottom of page, extending selection to new caret position. (Scintilla feature 2438)</summary>
        public void StutteredPageDownExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STUTTEREDPAGEDOWNEXTEND, Unused, Unused);
        }

        /// <summary>Move caret left one word, position cursor at end of word. (Scintilla feature 2439)</summary>
        public void WordLeftEnd()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDLEFTEND, Unused, Unused);
        }

        /// <summary>Move caret left one word, position cursor at end of word, extending selection to new caret position. (Scintilla feature 2440)</summary>
        public void WordLeftEndExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDLEFTENDEXTEND, Unused, Unused);
        }

        /// <summary>Move caret right one word, position cursor at end of word. (Scintilla feature 2441)</summary>
        public void WordRightEnd()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDRIGHTEND, Unused, Unused);
        }

        /// <summary>Move caret right one word, position cursor at end of word, extending selection to new caret position. (Scintilla feature 2442)</summary>
        public void WordRightEndExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_WORDRIGHTENDEXTEND, Unused, Unused);
        }

        /// <summary>
        /// Set the set of characters making up whitespace for when moving or selecting by word.
        /// Should be called after SetWordChars.
        /// (Scintilla feature 2443)
        /// </summary>
        public unsafe void SetWhitespaceChars(string characters)
        {
            fixed (byte* charactersPtr = Encoding.UTF8.GetBytes(characters))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETWHITESPACECHARS, Unused, (IntPtr) charactersPtr);
            }
        }

        /// <summary>Get the set of characters making up whitespace for when moving or selecting by word. (Scintilla feature 2647)</summary>
        public unsafe string GetWhitespaceChars()
        {
            byte[] charactersBuffer = new byte[10000];
            fixed (byte* charactersPtr = charactersBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETWHITESPACECHARS, Unused, (IntPtr) charactersPtr);
                return Encoding.UTF8.GetString(charactersBuffer).TrimEnd('\0');
            }
        }

        /// <summary>
        /// Set the set of characters making up punctuation characters
        /// Should be called after SetWordChars.
        /// (Scintilla feature 2648)
        /// </summary>
        public unsafe void SetPunctuationChars(string characters)
        {
            fixed (byte* charactersPtr = Encoding.UTF8.GetBytes(characters))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETPUNCTUATIONCHARS, Unused, (IntPtr) charactersPtr);
            }
        }

        /// <summary>Get the set of characters making up punctuation characters (Scintilla feature 2649)</summary>
        public unsafe string GetPunctuationChars()
        {
            byte[] charactersBuffer = new byte[10000];
            fixed (byte* charactersPtr = charactersBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPUNCTUATIONCHARS, Unused, (IntPtr) charactersPtr);
                return Encoding.UTF8.GetString(charactersBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Reset the set of characters for whitespace and word characters to the defaults. (Scintilla feature 2444)</summary>
        public void SetCharsDefault()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCHARSDEFAULT, Unused, Unused);
        }

        /// <summary>Get currently selected item position in the auto-completion list (Scintilla feature 2445)</summary>
        public int AutoCGetCurrent()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETCURRENT, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Get currently selected item text in the auto-completion list
        /// Returns the length of the item text
        /// Result is NUL-terminated.
        /// (Scintilla feature 2610)
        /// </summary>
        public unsafe string AutoCGetCurrentText()
        {
            byte[] sBuffer = new byte[10000];
            fixed (byte* sPtr = sBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETCURRENTTEXT, Unused, (IntPtr) sPtr);
                return Encoding.UTF8.GetString(sBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Set auto-completion case insensitive behaviour to either prefer case-sensitive matches or have no preference. (Scintilla feature 2634)</summary>
        public void AutoCSetCaseInsensitiveBehaviour(int behaviour)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETCASEINSENSITIVEBEHAVIOUR, behaviour, Unused);
        }

        /// <summary>Get auto-completion case insensitive behaviour. (Scintilla feature 2635)</summary>
        public int AutoCGetCaseInsensitiveBehaviour()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETCASEINSENSITIVEBEHAVIOUR, Unused, Unused);
            return (int) res;
        }

        /// <summary>Change the effect of autocompleting when there are multiple selections. (Scintilla feature 2636)</summary>
        public void AutoCSetMulti(int multi)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETMULTI, multi, Unused);
        }

        /// <summary>Retrieve the effect of autocompleting when there are multiple selections.. (Scintilla feature 2637)</summary>
        public int AutoCGetMulti()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETMULTI, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the way autocompletion lists are ordered. (Scintilla feature 2660)</summary>
        public void AutoCSetOrder(int order)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETORDER, order, Unused);
        }

        /// <summary>Get the way autocompletion lists are ordered. (Scintilla feature 2661)</summary>
        public int AutoCGetOrder()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCGETORDER, Unused, Unused);
            return (int) res;
        }

        /// <summary>Enlarge the document to a particular size of text bytes. (Scintilla feature 2446)</summary>
        public void Allocate(int bytes)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ALLOCATE, bytes, Unused);
        }

        /// <summary>
        /// Returns the target converted to UTF8.
        /// Return the length in bytes.
        /// (Scintilla feature 2447)
        /// </summary>
        public unsafe string TargetAsUTF8()
        {
            byte[] sBuffer = new byte[10000];
            fixed (byte* sPtr = sBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_TARGETASUTF8, Unused, (IntPtr) sPtr);
                return Encoding.UTF8.GetString(sBuffer).TrimEnd('\0');
            }
        }

        /// <summary>
        /// Set the length of the utf8 argument for calling EncodedFromUTF8.
        /// Set to -1 and the string will be measured to the first nul.
        /// (Scintilla feature 2448)
        /// </summary>
        public void SetLengthForEncode(int bytes)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETLENGTHFORENCODE, bytes, Unused);
        }

        /// <summary>
        /// Translates a UTF8 string into the document encoding.
        /// Return the length of the result in bytes.
        /// On error return 0.
        /// (Scintilla feature 2449)
        /// </summary>
        public unsafe string EncodedFromUTF8(string utf8)
        {
            fixed (byte* utf8Ptr = Encoding.UTF8.GetBytes(utf8))
            {
                byte[] encodedBuffer = new byte[10000];
                fixed (byte* encodedPtr = encodedBuffer)
                {
                    IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ENCODEDFROMUTF8, (IntPtr) utf8Ptr, (IntPtr) encodedPtr);
                    return Encoding.UTF8.GetString(encodedBuffer).TrimEnd('\0');
                }
            }
        }

        /// <summary>
        /// Find the position of a column on a line taking into account tabs and
        /// multi-byte characters. If beyond end of line, return line end position.
        /// (Scintilla feature 2456)
        /// </summary>
        public int FindColumn(int line, int column)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FINDCOLUMN, line, column);
            return (int) res;
        }

        /// <summary>Can the caret preferred x position only be changed by explicit movement commands? (Scintilla feature 2457)</summary>
        public int GetCaretSticky()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCARETSTICKY, Unused, Unused);
            return (int) res;
        }

        /// <summary>Stop the caret preferred x position changing when the user types. (Scintilla feature 2458)</summary>
        public void SetCaretSticky(int useCaretStickyBehaviour)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCARETSTICKY, useCaretStickyBehaviour, Unused);
        }

        /// <summary>Switch between sticky and non-sticky: meant to be bound to a key. (Scintilla feature 2459)</summary>
        public void ToggleCaretSticky()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_TOGGLECARETSTICKY, Unused, Unused);
        }

        /// <summary>Enable/Disable convert-on-paste for line endings (Scintilla feature 2467)</summary>
        public void SetPasteConvertEndings(bool convert)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETPASTECONVERTENDINGS, convert ? 1 : 0, Unused);
        }

        /// <summary>Get convert-on-paste setting (Scintilla feature 2468)</summary>
        public bool GetPasteConvertEndings()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPASTECONVERTENDINGS, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Duplicate the selection. If selection empty duplicate the line containing the caret. (Scintilla feature 2469)</summary>
        public void SelectionDuplicate()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SELECTIONDUPLICATE, Unused, Unused);
        }

        /// <summary>Set background alpha of the caret line. (Scintilla feature 2470)</summary>
        public void SetCaretLineBackAlpha(int alpha)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCARETLINEBACKALPHA, alpha, Unused);
        }

        /// <summary>Get the background alpha of the caret line. (Scintilla feature 2471)</summary>
        public int GetCaretLineBackAlpha()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCARETLINEBACKALPHA, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the style of the caret to be drawn. (Scintilla feature 2512)</summary>
        public void SetCaretStyle(int caretStyle)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCARETSTYLE, caretStyle, Unused);
        }

        /// <summary>Returns the current style of the caret. (Scintilla feature 2513)</summary>
        public int GetCaretStyle()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCARETSTYLE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the indicator used for IndicatorFillRange and IndicatorClearRange (Scintilla feature 2500)</summary>
        public void SetIndicatorCurrent(int indicator)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETINDICATORCURRENT, indicator, Unused);
        }

        /// <summary>Get the current indicator (Scintilla feature 2501)</summary>
        public int GetIndicatorCurrent()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETINDICATORCURRENT, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the value used for IndicatorFillRange (Scintilla feature 2502)</summary>
        public void SetIndicatorValue(int value)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETINDICATORVALUE, value, Unused);
        }

        /// <summary>Get the current indicator value (Scintilla feature 2503)</summary>
        public int GetIndicatorValue()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETINDICATORVALUE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Turn a indicator on over a range. (Scintilla feature 2504)</summary>
        public void IndicatorFillRange(int position, int fillLength)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICATORFILLRANGE, position, fillLength);
        }

        /// <summary>Turn a indicator off over a range. (Scintilla feature 2505)</summary>
        public void IndicatorClearRange(int position, int clearLength)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICATORCLEARRANGE, position, clearLength);
        }

        /// <summary>Are any indicators present at position? (Scintilla feature 2506)</summary>
        public int IndicatorAllOnFor(int position)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICATORALLONFOR, position, Unused);
            return (int) res;
        }

        /// <summary>What value does a particular indicator have at at a position? (Scintilla feature 2507)</summary>
        public int IndicatorValueAt(int indicator, int position)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICATORVALUEAT, indicator, position);
            return (int) res;
        }

        /// <summary>Where does a particular indicator start? (Scintilla feature 2508)</summary>
        public int IndicatorStart(int indicator, int position)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICATORSTART, indicator, position);
            return (int) res;
        }

        /// <summary>Where does a particular indicator end? (Scintilla feature 2509)</summary>
        public int IndicatorEnd(int indicator, int position)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICATOREND, indicator, position);
            return (int) res;
        }

        /// <summary>Set number of entries in position cache (Scintilla feature 2514)</summary>
        public void SetPositionCache(int size)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETPOSITIONCACHE, size, Unused);
        }

        /// <summary>How many entries are allocated to the position cache? (Scintilla feature 2515)</summary>
        public int GetPositionCache()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPOSITIONCACHE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Copy the selection, if selection empty copy the line with the caret (Scintilla feature 2519)</summary>
        public void CopyAllowLine()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_COPYALLOWLINE, Unused, Unused);
        }

        /// <summary>
        /// Compact the document buffer and return a read-only pointer to the
        /// characters in the document.
        /// (Scintilla feature 2520)
        /// </summary>
        public IntPtr GetCharacterPointer()
        {
            return Win32.SendMessage(scintilla, SciMsg.SCI_GETCHARACTERPOINTER, Unused, Unused);
        }

        /// <summary>
        /// Return a read-only pointer to a range of characters in the document.
        /// May move the gap so that the range is contiguous, but will only move up
        /// to rangeLength bytes.
        /// (Scintilla feature 2643)
        /// </summary>
        public IntPtr GetRangePointer(int position, int rangeLength)
        {
            return Win32.SendMessage(scintilla, SciMsg.SCI_GETRANGEPOINTER, position, rangeLength);
        }

        /// <summary>
        /// Return a position which, to avoid performance costs, should not be within
        /// the range of a call to GetRangePointer.
        /// (Scintilla feature 2644)
        /// </summary>
        public Position GetGapPosition()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETGAPPOSITION, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Set the alpha fill colour of the given indicator. (Scintilla feature 2523)</summary>
        public void IndicSetAlpha(int indicator, int alpha)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETALPHA, indicator, alpha);
        }

        /// <summary>Get the alpha fill colour of the given indicator. (Scintilla feature 2524)</summary>
        public int IndicGetAlpha(int indicator)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICGETALPHA, indicator, Unused);
            return (int) res;
        }

        /// <summary>Set the alpha outline colour of the given indicator. (Scintilla feature 2558)</summary>
        public void IndicSetOutlineAlpha(int indicator, int alpha)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETOUTLINEALPHA, indicator, alpha);
        }

        /// <summary>Get the alpha outline colour of the given indicator. (Scintilla feature 2559)</summary>
        public int IndicGetOutlineAlpha(int indicator)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_INDICGETOUTLINEALPHA, indicator, Unused);
            return (int) res;
        }

        /// <summary>Set extra ascent for each line (Scintilla feature 2525)</summary>
        public void SetExtraAscent(int extraAscent)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETEXTRAASCENT, extraAscent, Unused);
        }

        /// <summary>Get extra ascent for each line (Scintilla feature 2526)</summary>
        public int GetExtraAscent()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETEXTRAASCENT, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set extra descent for each line (Scintilla feature 2527)</summary>
        public void SetExtraDescent(int extraDescent)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETEXTRADESCENT, extraDescent, Unused);
        }

        /// <summary>Get extra descent for each line (Scintilla feature 2528)</summary>
        public int GetExtraDescent()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETEXTRADESCENT, Unused, Unused);
            return (int) res;
        }

        /// <summary>Which symbol was defined for markerNumber with MarkerDefine (Scintilla feature 2529)</summary>
        public int MarkerSymbolDefined(int markerNumber)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERSYMBOLDEFINED, markerNumber, Unused);
            return (int) res;
        }

        /// <summary>Set the text in the text margin for a line (Scintilla feature 2530)</summary>
        public unsafe void MarginSetText(int line, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARGINSETTEXT, line, (IntPtr) textPtr);
            }
        }

        /// <summary>Get the text in the text margin for a line (Scintilla feature 2531)</summary>
        public unsafe string MarginGetText(int line)
        {
            byte[] textBuffer = new byte[10000];
            fixed (byte* textPtr = textBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARGINGETTEXT, line, (IntPtr) textPtr);
                return Encoding.UTF8.GetString(textBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Set the style number for the text margin for a line (Scintilla feature 2532)</summary>
        public void MarginSetStyle(int line, int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARGINSETSTYLE, line, style);
        }

        /// <summary>Get the style number for the text margin for a line (Scintilla feature 2533)</summary>
        public int MarginGetStyle(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARGINGETSTYLE, line, Unused);
            return (int) res;
        }

        /// <summary>Set the style in the text margin for a line (Scintilla feature 2534)</summary>
        public unsafe void MarginSetStyles(int line, string styles)
        {
            fixed (byte* stylesPtr = Encoding.UTF8.GetBytes(styles))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARGINSETSTYLES, line, (IntPtr) stylesPtr);
            }
        }

        /// <summary>Get the styles in the text margin for a line (Scintilla feature 2535)</summary>
        public unsafe string MarginGetStyles(int line)
        {
            byte[] stylesBuffer = new byte[10000];
            fixed (byte* stylesPtr = stylesBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARGINGETSTYLES, line, (IntPtr) stylesPtr);
                return Encoding.UTF8.GetString(stylesBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Clear the margin text on all lines (Scintilla feature 2536)</summary>
        public void MarginTextClearAll()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARGINTEXTCLEARALL, Unused, Unused);
        }

        /// <summary>Get the start of the range of style numbers used for margin text (Scintilla feature 2537)</summary>
        public void MarginSetStyleOffset(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARGINSETSTYLEOFFSET, style, Unused);
        }

        /// <summary>Get the start of the range of style numbers used for margin text (Scintilla feature 2538)</summary>
        public int MarginGetStyleOffset()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARGINGETSTYLEOFFSET, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the margin options. (Scintilla feature 2539)</summary>
        public void SetMarginOptions(int marginOptions)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMARGINOPTIONS, marginOptions, Unused);
        }

        /// <summary>Get the margin options. (Scintilla feature 2557)</summary>
        public int GetMarginOptions()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMARGINOPTIONS, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the annotation text for a line (Scintilla feature 2540)</summary>
        public unsafe void AnnotationSetText(int line, string text)
        {
            fixed (byte* textPtr = Encoding.UTF8.GetBytes(text))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONSETTEXT, line, (IntPtr) textPtr);
            }
        }

        /// <summary>Get the annotation text for a line (Scintilla feature 2541)</summary>
        public unsafe string AnnotationGetText(int line)
        {
            byte[] textBuffer = new byte[10000];
            fixed (byte* textPtr = textBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONGETTEXT, line, (IntPtr) textPtr);
                return Encoding.UTF8.GetString(textBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Set the style number for the annotations for a line (Scintilla feature 2542)</summary>
        public void AnnotationSetStyle(int line, int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONSETSTYLE, line, style);
        }

        /// <summary>Get the style number for the annotations for a line (Scintilla feature 2543)</summary>
        public int AnnotationGetStyle(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONGETSTYLE, line, Unused);
            return (int) res;
        }

        /// <summary>Set the annotation styles for a line (Scintilla feature 2544)</summary>
        public unsafe void AnnotationSetStyles(int line, string styles)
        {
            fixed (byte* stylesPtr = Encoding.UTF8.GetBytes(styles))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONSETSTYLES, line, (IntPtr) stylesPtr);
            }
        }

        /// <summary>Get the annotation styles for a line (Scintilla feature 2545)</summary>
        public unsafe string AnnotationGetStyles(int line)
        {
            byte[] stylesBuffer = new byte[10000];
            fixed (byte* stylesPtr = stylesBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONGETSTYLES, line, (IntPtr) stylesPtr);
                return Encoding.UTF8.GetString(stylesBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Get the number of annotation lines for a line (Scintilla feature 2546)</summary>
        public int AnnotationGetLines(int line)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONGETLINES, line, Unused);
            return (int) res;
        }

        /// <summary>Clear the annotations from all lines (Scintilla feature 2547)</summary>
        public void AnnotationClearAll()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONCLEARALL, Unused, Unused);
        }

        /// <summary>Set the visibility for the annotations for a view (Scintilla feature 2548)</summary>
        public void AnnotationSetVisible(int visible)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONSETVISIBLE, visible, Unused);
        }

        /// <summary>Get the visibility for the annotations for a view (Scintilla feature 2549)</summary>
        public int AnnotationGetVisible()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONGETVISIBLE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Get the start of the range of style numbers used for annotations (Scintilla feature 2550)</summary>
        public void AnnotationSetStyleOffset(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONSETSTYLEOFFSET, style, Unused);
        }

        /// <summary>Get the start of the range of style numbers used for annotations (Scintilla feature 2551)</summary>
        public int AnnotationGetStyleOffset()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ANNOTATIONGETSTYLEOFFSET, Unused, Unused);
            return (int) res;
        }

        /// <summary>Release all extended (>255) style numbers (Scintilla feature 2552)</summary>
        public void ReleaseAllExtendedStyles()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_RELEASEALLEXTENDEDSTYLES, Unused, Unused);
        }

        /// <summary>Allocate some extended (>255) style numbers and return the start of the range (Scintilla feature 2553)</summary>
        public int AllocateExtendedStyles(int numberStyles)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ALLOCATEEXTENDEDSTYLES, numberStyles, Unused);
            return (int) res;
        }

        /// <summary>Add a container action to the undo stack (Scintilla feature 2560)</summary>
        public void AddUndoAction(int token, int flags)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ADDUNDOACTION, token, flags);
        }

        /// <summary>Find the position of a character from a point within the window. (Scintilla feature 2561)</summary>
        public Position CharPositionFromPoint(int x, int y)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHARPOSITIONFROMPOINT, x, y);
            return new Position((int) res);
        }

        /// <summary>
        /// Find the position of a character from a point within the window.
        /// Return INVALID_POSITION if not close to text.
        /// (Scintilla feature 2562)
        /// </summary>
        public Position CharPositionFromPointClose(int x, int y)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHARPOSITIONFROMPOINTCLOSE, x, y);
            return new Position((int) res);
        }

        /// <summary>Set whether switching to rectangular mode while selecting with the mouse is allowed. (Scintilla feature 2668)</summary>
        public void SetMouseSelectionRectangularSwitch(bool mouseSelectionRectangularSwitch)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMOUSESELECTIONRECTANGULARSWITCH, mouseSelectionRectangularSwitch ? 1 : 0, Unused);
        }

        /// <summary>Whether switching to rectangular mode while selecting with the mouse is allowed. (Scintilla feature 2669)</summary>
        public bool GetMouseSelectionRectangularSwitch()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMOUSESELECTIONRECTANGULARSWITCH, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set whether multiple selections can be made (Scintilla feature 2563)</summary>
        public void SetMultipleSelection(bool multipleSelection)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMULTIPLESELECTION, multipleSelection ? 1 : 0, Unused);
        }

        /// <summary>Whether multiple selections can be made (Scintilla feature 2564)</summary>
        public bool GetMultipleSelection()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMULTIPLESELECTION, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set whether typing can be performed into multiple selections (Scintilla feature 2565)</summary>
        public void SetAdditionalSelectionTyping(bool additionalSelectionTyping)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETADDITIONALSELECTIONTYPING, additionalSelectionTyping ? 1 : 0, Unused);
        }

        /// <summary>Whether typing can be performed into multiple selections (Scintilla feature 2566)</summary>
        public bool GetAdditionalSelectionTyping()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETADDITIONALSELECTIONTYPING, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set whether additional carets will blink (Scintilla feature 2567)</summary>
        public void SetAdditionalCaretsBlink(bool additionalCaretsBlink)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETADDITIONALCARETSBLINK, additionalCaretsBlink ? 1 : 0, Unused);
        }

        /// <summary>Whether additional carets will blink (Scintilla feature 2568)</summary>
        public bool GetAdditionalCaretsBlink()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETADDITIONALCARETSBLINK, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Set whether additional carets are visible (Scintilla feature 2608)</summary>
        public void SetAdditionalCaretsVisible(bool additionalCaretsBlink)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETADDITIONALCARETSVISIBLE, additionalCaretsBlink ? 1 : 0, Unused);
        }

        /// <summary>Whether additional carets are visible (Scintilla feature 2609)</summary>
        public bool GetAdditionalCaretsVisible()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETADDITIONALCARETSVISIBLE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>How many selections are there? (Scintilla feature 2570)</summary>
        public int GetSelections()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONS, Unused, Unused);
            return (int) res;
        }

        /// <summary>Is every selected range empty? (Scintilla feature 2650)</summary>
        public bool GetSelectionEmpty()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONEMPTY, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Clear selections to a single empty stream selection (Scintilla feature 2571)</summary>
        public void ClearSelections()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CLEARSELECTIONS, Unused, Unused);
        }

        /// <summary>Set a simple selection (Scintilla feature 2572)</summary>
        public int SetSelection(int caret, int anchor)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTION, caret, anchor);
            return (int) res;
        }

        /// <summary>Add a selection (Scintilla feature 2573)</summary>
        public int AddSelection(int caret, int anchor)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ADDSELECTION, caret, anchor);
            return (int) res;
        }

        /// <summary>Drop one selection (Scintilla feature 2671)</summary>
        public void DropSelectionN(int selection)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DROPSELECTIONN, selection, Unused);
        }

        /// <summary>Set the main selection (Scintilla feature 2574)</summary>
        public void SetMainSelection(int selection)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETMAINSELECTION, selection, Unused);
        }

        /// <summary>Which selection is the main selection (Scintilla feature 2575)</summary>
        public int GetMainSelection()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETMAINSELECTION, Unused, Unused);
            return (int) res;
        }

        /// <summary>Which selection is the main selection (Scintilla feature 2576)</summary>
        public void SetSelectionNCaret(int selection, Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTIONNCARET, selection, pos.Value);
        }

        /// <summary>Which selection is the main selection (Scintilla feature 2577)</summary>
        public Position GetSelectionNCaret(int selection)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONNCARET, selection, Unused);
            return new Position((int) res);
        }

        /// <summary>Which selection is the main selection (Scintilla feature 2578)</summary>
        public void SetSelectionNAnchor(int selection, Position posAnchor)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTIONNANCHOR, selection, posAnchor.Value);
        }

        /// <summary>Which selection is the main selection (Scintilla feature 2579)</summary>
        public Position GetSelectionNAnchor(int selection)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONNANCHOR, selection, Unused);
            return new Position((int) res);
        }

        /// <summary>Which selection is the main selection (Scintilla feature 2580)</summary>
        public void SetSelectionNCaretVirtualSpace(int selection, int space)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTIONNCARETVIRTUALSPACE, selection, space);
        }

        /// <summary>Which selection is the main selection (Scintilla feature 2581)</summary>
        public int GetSelectionNCaretVirtualSpace(int selection)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONNCARETVIRTUALSPACE, selection, Unused);
            return (int) res;
        }

        /// <summary>Which selection is the main selection (Scintilla feature 2582)</summary>
        public void SetSelectionNAnchorVirtualSpace(int selection, int space)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTIONNANCHORVIRTUALSPACE, selection, space);
        }

        /// <summary>Which selection is the main selection (Scintilla feature 2583)</summary>
        public int GetSelectionNAnchorVirtualSpace(int selection)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONNANCHORVIRTUALSPACE, selection, Unused);
            return (int) res;
        }

        /// <summary>Sets the position that starts the selection - this becomes the anchor. (Scintilla feature 2584)</summary>
        public void SetSelectionNStart(int selection, Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTIONNSTART, selection, pos.Value);
        }

        /// <summary>Returns the position at the start of the selection. (Scintilla feature 2585)</summary>
        public Position GetSelectionNStart(int selection)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONNSTART, selection, Unused);
            return new Position((int) res);
        }

        /// <summary>Sets the position that ends the selection - this becomes the currentPosition. (Scintilla feature 2586)</summary>
        public void SetSelectionNEnd(int selection, Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETSELECTIONNEND, selection, pos.Value);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2587)</summary>
        public Position GetSelectionNEnd(int selection)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSELECTIONNEND, selection, Unused);
            return new Position((int) res);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2588)</summary>
        public void SetRectangularSelectionCaret(Position pos)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETRECTANGULARSELECTIONCARET, pos.Value, Unused);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2589)</summary>
        public Position GetRectangularSelectionCaret()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETRECTANGULARSELECTIONCARET, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2590)</summary>
        public void SetRectangularSelectionAnchor(Position posAnchor)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETRECTANGULARSELECTIONANCHOR, posAnchor.Value, Unused);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2591)</summary>
        public Position GetRectangularSelectionAnchor()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETRECTANGULARSELECTIONANCHOR, Unused, Unused);
            return new Position((int) res);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2592)</summary>
        public void SetRectangularSelectionCaretVirtualSpace(int space)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETRECTANGULARSELECTIONCARETVIRTUALSPACE, space, Unused);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2593)</summary>
        public int GetRectangularSelectionCaretVirtualSpace()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETRECTANGULARSELECTIONCARETVIRTUALSPACE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2594)</summary>
        public void SetRectangularSelectionAnchorVirtualSpace(int space)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETRECTANGULARSELECTIONANCHORVIRTUALSPACE, space, Unused);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2595)</summary>
        public int GetRectangularSelectionAnchorVirtualSpace()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETRECTANGULARSELECTIONANCHORVIRTUALSPACE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2596)</summary>
        public void SetVirtualSpaceOptions(int virtualSpaceOptions)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETVIRTUALSPACEOPTIONS, virtualSpaceOptions, Unused);
        }

        /// <summary>Returns the position at the end of the selection. (Scintilla feature 2597)</summary>
        public int GetVirtualSpaceOptions()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETVIRTUALSPACEOPTIONS, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// On GTK+, allow selecting the modifier key to use for mouse-based
        /// rectangular selection. Often the window manager requires Alt+Mouse Drag
        /// for moving windows.
        /// Valid values are SCMOD_CTRL(default), SCMOD_ALT, or SCMOD_SUPER.
        /// (Scintilla feature 2598)
        /// </summary>
        public void SetRectangularSelectionModifier(int modifier)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETRECTANGULARSELECTIONMODIFIER, modifier, Unused);
        }

        /// <summary>Get the modifier key used for rectangular selection. (Scintilla feature 2599)</summary>
        public int GetRectangularSelectionModifier()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETRECTANGULARSELECTIONMODIFIER, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Set the foreground colour of additional selections.
        /// Must have previously called SetSelFore with non-zero first argument for this to have an effect.
        /// (Scintilla feature 2600)
        /// </summary>
        public void SetAdditionalSelFore(Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETADDITIONALSELFORE, fore.Value, Unused);
        }

        /// <summary>
        /// Set the background colour of additional selections.
        /// Must have previously called SetSelBack with non-zero first argument for this to have an effect.
        /// (Scintilla feature 2601)
        /// </summary>
        public void SetAdditionalSelBack(Colour back)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETADDITIONALSELBACK, back.Value, Unused);
        }

        /// <summary>Set the alpha of the selection. (Scintilla feature 2602)</summary>
        public void SetAdditionalSelAlpha(int alpha)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETADDITIONALSELALPHA, alpha, Unused);
        }

        /// <summary>Get the alpha of the selection. (Scintilla feature 2603)</summary>
        public int GetAdditionalSelAlpha()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETADDITIONALSELALPHA, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the foreground colour of additional carets. (Scintilla feature 2604)</summary>
        public void SetAdditionalCaretFore(Colour fore)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETADDITIONALCARETFORE, fore.Value, Unused);
        }

        /// <summary>Get the foreground colour of additional carets. (Scintilla feature 2605)</summary>
        public Colour GetAdditionalCaretFore()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETADDITIONALCARETFORE, Unused, Unused);
            return new Colour((int) res);
        }

        /// <summary>Set the main selection to the next selection. (Scintilla feature 2606)</summary>
        public void RotateSelection()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ROTATESELECTION, Unused, Unused);
        }

        /// <summary>Swap that caret and anchor of the main selection. (Scintilla feature 2607)</summary>
        public void SwapMainAnchorCaret()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SWAPMAINANCHORCARET, Unused, Unused);
        }

        /// <summary>
        /// Indicate that the internal state of a lexer has changed over a range and therefore
        /// there may be a need to redraw.
        /// (Scintilla feature 2617)
        /// </summary>
        public int ChangeLexerState(Position start, Position end)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CHANGELEXERSTATE, start.Value, end.Value);
            return (int) res;
        }

        /// <summary>
        /// Find the next line at or after lineStart that is a contracted fold header line.
        /// Return -1 when no more lines.
        /// (Scintilla feature 2618)
        /// </summary>
        public int ContractedFoldNext(int lineStart)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CONTRACTEDFOLDNEXT, lineStart, Unused);
            return (int) res;
        }

        /// <summary>Centre current line in window. (Scintilla feature 2619)</summary>
        public void VerticalCentreCaret()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_VERTICALCENTRECARET, Unused, Unused);
        }

        /// <summary>Move the selected lines up one line, shifting the line above after the selection (Scintilla feature 2620)</summary>
        public void MoveSelectedLinesUp()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MOVESELECTEDLINESUP, Unused, Unused);
        }

        /// <summary>Move the selected lines down one line, shifting the line below before the selection (Scintilla feature 2621)</summary>
        public void MoveSelectedLinesDown()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MOVESELECTEDLINESDOWN, Unused, Unused);
        }

        /// <summary>Set the identifier reported as IdFrom in notification messages. (Scintilla feature 2622)</summary>
        public void SetIdentifier(int identifier)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETIDENTIFIER, identifier, Unused);
        }

        /// <summary>Get the identifier. (Scintilla feature 2623)</summary>
        public int GetIdentifier()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETIDENTIFIER, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the width for future RGBA image data. (Scintilla feature 2624)</summary>
        public void RGBAImageSetWidth(int width)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_RGBAIMAGESETWIDTH, width, Unused);
        }

        /// <summary>Set the height for future RGBA image data. (Scintilla feature 2625)</summary>
        public void RGBAImageSetHeight(int height)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_RGBAIMAGESETHEIGHT, height, Unused);
        }

        /// <summary>Set the scale factor in percent for future RGBA image data. (Scintilla feature 2651)</summary>
        public void RGBAImageSetScale(int scalePercent)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_RGBAIMAGESETSCALE, scalePercent, Unused);
        }

        /// <summary>
        /// Define a marker from RGBA data.
        /// It has the width and height from RGBAImageSetWidth/Height
        /// (Scintilla feature 2626)
        /// </summary>
        public unsafe void MarkerDefineRGBAImage(int markerNumber, string pixels)
        {
            fixed (byte* pixelsPtr = Encoding.UTF8.GetBytes(pixels))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_MARKERDEFINERGBAIMAGE, markerNumber, (IntPtr) pixelsPtr);
            }
        }

        /// <summary>
        /// Register an RGBA image for use in autocompletion lists.
        /// It has the width and height from RGBAImageSetWidth/Height
        /// (Scintilla feature 2627)
        /// </summary>
        public unsafe void RegisterRGBAImage(int type, string pixels)
        {
            fixed (byte* pixelsPtr = Encoding.UTF8.GetBytes(pixels))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERRGBAIMAGE, type, (IntPtr) pixelsPtr);
            }
        }

        /// <summary>Scroll to start of document. (Scintilla feature 2628)</summary>
        public void ScrollToStart()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SCROLLTOSTART, Unused, Unused);
        }

        /// <summary>Scroll to end of document. (Scintilla feature 2629)</summary>
        public void ScrollToEnd()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SCROLLTOEND, Unused, Unused);
        }

        /// <summary>Set the technology used. (Scintilla feature 2630)</summary>
        public void SetTechnology(int technology)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETTECHNOLOGY, technology, Unused);
        }

        /// <summary>Get the tech. (Scintilla feature 2631)</summary>
        public int GetTechnology()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETTECHNOLOGY, Unused, Unused);
            return (int) res;
        }

        /// <summary>Create an ILoader*. (Scintilla feature 2632)</summary>
        public int CreateLoader(int bytes)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CREATELOADER, bytes, Unused);
            return (int) res;
        }

        /// <summary>On OS X, show a find indicator. (Scintilla feature 2640)</summary>
        public void FindIndicatorShow(Position start, Position end)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FINDINDICATORSHOW, start.Value, end.Value);
        }

        /// <summary>On OS X, flash a find indicator, then fade out. (Scintilla feature 2641)</summary>
        public void FindIndicatorFlash(Position start, Position end)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FINDINDICATORFLASH, start.Value, end.Value);
        }

        /// <summary>On OS X, hide the find indicator. (Scintilla feature 2642)</summary>
        public void FindIndicatorHide()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FINDINDICATORHIDE, Unused, Unused);
        }

        /// <summary>
        /// Move caret to before first visible character on display line.
        /// If already there move to first character on display line.
        /// (Scintilla feature 2652)
        /// </summary>
        public void VCHomeDisplay()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_VCHOMEDISPLAY, Unused, Unused);
        }

        /// <summary>Like VCHomeDisplay but extending selection to new caret position. (Scintilla feature 2653)</summary>
        public void VCHomeDisplayExtend()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_VCHOMEDISPLAYEXTEND, Unused, Unused);
        }

        /// <summary>Is the caret line always visible? (Scintilla feature 2654)</summary>
        public bool GetCaretLineVisibleAlways()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETCARETLINEVISIBLEALWAYS, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>Sets the caret line to always visible. (Scintilla feature 2655)</summary>
        public void SetCaretLineVisibleAlways(bool alwaysVisible)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETCARETLINEVISIBLEALWAYS, alwaysVisible ? 1 : 0, Unused);
        }

        /// <summary>Set the line end types that the application wants to use. May not be used if incompatible with lexer or encoding. (Scintilla feature 2656)</summary>
        public void SetLineEndTypesAllowed(int lineEndBitSet)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETLINEENDTYPESALLOWED, lineEndBitSet, Unused);
        }

        /// <summary>Get the line end types currently allowed. (Scintilla feature 2657)</summary>
        public int GetLineEndTypesAllowed()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINEENDTYPESALLOWED, Unused, Unused);
            return (int) res;
        }

        /// <summary>Get the line end types currently recognised. May be a subset of the allowed types due to lexer limitation. (Scintilla feature 2658)</summary>
        public int GetLineEndTypesActive()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINEENDTYPESACTIVE, Unused, Unused);
            return (int) res;
        }

        /// <summary>Set the way a character is drawn. (Scintilla feature 2665)</summary>
        public unsafe void SetRepresentation(string encodedCharacter, string representation)
        {
            fixed (byte* encodedCharacterPtr = Encoding.UTF8.GetBytes(encodedCharacter))
            {
                fixed (byte* representationPtr = Encoding.UTF8.GetBytes(representation))
                {
                    IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETREPRESENTATION, (IntPtr) encodedCharacterPtr, (IntPtr) representationPtr);
                }
            }
        }

        /// <summary>
        /// Set the way a character is drawn.
        /// Result is NUL-terminated.
        /// (Scintilla feature 2666)
        /// </summary>
        public unsafe string GetRepresentation(string encodedCharacter)
        {
            fixed (byte* encodedCharacterPtr = Encoding.UTF8.GetBytes(encodedCharacter))
            {
                byte[] representationBuffer = new byte[10000];
                fixed (byte* representationPtr = representationBuffer)
                {
                    IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETREPRESENTATION, (IntPtr) encodedCharacterPtr, (IntPtr) representationPtr);
                    return Encoding.UTF8.GetString(representationBuffer).TrimEnd('\0');
                }
            }
        }

        /// <summary>Remove a character representation. (Scintilla feature 2667)</summary>
        public unsafe void ClearRepresentation(string encodedCharacter)
        {
            fixed (byte* encodedCharacterPtr = Encoding.UTF8.GetBytes(encodedCharacter))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_CLEARREPRESENTATION, (IntPtr) encodedCharacterPtr, Unused);
            }
        }

        /// <summary>Start notifying the container of all key presses and commands. (Scintilla feature 3001)</summary>
        public void StartRecord()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STARTRECORD, Unused, Unused);
        }

        /// <summary>Stop notifying the container of all key presses and commands. (Scintilla feature 3002)</summary>
        public void StopRecord()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_STOPRECORD, Unused, Unused);
        }

        /// <summary>Set the lexing language of the document. (Scintilla feature 4001)</summary>
        public void SetLexer(int lexer)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETLEXER, lexer, Unused);
        }

        /// <summary>Retrieve the lexing language of the document. (Scintilla feature 4002)</summary>
        public int GetLexer()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLEXER, Unused, Unused);
            return (int) res;
        }

        /// <summary>Colourise a segment of the document using the current lexing language. (Scintilla feature 4003)</summary>
        public void Colourise(Position start, Position end)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_COLOURISE, start.Value, end.Value);
        }

        /// <summary>Set up a value that may be used by a lexer for some optional feature. (Scintilla feature 4004)</summary>
        public unsafe void SetProperty(string key, string value)
        {
            fixed (byte* keyPtr = Encoding.UTF8.GetBytes(key))
            {
                fixed (byte* valuePtr = Encoding.UTF8.GetBytes(value))
                {
                    IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETPROPERTY, (IntPtr) keyPtr, (IntPtr) valuePtr);
                }
            }
        }

        /// <summary>Set up the key words used by the lexer. (Scintilla feature 4005)</summary>
        public unsafe void SetKeyWords(int keywordSet, string keyWords)
        {
            fixed (byte* keyWordsPtr = Encoding.UTF8.GetBytes(keyWords))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETKEYWORDS, keywordSet, (IntPtr) keyWordsPtr);
            }
        }

        /// <summary>Set the lexing language of the document based on string name. (Scintilla feature 4006)</summary>
        public unsafe void SetLexerLanguage(string language)
        {
            fixed (byte* languagePtr = Encoding.UTF8.GetBytes(language))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETLEXERLANGUAGE, Unused, (IntPtr) languagePtr);
            }
        }

        /// <summary>Load a lexer library (dll / so). (Scintilla feature 4007)</summary>
        public unsafe void LoadLexerLibrary(string path)
        {
            fixed (byte* pathPtr = Encoding.UTF8.GetBytes(path))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_LOADLEXERLIBRARY, Unused, (IntPtr) pathPtr);
            }
        }

        /// <summary>
        /// Retrieve a "property" value previously set with SetProperty.
        /// Result is NUL-terminated.
        /// (Scintilla feature 4008)
        /// </summary>
        public unsafe string GetProperty(string key)
        {
            fixed (byte* keyPtr = Encoding.UTF8.GetBytes(key))
            {
                byte[] bufBuffer = new byte[10000];
                fixed (byte* bufPtr = bufBuffer)
                {
                    IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPROPERTY, (IntPtr) keyPtr, (IntPtr) bufPtr);
                    return Encoding.UTF8.GetString(bufBuffer).TrimEnd('\0');
                }
            }
        }

        /// <summary>
        /// Retrieve a "property" value previously set with SetProperty,
        /// with "$()" variable replacement on returned buffer.
        /// Result is NUL-terminated.
        /// (Scintilla feature 4009)
        /// </summary>
        public unsafe string GetPropertyExpanded(string key)
        {
            fixed (byte* keyPtr = Encoding.UTF8.GetBytes(key))
            {
                byte[] bufBuffer = new byte[10000];
                fixed (byte* bufPtr = bufBuffer)
                {
                    IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPROPERTYEXPANDED, (IntPtr) keyPtr, (IntPtr) bufPtr);
                    return Encoding.UTF8.GetString(bufBuffer).TrimEnd('\0');
                }
            }
        }

        /// <summary>
        /// Retrieve a "property" value previously set with SetProperty,
        /// interpreted as an int AFTER any "$()" variable replacement.
        /// (Scintilla feature 4010)
        /// </summary>
        public unsafe int GetPropertyInt(string key)
        {
            fixed (byte* keyPtr = Encoding.UTF8.GetBytes(key))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPROPERTYINT, (IntPtr) keyPtr, Unused);
                return (int) res;
            }
        }

        /// <summary>Retrieve the number of bits the current lexer needs for styling. (Scintilla feature 4011)</summary>
        public int GetStyleBitsNeeded()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSTYLEBITSNEEDED, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Retrieve the name of the lexer.
        /// Return the length of the text.
        /// Result is NUL-terminated.
        /// (Scintilla feature 4012)
        /// </summary>
        public unsafe string GetLexerLanguage()
        {
            byte[] textBuffer = new byte[10000];
            fixed (byte* textPtr = textBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLEXERLANGUAGE, Unused, (IntPtr) textPtr);
                return Encoding.UTF8.GetString(textBuffer).TrimEnd('\0');
            }
        }

        /// <summary>For private communication between an application and a known lexer. (Scintilla feature 4013)</summary>
        public int PrivateLexerCall(int operation, int pointer)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PRIVATELEXERCALL, operation, pointer);
            return (int) res;
        }

        /// <summary>
        /// Retrieve a '\n' separated list of properties understood by the current lexer.
        /// Result is NUL-terminated.
        /// (Scintilla feature 4014)
        /// </summary>
        public unsafe string PropertyNames()
        {
            byte[] namesBuffer = new byte[10000];
            fixed (byte* namesPtr = namesBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PROPERTYNAMES, Unused, (IntPtr) namesPtr);
                return Encoding.UTF8.GetString(namesBuffer).TrimEnd('\0');
            }
        }

        /// <summary>Retrieve the type of a property. (Scintilla feature 4015)</summary>
        public unsafe int PropertyType(string name)
        {
            fixed (byte* namePtr = Encoding.UTF8.GetBytes(name))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_PROPERTYTYPE, (IntPtr) namePtr, Unused);
                return (int) res;
            }
        }

        /// <summary>
        /// Describe a property.
        /// Result is NUL-terminated.
        /// (Scintilla feature 4016)
        /// </summary>
        public unsafe string DescribeProperty(string name)
        {
            fixed (byte* namePtr = Encoding.UTF8.GetBytes(name))
            {
                byte[] descriptionBuffer = new byte[10000];
                fixed (byte* descriptionPtr = descriptionBuffer)
                {
                    IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DESCRIBEPROPERTY, (IntPtr) namePtr, (IntPtr) descriptionPtr);
                    return Encoding.UTF8.GetString(descriptionBuffer).TrimEnd('\0');
                }
            }
        }

        /// <summary>
        /// Retrieve a '\n' separated list of descriptions of the keyword sets understood by the current lexer.
        /// Result is NUL-terminated.
        /// (Scintilla feature 4017)
        /// </summary>
        public unsafe string DescribeKeyWordSets()
        {
            byte[] descriptionsBuffer = new byte[10000];
            fixed (byte* descriptionsPtr = descriptionsBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DESCRIBEKEYWORDSETS, Unused, (IntPtr) descriptionsPtr);
                return Encoding.UTF8.GetString(descriptionsBuffer).TrimEnd('\0');
            }
        }

        /// <summary>
        /// Bit set of LineEndType enumertion for which line ends beyond the standard
        /// LF, CR, and CRLF are supported by the lexer.
        /// (Scintilla feature 4018)
        /// </summary>
        public int GetLineEndTypesSupported()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETLINEENDTYPESSUPPORTED, Unused, Unused);
            return (int) res;
        }

        /// <summary>Allocate a set of sub styles for a particular base style, returning start of range (Scintilla feature 4020)</summary>
        public int AllocateSubStyles(int styleBase, int numberStyles)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_ALLOCATESUBSTYLES, styleBase, numberStyles);
            return (int) res;
        }

        /// <summary>The starting style number for the sub styles associated with a base style (Scintilla feature 4021)</summary>
        public int GetSubStylesStart(int styleBase)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSUBSTYLESSTART, styleBase, Unused);
            return (int) res;
        }

        /// <summary>The number of sub styles associated with a base style (Scintilla feature 4022)</summary>
        public int GetSubStylesLength(int styleBase)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSUBSTYLESLENGTH, styleBase, Unused);
            return (int) res;
        }

        /// <summary>For a sub style, return the base style, else return the argument. (Scintilla feature 4027)</summary>
        public int GetStyleFromSubStyle(int subStyle)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSTYLEFROMSUBSTYLE, subStyle, Unused);
            return (int) res;
        }

        /// <summary>For a secondary style, return the primary style, else return the argument. (Scintilla feature 4028)</summary>
        public int GetPrimaryStyleFromStyle(int style)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETPRIMARYSTYLEFROMSTYLE, style, Unused);
            return (int) res;
        }

        /// <summary>Free allocated sub styles (Scintilla feature 4023)</summary>
        public void FreeSubStyles()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_FREESUBSTYLES, Unused, Unused);
        }

        /// <summary>Set the identifiers that are shown in a particular style (Scintilla feature 4024)</summary>
        public unsafe void SetIdentifiers(int style, string identifiers)
        {
            fixed (byte* identifiersPtr = Encoding.UTF8.GetBytes(identifiers))
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETIDENTIFIERS, style, (IntPtr) identifiersPtr);
            }
        }

        /// <summary>
        /// Where styles are duplicated by a feature such as active/inactive code
        /// return the distance between the two types.
        /// (Scintilla feature 4025)
        /// </summary>
        public int DistanceToSecondaryStyles()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_DISTANCETOSECONDARYSTYLES, Unused, Unused);
            return (int) res;
        }

        /// <summary>
        /// Get the set of base styles that can be extended with sub styles
        /// Result is NUL-terminated.
        /// (Scintilla feature 4026)
        /// </summary>
        public unsafe string GetSubStyleBases()
        {
            byte[] stylesBuffer = new byte[10000];
            fixed (byte* stylesPtr = stylesBuffer)
            {
                IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETSUBSTYLEBASES, Unused, (IntPtr) stylesPtr);
                return Encoding.UTF8.GetString(stylesBuffer).TrimEnd('\0');
            }
        }

        /// <summary>
        /// Deprecated in 2.30
        /// In palette mode?
        /// (Scintilla feature 2139)
        /// </summary>
        public bool GetUsePalette()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETUSEPALETTE, Unused, Unused);
            return 1 == (int) res;
        }

        /// <summary>
        /// In palette mode, Scintilla uses the environment's palette calls to display
        /// more colours. This may lead to ugly displays.
        /// (Scintilla feature 2039)
        /// </summary>
        public void SetUsePalette(bool usePalette)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETUSEPALETTE, usePalette ? 1 : 0, Unused);
        }

        /// <summary>
        /// Deprecated in 3.5.5
        /// Always interpret keyboard input as Unicode
        /// (Scintilla feature 2521)
        /// </summary>
        public void SetKeysUnicode(bool keysUnicode)
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_SETKEYSUNICODE, keysUnicode ? 1 : 0, Unused);
        }

        /// <summary>Are keys always interpreted as Unicode? (Scintilla feature 2522)</summary>
        public bool GetKeysUnicode()
        {
            IntPtr res = Win32.SendMessage(scintilla, SciMsg.SCI_GETKEYSUNICODE, Unused, Unused);
            return 1 == (int) res;
        }

        /* --Autogenerated -- end of section automatically generated from Scintilla.iface */
    }
}
