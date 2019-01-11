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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Collections.Concurrent;

namespace CrossGraphics
{
	public interface IGraphics
	{
		void BeginEntity(object entity);

		void SetFont(Font f);

		void SetColor(Color c);

		void Clear (Color c);

		void FillPolygon(Polygon poly);

		void DrawPolygon(Polygon poly,float w);

		void FillRect(float x,float y,float width, float height);

		void DrawRect(float x, float y, float width, float height, float w);

		void FillRoundedRect(float x, float y, float width, float height, float radius);

		void DrawRoundedRect(float x, float y, float width, float height, float radius, float w);

		void FillOval(float x, float y, float width, float height);

		void DrawOval(float x, float y, float width, float height, float w);

		void BeginLines(bool rounded);

		void DrawLine(float sx, float sy, float ex, float ey, float w);

		void EndLines();

		void FillArc(float cx, float cy, float radius, float startAngle, float endAngle);
		
		void DrawArc(float cx, float cy, float radius, float startAngle, float endAngle, float w);

		void DrawImage(IImage img, float x, float y, float width, float height);

		void DrawString(string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align);

		void DrawString(string s, float x, float y);
		
		void SaveState();
		
		void SetClippingRect (float x, float y, float width, float height);
		
		void Translate(float dx, float dy);
		
		void Scale(float sx, float sy);
		
		void RestoreState();

		IFontMetrics GetFontMetrics();

		IImage ImageFromFile(string path);
	}

	public enum LineBreakMode {
		None,
		Clip,
		WordWrap,
	}

	public enum TextAlignment {
		Left,
		Center,
		Right,
		Justified
	}

	public static class GraphicsEx
	{
        public static void DrawString (this IGraphics g, string s, PointF p)
        {
            g.DrawString(s, p.X, p.Y);
        }

		public static void DrawString(this IGraphics g, string s, PointF p, Font f)
		{
			g.SetFont (f);
			g.DrawString (s, p.X, p.Y);
		}

		public static void DrawString(this IGraphics g, string s, RectangleF p, Font f, LineBreakMode lineBreak, TextAlignment align)
		{
			g.SetFont (f);
			g.DrawString (s, p.Left, p.Top, p.Width, p.Height, lineBreak, align);
		}

		public static void DrawLine(this IGraphics g, PointF s, PointF e, float w)
		{
			g.DrawLine (s.X, s.Y, e.X, e.Y, w);
		}

		public static void DrawRoundedRect(this IGraphics g, RectangleF r, float radius, float w)
		{
			g.DrawRoundedRect (r.Left, r.Top, r.Width, r.Height, radius, w);
		}

        public static void DrawRoundedRect(this IGraphics g, Rectangle r, float radius, float w)
        {
            g.DrawRoundedRect(r.Left, r.Top, r.Width, r.Height, radius, w);
        }

		public static void FillRoundedRect(this IGraphics g, RectangleF r, float radius)
		{
			g.FillRoundedRect (r.Left, r.Top, r.Width, r.Height, radius);
		}

		public static void FillRoundedRect(this IGraphics g, Rectangle r, float radius)
		{
			g.FillRoundedRect (r.Left, r.Top, r.Width, r.Height, radius);
		}

		public static void FillRect(this IGraphics g, RectangleF r)
		{
			g.FillRect (r.Left, r.Top, r.Width, r.Height);
		}

        public static void FillRect(this IGraphics g, Rectangle r)
        {
            g.FillRect(r.Left, r.Top, r.Width, r.Height);
        }

		public static void DrawRect(this IGraphics g, RectangleF r, float w)
		{
			g.DrawRect (r.Left, r.Top, r.Width, r.Height, w);
		}

        public static void DrawRect(this IGraphics g, Rectangle r, float w)
        {
            g.DrawRect(r.Left, r.Top, r.Width, r.Height, w);
        }

		public static void FillOval(this IGraphics g, RectangleF r)
		{
			g.FillOval (r.Left, r.Top, r.Width, r.Height);
		}

		public static void DrawOval(this IGraphics g, RectangleF r, float w)
		{
			g.DrawOval (r.Left, r.Top, r.Width, r.Height, w);
		}
	}

	public interface IImage
	{
	}

	[Flags]
	public enum FontOptions
	{
		None = 0,
		Bold = 1
	}

	public class Font
	{
		static ConcurrentDictionary<(string FontFamily, FontOptions Options, int Size), Font> _fonts = new ConcurrentDictionary<(string FontFamily, FontOptions Options, int Size), Font> ();

		public string FontFamily { get; }
		
		public FontOptions Options { get; }

		public int Size { get; }

		public object Tag { get; set; }
		
		public bool IsBold => (Options & FontOptions.Bold) != 0;

		public Font (string fontFamily, FontOptions options, int size)
		{
			FontFamily = fontFamily;
			Options = options;
			Size = size;
		}

		public static Font Get (string fontFamily, FontOptions options, int size)
		{
			var key = (fontFamily, options, size);
			if (!_fonts.TryGetValue (key, out var font)) {
				font = new Font (fontFamily, options, size);
				_fonts.TryAdd (key, font);
			}
			return font;
		}

		public static Font SystemFontOfSize (int size) => Get ("SystemFont", FontOptions.None, size);
		public static Font BoldSystemFontOfSize (int size) => Get ("SystemFont", FontOptions.Bold, size);
		public static Font UserFixedPitchFontOfSize (int size) => Get ("Monospace", FontOptions.None, size);
		public static Font BoldUserFixedPitchFontOfSize (int size) => Get ("Monospace", FontOptions.Bold, size);

		public static Font FromName (string name, int size) => Get (name, FontOptions.None, size);

		public override string ToString()
		{
			return string.Format ("[Font: FontFamily={0}, Options={1}, Size={2}, Tag={3}]", FontFamily, Options, Size, Tag);
		}
	}

	public interface IFontMetrics
	{
		int StringWidth(string s, int startIndex, int length);

		int Height { get; }

		int Ascent { get; }

		int Descent { get; }
	}

	public static class FontMetricsEx
	{
		public static int StringWidth (this IFontMetrics fm, string s)
		{
			return fm.StringWidth (s, 0, s.Length);
		}
	}

	public class Color
	{
		public readonly int Red, Green, Blue, Alpha;
		public object Tag;

		public float RedValue {
			get { return Red / 255.0f; }
		}

		public float GreenValue {
			get { return Green / 255.0f; }
		}

		public float BlueValue {
			get { return Blue / 255.0f; }
		}

		public float AlphaValue {
			get { return Alpha / 255.0f; }
		}

		public Color (int red, int green, int blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = 255;
		}

		public Color (int red, int green, int blue, int alpha)
		{
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = alpha;
		}

		public int Intensity { get { return (Red + Green + Blue) / 3; } }

		public Color GetInvertedColor()
		{
			return new Color (255 - Red, 255 - Green, 255 - Blue, Alpha);
		}

		public static bool AreEqual(Color a, Color b)
		{
			if (a == null && b == null)
				return true;
			if (a == null && b != null)
				return false;
			if (a != null && b == null)
				return false;
			return (a.Red == b.Red && a.Green == b.Green && a.Blue == b.Blue && a.Alpha == b.Alpha);
		}

		public bool IsWhite {
			get { return (Red == 255) && (Green == 255) && (Blue == 255); }
		}

		public bool IsBlack {
			get { return (Red == 0) && (Green == 0) && (Blue == 0); }
		}

		public Color WithAlpha(int aa)
		{
			return new Color (Red, Green, Blue, aa);
		}

        public override bool Equals (object obj)
        {
            var o = obj as Color;
            return (o != null) && (o.Red == Red) && (o.Green == Green) && (o.Blue == Blue) && (o.Alpha == Alpha);
        }

        public override int GetHashCode ()
        {
            return (Red + Green + Blue + Alpha).GetHashCode ();
        }

		public override string ToString()
		{
			return string.Format ("[Color: RedValue={0}, GreenValue={1}, BlueValue={2}, AlphaValue={3}]", RedValue, GreenValue, BlueValue, AlphaValue);
		}
	}

	public static class Colors
	{
		public static readonly Color Yellow = new Color (255, 255, 0);
		public static readonly Color Red = new Color (255, 0, 0);
		public static readonly Color Green = new Color (0, 255, 0);
		public static readonly Color Blue = new Color (0, 0, 255);
		public static readonly Color White = new Color (255, 255, 255);
		public static readonly Color Cyan = new Color (0, 255, 255);
		public static readonly Color Black = new Color (0, 0, 0);
		public static readonly Color LightGray = new Color (212, 212, 212);
		public static readonly Color Gray = new Color (127, 127, 127);
		public static readonly Color DarkGray = new Color (64, 64, 64);
	}

	public class Polygon
	{
		public readonly List<PointF> Points;

		public object Tag { get; set; }

		public int Version { get; set; }

		public Polygon ()
		{
			Points = new List<PointF> ();
		}

		public Polygon (int capacity)
		{
			Points = new List<PointF> (capacity);
		}

		public Polygon (int[] xs, int[] ys, int c)
		{
			Points = new List<PointF> (c);
			for (var i = 0; i < c; i++) {
				Points.Add (new PointF (xs [i], ys [i]));
			}
		}

		public int Count {
			get { return Points.Count; }
		}

		public void Clear()
		{
			Points.Clear ();
			Version++;
		}

		public void AddPoint(PointF p)
		{
			Points.Add (p);
			Version++;
		}

		public void AddPoint(float x, float y)
		{
			Points.Add (new PointF (x, y));
			Version++;
		}
	}

    public struct Transform2D
    {
        public float M11, M12, M13;
        public float M21, M22, M23;
        //public float M31, M32, M33;

        public void Apply (float x, float y, out float xp, out float yp)
        {
            xp = M11 * x + M12 * y + M13;
            yp = M21 * x + M22 * y + M23;
        }

        public static Transform2D operator * (Transform2D l, Transform2D r)
        {
            var t = new Transform2D ();

			t.M11 = l.M11 * r.M11 + l.M12 * r.M21;// +l.M13 * r.M31;
			t.M12 = l.M11 * r.M12 + l.M12 * r.M22;// +l.M13 * r.M32;
			t.M13 = l.M11 * r.M13 + l.M12 * r.M23 + l.M13;// *r.M33;

			t.M21 = l.M21 * r.M11 + l.M22 * r.M21;// +l.M23 * r.M31;
			t.M22 = l.M21 * r.M12 + l.M22 * r.M22;// +l.M23 * r.M32;
			t.M23 = l.M21 * r.M13 + l.M22 * r.M23 + l.M23;// *r.M33;

            //t.M31 = l.M31 * r.M11 + l.M32 * r.M21 + l.M33 * r.M31;
            //t.M32 = l.M31 * r.M12 + l.M32 * r.M22 + l.M33 * r.M32;
            //t.M33 = l.M31 * r.M13 + l.M32 * r.M23 + l.M33 * r.M33;

            return t;
        }

        public static Transform2D Identity ()
        {
            var t = new Transform2D ();
            t.M11 = 1;
            t.M22 = 1;
            //t.M33 = 1;
            return t;
        }

        public static Transform2D Translate (float x, float y)
        {
            var t = new Transform2D ();
            t.M11 = 1;
            t.M22 = 1;
            //t.M33 = 1;
            t.M13 = x;
            t.M23 = y;
            return t;
        }
        public static Transform2D Scale (float x, float y)
        {
            var t = new Transform2D ();
            t.M11 = x;
            t.M22 = y;
            //t.M33 = 1;
            return t;
        }
    }

	public static class PointEx
	{
		#if __UNIFIED__
		public static PointF ToPointF (this global::CoreGraphics.CGPoint r)
		{
			return new PointF ((float)r.X, (float)r.Y);
		}
		#endif

		public static System.Drawing.Point GetCenter (this System.Drawing.Rectangle r)
		{
			return new System.Drawing.Point (r.Left + r.Width / 2,
				r.Top + r.Height / 2);
		}

		public static System.Drawing.PointF GetCenter (this System.Drawing.RectangleF r)
		{
			return new System.Drawing.PointF (r.X + r.Width / 2.0f,
				r.Y + r.Height / 2.0f);
		}
	}

	public static class RectangleEx
	{
		public static RectangleF ToRectangleF (this System.Drawing.Rectangle r)
		{
			return new RectangleF (r.X, r.Y, r.Width, r.Height);
		}
		#if __UNIFIED__
		public static RectangleF ToRectangleF (this global::CoreGraphics.CGRect r)
		{
			return new RectangleF ((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);
		}
		#endif

		public static List<RectangleF> GetIntersections (this List<RectangleF> boxes, RectangleF box)
		{
			var r = new List<RectangleF> ();
			foreach (var b in boxes) {
				if (b.IntersectsWith (box)) {
					r.Add (b);
				}
			}
			return r;
		}
	}
}

