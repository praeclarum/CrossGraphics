//
// Copyright (c) 2013 Frank A. Krueger
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;

namespace CrossGraphics
{
	public class WmfGraphics : IGraphics
	{
        const float TwipsFromUnits = 11.25f;

		BinaryWriter fw;

		WmfGraphicsFontMetrics _fontMetrics;
		//Font _lastFont = null;
		Color _lastColor = null;
		
		class State {
            public PointF Scale;
			public PointF Translation;
            public RectangleF ClippingRect;
		}
		readonly Stack<State> _states = new Stack<State> ();
		State _state = new State ();

		RectangleF _viewBox;

		public WmfGraphics (BinaryWriter tw, RectangleF viewBox)
		{
			_viewBox = viewBox;
			fw = tw;
			_fontMetrics = new WmfGraphicsFontMetrics ();
			SetColor (Colors.Black);			
			_states.Push (_state);
		}
		
		public WmfGraphics (Stream s, Rectangle viewBox)
            : this (new BinaryWriter (s, System.Text.Encoding.UTF8), new RectangleF (viewBox.Left, viewBox.Top, viewBox.Width, viewBox.Height))
		{
		}

        public WmfGraphics (Stream s, RectangleF viewBox) 
			: this(new BinaryWriter (s, System.Text.Encoding.UTF8), viewBox)
		{
		}

		public void BeginDrawing ()
		{
            fw.Write ((ushort)1); // HEADER Type
            fw.Write ((ushort)9); // HEADER HeaderSize
            fw.Write ((ushort)0x300); // HEADER Version

            StartRecord (Function.SetMapMode);
            rw.Write ((ushort)MapMode.Twips);
            EndRecord ();

            StartRecord (Function.Escape);
            rw.Write ((ushort)EscapeFunction.SetLineCap);
            rw.Write ((ushort)4);
            rw.Write ((int)PostScriptCap.Round);
            EndRecord ();

            objects.Clear ();

            // 0 = Null Pen
            StartRecord (Function.CreatePenIndirect);
            rw.Write ((ushort)PenStyle.Null);
            rw.Write ((uint)0); // PointS
            rw.Write ((uint)0); // ColorRef
            EndRecord ();
            objects.Add (null);

            // 1 = Null Brush
            StartRecord (Function.CreateBrushIndirect);
            rw.Write ((ushort)BrushStyle.Null);
            rw.Write ((uint)0); // ColorRef
            rw.Write ((ushort)HatchStyle.Horizontal); // BrushHatch
            EndRecord ();
            objects.Add (null);            
		}

		public void EndDrawing ()
		{
            StartRecord (Function.Eof);
            EndRecord ();

            var rs = (from r in records select ((MemoryStream)r.BaseStream).ToArray ()).ToArray ();
            var size = (rs.Sum (r => r.Length + 4) + (2+2+2+4+2+4+2))/2;
            var maxRecord = rs.Max (r => (r.Length + 4)/2);

            fw.Write ((uint)size);
            fw.Write ((ushort)objects.Count);
            fw.Write ((uint)maxRecord);
            fw.Write ((ushort)0); // Number of Members

            foreach (var r in rs) {
                fw.Write ((r.Length + 4) / 2);
                fw.Write (r);
            }
            fw.Flush ();
		}

        List<BinaryWriter> records = new List<BinaryWriter> ();

        BinaryWriter rw = null;
        void StartRecord (Function function)
        {
            rw = new BinaryWriter (new MemoryStream ());
            rw.Write ((ushort)function);
            records.Add (rw);
        }

        void EndRecord ()
        {
            rw.Flush ();
            rw = null;
        }
		
		public void SaveState ()
		{			
			var ns = new State() {
				Translation = _state.Translation,
			};
			_states.Push (ns);
			_state = ns;
		}

        public void Scale (float sx, float sy)
        {
            _state.Scale.X *= sx;
            _state.Scale.Y *= sy;
        }
		
		public void Translate (float dx, float dy)
		{
			_state.Translation.X += dx;
			_state.Translation.Y += dy;
		}

        public void SetClippingRect (float x, float y, float width, float height)
        {
            _state.ClippingRect = new RectangleF (x, y, width, height);
        }
		
		public void RestoreState ()
		{
			if (_states.Count > 1) {
				_state = _states.Pop ();
			}
		}

		public void SetFont (Font f)
		{
			//_lastFont = f;
		}

		public void SetColor (Color c)
		{
			_lastColor = c;
		}

		public void Clear (Color c)
		{
		}

		public void FillPolygon (Polygon poly)
		{
		}

		public void DrawPolygon (Polygon poly, float w)
		{
            SelectNullBrush ();
            SelectPenRecord (w);

            StartRecord (Function.Polygon);
            rw.Write ((ushort)poly.Points.Count);
            foreach (var p in poly.Points) {
                WriteRecordCoord (p.Y, p.X);
            }
            EndRecord ();
		}

		public void FillOval (float x, float y, float width, float height)
		{
            SelectNullPen ();
            SelectBrushRecord ();

            StartRecord (Function.Ellipse);
            WriteRecordCoord (x + width + 1, y + height + 1);
            WriteRecordCoord (x, y);
            EndRecord ();
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
            SelectNullBrush ();
            SelectPenRecord (w);

            StartRecord (Function.Ellipse);
            WriteRecordCoord (x + width + 1, y + height + 1);
            WriteRecordCoord (x, y);
            EndRecord ();
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
            SelectNullPen ();
            SelectBrushRecord ();
        }
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
            SelectNullBrush ();
            SelectPenRecord (w);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
            SelectNullPen ();
            SelectBrushRecord ();
        }

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
            SelectNullBrush ();
            SelectPenRecord (w);
		}

		public void FillRect (float x, float y, float width, float height)
		{
            SelectNullPen ();
            SelectBrushRecord ();

            StartRecord (Function.Rectangle);
            WriteRecordCoord (x + width + 1, y + height + 1);
            WriteRecordCoord (x, y);
            EndRecord ();
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
            SelectNullBrush ();
            SelectPenRecord (w);

            StartRecord (Function.Rectangle);
            WriteRecordCoord (x + width + 1, y + height + 1);
            WriteRecordCoord (x, y);
            EndRecord ();
		}
		
		bool _inPolyline = false;
		//bool _startedPolyline = false;

		public void BeginLines (bool rounded)
		{
			_inPolyline = true;
            //_startedPolyline = false;
		}

        void SelectObjectRecord (int objectIndex)
        {
            StartRecord (Function.SelectObject);
            rw.Write ((ushort)objectIndex);
            EndRecord ();
        }

        class GObject
        {
            public bool IsBrush; // Otherwise, Pen
            public int WidthInTwips;
            public Color Color;
        }
        List<GObject> objects = new List<GObject> ();

        void SelectNullPen ()
        {
            SelectObjectRecord (0);
        }

        void SelectNullBrush ()
        {
            SelectObjectRecord (1);
        }

        void SelectPenRecord (float lineWidth)
        {
            var lineWidthInTwips = (int)(lineWidth * TwipsFromUnits + 0.5f);

            var objectIndex = -1;

            for (var i = 2; i < objects.Count; i++) {
                var o = objects[i];
                if (!o.IsBrush && o.WidthInTwips == lineWidthInTwips && o.Color == _lastColor) {
                    objectIndex = i;
                    break;
                }
            }

            if (objectIndex < 0) {
                var pen = new GObject {
                    IsBrush = false,
                    WidthInTwips = lineWidthInTwips,
                    Color = _lastColor,
                };
                objectIndex = objects.Count;
                objects.Add (pen);

                StartRecord (Function.CreatePenIndirect);
                rw.Write ((ushort)PenStyle.Solid);
                rw.Write ((ushort)lineWidthInTwips); // PointS.x = width of pen (0 = default)
                rw.Write ((ushort)0); // PointS.y                
                rw.Write ((byte)_lastColor.Red); // ColorRef.Red
                rw.Write ((byte)_lastColor.Green); // ColorRef.Green
                rw.Write ((byte)_lastColor.Blue); // ColorRef.Blue
                rw.Write ((byte)0); // ColorRef.Reserved
                EndRecord ();
            }

            SelectObjectRecord (objectIndex);
        }

        void SelectBrushRecord ()
        {
            var objectIndex = -1;

            for (var i = 2; i < objects.Count; i++) {
                var o = objects[i];
                if (o.IsBrush && o.Color == _lastColor) {
                    objectIndex = i;
                    break;
                }
            }

            if (objectIndex < 0) {
                var brush = new GObject {
                    IsBrush = true,
                    Color = _lastColor,
                };
                objectIndex = objects.Count;
                objects.Add (brush);

                StartRecord (Function.CreateBrushIndirect);
                rw.Write ((ushort)BrushStyle.Solid);
                rw.Write ((byte)_lastColor.Red); // ColorRef.Red
                rw.Write ((byte)_lastColor.Green); // ColorRef.Green
                rw.Write ((byte)_lastColor.Blue); // ColorRef.Blue
                rw.Write ((byte)0); // ColorRef.Reserved
                rw.Write ((ushort)HatchStyle.Horizontal); // ColorRef.Reserved
                EndRecord ();
            }

            SelectObjectRecord (objectIndex);
        }

        [Flags]
        enum PenStyle : ushort
        {
            Solid = 0,
            Dash = 0x01,
            Dot = 0x02,
            Null = 0x05,
            EndCapSquare = 0x0100,
            EndCapFlat = 0x0200,
            JoinBevel = 0x1000,
            JoinMiter = 0x2000,
        }

        enum BrushStyle : ushort
        {
            Solid = 0,
            Null = 1,
            Hatched = 2,
            Pattern = 3,
            Indexed = 4,
        }

        enum HatchStyle : ushort
        {
            Horizontal = 0,
            Verical = 1,
        }

        public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_inPolyline) {
                //_startedPolyline = true;
			}
			else {
			}

            SelectPenRecord (w);

            StartRecord (Function.MoveTo);
            WriteRecordCoord (sx, sy);
            EndRecord ();

            StartRecord (Function.LineTo);
            WriteRecordCoord (ex, ey);
            EndRecord ();
		}

        void WriteRecordCoord (float x, float y)
        {
            var tx = (short)((x + _state.Translation.X) * TwipsFromUnits + 0.5f);
            var ty = (short)((y + _state.Translation.Y) * TwipsFromUnits + 0.5f);
            rw.Write (ty);
            rw.Write (tx);
        }

        enum Function : ushort
        {
            Eof = 0,
            SetMapMode = 0x0103,
            SelectObject = 0x012D,
            LineTo = 0x0213,
            MoveTo = 0x0214,
            CreatePenIndirect = 0x02FA,
            CreateBrushIndirect = 0x02FC,
            Polygon = 0x0324,
            Polyline = 0x0325,
            Ellipse = 0x0418,
            Rectangle = 0x041B,
            TextOut = 0x0521,
            Escape = 0x0626,
            Pie = 0x081A,
            Arc = 0x0817,
            ExtTextOut = 0x0A32,
        }

        enum MapMode : ushort
        {
            Text = 1,
            Twips = 6,
            Isotropic = 7,
            Anisotropic = 8,
        }

        enum EscapeFunction : ushort
        {
            SetLineCap = 0x0015,
        }

        enum PostScriptCap : int
        {
            NotSet = -2,
            Flat = 0,
            Round = 1,
            Square = 2,
        }

		public void EndLines ()
		{
			if (_inPolyline) {
                _inPolyline = false;
			}
		}
		
		public void DrawString(string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
		}

		public void DrawString (string s, float x, float y)
		{
		}

		public IFontMetrics GetFontMetrics ()
		{
			return _fontMetrics;
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
		}

		public IImage ImageFromFile (string filename)
		{
			return null;
		}
		
		public void BeginEntity (object entity)
		{
		}
	}

	class WmfGraphicsFontMetrics : IFontMetrics
	{
		int _height;

        public WmfGraphicsFontMetrics ()
		{
			_height = 10;
		}

		public int StringWidth (string str, int startIndex, int length)
		{
			return length * 10;
		}

		public int Height
		{
			get {
				return _height;
			}
		}

		public int Ascent
		{
			get {
				return Height;
			}
		}

		public int Descent
		{
			get {
				return 0;
			}
		}
	}
}
