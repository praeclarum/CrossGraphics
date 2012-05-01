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


namespace CrossGraphics.Droid
{
	public class DroidGraphics : IGraphics
	{
		Canvas _c;
		ColPaints _paints;
		Font _font;
		DroidFontMetrics _fm;

        public Canvas Canvas { get { return _c; } }

		class ColPaints
		{
			public Paint Fill;
			public Paint Stroke;
		}

		public DroidGraphics (Canvas canvas)
		{
			_c = canvas;
			_font = null;
			_fm = new DroidFontMetrics ();
			SetColor (Colors.Black);
		}

		public void BeginEntity (object entity)
		{
		}

		public void SetFont (Font font)
		{
			_font = font;
		}

		public void SetColor (Color c)
		{
			if (c.Tag == null) {
				var stroke = new Paint ();
				stroke.Color = Android.Graphics.Color.Argb (c.Alpha, c.Red, c.Green, c.Blue);
				stroke.AntiAlias = true;
				stroke.SetStyle (Paint.Style.Stroke);
				var fill = new Paint ();
				fill.Color = Android.Graphics.Color.Argb (c.Alpha, c.Red, c.Green, c.Blue);
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
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
            var sa = startAngle * RadiansToDegrees - 180.0f;
            var ea = endAngle * RadiansToDegrees - 180.0f;
            _c.DrawArc (new RectF (cx - radius, cy - radius, cx + radius, cy + radius), sa, ea - sa, false, _paints.Stroke);
		}

		bool _inLines = false;
		float _lineWidth = 1;
		float[] _linePoints = new float[2 * 100];
		int _numLineElements = 0;

		public void BeginLines (bool rounded)
		{
			if (!_inLines) {
				_inLines = true;
				_numLineElements = 0;
			}
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_inLines) {
				_lineWidth = w;
				if (_numLineElements == 0) {
					_linePoints[0] = sx;
					_linePoints[1] = sy;
					_linePoints[2] = ex;
					_linePoints[3] = ey;
					_numLineElements += 4;
				}
				else {
					if (_numLineElements < _linePoints.Length - 2) {
						_linePoints[_numLineElements++] = ex;
						_linePoints[_numLineElements++] = ey;
					}
				}
			}
			else {
				_paints.Stroke.StrokeWidth = w;
				_c.DrawLine (sx, sy, ex, ey, _paints.Stroke);
			}
		}

		public void EndLines ()
		{
			if (_inLines) {
				_paints.Stroke.StrokeWidth = _lineWidth;
				_c.DrawLines (_linePoints, 0, _numLineElements, _paints.Stroke);
				_inLines = false;
			}
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			var dimg = img as DroidImage;
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

		public void DrawString (string s, float x, float y)
		{
			if (string.IsNullOrWhiteSpace (s)) return;
            var fm = GetFontMetrics ();
			_c.DrawText (s, x, y + fm.Height, _paints.Fill);
		}

		public IFontMetrics GetFontMetrics ()
		{
			return _fm;
		}

		public IImage ImageFromFile (string path)
		{
			var bmp = BitmapFactory.DecodeFile (path);
			if (bmp == null) return null;
			
			var dimg = new DroidImage () {
				Bitmap = bmp
			};
			return dimg;
		}
		
		public void SaveState()
		{
			throw new NotImplementedException ();
		}
		
		public void SetClippingRect (float x, float y, float width, float height)
		{
			throw new NotImplementedException ();
		}
		
		public void Translate(float dx, float dy)
		{
			throw new NotImplementedException ();
		}
		
		public void Scale(float sx, float sy)
		{
			throw new NotImplementedException ();
		}
		
		public void RestoreState()
		{
			throw new NotImplementedException ();
		}

	}

	public class DroidImage : IImage
	{
		public Bitmap Bitmap;
	}

	public class DroidFontMetrics : IFontMetrics
	{
		public int StringWidth (string s, int startIndex, int length)
		{
			return length * 8;
		}

		public int Height
		{
			get
			{
				return 10;
			}
		}

		public int Ascent
		{
			get
			{
				return 10;
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
}
