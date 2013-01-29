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
using System.Linq;
using SharpDX.Direct2D1;
using SharpDX;
using SharpDX.WIC;
using System.Collections.Generic;

namespace CrossGraphics.Direct2D
{
	public class Direct2DGraphics : IGraphics
	{
		const int MaxFontSize = 120;
		Direct2DGraphicsFontMetrics[] _fontMetrics;
		int _fontSize = 10;

		Color lastColor;

		Factory factory;
		SharpDX.WIC.Bitmap bitmap;
		RenderTarget dc;
		StrokeStyle strokeStyle;

		SharpDX.DirectWrite.Factory dwFactory;
		SharpDX.DirectWrite.TextFormat textFormat;

		class State
		{
			public Matrix3x2 Transform;
		}
		Stack<State> states;

		public byte[] GetPixels ()
		{
			var width = bitmap.Size.Width;
			var numBytes = width * bitmap.Size.Height * 4;
			var bytes = new byte[numBytes];
			unsafe {
				fixed (byte* pb = bytes) {
					bitmap.CopyPixels (width * 4, new IntPtr (pb), numBytes);
				}
			}
			return bytes;
		}

		public Direct2DGraphics (int width, int height)
		{
			factory = new Factory (FactoryType.MultiThreaded);
			dwFactory = new SharpDX.DirectWrite.Factory ();

			//
			// Create the back buffer
			//
			var wicFactory = new ImagingFactory2 ();
			bitmap = new SharpDX.WIC.Bitmap (wicFactory, width, height, SharpDX.WIC.PixelFormat.Format32bppPBGRA, BitmapCreateCacheOption.CacheOnDemand);
			dc = new WicRenderTarget (
				factory,
				bitmap,
				new RenderTargetProperties (
					RenderTargetType.Default,
					new SharpDX.Direct2D1.PixelFormat (SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
					factory.DesktopDpi.Width, factory.DesktopDpi.Height,
					RenderTargetUsage.None,
					FeatureLevel.Level_DEFAULT));

			//
			// Initialize
			//
			strokeStyle = new StrokeStyle (factory, new StrokeStyleProperties {
				EndCap = CapStyle.Round,
				StartCap = CapStyle.Round,
			});

			states = new Stack<State> ();
			states.Push (new State {
				Transform = Matrix3x2.Identity,
			});

			lastColor = Colors.Black;

			SetFont (Font.SystemFontOfSize (16));
		}

		~Direct2DGraphics ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (dwFactory != null) {
				dwFactory.Dispose ();
				dwFactory = null;
			}
			if (textFormat != null) {
				textFormat.Dispose ();
				textFormat = null;
			}
			if (strokeStyle != null) {
				strokeStyle.Dispose ();
				strokeStyle = null;
			}
			if (dc != null) {                
				dc.Dispose ();
				dc = null;
			}
			if (bitmap != null) {
				bitmap.Dispose ();
				bitmap = null;
			}
			if (factory != null) {
				factory.Dispose ();
				factory = null;
			}
		}

		public void BeginDrawing ()
		{
			dc.BeginDraw ();
		}

		public void Clear (Color clearColor)
		{
			dc.Clear (new Color4 (clearColor.RedValue, clearColor.GreenValue, clearColor.BlueValue, clearColor.AlphaValue));
		}

		public void EndDrawing ()
		{
			dc.EndDraw ();
		}

		public void SetFont (Font f)
		{
			if (textFormat != null) {
				textFormat.Dispose ();
			}
			textFormat = new SharpDX.DirectWrite.TextFormat (dwFactory, "Segoe", f.Size);
			_fontSize = f.Size;
		}

		public void SetColor (Color c)
		{
			lastColor = c;
		}

		public void FillPolygon (Polygon poly)
		{
			var g = poly.GetGeometry (factory);
			dc.FillGeometry (g, lastColor.GetBrush (dc));
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			var g = poly.GetGeometry (factory);
			dc.DrawGeometry (g, lastColor.GetBrush (dc), w);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			dc.FillRoundedRectangle (new RoundedRectangle {
				Rect = new RectangleF (x, y, x + width, y + height),
				RadiusX = radius,
				RadiusY = radius,
			}, lastColor.GetBrush (dc));
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			dc.DrawRoundedRectangle (new RoundedRectangle {
				Rect = new RectangleF (x, y, x + width, y + height),
				RadiusX = radius,
				RadiusY = radius,
			}, lastColor.GetBrush (dc), w);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			dc.FillRectangle (new RectangleF (x, y, x + width, y + height), lastColor.GetBrush (dc));
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			dc.DrawRectangle (new RectangleF (x, y, x + width, y + height), lastColor.GetBrush (dc), w);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var rx = width / 2;
			var ry = height / 2;
			dc.FillEllipse (new Ellipse (new DrawingPointF (x + rx, y + ry), rx, ry), lastColor.GetBrush (dc));
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var rx = width / 2;
			var ry = height / 2;
			dc.DrawEllipse (new Ellipse (new DrawingPointF (x + rx, y + ry), rx, ry), lastColor.GetBrush (dc), w);
		}		

		public void BeginLines (bool rounded)
		{
		}	

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			dc.DrawLine (new DrawingPointF (sx, sy), new DrawingPointF (ex, ey), lastColor.GetBrush (dc), w, strokeStyle);
		}

		public void EndLines ()
		{
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
		}
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
		}

		const float FontOffsetScale = 0.25f;

		public void DrawString (string s, float x, float y)
		{
			var yy = y - FontOffsetScale * _fontSize;

			dc.DrawText (
				s,
				textFormat,
				new RectangleF (x, yy, x + 1000, yy + 1000),
				lastColor.GetBrush (dc));
		}

		public void DrawString (string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			var yy = y - FontOffsetScale * _fontSize;

			dc.DrawText (
				s,
				textFormat,
				new RectangleF (x, yy, x + width, yy + height),
				lastColor.GetBrush (dc));
		}

		public IFontMetrics GetFontMetrics ()
		{
			if (_fontMetrics == null) {
				_fontMetrics = new Direct2DGraphicsFontMetrics[MaxFontSize + 1];
			}
			var i = Math.Min (_fontMetrics.Length, _fontSize);
			if (_fontMetrics[i] == null) {
				_fontMetrics[i] = new Direct2DGraphicsFontMetrics (i);
			}
			return _fontMetrics[i];
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
		}
		
		public void SaveState ()
		{
			var s = states.Peek ();
			var n = new State {
				Transform = s.Transform,
			};
			states.Push (n);
			dc.Transform = n.Transform;
		}
		
		public void SetClippingRect (float x, float y, float width, float height)
		{
		}
		
		public void Translate (float dx, float dy)
		{
			dc.Transform = Matrix3x2.Multiply (Matrix3x2.Translation (dx, dy), dc.Transform);
		}
		
		public void Scale (float sx, float sy)
		{
			dc.Transform = Matrix3x2.Multiply (Matrix3x2.Scaling (sx, sy), dc.Transform);
		}
		
		public void RestoreState ()
		{
			if (states.Count > 1) {
				states.Pop ();
				var s = states.Peek ();
				dc.Transform = s.Transform;
			}
		}

		public IImage ImageFromFile (string filename)
		{
			return null;
		}
		
		public void BeginEntity (object entity)
		{
		}
	}

	class Direct2DGraphicsFontMetrics : IFontMetrics
	{
		int _height;
		int _charWidth;

		public Direct2DGraphicsFontMetrics (int size)
		{
			_height = size;
			_charWidth = (855 * size) / 1600;
		}

		public int StringWidth (string str, int startIndex, int length)
		{
			return length * _charWidth;
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

	public static class PolygonEx
	{
		public static Geometry GetGeometry (this Polygon poly, Factory factory)
		{
			var g = poly.Tag as Geometry;
			if (g == null) {
				var pg = new PathGeometry (factory);
				using (var gs = pg.Open ()) {
					gs.BeginFigure (new DrawingPointF (poly.Points[0].X, poly.Points[0].Y), FigureBegin.Filled);
					gs.AddLines (poly.Points.Select (p => new DrawingPointF (p.X, p.Y)).ToArray ());
					gs.EndFigure (FigureEnd.Closed);
					gs.Close ();
				}

				g = pg;
				//poly.Tag = g;
			}
			return g;
		}
	}

	public static class ColorEx
	{
		class ColorInfo
		{
			public RenderTarget Target;
			public SolidColorBrush Brush;
		}

		public static SolidColorBrush GetBrush (this Color color, RenderTarget renderTarget)
		{
			var ci = color.Tag as ColorInfo;

			if (ci != null) {
				if (ci.Target != renderTarget) {
					ci = null;
				}
			}

			if (ci == null) {
				ci = new ColorInfo {
					Target = renderTarget,
					Brush = new SolidColorBrush (
						renderTarget,
						new Color4 (color.RedValue, color.GreenValue, color.BlueValue, color.AlphaValue)),
				};
				color.Tag = ci;
			}

			return ci.Brush;
		}
	}
}

