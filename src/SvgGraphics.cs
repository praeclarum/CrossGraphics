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
using System.Numerics;

namespace CrossGraphics
{
	public class SvgGraphics : IGraphics
	{
		readonly TextWriter _tw;

		SvgGraphicsFontMetrics _fontMetrics;
		//Font _lastFont = null;
		string _lastColor = null;
		string _lastColorOpacity = null;

		readonly IFormatProvider icult = System.Globalization.CultureInfo.InvariantCulture;
		
		class State {
            public Matrix3x2 Transform = Matrix3x2.Identity;
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
			_fontMetrics = new SvgGraphicsFontMetrics (Font.SystemFontOfSize (16));
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
			WriteLine (string.Format (icult, format, args));
		}
		void Write (string s)
		{
			_tw.Write (s);
		}
		void Write (string format, params object[] args)
		{
			Write (string.Format (icult, format, args));
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
			var id = (entity != null) ? entity.ToString () : "";
			id = id.Trim ().Replace ("\n", " ").Replace ("&", "&amp;").Replace ("\"", "&quot;").Replace ("'", "&apos;").Replace ("<", "&lt;").Replace (">", "&gt;");
			WriteLine ("<g id=\"{0}\">", id);
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
				Transform = _state.Transform,
				ClippingRect = _state.ClippingRect,
			};
			_states.Push (ns);
			_state = ns;
		}

        public void Scale (float sx, float sy)
        {
			_state.Transform = Matrix3x2.Multiply (Matrix3x2.CreateScale (sx, sy), _state.Transform);
		}

		public void Translate (float dx, float dy)
		{
			_state.Transform = Matrix3x2.Multiply (Matrix3x2.CreateTranslation (dx, dy), _state.Transform);
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
			_fontMetrics = new SvgGraphicsFontMetrics (f);
		}

		static string FormatColor (Color c)
		{
			return string.Format("#{0:X2}{1:X2}{2:X2}", c.Red, c.Green, c.Blue);
		}
		
		static string FormatColor (byte r, byte g, byte b)
		{
			return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
		}

		public void SetColor (Color c)
		{
			_lastColor = FormatColor (c);
			_lastColorOpacity = string.Format (icult, "{0}", c.Alpha / 255.0);
		}
		
		public void SetRgba (byte r, byte g, byte b, byte a)
		{
			_lastColor = FormatColor (r, g, b);
			_lastColorOpacity = string.Format (icult, "{0}", a / 255.0);
		}

		PointF Transform (PointF p)
		{
			var t = Vector2.Transform (new Vector2 (p.X, p.Y), _state.Transform);
			return new PointF (t.X, t.Y);
		}

		PointF Transform (float x, float y)
		{
			var t = Vector2.Transform (new Vector2 (x, y), _state.Transform);
			return new PointF (t.X, t.Y);
		}

		RectangleF Transform (float x, float y, float width, float height)
		{
			var t0 = Vector2.Transform (new Vector2 (x, y), _state.Transform);
			var t1 = Vector2.Transform (new Vector2 (x + width, y + height), _state.Transform);
			if (t1.X < t0.X) {
				var t = t1.X;
				t1.X = t0.X;
				t0.X = t;
			}
			if (t1.Y < t0.Y) {
				var t = t1.Y;
				t1.Y = t0.Y;
				t0.Y = t;
			}
			return new RectangleF (t0.X, t0.Y, t1.X - t0.X, t1.Y - t0.Y);
		}

		float XScale => _state.Transform.M11;

		public void FillPolygon (Polygon poly)
		{
			Write ("<polygon fill=\"{0}\" fill-opacity=\"{1}\" stroke=\"none\" points=\"", _lastColor, _lastColorOpacity);
			foreach (var p in poly.Points) {
				var t = Transform (p);
				Write("{0}", t.X);
				Write(",");
				Write ("{0}", t.Y);
				Write(" ");
			}
			WriteLine("\" />");
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			Write("<polygon stroke=\"{0}\" stroke-opacity=\"{1}\" stroke-width=\"{2}\" fill=\"none\" points=\"", _lastColor, _lastColorOpacity, w * XScale);
			foreach (var p in poly.Points) {
				var t = Transform (p);
				Write ("{0}", t.X);
				Write(",");
				Write("{0}", t.Y);
				Write(" ");
			}
			WriteLine("\" />");
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var t = Transform (x, y, width, height);
			var rx = t.Width / 2;
			var ry = t.Height / 2;
			var cx = t.X + rx;
			var cy = t.Y + ry;
			WriteLine("<ellipse cx=\"{0}\" cy=\"{1}\" rx=\"{2}\" ry=\"{3}\" fill=\"{4}\" fill-opacity=\"{5}\" stroke=\"none\" />", 
				cx, cy, rx, ry, _lastColor, _lastColorOpacity);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var t = Transform (x, y, width, height);
			var rx = t.Width / 2;
			var ry = t.Height / 2;
			var cx = t.X + rx;
			var cy = t.Y + ry;
			WriteLine ("<ellipse cx=\"{0}\" cy=\"{1}\" rx=\"{2}\" ry=\"{3}\" stroke=\"{4}\" stroke-opacity=\"{5}\" stroke-width=\"{6}\" fill=\"none\" />", 
				cx, cy, rx, ry, _lastColor, _lastColorOpacity, w * XScale);
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			WriteArc (cx, cy, radius, startAngle, endAngle, 0, "none", "0", _lastColor, _lastColorOpacity);
		}
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			WriteArc (cx, cy, radius, startAngle, endAngle, w, _lastColor, _lastColorOpacity, "none", "0");
		}

		public void WriteArc (float cx, float cy, float radius, float startAngle, float endAngle, float w, string stroke, string strokeOp, string fill, string fillOp)
		{
			var sa = startAngle;
			var ea = endAngle;
			
			var sx = cx + radius * Math.Cos (sa);
			var sy = cy - radius * Math.Sin (sa);
			var ex = cx + radius * Math.Cos (ea);
			var ey = cy - radius * Math.Sin (ea);
			var c = Transform (cx, cy);
			var s = Transform ((float)sx, (float)sy);
			sx = s.X;
			sy = s.Y;
			var e = Transform ((float)ex, (float)ey);
			ex = e.X;
			ey = e.Y;
			var sc = XScale;
			
			var largeArcFlag = 0;
			var sweepFlag = 0;

			var dx = ex - sx;
			var dy = ey - sy;
			var distance = Math.Sqrt(dx * dx + dy * dy);
    
			if (distance < radius * 2) {
				var h  = Math.Sqrt (radius * radius - (distance * distance / 4));
				var ux  = -dy / distance;
				var uy  = dx / distance;
				var midX = (sx + ex) / 2;
				var midY = (sy + ey) / 2;
				var shortArcCenterX  = midX - h * ux;
				var shortArcCenterY  = midY - h * uy;
				var longArcCenterX  = midX + h * ux;
				var longArcCenterY  = midY + h * uy;
				var shortDistSqr = (shortArcCenterX - c.X) * (shortArcCenterX - c.X) + (shortArcCenterY - c.Y) * (shortArcCenterY - c.Y);
				var longDistSqr = (longArcCenterX - c.X) * (longArcCenterX - c.X) + (longArcCenterY - c.Y) * (longArcCenterY - c.Y);
				if (longDistSqr < shortDistSqr) {
					largeArcFlag = 1;
				}
			}

			WriteLine ("<path d=\"M {0} {1} A {2} {3} 0 {11} {12} {4} {5}\" stroke=\"{6}\" stroke-opacity=\"{7}\" stroke-width=\"{8}\" fill=\"{9}\" fill-opacity=\"{10}\" />", 
				sx, sy,
				radius * sc, radius * sc,
				ex, ey,				 
				stroke, strokeOp, w * sc, fill, fillOp,
				largeArcFlag, sweepFlag);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			var sc = XScale;
			var t = Transform (x, y, width, height);
			WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{6}\" ry=\"{6}\" fill=\"{4}\" fill-opacity=\"{5}\" stroke=\"none\" />", 
				t.X, t.Y, t.Width, t.Height, _lastColor, _lastColorOpacity, radius * XScale);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			var sc = XScale;
			var t = Transform (x, y, width, height);
			WriteLine ("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{7}\" ry=\"{7}\" stroke=\"{4}\" stroke-opacity=\"{5}\" stroke-width=\"{6}\" fill=\"none\" />",
				t.X, t.Y, t.Width, t.Height, _lastColor, _lastColorOpacity, w * sc, radius * sc);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			var t = Transform (x, y, width, height);
			WriteLine ("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\" fill-opacity=\"{5}\" stroke=\"none\" />",
				t.X, t.Y, t.Width, t.Height, _lastColor, _lastColorOpacity);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			var sc = XScale;
			var t = Transform (x, y, width, height);
			WriteLine ("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" stroke=\"{4}\" stroke-opacity=\"{5}\" stroke-width=\"{6}\" fill=\"none\" />",
				t.X, t.Y, t.Width, t.Height, _lastColor, _lastColorOpacity, w * sc);
		}
		
		bool _inPolyline = false;
		bool _startedPolyline = false;

		public void BeginLines (bool rounded)
		{
			_inPolyline = true;
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			var sc = XScale;
			var s = Transform (sx, sy);
			var e = Transform (ex, ey);
			if (_inPolyline) {
				if (!_startedPolyline) {
					Write ("<polyline stroke=\"{0}\" stroke-opacity=\"{1}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"{2}\" fill=\"none\" points=\"",
						_lastColor, _lastColorOpacity, w * sc);
					Write("{0},{1} ", s.X, s.Y);
					_startedPolyline = true;
				}
				Write("{0},{1} ", e.X, e.Y);
			}
			else {
				WriteLine("<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"{4}\" stroke-opacity=\"{5}\" stroke-width=\"{6}\" stroke-linecap=\"round\" fill=\"none\" />",
					s.X, s.Y, e.X, e.Y, _lastColor, _lastColorOpacity, w * sc);
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
			var sc = XScale;
			var t = Transform (x, y, width, height);
			var tx = t.X;
			if (align == TextAlignment.Right) {
				tx = t.Right - _fontMetrics.StringWidth (s) * sc;
			}
			WriteLine ("<text x=\"{0}\" y=\"{1}\" font-family=\"sans-serif\" font-size=\"{2}\" fill=\"{4}\">{3}</text>",
				tx, t.Y + _fontMetrics.Height * 0.8f * sc,
				_fontMetrics.Height * sc,
				s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"),
				_lastColor);
		}

		public void DrawString (string s, float x, float y)
		{
			var sc = XScale;
			var t = Transform (x, y);
			WriteLine("<text x=\"{0}\" y=\"{1}\" font-family=\"sans-serif\" font-size=\"{2}\" fill=\"{4}\">{3}</text>",
				t.X, t.Y + _fontMetrics.Height * 0.8f * sc,
				_fontMetrics.Height * sc,
				s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"),
				_lastColor);
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

	class SvgGraphicsFontMetrics : NullGraphicsFontMetrics
	{
		public SvgGraphicsFontMetrics (Font font) : base (size: font.Size, isBold: font.IsBold, isMonospace: font.IsMonospace)
		{
		}
	}
}

