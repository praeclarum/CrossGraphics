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
using Android.Graphics;


namespace CrossGraphics.Droid
{
	public class DroidGraphics : IGraphics
	{
		Canvas _c;
		ColPaints _paints;
		Font _font;
		FontMetrics _fm;

		class ColPaints
		{
			public Paint Fill;
			public Paint Stroke;
		}

		public DroidGraphics (Canvas canvas)
		{
			_c = canvas;
			_font = null;
			_fm = new FontMetrics ();
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
			_c.DrawPath (GetPolyPath (poly), _paints.Fill);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			_c.DrawRoundRect (new RectF (x, y, x + width, y + height), radius, radius, _paints.Fill);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			_c.DrawRoundRect (new RectF (x, y, x + width, y + height), radius, radius, _paints.Stroke);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			_c.DrawRect (new RectF (x, y, x + width, y + height), _paints.Fill);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			_c.DrawRect (new RectF (x, y, x + width, y + height), _paints.Stroke);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			_c.DrawOval (new RectF (x, y, x + width, y + width), _paints.Fill);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			_c.DrawOval (new RectF (x, y, x + width, y + width), _paints.Stroke);
		}

		bool _inLines = false;
		float[] _linePoints = new float[2 * 100];
		int _numLineElements = 0;

		public void BeginLines ()
		{
			if (!_inLines) {
				_inLines = true;
				_numLineElements = 0;
			}
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_inLines) {
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
				_c.DrawLine (sx, sy, ex, ey, _paints.Stroke);
			}
		}

		public void EndLines ()
		{
			if (_inLines) {
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

		public void DrawString (string s, float x, float y)
		{
			_c.DrawText (s, x, y, _paints.Fill);
		}

		public IFontMetrics GetFontMetrics ()
		{
			return _fm;
		}

		public IImage ImageFromFile (string path)
		{
			var bmp = BitmapFactory.DecodeFile (path);
			var dimg = new DroidImage () {
				Bitmap = bmp
			};
			return dimg;
		}
	}

	public class DroidImage : IImage
	{
		public Bitmap Bitmap;
	}

	public class FontMetrics : IFontMetrics
	{
		public int StringWidth (string s)
		{
			return s.Length * 8;
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
