//
// Copyright (c) 2010-2013 Frank A. Krueger
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

namespace CrossGraphics
{
	public class SvgGraphics : IGraphics
	{
		TextWriter _tw;

		SvgGraphicsFontMetrics _fontMetrics;
		//Font _lastFont = null;
		string _lastColor = null;
		
		class State {
            public PointF Scale;
			public PointF Translation;
            public RectangleF ClippingRect;
		}
		readonly Stack<State> _states = new Stack<State> ();
		State _state = new State ();

		RectangleF _viewBox;

		public SvgGraphics (TextWriter tw, RectangleF viewBox)
		{
			_viewBox = viewBox;
			_tw = tw;
			IncludeXmlAndDoctype = true;
			_fontMetrics = new SvgGraphicsFontMetrics ();
			SetColor (Colors.Black);			
			_states.Push (_state);
		}
		
		public SvgGraphics (TextWriter tw, Rectangle viewBox) 
			: this(tw, new RectangleF(viewBox.Left, viewBox.Top, viewBox.Width, viewBox.Height))
		{
		}

		public SvgGraphics (Stream s, RectangleF viewBox) 
			: this(new StreamWriter(s, System.Text.Encoding.UTF8), viewBox)
		{
		}

		public bool IncludeXmlAndDoctype { get; set; }

		void WriteLine (string s)
		{
			_tw.WriteLine (s);
		}
		void WriteLine (string format, params object[] args)
		{
			WriteLine (string.Format (System.Globalization.CultureInfo.InvariantCulture, format, args));
		}
		void Write (string s)
		{
			_tw.Write (s);
		}
		void Write (string format, params object[] args)
		{
			Write (string.Format (System.Globalization.CultureInfo.InvariantCulture, format, args));
		}

		public void BeginDrawing ()
		{
			if (IncludeXmlAndDoctype) {
				WriteLine (@"<?xml version=""1.0""?>
<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 1.1//EN"" ""http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"">");
			}
			WriteLine (@"<svg viewBox=""{0} {1} {2} {3}"" preserveAspectRatio=""xMinYMin meet"" version=""1.1"" xmlns=""http://www.w3.org/2000/svg"">",
				_viewBox.Left, _viewBox.Top, _viewBox.Width, _viewBox.Height);

			inGroup = false;
		}

		bool inGroup = false;
		public void BeginEntity (object entity)
		{
			if (inGroup) {
				WriteLine ("</g>");
			}
			var klass = (entity != null) ? entity.ToString () : "";
			WriteLine ("<g class=\"{0}\">", klass);
			inGroup = true;
		}

		public void Clear (Color clearColor)
		{
			WriteLine ("<g id=\"background\"><rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\" stroke=\"none\"/></g>",
					_viewBox.X, _viewBox.Y, _viewBox.Width, _viewBox.Height, FormatColor (clearColor));
		}

		public void EndDrawing ()
		{
			if (inGroup) {
				WriteLine ("</g>");
				inGroup = false;
			}
			WriteLine("</svg>");
			_tw.Flush();
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

		static string FormatColor (Color c)
		{
			return string.Format("#{0:X2}{1:X2}{2:X2}", c.Red, c.Green, c.Blue);
		}

		public void SetColor (Color c)
		{
			_lastColor = FormatColor (c);
		}

		public void FillPolygon (Polygon poly)
		{
			Write("<polygon fill=\"{0}\" stroke=\"none\" points=\"", _lastColor);
			foreach (var p in poly.Points) {
				Write("{0}", p.X);
				Write(",");
				Write ("{0}", p.Y);
				Write(" ");
			}
			WriteLine("\" />");
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			Write("<polygon stroke=\"{0}\" stroke-width=\"{1}\" fill=\"none\" points=\"", _lastColor, w);
			foreach (var p in poly.Points) {
				Write("{0}", p.X);
				Write(",");
				Write("{0}", p.Y);
				Write(" ");
			}
			WriteLine("\" />");
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var rx = width / 2;
			var ry = height / 2;
			var cx = x + rx;
			var cy = y + ry;
			WriteLine("<ellipse cx=\"{0}\" cy=\"{1}\" rx=\"{2}\" ry=\"{3}\" fill=\"{4}\" stroke=\"none\" />", 
				cx, cy, rx, ry, _lastColor);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var rx = width / 2;
			var ry = height / 2;
			var cx = x + rx;
			var cy = y + ry;
			WriteLine("<ellipse cx=\"{0}\" cy=\"{1}\" rx=\"{2}\" ry=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\" fill=\"none\" />", 
				cx, cy, rx, ry, _lastColor, w);
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			WriteArc (cx, cy, radius, startAngle, endAngle, 0, "none", _lastColor);
		}
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			WriteArc (cx, cy, radius, startAngle, endAngle, w, _lastColor, "none");
		}

		public void WriteArc (float cx, float cy, float radius, float startAngle, float endAngle, float w, string stroke, string fill)
		{
			var sa = startAngle + Math.PI;
			var ea = endAngle + Math.PI;
			
			var sx = cx + radius * Math.Cos (sa);
			var sy = cy + radius * Math.Sin (sa);
			var ex = cx + radius * Math.Cos (ea);
			var ey = cy + radius * Math.Sin (ea);
			
			WriteLine("<path d=\"M {0} {1} A {2} {3} 0 0 1 {4} {5}\" stroke=\"{6}\" stroke-width=\"{7}\" fill=\"{8}\" />", 
				sx, sy,
				radius, radius,
				ex, ey,				 
				stroke, w, fill);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{5}\" ry=\"{5}\" fill=\"{4}\" stroke=\"none\" />", 
				x, y, width, height, _lastColor, radius);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{6}\" ry=\"{6}\" stroke=\"{4}\" stroke-width=\"{5}\" fill=\"none\" />", 
				x, y, width, height, _lastColor, w, radius);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\" stroke=\"none\" />", 
				x, y, width, height, _lastColor);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\" fill=\"none\" />", 
				x, y, width, height, _lastColor, w);
		}
		
		bool _inPolyline = false;
		bool _startedPolyline = false;

		public void BeginLines (bool rounded)
		{
			_inPolyline = true;
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_inPolyline) {
				if (!_startedPolyline) {
					Write ("<polyline stroke=\"{0}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"{1}\" fill=\"none\" points=\"", _lastColor, w);
					Write("{0},{1} ", sx, sy);
					_startedPolyline = true;
				}
				Write("{0},{1} ", ex, ey);
			}
			else {
				WriteLine("<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\" stroke-linecap=\"round\" fill=\"none\" />", sx, sy, ex, ey, _lastColor, w);
			}
		}

		public void EndLines ()
		{
			if (_inPolyline) {
				WriteLine("\" />");
				_inPolyline = false;
				_startedPolyline = false;
			}
		}
		
		public void DrawString(string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			WriteLine("<text x=\"{0}\" y=\"{1}\" font-family=\"sans-serif\">{2}</text>",
				x, y + _fontMetrics.Height,
				s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
		}

		public void DrawString (string s, float x, float y)
		{
			WriteLine("<text x=\"{0}\" y=\"{1}\" font-family=\"sans-serif\">{2}</text>",
				x, y + _fontMetrics.Height,
				s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
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
	}

	class SvgGraphicsFontMetrics : IFontMetrics
	{
		int _height;
		
		public SvgGraphicsFontMetrics ()
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