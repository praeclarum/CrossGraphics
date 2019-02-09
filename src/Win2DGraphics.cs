//
// Copyright (c) 2010-2019 Frank A. Krueger
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
using System.Drawing;
using System.Collections.Generic;

using System.Numerics;
using NativePoint = System.Numerics.Vector2;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;

namespace CrossGraphics.Win2D
{
	public class Win2DGraphics : IGraphics
	{
		CanvasDrawingSession _c;
		Font _font;
		Color activeColor;
		Windows.UI.Color wcolor;

		CanvasPathBuilder _linesPath = null;
		int _linesCount = 0;
		float _lineWidth = 1;

		public CanvasDrawingSession Canvas { get { return _c; } }

		public Win2DGraphics (CanvasDrawingSession drawingSession)
		{
			_c = drawingSession;
			_font = null;
			SetColor (Colors.Black);
		}

		public void BeginEntity (object entity)
		{
		}

		public void SetFont (Font font)
		{
			_font = font;
		}

		public void Clear (Color c)
		{
		}

		public void SetColor (Color c)
		{
			activeColor = c;
			wcolor = c.ToWindowsColor ();
		}

		CanvasGeometry GetPolyPath (Polygon poly, CanvasFigureFill fill)
		{
			using (var p = new CanvasPathBuilder (_c)) {
				var ps = poly.Points;
				if (ps.Count > 2) {
					p.BeginFigure (ps[0].X, ps[0].Y, fill);
					for (var i = 1; i < ps.Count; i++) {
						var pt = ps[i];
						p.AddLine (pt.X, pt.Y);
					}
					p.EndFigure (CanvasFigureLoop.Closed);
				}
				return CanvasGeometry.CreatePath (p);
			}
		}

		public void FillPolygon (Polygon poly)
		{
			using (var g = GetPolyPath (poly, CanvasFigureFill.Default)) {
				_c.FillGeometry (g, wcolor);
			}
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			using (var g = GetPolyPath (poly, CanvasFigureFill.Default)) {
				_c.DrawGeometry (g, wcolor, w);
			}
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			_c.FillRoundedRectangle (x, y, width, height, radius, radius, wcolor);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{			
			_c.DrawRoundedRectangle (x, y, width, height, radius, radius, wcolor, w);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			_c.FillRectangle (x, y, width, height, wcolor);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			_c.DrawRectangle (x, y, width, height, wcolor, w);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			_c.FillEllipse (new Vector2 (x + width / 2, y + height / 2), width / 2, height / 2, wcolor);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			_c.DrawEllipse (new Vector2 (x + width / 2, y + height / 2), width / 2, height / 2, wcolor, w);
		}

		const float RadiansToDegrees = (float)(180 / Math.PI);

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
		}

		public void BeginLines (bool rounded)
		{
			if (_linesPath != null) {
				_linesPath = new CanvasPathBuilder (_c);
				_linesCount = 0;
				_lineWidth = 1;
			}
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_linesPath != null) {
				if (_linesCount == 0) {
					_linesPath.BeginFigure (sx, sy, CanvasFigureFill.DoesNotAffectFills);
				}
				_linesPath.AddLine (ex, ey);
				_lineWidth = w;
				_linesCount++;
			}
			else {
				_c.DrawLine (sx, sy, ex, ey, wcolor, w);
			}
		}

		public void EndLines ()
		{
			if (_linesPath != null) {
				using (var g = CanvasGeometry.CreatePath (_linesPath)) {
					_c.DrawGeometry (g, wcolor, _lineWidth);
				}
				_linesPath.Dispose ();
				_linesPath = null;
			}
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			var dimg = img as Win2DImage;
			if (dimg != null) {
			}
		}

		public void DrawString (string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			if (string.IsNullOrWhiteSpace (s)) return;
			DrawString (s, x, y);
		}

		public void DrawString (string s, float x, float y)
		{
			if (string.IsNullOrWhiteSpace (s)) return;

			_c.DrawText (s, x, y, wcolor);
		}

		public IFontMetrics GetFontMetrics ()
		{
			return GetFontInfo (_font).FontMetrics;
		}

		static Win2DFontInfo GetFontInfo (Font f)
		{
			var fi = f.Tag as Win2DFontInfo;
			if (fi == null) {
				var name = "Helvetica";
				if (f.FontFamily == "Monospace") {
					name = "Courier";
				}
				else if (f.FontFamily == "DBLCDTempBlack") {
#if __MACOS__
					name = "Courier-Bold";
#else
					name = f.FontFamily;
#endif
				}

				fi = new Win2DFontInfo ();
				f.Tag = fi;
			}
			return fi;
		}
		public IImage ImageFromFile (string path)
		{
			return null;
		}

		public void SaveState ()
		{
		}

		public void SetClippingRect (float x, float y, float width, float height)
		{
		}

		public void Translate (float dx, float dy)
		{
		}

		public void Scale (float sx, float sy)
		{
		}

		public void RestoreState ()
		{
		}
	}

	class Win2DFontInfo
	{
		public Win2DFontMetrics FontMetrics;
	}

	public class Win2DImage : IImage
	{
	}

	public class Win2DFontMetrics : NullGraphicsFontMetrics
	{
		public Win2DFontMetrics (int size, bool isBold = false) : base (size, isBold)
		{
		}
	}

	public static partial class Conversions
	{
		public static Windows.UI.Color ToWindowsColor (this Color c) =>
			Windows.UI.Color.FromArgb ((byte)c.Alpha, (byte)c.Red, (byte)c.Green, (byte)c.Blue);
	}
}

