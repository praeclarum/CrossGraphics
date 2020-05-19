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

namespace CrossGraphics
{
	public class NullGraphics : IGraphics
	{
		const int MaxFontSize = 120;
		NullGraphicsFontMetrics[] _fontMetrics;

		int _fontSize = 10;

		public NullGraphics ()
		{
		}

		public void SetFont (Font f)
		{
			_fontSize = f.Size;
		}

		public void SetColor (Color c)
		{
		}

		public void Clear (Color c)
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

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
		}
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
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
			if (_fontMetrics == null) {
				_fontMetrics = new NullGraphicsFontMetrics[MaxFontSize + 1];
			}
			var i = Math.Min (_fontMetrics.Length, _fontSize);
			if (_fontMetrics[i] == null) {
				_fontMetrics[i] = new NullGraphicsFontMetrics (i);
			}
			return _fontMetrics[i];
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

	public class NullGraphicsFontMetrics : IFontMetrics
	{
		int _height;
		int _charWidth;
		bool isBold;

		public NullGraphicsFontMetrics (int size, bool isBold = false)
		{
			_height = size;
			_charWidth = (855 * size) / 1600;
			this.isBold = isBold;
		}

		public int Height => _height;

		public int Ascent => Height;

		public int Descent => 0;

		public int StringWidth (string text, int startIndex, int length)
		{
			if (string.IsNullOrEmpty (text))
				return 0;

			var fontHeight = _height;
			var lineHeight = (int)(_height * 1.42857143); // Floor is intentional -- browsers round down

			var props = isBold ? BoldCharacterProportions : CharacterProportions;
			var avgp = isBold ? BoldAverageCharProportion : AverageCharProportion;

			var px = 0.0;
			var lines = 1;
			var maxPWidth = 0.0;
			var pwidthConstraint = double.PositiveInfinity;
			var firstSpaceX = -1.0;
			var lastSpaceIndex = -1;
			var lineStartIndex = 0;

			var end = startIndex + length;
			for (var i = startIndex; i < end; i++) {
				var c = (int)text[i];
				var pw = (c < 128) ? props[c] : (c > 1024 ? 1.0 : avgp);
				// Should we wrap?
				if (px + pw > pwidthConstraint && lastSpaceIndex > 0) {
					lines++;
					maxPWidth = Math.Max (maxPWidth, firstSpaceX);
					i = lastSpaceIndex;
					while (i < end && text[i] == ' ') i++;
					i--;
					px = 0;
					firstSpaceX = -1;
					lastSpaceIndex = -1;
					lineStartIndex = i + 1;
				}
				else {
					if (c == ' ') {
						if (i >= lineStartIndex && i > 0 && text[i - 1] != ' ')
							firstSpaceX = px;
						lastSpaceIndex = i;
					}
					px += pw;
				}
			}
			maxPWidth = Math.Max (maxPWidth, px);
			var width = (int)Math.Ceiling (_height * maxPWidth);
			var height = lines * lineHeight;

			//Console.WriteLine ($"MEASURE TEXT SIZE {widthConstraint}x{heightConstraint} ==> {width}x{height} \"{text}\"");

			return width;
		}

		static readonly double[] CharacterProportions = {
			0, 0, 0, 0, 0, 0, 0, 0,
			0, 0.27799999713897705, 0.27799999713897705, 0.27799999713897705, 0.27799999713897705, 0.27799999713897705, 0, 0,
			0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0, 0, 0,
			0.27799999713897705, 0.25899994373321533, 0.4259999990463257, 0.5560001134872437, 0.5560001134872437, 1.0000001192092896, 0.6299999952316284, 0.27799999713897705,
			0.25899994373321533, 0.25899994373321533, 0.3520001173019409, 0.6000000238418579, 0.27799999713897705, 0.3890000581741333, 0.27799999713897705, 0.3330000638961792,
			0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437,
			0.5560001134872437, 0.5560001134872437, 0.27799999713897705, 0.27799999713897705, 0.6000000238418579, 0.6000000238418579, 0.6000000238418579, 0.5560001134872437,
			0.8000000715255737, 0.6480001211166382, 0.6850000619888306, 0.722000002861023, 0.7040001153945923, 0.6110001802444458, 0.5740000009536743, 0.7589999437332153,
			0.722000002861023, 0.25899994373321533, 0.5190001726150513, 0.6669999361038208, 0.5560001134872437, 0.8709999322891235, 0.722000002861023, 0.7600001096725464,
			0.6480001211166382, 0.7600001096725464, 0.6850000619888306, 0.6480001211166382, 0.5740000009536743, 0.722000002861023, 0.6110001802444458, 0.9259999990463257,
			0.6110001802444458, 0.6480001211166382, 0.6110001802444458, 0.25899994373321533, 0.3330000638961792, 0.25899994373321533, 0.6000000238418579, 0.5000001192092896,
			0.22200000286102295, 0.5370000600814819, 0.593000054359436, 0.5370000600814819, 0.593000054359436, 0.5370000600814819, 0.2960001230239868, 0.5740000009536743,
			0.5560001134872437, 0.22200000286102295, 0.22200000286102295, 0.5190001726150513, 0.22200000286102295, 0.8530000448226929, 0.5560001134872437, 0.5740000009536743,
			0.593000054359436, 0.593000054359436, 0.3330000638961792, 0.5000001192092896, 0.31500017642974854, 0.5560001134872437, 0.5000001192092896, 0.7580000162124634,
			0.5180000066757202, 0.5000001192092896, 0.4800001382827759, 0.3330000638961792, 0.22200000286102295, 0.3330000638961792, 0.6000000238418579, 0
		};
		const double AverageCharProportion = 0.5131400561332703;

		static readonly double[] BoldCharacterProportions = {
			0, 0, 0, 0, 0, 0, 0, 0,
			0, 0.27799999713897705, 0.27799999713897705, 0.27799999713897705, 0.27799999713897705, 0.27799999713897705, 0, 0,
			0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0, 0, 0,
			0.27799999713897705, 0.27799999713897705, 0.46299993991851807, 0.5560001134872437, 0.5560001134872437, 1.0000001192092896, 0.6850000619888306, 0.27799999713897705,
			0.2960001230239868, 0.2960001230239868, 0.40700018405914307, 0.6000000238418579, 0.27799999713897705, 0.40700018405914307, 0.27799999713897705, 0.37099993228912354,
			0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437, 0.5560001134872437,
			0.5560001134872437, 0.5560001134872437, 0.27799999713897705, 0.27799999713897705, 0.6000000238418579, 0.6000000238418579, 0.6000000238418579, 0.5560001134872437,
			0.8000000715255737, 0.6850000619888306, 0.7040001153945923, 0.7410000562667847, 0.7410000562667847, 0.6480001211166382, 0.593000054359436, 0.7589999437332153,
			0.7410000562667847, 0.29499995708465576, 0.5560001134872437, 0.722000002861023, 0.593000054359436, 0.9070001840591431, 0.7410000562667847, 0.777999997138977,
			0.6669999361038208, 0.777999997138977, 0.722000002861023, 0.6490000486373901, 0.6110001802444458, 0.7410000562667847, 0.6299999952316284, 0.9440001249313354,
			0.6669999361038208, 0.6669999361038208, 0.6480001211166382, 0.3330000638961792, 0.37099993228912354, 0.3330000638961792, 0.6000000238418579, 0.5000001192092896,
			0.25899994373321533, 0.5740000009536743, 0.6110001802444458, 0.5740000009536743, 0.6110001802444458, 0.5740000009536743, 0.3330000638961792, 0.6110001802444458,
			0.593000054359436, 0.2580000162124634, 0.27799999713897705, 0.5740000009536743, 0.2580000162124634, 0.906000018119812, 0.593000054359436, 0.6110001802444458,
			0.6110001802444458, 0.6110001802444458, 0.3890000581741333, 0.5370000600814819, 0.3520001173019409, 0.593000054359436, 0.5200001001358032, 0.8140000104904175,
			0.5370000600814819, 0.5190001726150513, 0.5190001726150513, 0.3330000638961792, 0.223000168800354, 0.3330000638961792, 0.6000000238418579, 0
		};
		const double BoldAverageCharProportion = 0.5346300601959229;
	}
}

