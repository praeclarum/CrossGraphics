//
// Copyright (c) 2010 Frank A. Krueger
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

namespace CrossGraphics.Svg
{
	public class SvgGraphics : IGraphics
	{
		TextWriter _tw;

		SvgGraphicsFontMetrics _fontMetrics;
		//Font _lastFont = null;
		string _lastColor = null;
		
		public SvgGraphics (TextWriter tw, RectangleF viewBox)
		{
			_tw = tw;
			_fontMetrics = new SvgGraphicsFontMetrics ();
			SetColor (Colors.Black);
			
			_tw.WriteLine(@"<?xml version=""1.0""?>
<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 1.1//EN"" ""http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"">");
			_tw.WriteLine(@"<svg viewBox=""{0} {1} {2} {3}"" preserveAspectRatio=""xMinYMin meet"" version=""1.1"" xmlns=""http://www.w3.org/2000/svg"">",
				viewBox.Left, viewBox.Top, viewBox.Width, viewBox.Height);
		}
		
		public SvgGraphics (TextWriter tw, Rectangle viewBox) : this(tw, new RectangleF(viewBox.Left, viewBox.Top, viewBox.Width, viewBox.Height))
		{
		}

		public SvgGraphics (Stream s, RectangleF viewBox) : this(new StreamWriter(s, System.Text.Encoding.UTF8), viewBox)
		{
		}
		
		public void Finish() {
			_tw.WriteLine("</svg>");
			_tw.Flush();
		}

		public void SetFont (Font f)
		{
			//_lastFont = f;
		}

		public void SetColor (Color c)
		{
			_lastColor = string.Format("#{0:X2}{1:X2}{2:X2}", c.Red, c.Green, c.Blue);
		}

		public void FillPolygon (Polygon poly)
		{
			_tw.Write("<polygon fill=\"{0}\" stroke=\"none\" points=\"", _lastColor);
			foreach (var p in poly.Points) {
				_tw.Write(p.X);
				_tw.Write(",");
				_tw.Write(p.Y);
				_tw.Write(" ");
			}
			_tw.WriteLine("\" />");
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			_tw.Write("<polygon stroke=\"{0}\" stroke-width=\"{1}\" fill=\"none\" points=\"", _lastColor, w);
			foreach (var p in poly.Points) {
				_tw.Write(p.X);
				_tw.Write(",");
				_tw.Write(p.Y);
				_tw.Write(" ");
			}
			_tw.WriteLine("\" />");
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var rx = width / 2;
			var ry = height / 2;
			var cx = x + rx;
			var cy = y + ry;
			_tw.WriteLine("<ellipse cx=\"{0}\" cy=\"{1}\" rx=\"{2}\" ry=\"{3}\" fill=\"{4}\" stroke=\"none\" />", 
				cx, cy, rx, ry, _lastColor);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var rx = width / 2;
			var ry = height / 2;
			var cx = x + rx;
			var cy = y + ry;
			_tw.WriteLine("<ellipse cx=\"{0}\" cy=\"{1}\" rx=\"{2}\" ry=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\" fill=\"none\" />", 
				cx, cy, rx, ry, _lastColor, w);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			_tw.WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{5}\" ry=\"{5}\" fill=\"{4}\" stroke=\"none\" />", 
				x, y, width, height, _lastColor, radius);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			_tw.WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{6}\" ry=\"{6}\" stroke=\"{4}\" stroke-width=\"{5}\" fill=\"none\" />", 
				x, y, width, height, _lastColor, w, radius);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			_tw.WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\" stroke=\"none\" />", 
				x, y, width, height, _lastColor);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			_tw.WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\" fill=\"none\" />", 
				x, y, width, height, _lastColor, w);
		}
		
		bool _inPolyline = false;
		bool _startedPolyline = false;

		public void BeginLines ()
		{
			_inPolyline = true;
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_inPolyline) {
				if (!_startedPolyline) {				
					_tw.Write("<polyline stroke=\"{0}\" stroke-width=\"{1}\" fill=\"none\" points=\"", _lastColor, w);
					_tw.Write("{0},{1} ", sx, sy);
					_startedPolyline = true;
				}
				_tw.Write("{0},{1} ", ex, ey);
			}
			else {
				_tw.WriteLine("<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\" stroke-linecap=\"round\" fill=\"none\" />", sx, sy, ex, ey, _lastColor, w);
			}
		}

		public void EndLines ()
		{
			if (_inPolyline) {
				_tw.WriteLine("\" />");
				_inPolyline = false;
				_startedPolyline = false;
			}
		}

		public void DrawString (string s, float x, float y)
		{
			_tw.WriteLine("<text x=\"{0}\" y=\"{1}\" font-family=\"sans-serif\">{2}</text>",
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
		
		public void BeginEntity (object entity)
		{
		}
	}

	public class SvgGraphicsFontMetrics : IFontMetrics
	{
		int _height;
		
		public SvgGraphicsFontMetrics ()
		{
			_height = 10;
		}

		public int StringWidth (string str)
		{
			return str.Length * 10;
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

