//
// Copyright (c) 2010-2018 Frank A. Krueger
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

using SkiaSharp;

namespace CrossGraphics.Skia
{
	public class SkiaGraphics : IGraphics
	{
		SKCanvas _c;
		ColPaints _paints;
		Font _font;

		public SKCanvas Canvas { get { return _c; } }

		class ColPaints
		{
			public SKPaint Fill;
			public SKPaint Stroke;
			public Font Font;
		}

		public SkiaGraphics (SKSurface surface)
			: this (surface.Canvas)
		{
		}

		public SkiaGraphics (SKCanvas canvas)
		{
			_c = canvas;
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
			if (c.SkiaTag is ColPaints paints) {
				_paints = paints;
				return;
			}

			var stroke = new SKPaint ();
			stroke.Color = c.ToSkiaColor ();
			stroke.IsAntialias = true;
			stroke.Style = SKPaintStyle.Stroke;
			stroke.StrokeCap = SKStrokeCap.Round;
			stroke.StrokeJoin = SKStrokeJoin.Round;
			var fill = new SKPaint ();
			fill.Color = stroke.Color;
			fill.IsAntialias = true;
			fill.Style = SKPaintStyle.Fill;
			paints = new ColPaints {
				Fill = fill,
				Stroke = stroke
			};
			c.SkiaTag = paints;
			_paints = paints;
		}

		SKPath GetPolyPath (Polygon poly)
		{
			var p = poly.SkiaTag as SKPath;
			if (p == null || p.PointCount != poly.Points.Count) {
				p = new SKPath ();
				var ps = poly.Points;
				if (ps.Count > 2) {
					p.MoveTo (ps[0].X, ps[0].Y);
					for (var i = 1; i < ps.Count; i++) {
						var pt = ps[i];
						p.LineTo (pt.X, pt.Y);
					}
					p.Close ();
				}
				poly.SkiaTag = p;
			}
			return p;
		}

		public void FillPolygon (Polygon poly)
		{
			_c.DrawPath (GetPolyPath (poly), _paints.Fill);
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawPath (GetPolyPath (poly), _paints.Stroke);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			_c.DrawRoundRect (new SKRect (x, y, x + width, y + height), radius, radius, _paints.Fill);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawRoundRect (new SKRect (x, y, x + width, y + height), radius, radius, _paints.Stroke);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			_c.DrawRect (new SKRect (x, y, x + width, y + height), _paints.Fill);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawRect (new SKRect (x, y, x + width, y + height), _paints.Stroke);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			_c.DrawOval (new SKRect (x, y, x + width, y + width), _paints.Fill);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawOval (new SKRect (x, y, x + width, y + width), _paints.Stroke);
		}

		const float RadiansToDegrees = (float)(180 / Math.PI);

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			var sa = -startAngle * RadiansToDegrees;
			var ea = -endAngle * RadiansToDegrees;
			using (var p = new SKPath ()) {
				p.AddArc (new SKRect (cx - radius, cy - radius, cx + radius, cy + radius), sa, ea - sa);
				_c.DrawPath (p, _paints.Fill);
			}
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			var sa = -startAngle * RadiansToDegrees;
			var ea = -endAngle * RadiansToDegrees;
			_paints.Stroke.StrokeWidth = w;
			using (var p = new SKPath ()) {
				p.AddArc (new SKRect (cx - radius, cy - radius, cx + radius, cy + radius), sa, ea - sa);
				_c.DrawPath (p, _paints.Stroke);
			}
		}

		bool _inLines = false;
		SKPath _linesPath = null;
		int _linesCount = 0;
		float _lineWidth = 1;

		public void BeginLines (bool rounded)
		{
			if (!_inLines) {
				_inLines = true;
				_linesPath = new SKPath ();
				_linesCount = 0;
			}
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_inLines) {
				if (_linesCount == 0) {
					_linesPath.MoveTo (sx, sy);
				}
				_linesPath.LineTo (ex, ey);
				_lineWidth = w;
				_linesCount++;
			}
			else {
				_paints.Stroke.StrokeWidth = w;
				_c.DrawLine (sx, sy, ex, ey, _paints.Stroke);
			}
		}

		public void EndLines ()
		{
			if (_inLines) {
				_inLines = false;
				_paints.Stroke.StrokeWidth = _lineWidth;
				_paints.Stroke.StrokeJoin = SKStrokeJoin.Round;
				_c.DrawPath (_linesPath, _paints.Stroke);
				_linesPath.Dispose ();
				_linesPath = null;
			}
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			var dimg = img as SkiaImage;
			if (dimg != null) {
				SetColor (Colors.White);
				_c.DrawBitmap (
					dimg.Bitmap,
					new SKRect (0, 0, dimg.Bitmap.Width, dimg.Bitmap.Height),
					new SKRect (x, y, x + width, y + height),
					_paints.Fill);
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

			SetFontOnPaints ();
			var fm = GetFontMetrics ();
			_c.DrawText (s, x, y + fm.Ascent, _paints.Fill);
		}

		public IFontMetrics GetFontMetrics ()
		{
			return _font.SkiaTag ?? GetFontInfo (_font);
		}

		static SkiaFontMetrics GetFontInfo (Font f, SKPaint p = null)
		{
			var fi = f.SkiaTag as SkiaFontMetrics;
			if (fi == null) {
				fi = new SkiaFontMetrics (f, p ?? new SKPaint ());
				f.SkiaTag = fi;
			}
			return fi;
		}

		static void ApplyFontToPaint (Font f, SKPaint p)
		{
			var fi = GetFontInfo (f, p);
			p.Typeface = fi.Typeface;
			p.TextSize = f.Size;
		}

		void SetFontOnPaints ()
		{
			var f = _paints.Font;
			if (f == null || f != _font) {
				f = _font;
				_paints.Font = f;
				ApplyFontToPaint (f, _paints.Fill);
			}
		}

		public IImage ImageFromFile (string path)
		{
			var bmp = SKBitmap.Decode (path);
			if (bmp == null) return null;

			var dimg = new SkiaImage () {
				Bitmap = bmp
			};
			return dimg;
		}

		public void SaveState ()
		{
			_c.Save ();
		}

		public void SetClippingRect (float x, float y, float width, float height)
		{
			_c.ClipRect (new SKRect (x, y, x + width, y + height));
		}

		public void Translate (float dx, float dy)
		{
			_c.Translate (dx, dy);
		}

		public void Scale (float sx, float sy)
		{
			_c.Scale (sx, sy);
		}

		public void RestoreState ()
		{
			_c.Restore ();
		}
	}

	public class SkiaImage : IImage
	{
		public SKBitmap Bitmap;
	}

	public class SkiaFontMetrics : IFontMetrics
	{
		readonly SKPaint paint;

		public readonly SKTypeface Typeface;

		public SkiaFontMetrics (Font font, SKPaint paint)
		{
			var name = "Helvetica";
			if (font.FontFamily == "Monospace") {
				name = "Courier";
			}
			else if (font.FontFamily == "DBLCDTempBlack") {
#if __MACOS__
					name = "Courier-Bold";
#else
				name = font.FontFamily;
#endif
			}

			Typeface = SKTypeface.FromFamilyName (name, font.IsBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
			paint.Typeface = Typeface;
			paint.TextSize = font.Size;

			this.paint = paint;
			Ascent = (int)Math.Abs (paint.FontMetrics.Ascent + 0.5f);
			Descent = (int)Math.Abs (paint.FontMetrics.Descent + 0.5f);
			Height = Ascent;
		}

		public int StringWidth (string s, int startIndex, int length)
		{
			if (string.IsNullOrEmpty (s)) return 0;
			return (int)(paint.MeasureText (s) + 0.5f);
		}

		public int Height { get; private set; }

		public int Ascent { get; private set; }

		public int Descent { get; private set; }
	}

	public static partial class Conversions
	{
		public static SKColor ToSkiaColor (this Color c) => new SKColor ((byte)c.Red, (byte)c.Green, (byte)c.Blue, (byte)c.Alpha);

#if __IOS__ || __MACOS__
		public static global::CoreGraphics.CGRect ToCGRect (this SKRect rect) => new global::CoreGraphics.CGRect (rect.Left, rect.Top, rect.Width, rect.Height);
		public static global::CoreGraphics.CGRect ToCGRect (this SKRectI rect) => new global::CoreGraphics.CGRect (rect.Left, rect.Top, rect.Width, rect.Height);
#endif
	}

#if __ANDROID__
	public class AndroidSkiaGLCanvas : SkiaSharp.Views.Android.SKGLSurfaceView, ICanvas
	{
		int _fps;
		global::Android.OS.Handler _handler;

		const double CpuUtilization = 0.25;
		public int MinFps { get; set; }
		public int MaxFps { get; set; }
		static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds (1.0);

		double _drawTime;
		int _drawCount;
		DateTime _lastThrottleTime = DateTime.Now;

		float _zoom = 1.0f;
		public float Zoom {
			get { return _zoom; }
			set {
				if (value > 0) {
					_zoom = value;
				}
			}
		}

		CanvasContent _content;
		public CanvasContent Content {
			get { return _content; }
			set {
				if (_content != value) {
					if (_content != null) {
						_content.NeedsDisplay -= HandleNeedsDisplay;
					}
					_content = value;
					if (_content != null) {
						_content.NeedsDisplay += HandleNeedsDisplay;
					}
				}
			}
		}

		public AndroidSkiaGLCanvas (global::Android.Content.Context context, global::Android.Util.IAttributeSet attrs)
			: base (context, attrs)
		{
			Initialize (context);
		}

		public AndroidSkiaGLCanvas (global::Android.Content.Context context)
			: base (context)
		{
			Initialize (context);
		}

		void Initialize (global::Android.Content.Context context)
		{
			_touchMan = new Android.AndroidCanvasTouchManager (0);
			_touchMan.LocationFromViewLocationFunc = p => new PointF (p.X / _zoom, p.Y / _zoom);

			MinFps = 4;
			MaxFps = 30;
			_fps = (MinFps + MaxFps) / 2;

			var a = context as global::Android.App.Activity;
			if (a != null) {
				Zoom = Android.ActivityEx.GetDpi (a) / 160.0f;
			}
		}

		#region Touching

		Android.AndroidCanvasTouchManager _touchMan;

		public override bool OnTouchEvent (global::Android.Views.MotionEvent e)
		{
			var del = Content;
			if (del != null) {
				_touchMan.OnTouchEvent (e, Content);
				return true;
			}
			else {
				return false;
			}
		}

		#endregion

		#region Drawing

		void HandleNeedsDisplay (object sender, EventArgs e)
		{
		}

		bool _running = false;

		public void Start ()
		{
			_running = true;
			_drawCount = 0;
			_drawTime = 0;
			_lastThrottleTime = DateTime.Now;
		}

		public void Stop ()
		{
			_running = false;
		}

		public event EventHandler DrewFrame;

		protected override void OnPaintSurface (SkiaSharp.Views.Android.SKPaintGLSurfaceEventArgs e)
		{
			//
			// Start drawing
			//
			var del = Content;
			if (del == null)
				return;

			var startT = DateTime.Now;

			var _graphics = new SkiaGraphics (e.Surface);
			_graphics.SaveState ();
			_graphics.Scale (Zoom, Zoom);


			//
			// Draw
			//
			del.Frame = new RectangleF (0, 0, Width / Zoom, Height / Zoom);
			try {
				del.Draw (_graphics);
			}
			catch (Exception) {
			}

			_graphics.RestoreState ();

			var endT = DateTime.Now;

			_drawTime += (endT - startT).TotalSeconds;
			_drawCount++;

			//
			// Throttle
			//
			if (_running && _drawCount > 2 && (DateTime.Now - _lastThrottleTime) >= ThrottleInterval) {

				_lastThrottleTime = DateTime.Now;

				var maxfps = 1.0 / (_drawTime / _drawCount);
				_drawTime = 0;
				_drawCount = 0;

				var fps = ClampUpdateFreq ((int)(maxfps * CpuUtilization));

				if (Math.Abs (fps - _fps) > 1) {
					_fps = fps;
					Start ();
				}
			}

			//
			// Notify
			//
			DrewFrame?.Invoke (this, EventArgs.Empty);
		}

		int ClampUpdateFreq (int fps)
		{
			return Math.Min (MaxFps, Math.Max (MinFps, fps));
		}

		#endregion
	}
#endif
}

