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

namespace System.Drawing
{
    public struct RectangleF
    {
        public float Left, Top, Width, Height;

        public float Bottom { get { return Top + Height; } }

        public RectangleF (float left, float top, float width, float height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public void Inflate (float width, float height)
        {
            Inflate (new SizeF (width, height));
        }

        public void Inflate (SizeF size)
        {
            Left -= size.Width;
            Top -= size.Height;
            Width += size.Width * 2;
            Height += size.Height * 2;
        }
    }

    public struct Rectangle
    {
        public int Left, Top, Width, Height;

        public int X { get { return Left; } }

        public int Y { get { return Top; } }

        public int Bottom { get { return Top + Height; } }

        public int Right { get { return Left + Width; } }

        public Rectangle (int left, int top, int width, int height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public void Offset (int dx, int dy)
        {
            Left += dx;
            Top += dy;
        }

        public bool Contains (int x, int y)
        {
            return (x >= Left && x < Left + Width) && (y >= Top && y < Top + Height);
        }

        public static Rectangle Union (Rectangle a, Rectangle b)
        {
            return new Rectangle (Math.Min (a.Left, b.Left),
                     Math.Min (a.Top, b.Top),
                     Math.Max (a.Right, b.Right),
                     Math.Max (a.Bottom, b.Bottom));
        }

        public bool IntersectsWith (Rectangle rect)
        {
            return !((Left >= rect.Right) || (Right <= rect.Left) ||
                (Top >= rect.Bottom) || (Bottom <= rect.Top));
        }

        public void Inflate (int width, int height)
        {
            Inflate (new Size (width, height));
        }

        public void Inflate (Size size)
        {
            Left -= size.Width;
            Top -= size.Height;
            Width += size.Width * 2;
            Height += size.Height * 2;
        }
    }

    public struct PointF
    {
        public float X, Y;

        public PointF (float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Point
    {
        public int X, Y;

        public Point (int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Size
    {
        public int Width, Height;

        public Size (int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    public struct SizeF
    {
        public float Width, Height;

        public SizeF (float width, float height)
        {
            Width = width;
            Height = height;
        }
    }
}

