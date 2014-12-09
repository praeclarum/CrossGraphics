//
// Copyright (c) 2010-2014 Frank A. Krueger
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

#if MONOMAC
using MonoMac.CoreGraphics;
using MonoMac.AppKit;
#else
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
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
	
	public class CoreGraphicsGraphics : IGraphics
	{
		CGContext _c;

        private static Dictionary<string, CacheObjectDrawString> CacheObjectDrawStringDict = new Dictionary<string, CacheObjectDrawString>();

		//bool _highQuality = false;

		static CoreGraphicsGraphics ()
		{
			//foreach (var f in UIFont.FamilyNames) {
			//	Console.WriteLine (f);
			//	var fs = UIFont.FontNamesForFamilyName (f);
			//	foreach (var ff in fs) {
			//		Console.WriteLine ("  " + ff);
			//	}
			//}

			//float scale = UIScreen.MainScreen.Scale; // [[UIScreen mainScreen] scale];
			//UIGraphics.GetCurrentContext ().ScaleCTM (scale, scale);

			_textMatrix = CGAffineTransform.MakeScale (1, -1);
		}

		public CoreGraphicsGraphics (CGContext c, bool highQuality)
		{
			if (c == null) throw new ArgumentNullException ("c");

			_c = c;
			//_highQuality = highQuality;

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
			var cgcol = c.GetCGColor ();
#if MONOMAC
			_c.SetFillColorWithColor (cgcol);
			_c.SetStrokeColorWithColor (cgcol);
#else
			_c.SetFillColor (cgcol);
			_c.SetStrokeColor (cgcol);
#endif
		}

		public void Clear (Color color)
		{
			_c.ClearRect (_c.GetClipBoundingBox ());
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
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			_c.SetLineWidth (w);
			_c.AddArc (cx, cy, radius, -startAngle, -endAngle, true);
			_c.StrokePath ();
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			_c.AddArc (cx, cy, radius, -startAngle, -endAngle, true);
			_c.FillPath ();
		}

		const int _linePointsCount = 1024;
		PointF[] _linePoints = new PointF[_linePointsCount];
		bool _linesBegun = false;
		int _numLinePoints = 0;
		float _lineWidth = 1;
		bool _lineRounded = false;

		public void BeginLines (bool rounded)
		{
			_linesBegun = true;
			_lineRounded = rounded;
			_numLinePoints = 0;
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			#if DEBUG
			if (float.IsNaN (sx) || float.IsNaN (sy) || float.IsNaN (ex) || float.IsNaN (ey) || float.IsNaN (w)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.DrawLine");
			}
			#endif
			if (_linesBegun) {
				
				_lineWidth = w;
				if (_numLinePoints < _linePointsCount) {
					if (_numLinePoints == 0) {
						_linePoints[_numLinePoints].X = sx;
						_linePoints[_numLinePoints].Y = sy;
						_numLinePoints++;
					}
					_linePoints[_numLinePoints].X = ex;
					_linePoints[_numLinePoints].Y = ey;
					_numLinePoints++;
				}
				
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
			_c.SaveState ();
			_c.SetLineJoin (_lineRounded ? CGLineJoin.Round : CGLineJoin.Miter);
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
			_c.RestoreState ();
			_linesBegun = false;
		}
		
		static CGAffineTransform _textMatrix;

		Font _lastFont = null;

		public void SetFont (Font f)
		{
			if (f != _lastFont) {
				_lastFont = f;
				SelectFont ();
			}
		}

        void SelectFont()
        {
            var f = _lastFont;
            var name = "Helvetica";
            if (f.FontFamily == "Monospace" || f.FontFamily == "SystemFont")
            {
                if (f.IsBold)
                {
                    name = "Courier-Bold";
                }
                else
                {
                    name = "Courier";
                }
            }
            else if (f.FontFamily == "DBLCDTempBlack")
            {
#if MONOMAC
				name = "Courier-Bold";
#else
                name = f.FontFamily;
#endif
            }
            else if (!string.IsNullOrEmpty(f.FontFamily))
            {
                name = f.FontFamily;
            }
            else if (f.IsBold)
            {
                name = "Helvetica-Bold";
            }
            _c.SelectFont(name, f.Size, CGTextEncoding.MacRoman);
            _c.TextMatrix = _textMatrix;
        }
		
		static Dictionary<string, byte[]> _stringFixups = new Dictionary<string, byte[]>();

        byte[] FixupString(string s)
        {
            byte[] fix;
            if (_stringFixups.TryGetValue(s, out fix))
            {
                return fix;
            }
            else
            {
                fix = MacRomanEncoding.GetBytes(s.Replace("\u03A9", "Ohm"));
                _stringFixups[s] = fix;
                return fix;

                //var n = s.Length;
                //var bad = false;
                //for (var i = 0; i < n && !bad; i++)
                //{
                //    bad = ((int)s[i] > 127);
                //}
                //if (bad)
                //{
                //    fix = MacRomanEncoding.GetBytes(s.Replace("\u03A9", "Ohm"));
                //    _stringFixups[s] = fix;
                //    return fix;
                //}
                //else
                //{
                //    return null;
                //}
            }
		}
		
		public void SetClippingRect (float x, float y, float width, float height)
		{
			_c.ClipToRect (new RectangleF (x, y, width, height));
		}
		
		public double[] DrawString (string s, float x, float y)
		{			
            return DrawString(s, x, y, 0.0f, 0.0f, LineBreakMode.None, TextAlignment.Left);
        }

        public double[] DrawString(string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
            if (_lastFont == null) return new double[] { 0, 0 };
            float maxWidth = 0.0f;
            var cacheObjectKey = s + "_" + x + "_" + y + "_" + width + "_" + height + "_" + lineBreak + "_" + align;
            CacheObjectDrawString cacheObject = null;
            CacheObjectDrawStringDict.TryGetValue(cacheObjectKey, out cacheObject);
            if (cacheObject == null)
            {
                cacheObject = new CacheObjectDrawString();
                cacheObject.DeleteTag = false;
                cacheObject.Key = cacheObjectKey;
                //s = FixupString(s);
                cacheObject.StringLines = new List<string>() { s };
                CacheObjectDrawStringDict[cacheObjectKey] = cacheObject;
            }
            var fm = GetFontMetrics();
            string sPart = "";
            float stringWidth = 0.0f;
            if (cacheObject.LineBreak != lineBreak)
            {
                cacheObject.StringLines = new List<string>();
                cacheObject.LineBreak = lineBreak;
                switch (lineBreak)
                {
                    case LineBreakMode.WordWrap:
                        var wordParts = s.Split(new string[] { " " }, StringSplitOptions.None);
                        stringWidth = 0.0f;
                        sPart = "";
                        for (int i = 0; i < wordParts.Length; i++)
                        {
                            var item = wordParts[i];
                            stringWidth = fm.StringWidth(sPart + item + " ");
                            if (stringWidth > width && sPart.Length > 0)
                            {
                                sPart = sPart.Remove(sPart.Length - 1); //Remove space at the end
                                cacheObject.StringLines.Add(sPart);
                                sPart = "";
                            }
                            sPart += item + " ";
                        }
                        if (sPart.Length > 0)
                        {
                            sPart = sPart.Remove(sPart.Length - 1); //Remove space at the end
                            cacheObject.StringLines.Add(sPart);
                        }
                        break;

                    case LineBreakMode.Wrap:
                        //Cut the string if the width is reached
                        var charArray = s.ToCharArray();
                        sPart = "";
                        stringWidth = 0.0f;
                        for (int i = 0; i < charArray.Length; i++)
                        {
                            var item = charArray[i];
                            stringWidth = fm.StringWidth(sPart + item);
                            if (stringWidth > width && sPart.Length > 0)
                            {
                                cacheObject.StringLines.Add(sPart);
                                sPart = "";
                            }
                            sPart += item;
                        }
                        if (sPart.Length > 0)
                        {
                            cacheObject.StringLines.Add(sPart);
                        }
                        break;

                    default:
                        cacheObject.StringLines.Add(s);
                        break;
                }
                cacheObject.StringLines = cacheObject.StringLines;
            }

            switch (align)
            {
                case TextAlignment.Right:
                    //y += fm.Ascent - fm.Descent;
                    y += fm.Ascent;
                    foreach (var item in cacheObject.StringLines)
                    {
                        stringWidth = fm.StringWidth(item);
                        if (stringWidth > maxWidth)
                        {
                            maxWidth = stringWidth;
                        }
                        //_c.DrawText(item, x + width - stringWidth, y, _paints.Fill);
                        _c.ShowTextAtPoint(x + width - stringWidth, y, FixupString(item));

                        y += fm.Ascent + fm.Descent;
                    }
                    break;

                case TextAlignment.Center:
                    //y += fm.Ascent - fm.Descent;
                    y += fm.Ascent;
                    foreach (var item in cacheObject.StringLines)
                    {
                        stringWidth = fm.StringWidth(item);
                        if (stringWidth > maxWidth)
                        {
                            maxWidth = stringWidth;
                        }
                        //_c.DrawText(item, x + width * 0.5f - stringWidth * 0.5f, y, _paints.Fill);
                        _c.ShowTextAtPoint(x + width * 0.5f - stringWidth * 0.5f, y, FixupString(item));
                        y += fm.Ascent + fm.Descent;
                    }
                    break;

                default:
                    //y += fm.Ascent - fm.Descent;
                    y += fm.Ascent;
                    foreach (var item in cacheObject.StringLines)
                    {
                        stringWidth = fm.StringWidth(item);
                        if (stringWidth > maxWidth)
                        {
                            maxWidth = stringWidth;
                        }
                        //_c.DrawText(item, x, y, _paints.Fill);
                        _c.ShowTextAtPoint(x, y, FixupString(item));
                        y += fm.Ascent + fm.Descent;
                    }
                    break;

            }
            return new double[] { maxWidth, (fm.Ascent + fm.Descent) * cacheObject.StringLines.Count };

            //alt:
            //var fm = GetFontMetrics ();
            //var fix = FixupString (s);
            //var xx = x;
            //var yy = y;
            //if (align == TextAlignment.Center) {
            //    xx = (x + width / 2) - (fm.StringWidth (s) / 2);
            //}
            //else if (align == TextAlignment.Right) {
            //    xx = (x + width) - fm.StringWidth (s);
            //}
			
            //if (fix == null) {
            //    _c.ShowTextAtPoint(xx, yy + fm.Height, s);
            //}
            //else {
            //    _c.ShowTextAtPoint(xx, yy + fm.Height, fix);
            //}
            ////return new double[] { (double)fm.StringWidth(s), (double)fm.Height };
            //return new double[] { 0, 0 };
        }

		public IFontMetrics GetFontMetrics ()
		{
			var f = _lastFont;
			if (f == null) throw new InvalidOperationException ("Cannot call GetFontMetrics before calling SetFont.");

			var fm = f.Tag as CoreGraphicsFontMetrics;
			if (fm == null) {
				fm = new CoreGraphicsFontMetrics ();
				f.Tag = fm;
			}
			
			if (fm.Widths == null) {
				fm.MeasureText (_c, _lastFont);
			}

			return fm;
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			if (img is UIKitImage) {
				var uiImg = ((UIKitImage)img).Image;
				_c.DrawImage (new RectangleF (x, y, width, height), uiImg);
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
				SelectFont ();
			}
		}

		public IImage ImageFromFile (string filename)
		{
#if MONOMAC
			var img = new NSImage ("Images/" + filename);
			var rect = new RectangleF (PointF.Empty, img.Size);
			return new UIKitImage (img.AsCGImage (ref rect, NSGraphicsContext.CurrentContext, new MonoMac.Foundation.NSDictionary ()));
#else
			return new UIKitImage (UIImage.FromFile ("Images/" + filename).CGImage);
#endif
		}
		
		public void BeginEntity (object entity)
		{
		}
	}

	public static class ColorEx
	{
		class ColorTag {
#if MONOMAC
			public NSColor NSColor;
#else
			public UIColor UIColor;
#endif
			public CGColor CGColor;
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
			if (t == null) {
				t = new ColorTag ();
				c.Tag = t;
			}
			if (t.UIColor == null) {
				t.UIColor = UIColor.FromRGBA (c.Red / 255.0f, c.Green / 255.0f, c.Blue / 255.0f, c.Alpha / 255.0f);
			}
			return t.UIColor;
		}
#endif

		public static CGColor GetCGColor (this Color c)
		{
			var t = c.Tag as ColorTag;
			if (t == null) {
				t = new ColorTag ();
				c.Tag = t;
			}
			if (t.CGColor == null) {
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

	public class CoreGraphicsFontMetrics : IFontMetrics
	{
		int _height;
		public float[] Widths;
		
		const float DefaultWidth = 8.0f;

		public void MeasureText (CGContext c, Font f)
		{
//			Console.WriteLine ("MEASURE {0}", f);

            c.SetTextDrawingMode(CGTextDrawingMode.Invisible);
            
            c.TextPosition = new PointF(0, 0);
			c.ShowText ("MM");

            var mmWidth = c.TextPosition.X;
			
           

			_height = f.Size - 5;
			
			Widths = new float[0x80];

			for (var i = ' '; i < 127; i++) {

				var s = "M" + ((char)i).ToString() + "M";
				
				c.TextPosition = new PointF(0, 0);
				c.ShowText (s);
				
				var sz = c.TextPosition.X - mmWidth;
				
				if (sz < 0.1f) {
					Widths = null;
					return;
				}
				
				Widths[i] = sz;
			}
			
			c.SetTextDrawingMode (CGTextDrawingMode.Fill);
		}

		public CoreGraphicsFontMetrics ()
		{
		}
		
		public int StringWidth (string str, int startIndex, int length)
		{
			if (str == null) return 0;

			var end = startIndex + str.Length;
			if (end <= 0) return 0;
			
			if (Widths == null) {
				return 0;
			}

			var w = 0.0f;

			for (var i = startIndex; i < end; i++) {
				var ch = (int)str[i];
				if (ch < Widths.Length) {
					w += Widths[ch];
				}
				else {
					w += DefaultWidth;
				}
			}
			return (int)(w + 0.5f);
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

        private int _descent = 0;
		public int Descent
		{
			get {
                if (_descent == 0)
                {
                    //Dirty fix
                    _descent = (int)(Height * 0.1f);
                }
				return _descent;
			}
		}
	}

	public static class CGContextEx
	{
#if !MONOMAC
		[System.Runtime.InteropServices.DllImport (MonoTouch.Constants.CoreGraphicsLibrary)]
		extern static void CGContextShowTextAtPoint(IntPtr c, float x, float y, byte[] bytes, int size_t_length);
		public static void ShowTextAtPoint (this CGContext c, float x, float y, byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException ("bytes");
			CGContextShowTextAtPoint (c.Handle, x, y, bytes, bytes.Length);
		}
#endif

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
	
	public class MacRomanEncoding {
		static Dictionary<int, byte> _uniToMac= new Dictionary<int, byte>() {
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
			if (str == null) throw new ArgumentNullException ("str");
			var n = str.Length;
			var r = new byte [n];
			for (var i = 0; i < n; i++) {
				var ch = (int)str [i];
				var mac = (byte)'?';
				if (ch <= 127) {
					mac = (byte)ch;
				}
				else if (_uniToMac.TryGetValue (ch, out mac)) {
				}
				else {
					mac = (byte)'?';
				}
				r [i] = mac;
			}
			return r;
		}
	}
}

