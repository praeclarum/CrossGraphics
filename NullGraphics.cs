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

namespace CrossGraphics
{
	public class NullGraphics : IGraphics
	{
		NullGraphicsFontMetrics _fontMetrics;

		public NullGraphics ()
		{
			_fontMetrics = new NullGraphicsFontMetrics ();
		}

		public void SetFont (Font f)
		{
		}

		public void SetColor (Color c)
		{
		}

		public void FillPolygon (Polygon poly)
		{
		}

		public void DrawPolygon (Polygon poly, float w)
		{
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
		}

		public void FillRect (float x, float y, float width, float height)
		{
		}

		public void FillOval (float x, float y, float width, float height)
		{
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
		}

		public void BeginLines (bool rounded)
		{
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
		}

		public void EndLines ()
		{
		}
		
		public void DrawArc(float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
		}

		public void DrawString (string s, float x, float y)
		{
		}

		public void DrawString (string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
		}

		public IFontMetrics GetFontMetrics ()
		{
			return _fontMetrics;
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
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

		public IImage ImageFromFile (string filename)
		{
			return null;
		}
		
		public void BeginEntity (object entity)
		{
		}
	}

	class NullGraphicsFontMetrics : IFontMetrics
	{
		const int DefaultWidth = 10;
		int _height = 10;

		public NullGraphicsFontMetrics ()
		{
			_height = 10;
		}

		public int StringWidth (string str)
		{
			if (str == null) return 0;

			var n = str.Length;
			if (n == 0) return 0;

			var w = 0;

			for (var i = 0; i < n; i++) {
				w += DefaultWidth;
			}
			return w;
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

