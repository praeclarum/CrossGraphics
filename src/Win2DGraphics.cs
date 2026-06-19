//
// Copyright (c) 2010-2026 Frank A. Krueger
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
using Microsoft.Graphics.Canvas.Text;

namespace CrossGraphics.Win2D
{
	public class Win2DGraphics : IGraphics
	{
		readonly CanvasDrawingSession _c;
		Font _font;
		Color activeColor;
		Windows.UI.Color wcolor;

		CanvasPathBuilder _linesPath = null;
		int _linesCount = 0;
		float _lineWidth = 1;
		static readonly CanvasStrokeStyle strokeStyle = new CanvasStrokeStyle {
			StartCap = CanvasCapStyle.Round,
			EndCap = CanvasCapStyle.Round,
			LineJoin = CanvasLineJoin.Round,
		};

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

		public void SetRgba (byte r, byte g, byte b, byte a)
		{
			activeColor = null;
			wcolor = Windows.UI.Color.FromArgb (a, r, g, b);
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
				_c.DrawGeometry (g, wcolor, w, strokeStyle);
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

		/// <summary>
		/// Match the existing backends' full-circle detection, including over-full sweeps.
		/// </summary>
		static float PositiveAngle (float a)
		{
			var twoPi = MathF.PI * 2.0f;
			var na = a % twoPi;
			if (na < 0) {
				a += twoPi;
			}
			return a;
		}

		static float NormalizeAngle (float a)
		{
			var twoPi = MathF.PI * 2.0f;
			var na = a % twoPi;
			if (na < 0) {
				na += twoPi;
			}
			return na;
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			using (var g = CreateArcGeometry (cx, cy, radius, startAngle, endAngle, CanvasFigureLoop.Closed)) {
				if (g is not null) {
					_c.FillGeometry (g, wcolor);
				}
			}
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			if (float.IsNaN (w) || w <= 0) {
				return;
			}
			using (var g = CreateArcGeometry (cx, cy, radius, startAngle, endAngle, CanvasFigureLoop.Open)) {
				if (g is not null) {
					_c.DrawGeometry (g, wcolor, w, strokeStyle);
				}
			}
		}

		CanvasGeometry CreateArcGeometry (float cx, float cy, float radius, float startAngle, float endAngle, CanvasFigureLoop figureLoop)
		{
			if (float.IsNaN (cx) || float.IsNaN (cy) || float.IsNaN (radius) || float.IsNaN (startAngle) || float.IsNaN (endAngle) || radius <= 0) {
				return null;
			}

			var sweep = PositiveAngle (endAngle - startAngle);
			if (MathF.Abs (sweep) < 1.0e-6f) {
				return null;
			}
			if (MathF.Abs (sweep) >= MathF.PI * 2.0f - 1.0e-6f) {
				return CanvasGeometry.CreateEllipse (_c, cx, cy, radius, radius);
			}

			var startPoint = new Vector2 (
				cx + radius * MathF.Cos (startAngle),
				cy - radius * MathF.Sin (startAngle));
			var endPoint = new Vector2 (
				cx + radius * MathF.Cos (endAngle),
				cy - radius * MathF.Sin (endAngle));
			var dx = endPoint.X - startPoint.X;
			var dy = endPoint.Y - startPoint.Y;
			if (dx * dx + dy * dy < 1.0e-12f) {
				return null;
			}

			var normalizedSweep = NormalizeAngle (endAngle - startAngle);
			var arcSize = normalizedSweep > MathF.PI ? CanvasArcSize.Large : CanvasArcSize.Small;

			using (var p = new CanvasPathBuilder (_c)) {
				p.BeginFigure (startPoint, CanvasFigureFill.Default);
				p.AddArc (endPoint, radius, radius, 0, CanvasSweepDirection.CounterClockwise, arcSize);
				p.EndFigure (figureLoop);
				return CanvasGeometry.CreatePath (p);
			}
		}

		public void BeginLines (bool rounded)
		{
			if (_linesPath is null) {
				_linesPath = new CanvasPathBuilder (_c);
				_linesPath.SetSegmentOptions (rounded ? CanvasFigureSegmentOptions.ForceRoundLineJoin : CanvasFigureSegmentOptions.None);
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
				_c.DrawLine (sx, sy, ex, ey, wcolor, w, strokeStyle);
			}
		}

		public void EndLines ()
		{
			if (_linesPath is not null) {
				_linesPath.EndFigure (CanvasFigureLoop.Open);
				using (var g = CanvasGeometry.CreatePath (_linesPath)) {
					_c.DrawGeometry (g, wcolor, _lineWidth, strokeStyle);
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

			var fm = GetWin2DFontMetrics (_font);

			var fy = y - fm.Height * 0.2f;

			_c.DrawText (s, x, fy, wcolor, fm.Format);
		}

		public IFontMetrics GetFontMetrics ()
		{
			return GetWin2DFontMetrics (_font);
		}

		static Win2DFontMetrics GetWin2DFontMetrics (Font f)
		{
			var fi = f.Win2DTag as Win2DFontMetrics;
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

				fi = new Win2DFontMetrics (name: name, size: f.Size, isBold: f.IsBold);
				f.Win2DTag = fi;
			}
			return fi;
		}
		public IImage ImageFromFile (string path)
		{
			return null;
		}

		struct State
		{
			public Matrix3x2 Transform;
		}
		readonly List<State> _states = new List<State>();

		public void SaveState ()
		{
			var s = new State {
				Transform = _c.Transform
			};
			_states.Add (s);
		}

		public void SetClippingRect (float x, float y, float width, float height)
		{
		}

		public void Translate (float dx, float dy)
		{
			_c.Transform = Matrix3x2.CreateTranslation(dx, dy) * _c.Transform;
		}

		public void Scale (float sx, float sy)
		{
			_c.Transform = Matrix3x2.CreateScale (sx, sy) * _c.Transform;
		}

		public void RestoreState ()
		{
			if (_states.Count > 0) {
				var s = _states[_states.Count - 1];
				_states.RemoveAt (_states.Count - 1);
				_c.Transform = s.Transform;
			}
		}
	}

	public class Win2DImage : IImage
	{
	}

	public class Win2DFontMetrics : NullGraphicsFontMetrics
	{
		CanvasTextFormat _format;
		public CanvasTextFormat Format {
			get
			{
				if (_format is not null) return _format;
				var f = new CanvasTextFormat () {
					FontSize = Height,
					FontWeight = new Windows.UI.Text.FontWeight (IsBold ? (ushort)700 : (ushort)400),
				};
				_format = f;
				return f;
			}
		}
		public Win2DFontMetrics (string name, int size, bool isBold = false) : base (size, isBold)
		{
		}
	}

	public static partial class Conversions
	{
		public static Windows.UI.Color ToWindowsColor (this Color c) =>
			Windows.UI.Color.FromArgb ((byte)c.Alpha, (byte)c.Red, (byte)c.Green, (byte)c.Blue);
	}
}
