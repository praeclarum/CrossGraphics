//
// Copyright (c) 2010-2025 Frank A. Krueger
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
#nullable enable

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

		void SetRgba(byte r, byte g, byte b, byte a);

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
		public static void SetColor (this IGraphics g, ValueColor c)
		{
			g.SetRgba (c.Red, c.Green, c.Blue, c.Alpha);
		}

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

    public struct FontRef
    {
        public readonly string FontFamily;
        public readonly FontOptions Options;
        public readonly int Size;
        public FontRef (string fontFamily, FontOptions options, int size)
        {
            FontFamily = fontFamily;
            Options = options;
            Size = size;
        }
    }

	public class FontRefEqualityComparer : IEqualityComparer<FontRef>
	{
		public bool Equals (FontRef x, FontRef y)
		{
			return x.Size == y.Size && x.Options == y.Options && x.FontFamily == y.FontFamily;
		}

		public int GetHashCode (FontRef obj)
		{
			return obj.Size.GetHashCode () + obj.Options.GetHashCode () * 2 + obj.FontFamily.GetHashCode () * 3;
		}
	}

	public class Font
	{
		readonly static ConcurrentDictionary<FontRef, Font> _fonts = new ();

		public string FontFamily { get; }
		
		public FontOptions Options { get; }

		public int Size { get; }

		public IFontMetrics? Tag { get; set; }
		public IFontMetrics? AndroidTag { get; set; }
		public IFontMetrics? SkiaTag { get; set; }

		public bool IsBold => (Options & FontOptions.Bold) != 0;

		public bool IsMonospace => FontFamily == "Monospace";

		public Font (string fontFamily, FontOptions options, int size)
		{
			FontFamily = fontFamily;
			Options = options;
			Size = size;
		}

		public static Font Get (string fontFamily, FontOptions options, int size)
		{
			var key = new FontRef (fontFamily, options, size);
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

		public override bool Equals (object? obj)
		{
			if (obj is Font o) {
				return o.FontFamily == FontFamily && o.Options == Options && o.Size == Size;
			}
			return false;
		}

		public override int GetHashCode ()
		{
			return FontFamily.GetHashCode () + Options.GetHashCode () * 2 + Size.GetHashCode () * 3;
		}

		public static bool operator == (Font a, Font b)
		{
			return a.Equals (b);
		}
		public static bool operator != (Font a, Font b)
		{
			return !a.Equals (b);
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
		public object? Tag;
		public object? SkiaTag;

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

		public static bool AreEqual(Color? a, Color? b)
		{
			if (a is null) {
				return b is null;
			}
			if (b is null) {
				return false;
			}
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

        public override bool Equals (object? obj)
        {
            if (obj is Color o)
				return (o.Red == Red) && (o.Green == Green) && (o.Blue == Blue) && (o.Alpha == Alpha);
	        return false;
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
		public static readonly Color Clear = new Color (0, 0, 0, 0);
	}

	public class ColorCache
	{
		private readonly Dictionary<uint, Color> _colors = new ();
		
		public Color GetColor (byte r, byte g, byte b, byte a = 255)
		{
			var key = ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | a;
			if (!_colors.TryGetValue (key, out var c)) {
				c = new Color (r, g, b, a);
				_colors[key] = c;
			}
			return c;
		}
		
		public Color GetColor (ValueColor color)
		{
			return GetColor (color.Red, color.Green, color.Blue, color.Alpha);
		}
	}

	public struct ValueColor
	{
		public byte Red, Green, Blue, Alpha;

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

		public ValueColor (byte red, byte green, byte blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = 255;
		}

		public ValueColor (byte red, byte green, byte blue, byte alpha)
		{
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = alpha;
		}

		public Color GetColor () => new Color (Red, Green, Blue, Alpha);

		public int Intensity { get { return (Red + Green + Blue) / 3; } }

		public ValueColor GetInvertedColor()
		{
			return new ValueColor ((byte)(255 - Red), (byte)(255 - Green), (byte)(255 - Blue), Alpha);
		}

		public static bool AreEqual(Color a, Color b)
		{
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

		public override bool Equals (object? obj)
		{
			if (obj is ValueColor o) {
				return (o.Red == Red) && (o.Green == Green) && (o.Blue == Blue) && (o.Alpha == Alpha);
			}
			return false;
		}

		public override int GetHashCode ()
		{
			return (Red + Green + Blue + Alpha).GetHashCode ();
		}

		public override string ToString()
		{
			return string.Format ("[ValueColor: RedValue={0}, GreenValue={1}, BlueValue={2}, AlphaValue={3}]", RedValue, GreenValue, BlueValue, AlphaValue);
		}

		public static bool operator == (ValueColor left, ValueColor right)
		{
			return left.Red == right.Red && left.Green == right.Green && left.Blue == right.Blue && left.Alpha == right.Alpha;
		}

		public static bool operator != (ValueColor left, ValueColor right)
		{
			return left.Red != right.Red || left.Green != right.Green || left.Blue != right.Blue || left.Alpha != right.Alpha;
		}
	}

	public static class ColorConversions
	{
		static double PiecewiseGaussian (double x, double mu, double sigma1, double sigma2)
		{
			var dx2 = (x - mu) * (x - mu);
			return x < mu ? Math.Exp (-0.5*dx2/(sigma1*sigma1)) : Math.Exp (-0.5*dx2/(sigma2*sigma2));
		}

		/// <summary>
		/// From https://en.wikipedia.org/wiki/CIE_1931_color_space#Analytical_approximation
		/// </summary>
		public static void WavelengthToXyz (double wavelengthInMeters, out double x, out double y, out double z)
		{
			// lambda = wavelengthInNanometers
			var lambda = 1.0e9 * wavelengthInMeters;
			x = 1.056* PiecewiseGaussian (lambda, 599.8, 37.9, 31.0) +
				0.362* PiecewiseGaussian (lambda, 442.0, 16.0, 26.7) -
				0.065* PiecewiseGaussian (lambda, 501.1,  20.4, 26.2);
			y = 0.821* PiecewiseGaussian (lambda, 568.8, 46.9, 40.5) +
			    0.286* PiecewiseGaussian (lambda, 530.9, 16.3, 31.1);
			z = 1.217* PiecewiseGaussian (lambda, 437.0, 11.8, 36.0) +
			    0.681* PiecewiseGaussian (lambda, 459.0, 26.0, 13.8);
		}
		
		/// <summary>
		/// From https://www.image-engineering.de/library/technotes/958-how-to-convert-between-srgb-and-ciexyz
		/// </summary>
		public static void XyzToLinearRgb (double x, double y, double z, out double r, out double g, out double b)
		{
			r = Math.Max (0, 3.2404542*x - 1.5371385*y - 0.4985314*z);
			g = Math.Max (0, -0.9692660*x + 1.8760108*y + 0.0415560*z);
			b = Math.Max (0, 0.0556434*x - 0.2040259*y + 1.0572252*z);
		}
		
		static double ApplyGamma (double v)
		{
			if (v <= 0.0031308) {
				return 12.92 * v;
			}
			return 1.055 * Math.Pow (v, 1.0/2.4) - 0.055;
		}
		
		/// <summary>
		/// From https://www.image-engineering.de/library/technotes/958-how-to-convert-between-srgb-and-ciexyz
		/// </summary>
		public static void LinearRgbToSrgb (double r, double g, double b, out byte sr, out byte sg, out byte sb)
		{
			sr = (byte)Math.Max (0, Math.Min (255, (int)(0.5 + ApplyGamma (r) * 255)));
			sg = (byte)Math.Max (0, Math.Min (255, (int)(0.5 + ApplyGamma (g) * 255)));
			sb = (byte)Math.Max (0, Math.Min (255, (int)(0.5 + ApplyGamma (b) * 255)));
		}
		
		public static double SrgbToHue(byte sr, byte sg, byte sb) {
			double r = sr / 255.0;
			double g = sg / 255.0;
			double b = sb / 255.0;
			double max = Math.Max(Math.Max(r, g), b);
			double min = Math.Min(Math.Min(r, g), b);
			double diff = max - min;
			double h = 0.0;
			if (diff == 0)
				h = 0;
			else if (max == r)
				h = 60 * (((g - b) / diff) % 6);
			else if (max == g)
				h = 60 * (((b - r) / diff) + 2);
			else if (max == b)
				h = 60 * (((r - g) / diff) + 4);
			if (h < 0)
				h += 360;
			return h;
		}
		
		public static void HsvToSrgb(double h, double s, double v, out byte sr, out byte sg, out byte sb) {
			double c = v * s;
			double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
			double m = v - c;
			double r = 0, g = 0, b = 0;
			if (h < 60) {
				r = c;
				g = x;
			} else if (h < 120) {
				r = x;
				g = c;
			} else if (h < 180) {
				g = c;
				b = x;
			} else if (h < 240) {
				g = x;
				b = c;
			} else if (h < 300) {
				r = x;
				b = c;
			} else if (h < 360) {
				r = c;
				b = x;
			}
			sr = (byte)Math.Max (0, Math.Min (255, (int)(0.5 + (r + m) * 255)));
			sg = (byte)Math.Max (0, Math.Min (255, (int)(0.5 + (g + m) * 255)));
			sb = (byte)Math.Max (0, Math.Min (255, (int)(0.5 + (b + m) * 255)));
		}

		/// <summary>
		/// Convert to XYZ
		/// Then to sRGB' then to sRGB
		/// </summary>
		public static void WavelengthToSrgb (double wavelengthInMeters, out byte sr, out byte sg, out byte sb)
		{
			WavelengthToXyz (wavelengthInMeters, out var x, out var y, out var z);
			XyzToLinearRgb (x, y, z, out var r, out var g, out var b);
			LinearRgbToSrgb (r, g, b, out sr, out sg, out sb);
		}
		
		/// <summary>
		/// Convert to XYZ
		/// Then to sRGB' then to sRGB
		/// Get the hue
		/// Generate a new fully saturated color with that hue
		/// </summary>
		public static void WavelengthToSaturatedSrgb (double wavelengthInMeters, out byte sr, out byte sg, out byte sb)
		{
			WavelengthToSrgb (wavelengthInMeters, out sr, out sg, out sb);
			var hue = SrgbToHue (sr, sg, sb);
			HsvToSrgb (hue, 1, 1, out sr, out sg, out sb);
		}
		
		public static Color WavelengthToSaturatedColor (double wavelengthInMeters)
		{
			WavelengthToSaturatedSrgb (wavelengthInMeters, out var r, out var g, out var b);
			return new Color (r, g, b);
		}

		public static ValueColor WavelengthToSaturatedValueColor (double wavelengthInMeters)
		{
			WavelengthToSaturatedSrgb (wavelengthInMeters, out var r, out var g, out var b);
			return new ValueColor (r, g, b);
		}
	}

	public class Polygon
	{
		public readonly List<PointF> Points;

		public object? Tag;
		public object? SkiaTag;

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
#if __MACOS__ || __IOS__ || __MACCATALYST__
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
#if __MACOS__ || __IOS__ || __MACCATALYST__
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

