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
using System.Collections.Generic;
using System.Drawing;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.CoreText;

namespace CrossGraphics.AppKit
{
	public class AppKitGraphics : IGraphics
	{
		CGContext _c;

		UIKitGraphicsFontMetrics _fontMetrics;

		static CGAffineTransform _textMatrix;

		Font _lastFont = null;

		static AppKitGraphics ()
		{
//			foreach (var f in UIFont.FamilyNames) {
//				Console.WriteLine (f);
//				var fs = UIFont.FontNamesForFamilyName (f);
//				foreach (var ff in fs) {
//					Console.WriteLine ("  " + ff);
//				}
//			}

			_textMatrix = CGAffineTransform.MakeScale (1, -1);
		}

		public AppKitGraphics (CGContext c, float viewHeight)
		{
			_c = c;
			_fontMetrics = new UIKitGraphicsFontMetrics ();
			c.SetLineCap (CGLineCap.Round);
			SetColor (Colors.Black);

			c.ScaleCTM(1, -1);
			c.TranslateCTM(0, -viewHeight);
		}

		public void SetFont (Font f)
		{
			if (f != _lastFont) {
				_lastFont = f;
				_c.SelectFont (((f.Options & FontOptions.Bold) != 0) ? "Helvetica-Bold" : "Helvetica", 16, CGTextEncoding.MacRoman);
				_c.TextMatrix = _textMatrix;
			}
		}

		public void SetColor (Color c)
		{
			c.GetUIColor ().Set ();
		}

		public void FillPolygon (Polygon poly)
		{
			var count = poly.Points.Count;
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
			_c.MoveTo (poly.Points[0].X, poly.Points[0].Y);
			for (var i = 1; i < poly.Points.Count; i++) {
				var p = poly.Points[i];
				_c.AddLineToPoint (p.X, p.Y);
			}
			_c.AddLineToPoint (poly.Points[0].X, poly.Points[0].Y);
			_c.StrokePath ();
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			_c.AddRoundedRect (new RectangleF (x, y, width, height), radius);
			_c.FillPath ();
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			_c.SetLineWidth (w);
			_c.AddRoundedRect (new RectangleF (x, y, width, height), radius);
			_c.StrokePath ();
		}

		public void FillRect (float x, float y, float width, float height)
		{
			_c.FillRect (new RectangleF (x, y, width, height));
		}

		public void FillOval (float x, float y, float width, float height)
		{
			_c.FillEllipseInRect (new RectangleF (x, y, width, height));
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			_c.SetLineWidth (w);
			_c.StrokeEllipseInRect (new RectangleF (x, y, width, height));
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			_c.SetLineWidth (w);
			_c.StrokeRect (new RectangleF (x, y, width, height));
		}

		List<PointF> _linePoints = new List<PointF> ();
		bool _linesBegun = false;
		int _numLinePoints = 0;
		float _lineWidth = 1;

		public void BeginLines ()
		{
			_linesBegun = true;
			_numLinePoints = 0;
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_linesBegun) {
				
				_lineWidth = w;
				if (_linePoints.Count < _numLinePoints + 2) {
					_linePoints.Add (PointF.Empty);
					_linePoints.Add (PointF.Empty);
				}
				if (_numLinePoints == 0) {
					_linePoints[_numLinePoints] = new PointF (sx, sy);
					_numLinePoints++;
				}
				_linePoints[_numLinePoints] = new PointF (ex, ey);
				_numLinePoints++;
				
			} else {
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
			_c.SetLineJoin (CGLineJoin.Round);
			_c.SetLineWidth (_lineWidth);
			for (var i = 0; i < _numLinePoints; i++) {
				var p = _linePoints[i];
				if (i == 0) {
					_c.MoveTo (p.X, p.Y);
				} else {
					_c.AddLineToPoint (p.X, p.Y);
				}
			}
			_c.StrokePath ();
			_linesBegun = false;
		}

		public void DrawString (string s, float x, float y)
		{
			_c.ShowTextAtPoint (x, y + 11, s);
		}

		public IFontMetrics GetFontMetrics ()
		{
			return _fontMetrics;
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
//			if (img is UIKitImage) {
//				var uiImg = ((UIKitImage)img).Image;
//				_c.DrawImage (new RectangleF (x, y, width, height), uiImg.AsCGImage());
//			}
		}

		public IImage ImageFromFile (string filename)
		{
			return null;
//			return new UIKitImage (NSImage.FromFile (filename));
		}
		
		public void BeginEntity (object entity)
		{
		}
	}

	public static class ColorEx
	{
		public static NSColor GetUIColor (this Color c)
		{
			var t = c.Tag as NSColor;
			if (t == null) {
				t = NSColor.FromDeviceRgba (c.Red / 255.0f, c.Green / 255.0f, c.Blue / 255.0f, c.Alpha / 255.0f);
				c.Tag = t;
			}
			return t;
		}
	}

	public static class FontEx
	{
		public static CTFont GetCTFont (this Font f)
		{
			var t = f.Tag as CTFont;
			if (t == null) {
				if (f.Options == FontOptions.Bold) {
					t = new CTFont ("HelveticaNeue-Bold", f.Size);
				} else {
					t = new CTFont ("HelveticaNeue", f.Size);
				}
				f.Tag = t;
			}
			return t;
		}
	}

	public class UIKitImage : IImage
	{
		public NSImage Image { get; private set; }
		public UIKitImage (NSImage image)
		{
			Image = image;
		}
	}

	public class UIKitGraphicsFontMetrics : IFontMetrics
	{
		int _height;
		static int[] _widths;
		const int DefaultWidth = 10;

		static UIKitGraphicsFontMetrics() {
			_widths = new int[128];
			for (var i = 0; i < _widths.Length; i++) {
				_widths[i] = DefaultWidth;
			}
		}

		public static void MeasureText(NSView view, NSFont font) {

			//var mmSize = view.StringSize("MM", font);

			for (var i = ' '; i < 127; i++) {

//				var s = "M" + ((char)i).ToString() + "M";

//				var sz = view.StringSize(s, font);

				_widths[i] = 12;//(int)(sz.Width - mmSize.Width + 0.5f);
			}
		}

		public UIKitGraphicsFontMetrics ()
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
				var ch = (int)str[i];
				if (ch < _widths.Length) {
					w += _widths[ch];
				}
				else {
					w += DefaultWidth;
				}
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

	public static class CGContextEx
	{
		public static void AddRoundedRect (this CGContext c, RectangleF b, float r)
		{
			c.MoveTo (b.Left, b.Top + r);
			c.AddLineToPoint (b.Left, b.Bottom - r);
			
			c.AddArc (b.Left + r, b.Bottom - r, r, (float)(Math.PI), (float)(Math.PI / 2), true);
			
			c.AddLineToPoint (b.Right - r, b.Bottom);
			
			c.AddArc (b.Right - r, b.Bottom - r, r, (float)(-3 * Math.PI / 2), (float)(0), true);
			
			c.AddLineToPoint (b.Right, b.Top + r);
			
			c.AddArc (b.Right - r, b.Top + r, r, (float)(0), (float)(-Math.PI / 2), true);
			
			c.AddLineToPoint (b.Left + r, b.Top);
			
			c.AddArc (b.Left + r, b.Top + r, r, (float)(-Math.PI / 2), (float)(Math.PI), true);
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
}

