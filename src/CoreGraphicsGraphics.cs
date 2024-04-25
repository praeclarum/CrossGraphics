#nullable enable

//
// Copyright (c) 2010-2024 Frank A. Krueger
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
using System.Diagnostics.CodeAnalysis;

using CoreGraphics;
using CoreText;
using Foundation;
using NativeSize = CoreGraphics.CGSize;
using NativePoint = CoreGraphics.CGPoint;

#if NET8_0_OR_GREATER
using NativeRect = CoreGraphics.CGRect;
#else
using NativeRect = System.Drawing.RectangleF;
#endif

#if MONOMAC
using AppKit;
using NativeStringAttributes = AppKit.NSStringAttributes;
using NativeColor = AppKit.NSColor;
#else
using UIKit;
using NativeStringAttributes = UIKit.UIStringAttributes;
using NativeColor = UIKit.UIColor;
#endif

namespace CrossGraphics.CoreGraphics
{
#if !MONOMAC
	public class UIKitGraphics : CrossGraphics.CoreGraphics.CoreGraphicsGraphics
	{
		public UIKitGraphics (bool highQuality)
			: base (UIGraphics.GetCurrentContext (), highQuality)
		{
		}
	}
#endif

	[SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
	public class CoreGraphicsGraphics : IGraphics
	{
		readonly CGContext _c;
		ValueColor _lastColor;

		readonly bool flipText;
		readonly CGAffineTransform textMatrix;

		Font? _lastFont;
		static readonly Dictionary<(string, int), NativeStringAttributes> cachedNSAttrs = new Dictionary<(string, int), NativeStringAttributes> ();
		static readonly Dictionary<(string, int), CTStringAttributes> cachedCTAttrs = new Dictionary<(string, int), CTStringAttributes> ();

		static void DumpFonts ()
		{
			//foreach (var f in UIFont.FamilyNames) {
			//	Console.WriteLine (f);
			//	var fs = UIFont.FontNamesForFamilyName (f);
			//	foreach (var ff in fs) {
			//		Console.WriteLine ("  " + ff);
			//	}
			//}			
		}

		public CoreGraphicsGraphics (CGContext c, bool highQuality, bool flipText = false)
		{
			if (c == null)
				throw new ArgumentNullException (nameof (c));

			_c = c;

			this.flipText = flipText;
			textMatrix = CGAffineTransform.MakeScale (1, -1);
			c.TextMatrix = textMatrix;

			if (highQuality) {
				c.SetLineCap (CGLineCap.Round);
			}
			else {
				c.SetLineCap (CGLineCap.Butt);
			}

			SetColor (Colors.Black);
		}

		public void SetColor (Color c)
		{
			var red = (nfloat)(c.Red / 255.0);
			var green = (nfloat)(c.Green / 255.0);
			var blue = (nfloat)(c.Blue / 255.0);
			var alpha = (nfloat)(c.Alpha / 255.0);
			_c.SetFillColor (red: red, green: green, blue: blue, alpha: alpha);
			_c.SetStrokeColor (red: red, green: green, blue: blue, alpha: alpha);
			_lastColor = new ValueColor ((byte)c.Red, (byte)c.Green, (byte)c.Blue, (byte)c.Alpha);
		}
		
		public void SetRgba (byte r, byte g, byte b, byte a)
		{
			var red = (nfloat)(r / 255.0);
			var green = (nfloat)(g / 255.0);
			var blue = (nfloat)(b / 255.0);
			var alpha = (nfloat)(a / 255.0);
			_c.SetFillColor (red: red, green: green, blue: blue, alpha: alpha);
			_c.SetStrokeColor (red: red, green: green, blue: blue, alpha: alpha);
			_lastColor = new ValueColor (r, g, b, a);
		}

		public void Clear (Color color)
		{
			_c.ClearRect (_c.GetClipBoundingBox ());
		}

		public void FillPolygon (Polygon poly)
		{
			var count = poly.Points.Count;
			_c.SetLineJoin (CGLineJoin.Round);
			_c.MoveTo (poly.Points[0].X, poly.Points[0].Y);
			for (var i = 1; i < count; i++) {
				var p = poly.Points[i];
				_c.AddLineToPoint (p.X, p.Y);
			}
			_c.FillPath ();
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			_c.SetLineWidth (w);
			_c.SetLineJoin (CGLineJoin.Round);
			_c.MoveTo (poly.Points[0].X, poly.Points[0].Y);
			for (var i = 1; i < poly.Points.Count; i++) {
				var p = poly.Points[i];
				_c.AddLineToPoint (p.X, p.Y);
			}
			_c.ClosePath ();
			_c.StrokePath ();
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			if (float.IsNaN (x) || float.IsNaN (y) || float.IsNaN (width) || float.IsNaN (height) || float.IsNaN (radius)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.FillRoundedRect");
				return;
			}

			_c.AddRoundedRect (new RectangleF (x, y, width, height), radius);
			_c.FillPath ();
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			if (float.IsNaN (x) || float.IsNaN (y) || float.IsNaN (width) || float.IsNaN (height) || float.IsNaN (radius) || float.IsNaN (w)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.DrawRoundedRect");
				return;
			}
			_c.SetLineWidth (w);
			_c.AddRoundedRect (new RectangleF (x, y, width, height), radius);
			_c.StrokePath ();
		}

		public void FillRect (float x, float y, float width, float height)
		{
			if (float.IsNaN (x) || float.IsNaN (y) || float.IsNaN (width) || float.IsNaN (height)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.FillRect");
				return;
			}
			_c.FillRect (new NativeRect (x, y, width, height));
		}

		public void FillOval (float x, float y, float width, float height)
		{
			if (float.IsNaN (x) || float.IsNaN (y) || float.IsNaN (width) || float.IsNaN (height)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.FillOval");
				return;
			}
			_c.FillEllipseInRect (new NativeRect (x, y, width, height));
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			if (float.IsNaN (x) || float.IsNaN (y) || float.IsNaN (width) || float.IsNaN (height) || float.IsNaN (w)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.DrawOval");
				return;
			}
			_c.SetLineWidth (w);
			_c.StrokeEllipseInRect (new NativeRect (x, y, width, height));
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			if (float.IsNaN (x) || float.IsNaN (y) || float.IsNaN (width) || float.IsNaN (height) || float.IsNaN (w)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.DrawRect");
				return;
			}
			_c.SetLineWidth (w);
			_c.StrokeRect (new NativeRect (x, y, width, height));
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			if (float.IsNaN (cx) || float.IsNaN (cy) || float.IsNaN (radius) || float.IsNaN (startAngle) || float.IsNaN (endAngle) || float.IsNaN (w)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.DrawArc");
				return;
			}
			_c.SetLineWidth (w);
			_c.AddArc (cx, cy, radius, -startAngle, -endAngle, true);
			_c.StrokePath ();
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			if (float.IsNaN (cx) || float.IsNaN (cy) || float.IsNaN (radius) || float.IsNaN (startAngle) || float.IsNaN (endAngle)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.FillArc");
				return;
			}
			_c.AddArc (cx, cy, radius, -startAngle, -endAngle, true);
			_c.FillPath ();
		}

		bool _linesBegun = false;
		int _numLinePoints = 0;
		float _lineWidth = 1;
		bool _lineRounded = false;

		public void BeginLines (bool rounded)
		{
			_linesBegun = true;
			_lineRounded = rounded;
			_numLinePoints = 0;
			_c.SaveState ();
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (float.IsNaN (sx) || float.IsNaN (sy) || float.IsNaN (ex) || float.IsNaN (ey) || float.IsNaN (w)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.DrawLine");
				return;
			}
			if (_linesBegun) {
				_lineWidth = w;
				if (_numLinePoints == 0) {
					_c.MoveTo (sx, sy);
				}
				_c.AddLineToPoint (ex, ey);
				_numLinePoints++;
			}
			else {
				_c.MoveTo (sx, sy);
				_c.AddLineToPoint (ex, ey);
				_c.SetLineWidth (w);
				_c.StrokePath ();
			}
		}

		public void EndLines ()
		{
			if (!_linesBegun)
				return;
			_c.SetLineJoin (_lineRounded ? CGLineJoin.Round : CGLineJoin.Miter);
			_c.SetLineWidth (_lineWidth);
			_c.StrokePath ();
			_c.RestoreState ();
			_linesBegun = false;
		}

		public void SetFont (Font f)
		{
			if (f != null && f != _lastFont) {
				_lastFont = f;
				SelectFont (f);
			}
		}

		const string DefaultFontName = "Helvetica";
		const string DefaultBoldFontName = "Helvetica-Bold";

		static string GetFontName (Font f)
		{
			var name = DefaultFontName;
			if (f.FontFamily == "Monospace") {
				if (f.IsBold) {
					name = "Courier-Bold";
				}
				else {
					name = "Courier";
				}
			}
			else if (f.FontFamily == "DBLCDTempBlack") {
#if MONOMAC
				name = "Courier-Bold";
#else
				name = f.FontFamily;
#endif
			}
			else if (f.IsBold) {
				name = DefaultBoldFontName;
			}
			return name;
		}

		void SelectFont (Font f)
		{
			var name = GetFontName (f);
			_c.SelectFont (name, f.Size, CGTextEncoding.MacRoman);
		}

		NativeStringAttributes GetNativeStringAttributes (Font f)
		{
			var name = GetFontName (f);
			var key = (name, (int)(f.Size + 0.5f));
			lock (cachedNSAttrs) {
				if (!cachedNSAttrs.TryGetValue (key, out var _nsattrs)) {
					_nsattrs = new NativeStringAttributes {
#if MONOMAC
						Font = NSFont.FromFontName (name, f.Size),
#else
						Font = UIFont.FromName (name, f.Size),
#endif
					};
					cachedNSAttrs.Add (key, _nsattrs);
				}
				return _nsattrs;
			}
		}

		CTStringAttributes GetCTStringAttributes (Font f)
		{
			var name = GetFontName (f);
			var key = (name, (int)(f.Size + 0.5f));
			lock (cachedNSAttrs) {
				if (!cachedCTAttrs.TryGetValue (key, out var _attrs)) {
					_attrs = new CTStringAttributes {
						Font = new CTFont (name, f.Size),
						ForegroundColorFromContext = true,
					};
					cachedCTAttrs.Add (key, _attrs);
				}
				return _attrs;
			}
		}

		public void SetClippingRect (float x, float y, float width, float height)
		{
			_c.ClipToRect (new NativeRect (x, y, width, height));
		}

		public void DrawString (string s, float x, float y)
		{
			if (string.IsNullOrEmpty (s))
				return;

			var f = _lastFont;
			if (f == null)
				return;
			var fsize = f.Size;

			var yy = y + fsize * 0.8333f;

			var isSafe = true;
			for (var i = 0; isSafe && i < s.Length; i++) {
				isSafe = s[i] < 127;
				if (!isSafe) {
					//Console.WriteLine ($"UNSAFE {s} at #{i}");
				}
			}

			if (isSafe) {
				_c.TextMatrix = textMatrix;
				if (flipText) {
					_c.ShowTextAtPoint (x, yy, s);
				}
				else {
					_c.ShowTextAtPoint (x, yy, s);
				}
				return;
			}

			_c.TextMatrix = textMatrix;
			var _nsattrs = GetNativeStringAttributes (f);
#if MONOMAC
			_nsattrs.ForegroundColor = NativeColor.FromRgba (_lastColor.Red, _lastColor.Green, _lastColor.Blue, _lastColor.Alpha);
			using var astr2 = new NSAttributedString (s, _nsattrs);
			var size = astr2.GetSize ();
#else
			_nsattrs.ForegroundColor = NativeColor.FromRGBA (_lastColor.Red, _lastColor.Green, _lastColor.Blue, _lastColor.Alpha);
			using var astr2 = new NSAttributedString (s, _nsattrs);
			var size = astr2.Size;
#endif
			yy -= (float)((float)size.Height * 0.8333f);
			if (flipText) {
				_c.SaveState ();
				_c.TranslateCTM (x, yy + size.Height);
				_c.ScaleCTM (1, -1);
#if MONOMAC
				astr2.DrawAtPoint (new CGPoint (0, 0));
#else
				astr2.DrawString (new CGPoint (0, 0));
#endif
				_c.RestoreState ();
			}
			else {
#if MONOMAC
				astr2.DrawAtPoint (new CGPoint (x, yy));
#else
				astr2.DrawString (new CGPoint (x, yy));
#endif
			}

			SelectFont (f);

			return;
		}

		public void DrawString (string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			if (_lastFont == null)
				return;
			var fm = GetFontMetrics ();
			var xx = x;
			var yy = y;
			if (align == TextAlignment.Center) {
				xx = (x + width / 2) - (fm.StringWidth (s) / 2);
			}
			else if (align == TextAlignment.Right) {
				xx = (x + width) - fm.StringWidth (s);
			}

			DrawString (s, xx, yy);
		}

		public IFontMetrics GetFontMetrics ()
		{
			var f = _lastFont;
			if (f == null)
				throw new InvalidOperationException ("Cannot call GetFontMetrics before calling SetFont.");
			var name = GetFontName (f);
			if (name == DefaultFontName || name == DefaultBoldFontName) {
				var fm = f.Tag as CoreGraphicsFontMetrics;
				if (fm == null) {
					fm = new CoreGraphicsFontMetrics (f.Size, f.IsBold);
					f.Tag = fm;
				}
				return fm;
			}
			else {
				var fm = f.Tag as NSStringFontMetrics;
				if (fm == null) {
					fm = new NSStringFontMetrics (GetNativeStringAttributes (f));
					f.Tag = fm;
				}
				return fm;
			}
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			if (img is UIKitImage) {
				var uiImg = ((UIKitImage)img).Image;
				_c.DrawImage (new NativeRect (x, y, width, height), uiImg);
			}
		}

		public void SaveState ()
		{
			_c.SaveState ();
		}

		public void Translate (float dx, float dy)
		{
			_c.TranslateCTM (dx, dy);
		}

		public void Scale (float sx, float sy)
		{
			_c.ScaleCTM (sx, sy);
		}

		public void RestoreState ()
		{
			_c.RestoreState ();
			if (_lastFont != null) {
				SelectFont (_lastFont);
			}
		}

		public IImage ImageFromFile (string filename)
		{
#if MONOMAC
			var img = new NSImage ("Images/" + filename);
			var rect = new CGRect (CGPoint.Empty, img.Size);
			return new UIKitImage (img.AsCGImage (ref rect, NSGraphicsContext.CurrentContext, new Foundation.NSDictionary ()));
#else
			var uiimage = UIImage.FromFile ("Images/" + filename);
			if (uiimage == null || uiimage.CGImage == null)
				throw new Exception ("Could not load " + filename);
			return new UIKitImage (uiimage.CGImage);
#endif
		}

		public void BeginEntity (object entity)
		{
		}
	}

	public static class ColorEx
	{
		class ColorTag
		{
#if MONOMAC
			public NSColor? NSColor;
#else
			public UIColor? UIColor;
#endif
			public CGColor? CGColor;
		}

#if MONOMAC
		public static NSColor GetNSColor (this Color c)
		{
			var t = c.Tag as ColorTag;
			if (t == null) {
				t = new ColorTag ();
				c.Tag = t;
			}
			if (t.NSColor == null) {
				t.NSColor = NSColor.FromDeviceRgba (c.Red / 255.0f, c.Green / 255.0f, c.Blue / 255.0f, c.Alpha / 255.0f);
			}
			return t.NSColor;
		}
#else
		public static UIColor GetUIColor (this Color c)
		{
			var t = c.Tag as ColorTag;
			if (t is null) {
				t = new ColorTag ();
				c.Tag = t;
			}
			if (t.UIColor is null) {
				t.UIColor = UIColor.FromRGBA (c.Red / 255.0f, c.Green / 255.0f, c.Blue / 255.0f, c.Alpha / 255.0f);
			}
			return t.UIColor;
		}
#endif

		public static CGColor GetCGColor (this Color c)
		{
			var t = c.Tag as ColorTag;
			if (t is null) {
				t = new ColorTag ();
				c.Tag = t;
			}
			if (t.CGColor is null) {
				t.CGColor = new CGColor (c.Red / 255.0f, c.Green / 255.0f, c.Blue / 255.0f, c.Alpha / 255.0f);
			}
			return t.CGColor;
		}
	}

	public static class FontEx
	{
		/*public static UIFont CreateUIFont (this Font f)
		{
			if (f.FontFamily == "") {
				return UIFont.FromName (f.FontFamily, f.Size);
			}
			else {
				if ((f.Options & FontOptions.Bold) != 0) {
					return UIFont.BoldSystemFontOfSize (f.Size);
				}
				else {
					return UIFont.SystemFontOfSize (f.Size);
				}
			}
		}*/

		/*public static CTFont GetCTFont (this Font f)
		{
			var t = f.Tag as CTFont;
			if (t == null) {
				if (f.Options == FontOptions.Bold) {
					t = new CTFont ("Helvetica-Bold", f.Size);
				} else {
					t = new CTFont ("Helvetica", f.Size);
				}
				f.Tag = t;
			}
			return t;
		}*/
	}

	public class UIKitImage : IImage
	{
		public CGImage Image { get; private set; }
		public UIKitImage (CGImage image)
		{
			Image = image;
		}
	}

	public class CoreGraphicsFontMetrics : NullGraphicsFontMetrics
	{
		public CoreGraphicsFontMetrics (int size, bool isBold)
			: base (size, isBold)
		{
		}
	}

	[SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
	public class NSStringFontMetrics : IFontMetrics
	{
		int _ascent;
		int _descent;

		readonly NativeStringAttributes nsattrs;

		public NSStringFontMetrics (NativeStringAttributes nsattrs)
		{
			this.nsattrs = nsattrs;
		}

		public int StringWidth (string str, int startIndex, int length)
		{
			return StringSize (str, startIndex, length).Width;
		}

		(int Width, int Ascent, int Descent) StringSize (string str, int startIndex, int length)
		{
			if (str == null || length <= 0)
				return (0, 0, 0);
#if MONOMAC
			using var s = new NSAttributedString (str, nsattrs);
			var ssize = s.GetSize ();
#else
			var ssize = UIKit.UIStringDrawing.StringSize (str, nsattrs.Font);
#endif
			return ((int)(ssize.Width + 0.5), (int)(ssize.Height + 0.5), 0);
		}

		public int Height {
			get {
				if (_ascent <= 0) {
					var s = StringSize ("M", 0, 1);
					_ascent = s.Ascent;
					_descent = s.Descent;
				}
				return _ascent;
			}
		}

		public int Ascent {
			get {
				if (_ascent <= 0) {
					var s = StringSize ("M", 0, 1);
					_ascent = s.Ascent;
					_descent = s.Descent;
				}
				return _ascent;
			}
		}

		public int Descent {
			get {
				if (_ascent <= 0) {
					var s = StringSize ("M", 0, 1);
					_ascent = s.Ascent;
					_descent = s.Descent;
				}
				return _descent;
			}
		}
	}

	public static class CGContextEx
	{
#if !MONOMAC
		[System.Runtime.InteropServices.DllImport (ObjCRuntime.Constants.CoreGraphicsLibrary)]
		extern static void CGContextShowTextAtPoint(IntPtr c, nfloat x, nfloat y, byte[] bytes, nint size_t_length);
		public static void ShowTextAtPoint (this CGContext c, nfloat x, nfloat y, byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException ("bytes");
			CGContextShowTextAtPoint (c.Handle, x, y, bytes, bytes.Length);
		}
#endif

		public static void AddRoundedRect (this CGContext c, RectangleF b, float r)
		{
			var x = b.X;
			var y = b.Y;
			var w = b.Width;
			var h = b.Height;
			if (w < 0) {
				x += w;
				w = Math.Abs (w);
			}
			if (h < 0) {
				y += h;
				h = Math.Abs (h);
			}

			var ri = x + w;
			var bo = y + h;

			c.MoveTo (x, y + r);
			c.AddLineToPoint (x, bo - r);

			c.AddArc (x + r, bo - r, r, (float)(Math.PI), (float)(Math.PI / 2), true);

			c.AddLineToPoint (ri - r, bo);

			c.AddArc (ri - r, bo - r, r, (float)(-3 * Math.PI / 2), (float)(0), true);

			c.AddLineToPoint (ri, y + r);

			c.AddArc (ri - r, y + r, r, (float)(0), (float)(-Math.PI / 2), true);

			c.AddLineToPoint (x + r, y);

			c.AddArc (x + r, y + r, r, (float)(-Math.PI / 2), (float)(Math.PI), true);
		}

		public static void AddBottomRoundedRect (this CGContext c, RectangleF b, float r)
		{
			c.MoveTo (b.Left, b.Top + r);
			c.AddLineToPoint (b.Left, b.Bottom - r);

			c.AddArc (b.Left + r, b.Bottom - r, r, (float)(Math.PI), (float)(Math.PI / 2), true);

			c.AddLineToPoint (b.Right - r, b.Bottom);

			c.AddArc (b.Right - r, b.Bottom - r, r, (float)(-3 * Math.PI / 2), (float)(0), true);

			c.AddLineToPoint (b.Right, b.Top);

			c.AddLineToPoint (b.Left, b.Top);
		}
	}

	public class MacRomanEncoding
	{
		static readonly Dictionary<int, byte> _uniToMac = new Dictionary<int, byte> () {
			{160, 202}, {161, 193}, {162, 162}, {163, 163}, {165, 180}, {167, 164}, {168, 172}, {169,
			169}, {170, 187}, {171, 199}, {172, 194}, {174, 168}, {175, 248}, {176, 161}, {177, 177},
			{180, 171}, {181, 181}, {182, 166}, {183, 225}, {184, 252}, {186, 188}, {187, 200}, {191,
			192}, {192, 203}, {193, 231}, {194, 229}, {195, 204}, {196, 128}, {197, 129}, {198, 174},
			{199, 130}, {200, 233}, {201, 131}, {202, 230}, {203, 232}, {204, 237}, {205, 234}, {206,
			235}, {207, 236}, {209, 132}, {210, 241}, {211, 238}, {212, 239}, {213, 205}, {214, 133},
			{216, 175}, {217, 244}, {218, 242}, {219, 243}, {220, 134}, {223, 167}, {224, 136}, {225,
			135}, {226, 137}, {227, 139}, {228, 138}, {229, 140}, {230, 190}, {231, 141}, {232, 143},
			{233, 142}, {234, 144}, {235, 145}, {236, 147}, {237, 146}, {238, 148}, {239, 149}, {241,
			150}, {242, 152}, {243, 151}, {244, 153}, {245, 155}, {246, 154}, {247, 214}, {248, 191},
			{249, 157}, {250, 156}, {251, 158}, {252, 159}, {255, 216}, {305, 245}, {338, 206}, {339,
			207}, {376, 217}, {402, 196}, {710, 246}, {711, 255}, {728, 249}, {729, 250}, {730, 251},
			{731, 254}, {732, 247}, {733, 253}, {937, 189}, {960, 185}, {8211, 208}, {8212, 209},
			{8216, 212}, {8217, 213}, {8218, 226}, {8220, 210}, {8221, 211}, {8222, 227}, {8224, 160},
			{8225, 224}, {8226, 165}, {8230, 201}, {8240, 228}, {8249, 220}, {8250, 221}, {8260, 218},
			{8364, 219}, {8482, 170}, {8706, 182}, {8710, 198}, {8719, 184}, {8721, 183}, {8730, 195},
			{8734, 176}, {8747, 186}, {8776, 197}, {8800, 173}, {8804, 178}, {8805, 179}, {9674, 215},
			{63743, 240}, {64257, 222}, {64258, 223},
		};
		public static byte[] GetBytes (string str)
		{
			if (str == null)
				throw new ArgumentNullException (nameof(str));
			var n = str.Length;
			var r = new byte[n];
			for (var i = 0; i < n; i++) {
				var ch = (int)str[i];
				var mac = (byte)'?';
				if (ch <= 127) {
					mac = (byte)ch;
				}
				else if (_uniToMac.TryGetValue (ch, out mac)) {
				}
				else {
					mac = (byte)'?';
				}
				r[i] = mac;
			}
			return r;
		}
	}
}

