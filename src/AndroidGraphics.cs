//
// Copyright (c) 2010-2012 Frank A. Krueger
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
using Android.Graphics;


namespace CrossGraphics
{
	public class AndroidGraphics : IGraphics
	{
		Canvas _c;
		ColPaints _paints;
		Font _font;

		public Canvas Canvas { get { return _c; } }

		class ColPaints
		{
			public Paint Fill;
			public Paint Stroke;
			public Font Font;
		}

		public AndroidGraphics (Canvas canvas)
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
			if (c.Tag == null) {
				var stroke = new Paint ();
				stroke.Color = global::Android.Graphics.Color.Argb (c.Alpha, c.Red, c.Green, c.Blue);
				stroke.AntiAlias = true;
				stroke.SetStyle (Paint.Style.Stroke);
				var fill = new Paint ();
				fill.Color = global::Android.Graphics.Color.Argb (c.Alpha, c.Red, c.Green, c.Blue);
				fill.AntiAlias = true;
				fill.SetStyle (Paint.Style.Fill);
				var paints = new ColPaints () {
					Fill = fill,
					Stroke = stroke
				};
				c.Tag = paints;
				_paints = paints;
			}
			else {
				_paints = (ColPaints)c.Tag;
			}
		}

		Path GetPolyPath (Polygon poly)
		{
			var p = poly.Tag as Path;
			if (p == null) {
				p = new Path ();
				p.MoveTo (poly.Points[0].X, poly.Points[0].Y);
				for (var i = 1; i < poly.Points.Count; i++) {
					var pt = poly.Points[i];
					p.LineTo (pt.X, pt.Y);
				}
				p.LineTo (poly.Points[0].X, poly.Points[0].Y);
				poly.Tag = p;
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
			_c.DrawRoundRect (new RectF (x, y, x + width, y + height), radius, radius, _paints.Fill);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawRoundRect (new RectF (x, y, x + width, y + height), radius, radius, _paints.Stroke);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			_c.DrawRect (new RectF (x, y, x + width, y + height), _paints.Fill);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawRect (new RectF (x, y, x + width, y + height), _paints.Stroke);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			_c.DrawOval (new RectF (x, y, x + width, y + width), _paints.Fill);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawOval (new RectF (x, y, x + width, y + width), _paints.Stroke);
		}

		const float RadiansToDegrees = (float)(180 / Math.PI);

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			var sa = -startAngle * RadiansToDegrees;
			var ea = -endAngle * RadiansToDegrees;
			_c.DrawArc (new RectF (cx - radius, cy - radius, cx + radius, cy + radius), sa, ea - sa, false, _paints.Fill);
		}
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			var sa = -startAngle * RadiansToDegrees;
			var ea = -endAngle * RadiansToDegrees;
			_paints.Stroke.StrokeWidth = w;
			_c.DrawArc (new RectF (cx - radius, cy - radius, cx + radius, cy + radius), sa, ea - sa, false, _paints.Stroke);
		}

		bool _inLines = false;
		Path _linesPath = null;
		int _linesCount = 0;
		float _lineWidth = 1;

		public void BeginLines (bool rounded)
		{
			if (!_inLines) {
				_inLines = true;
				_linesPath = new Path ();
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
				_paints.Stroke.StrokeJoin = Paint.Join.Round;
				_c.DrawPath (_linesPath, _paints.Stroke);
				_linesPath.Dispose ();
				_linesPath = null;
			}
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			var dimg = img as AndroidImage;
			if (dimg != null) {
				SetColor (Colors.White);
				_c.DrawBitmap (
					dimg.Bitmap,
					new Rect (0, 0, dimg.Bitmap.Width, dimg.Bitmap.Height),
					new RectF (x, y, x + width, y + height),
					_paints.Fill);
			}
		}
		
		public void DrawString(string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			if (string.IsNullOrWhiteSpace (s)) return;
			DrawString (s, x, y);
		}

		static AndroidFontInfo GetFontInfo (Font f)
		{
			var fi = f.Tag as AndroidFontInfo;
			if (fi == null) {
				var tf = f.IsBold ? Typeface.DefaultBold : Typeface.Default;
				fi = new AndroidFontInfo {
					Typeface = tf,
				};
				f.Tag = fi;
			}
			return fi;
		}

		static void ApplyFontToPaint (Font f, Paint p)
		{
			var fi = GetFontInfo (f);

			p.SetTypeface (fi.Typeface);
			p.TextSize = f.Size;

			if (fi.FontMetrics == null) {
				fi.FontMetrics = new AndroidFontMetrics (p);
			}
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

		public void DrawString (string s, float x, float y)
		{
			if (string.IsNullOrWhiteSpace (s)) return;

			SetFontOnPaints ();
			var fm = GetFontMetrics ();
			_c.DrawText (s, x, y + fm.Ascent - fm.Descent, _paints.Fill);
		}

		public IFontMetrics GetFontMetrics ()
		{
			SetFontOnPaints ();
			return ((AndroidFontInfo)_paints.Font.Tag).FontMetrics;
		}

		public static IFontMetrics GetFontMetrics (Font font)
		{
			var fi = GetFontInfo (font);
			if (fi.FontMetrics == null) {
				var paint = new Paint ();
				ApplyFontToPaint (font, paint); // This ensures font metrics
			}
			return fi.FontMetrics;
		}

		public IImage ImageFromFile (string path)
		{
			var bmp = BitmapFactory.DecodeFile (path);
			if (bmp == null) return null;
			
			var dimg = new AndroidImage () {
				Bitmap = bmp
			};
			return dimg;
		}
		
		public void SaveState()
		{
			_c.Save ();
		}
		
		public void SetClippingRect (float x, float y, float width, float height)
		{
			_c.ClipRect (x, y, x + width, y + height);
		}
		
		public void Translate(float dx, float dy)
		{
			_c.Translate (dx, dy);
		}
		
		public void Scale(float sx, float sy)
		{
			_c.Scale (sx, sy);
		}
		
		public void RestoreState()
		{
			_c.Restore ();
		}
	}

	class AndroidFontInfo
	{
		public Typeface Typeface;
		public AndroidFontMetrics FontMetrics;
	}

	public class AndroidImage : IImage
	{
		public Bitmap Bitmap;
	}

	public class AndroidFontMetrics : IFontMetrics
	{
		const int NumWidths = 128;
		float[] _widths;

		static char[] _chars;
		static AndroidFontMetrics ()
		{
			_chars = new char[NumWidths];
			for (var i = 0; i < NumWidths; i++) {
				if (i <= ' ') {
					_chars[i] = ' ';
				}
				else {
					_chars[i] = (char)i;
				}
			}
		}

		public AndroidFontMetrics (Paint paint)
		{
			_widths = new float[NumWidths];
			paint.GetTextWidths (_chars, 0, NumWidths, _widths);
			Ascent = (int)(Math.Abs (paint.Ascent ()) + 0.5f);
			Descent = (int)(paint.Descent ()/2 + 0.5f);
			Height = Ascent;
		}

		public int StringWidth (string s, int startIndex, int length)
		{
			if (string.IsNullOrEmpty (s)) return 0;

			var end = startIndex + length;
			if (end <= 0) return 0;

			var a = 0.0f;
			for (var i = startIndex; i < end; i++) {
				if (s[i] < NumWidths) {
					a += _widths[s[i]];
				}
				else {
					a += _widths[' '];
				}
			}

			return (int)(a + 0.5f);
		}

		public int Height { get; private set; }

		public int Ascent { get; private set; }

		public int Descent { get; private set; }
	}

	public static partial class Conversions
	{
		public static global::Android.Graphics.Color ToAndroidColor (this Color c)
		{
			return global::Android.Graphics.Color.Argb (c.Alpha, c.Red, c.Green, c.Blue);
		}
	}
}
