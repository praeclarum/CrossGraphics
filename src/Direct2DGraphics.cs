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
using System.Linq;
using SharpDX.Direct2D1;
using SharpDX;

namespace CrossGraphics
{
	public class Direct2DGraphics : IGraphics
	{
		const int MaxFontSize = 120;
		Direct2DGraphicsFontMetrics[] _fontMetrics;
		int _fontSize = 10;

        SolidColorBrush lastBrush;

		Factory d2dFactory;
		SharpDX.DirectWrite.Factory dwFactory;

		RenderTarget dc;

		SharpDX.DXGI.Surface surface;
		SharpDX.WIC.Bitmap bitmap;

		StrokeStyle strokeStyle;
		SharpDX.DirectWrite.TextFormat textFormat;

		class State
		{
			public Matrix3x2 Transform;
		}
		Stack<State> states;

		Direct2DGraphics ()
		{
			d2dFactory = DisposeLater (new Factory (FactoryType.MultiThreaded));
			dwFactory = DisposeLater (new SharpDX.DirectWrite.Factory ());
		}

		public Direct2DGraphics (SharpDX.DXGI.SwapChain swapChain)
			: this ()
		{
			surface = DisposeLater (SharpDX.DXGI.Surface.FromSwapChain (swapChain, 0));
			dc = DisposeLater (new RenderTarget (
				d2dFactory,
				surface,
                new RenderTargetProperties(new PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied)))
            );

			Initialize ();
		}

        public Direct2DGraphics(IntPtr handle, int width, int height)
            : this()
        {
            //surface = DisposeLater(SharpDX.DXGI.Surface.FromSwapChain(swapChain, 0));
            dc = DisposeLater(new WindowRenderTarget(
                d2dFactory,
                new RenderTargetProperties(new PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied)),
                new HwndRenderTargetProperties() { Hwnd = handle, PixelSize = new Size2(width, height), PresentOptions = PresentOptions.None }
                )
            );

            Initialize();
        }

		public Direct2DGraphics (int width, int height)
			: this ()
		{
			var wicFactory = new SharpDX.WIC.ImagingFactory();
			bitmap = DisposeLater (new SharpDX.WIC.Bitmap (
				wicFactory,
				width, height,
				SharpDX.WIC.PixelFormat.Format32bppPBGRA,
				SharpDX.WIC.BitmapCreateCacheOption.CacheOnDemand));
			dc = DisposeLater (new WicRenderTarget (
				d2dFactory,
				bitmap,
				new RenderTargetProperties (
					RenderTargetType.Default,
					new SharpDX.Direct2D1.PixelFormat (SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
					d2dFactory.DesktopDpi.Width, d2dFactory.DesktopDpi.Height,
					RenderTargetUsage.None,
					FeatureLevel.Level_DEFAULT)));

			Initialize ();
		}

		void Initialize ()
		{
			strokeStyle = DisposeLater (new StrokeStyle (d2dFactory, new StrokeStyleProperties {
				EndCap = CapStyle.Round,
				StartCap = CapStyle.Round,
			}));

			states = new Stack<State> ();
			states.Push (new State {
				Transform = Matrix3x2.Identity,
			});

            lastBrush = new SolidColorBrush(dc, Color4.Black);

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

		List<IDisposable> toDispose;

		T DisposeLater<T> (T d) where T : IDisposable
		{
			if (toDispose == null) toDispose = new List<IDisposable> ();
			toDispose.Add (d);
			return d;
		}

		protected virtual void Dispose (bool disposing)
		{
			if (toDispose == null) return;
			toDispose.Reverse ();
			foreach (var d in toDispose) {
				if (d != null) {
					d.Dispose ();
				}
			}
			toDispose.Clear ();
			toDispose = null;

			if (textFormat != null) {
				textFormat.Dispose ();
				textFormat = null;
			}
            if (lastBrush != null)
            {
                lastBrush.Dispose();
                lastBrush = null;
            }
		}

		public byte[] GetPixels ()
		{
			if (bitmap == null) {
				throw new InvalidOperationException ("Cannot GetPixels on SwapChain graphics");
			}

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
            var color4 = new Color4(c.RedValue, c.GreenValue, c.BlueValue, c.AlphaValue);
            if (lastBrush.Color != color4)
            {
                lastBrush.Dispose();
                lastBrush = new SolidColorBrush(dc, color4);
            }
		}

		public void FillPolygon (Polygon poly)
		{
			var g = poly.GetGeometry (d2dFactory);
			dc.FillGeometry (g, lastBrush);
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			var g = poly.GetGeometry (d2dFactory);
            dc.DrawGeometry(g, lastBrush, w);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			dc.FillRoundedRectangle (new RoundedRectangle {
				Rect = new RectangleF (x, y, width, height),
				RadiusX = radius,
				RadiusY = radius,
            }, lastBrush);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			dc.DrawRoundedRectangle (new RoundedRectangle {
				Rect = new RectangleF (x, y, width, height),
				RadiusX = radius,
				RadiusY = radius,
            }, lastBrush, w);
		}

		public void FillRect (float x, float y, float width, float height)
		{
            dc.FillRectangle(new RectangleF(x, y, width, height), lastBrush);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
            dc.DrawRectangle(new RectangleF(x, y, width, height), lastBrush, w);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var rx = width / 2;
			var ry = height / 2;
            dc.FillEllipse(new Ellipse(new Vector2(x + rx, y + ry), rx, ry), lastBrush);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var rx = width / 2;
			var ry = height / 2;
            dc.DrawEllipse(new Ellipse(new Vector2(x + rx, y + ry), rx, ry), lastBrush, w);
		}

		public void BeginLines (bool rounded)
		{
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
            dc.DrawLine(new Vector2(sx, sy), new Vector2(ex, ey), lastBrush, w, strokeStyle);
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
				new RectangleF (x, yy, 1000, 1000),
                lastBrush);
		}

		public void DrawString (string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			var yy = y - FontOffsetScale * _fontSize;

			dc.DrawText (
				s,
				textFormat,
				new RectangleF (x, yy, width, height),
                lastBrush);
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

		public Transform2D Transform
		{
			get
			{
				var dt = dc.Transform;
				var t = new Transform2D ();
				t.M11 = dt.M11;
				t.M21 = dt.M12;
				t.M12 = dt.M21;
				t.M22 = dt.M22;
				t.M13 = dt.M31;
				t.M23 = dt.M32;
				return t;
			}
			set
			{
				var dt = new Matrix3x2 ();
				dt.M11 = value.M11;
				dt.M12 = value.M21;
				dt.M21 = value.M12;
				dt.M22 = value.M22;
				dt.M31 = value.M13;
				dt.M32 = value.M23;
				dc.Transform = dt;
			}
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
			get
			{
				return _height;
			}
		}

		public int Ascent
		{
			get
			{
				return Height;
			}
		}

		public int Descent
		{
			get
			{
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
                    gs.BeginFigure(new Vector2(poly.Points[0].X, poly.Points[0].Y), FigureBegin.Filled);
					gs.AddLines (poly.Points.Select (p => new Vector2 (p.X, p.Y)).ToArray ());
					gs.EndFigure (FigureEnd.Closed);
					gs.Close ();
				}

				g = pg;
				//poly.Tag = g;
			}
			return g;
		}
	}
}

