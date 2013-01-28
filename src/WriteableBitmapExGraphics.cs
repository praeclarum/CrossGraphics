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
using System.IO;
using System.Drawing;
using System.Linq;

using Windows.UI.Xaml.Media.Imaging;
using NativeColor = Windows.UI.Color;

namespace CrossGraphics.WriteableBitmapEx
{
	public class WriteableBitmapExGraphics : IGraphics
	{
        WriteableBitmap bmp;
        public WriteableBitmap Bitmap { get { return bmp; } }

        WriteableBitmapExGraphicsFontMetrics _fontMetrics;
		
        //Font _lastFont = null;
        NativeColor lastColor = Windows.UI.Colors.Black;

        class State
        {
            public Transform2D Transform;
        }

        readonly Stack<State> states = new Stack<State> ();
		
		public WriteableBitmapExGraphics (int pixelWidth, int pixelHeight)
		{
            bmp = new WriteableBitmap (pixelWidth, pixelHeight);

            _fontMetrics = new WriteableBitmapExGraphicsFontMetrics ();
			SetColor (Colors.Black);
            states.Push (new State {
                Transform = new Transform2D {
                    M11 = 1,
                    M22 = 1,
                    M33 = 1,
                },
            });
		}
		
		public void BeginDrawing ()
		{
            bmp.Clear (NativeColor.FromArgb (255, 255, 255, 255));
		}

		public void EndDrawing ()
		{
            bmp.Invalidate ();
		}

		public void SaveState ()
		{
            var l = states.Peek ();
            var s = new State {
                Transform = l.Transform,
            };
            states.Push (s);
		}

        public void Scale (float sx, float sy)
        {
            var t = Transform2D.Scale (sx, sy);
            var s = states.Peek ();
            s.Transform = s.Transform * t;
        }
		
		public void Translate (float dx, float dy)
		{
            var t = Transform2D.Translate (dx, dy);
            var s = states.Peek ();
            s.Transform = s.Transform * t;
		}

        public void SetClippingRect (float x, float y, float width, float height)
        {
        }
		
		public void RestoreState ()
		{
            if (states.Count > 1) {
                states.Pop ();
            }
		}

		public void SetFont (Font f)
		{
			//_lastFont = f;
		}

		public void SetColor (Color c)
		{
            lastColor = NativeColor.FromArgb ((byte)c.Alpha, (byte)c.Red, (byte)c.Green, (byte)c.Blue);
		}

		public void FillPolygon (Polygon poly)
		{
		}

		public void DrawPolygon (Polygon poly, float w)
		{
		}

		public void FillOval (float x, float y, float width, float height)
		{
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
        }
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
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

		public void DrawRect (float x, float y, float width, float height, float w)
		{
		}
		
		bool _inPolyline = false;
		bool _startedPolyline = false;

		public void BeginLines (bool rounded)
		{
			_inPolyline = true;
            _startedPolyline = false;
		}

        public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
            var s = states.Peek ();

			if (_inPolyline) {
                _startedPolyline = true;
			}
			else {
			}

            float x1, y1, x2, y2;
            s.Transform.Apply (sx, sy, out x1, out y1);
            s.Transform.Apply (ex, ey, out x2, out y2);

            bmp.DrawLineAa ((int)x1, (int)y1, (int)x2, (int)y2, lastColor);
		}

		public void EndLines ()
		{
			if (_inPolyline) {
                _inPolyline = false;
			}
		}
		
		public void DrawString(string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
		}

		public void DrawString (string s, float x, float y)
		{
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

	public class WriteableBitmapExGraphicsFontMetrics : IFontMetrics
	{
		int _height;

        public WriteableBitmapExGraphicsFontMetrics ()
		{
			_height = 10;
		}

		public int StringWidth (string str, int startIndex, int length)
		{
			return length * 10;
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
