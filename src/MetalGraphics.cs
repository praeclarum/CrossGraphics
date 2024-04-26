#nullable enable

//
// Copyright (c) 2024 Frank A. Krueger
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
using System.Numerics;

namespace CrossGraphics.Metal
{
	public class MetalGraphics : IGraphics
	{
		Font _currentFont = CrossGraphics.Font.SystemFontOfSize (16);
		ValueColor _currentColor = new ValueColor (0, 0, 0, 255);

		ValueColor _clearColor = new ValueColor (0, 0, 0, 0);
		public ValueColor ClearColor => _clearColor;

		struct State
		{
			public Matrix3x2 Transform;
		}

		private readonly List<State> _states = new List<State> () { new State () { Transform = Matrix3x2.Identity } };

		public void BeginEntity (object entity)
		{
		}

		public IFontMetrics GetFontMetrics ()
		{
			return new NullGraphicsFontMetrics (_currentFont.Size, isBold: _currentFont.IsBold);
		}

		public void SetFont (Font f)
		{
			_currentFont = f;
		}

		public void SetColor (Color c)
		{
			_currentColor = new ValueColor((byte)c.Red, (byte)c.Green, (byte)c.Blue, (byte)c.Alpha);
		}

		public void SetRgba (byte r, byte g, byte b, byte a)
		{
			_currentColor = new ValueColor(r, g, b, a);
		}

		public void Clear (Color c)
		{
			_clearColor = new ValueColor((byte)c.Red, (byte)c.Green, (byte)c.Blue, (byte)c.Alpha);
		}

		public void SaveState ()
		{
			_states.Add(_states[^1]);
		}

		public void SetClippingRect (float x, float y, float width, float height)
		{
		}

		public void Translate (float dx, float dy)
		{
			var state = _states[^1];
			state.Transform *= Matrix3x2.CreateTranslation(dx, dy);
			_states[^1] = state;
		}

		public void Scale (float sx, float sy)
		{
			var state = _states[^1];
			state.Transform *= Matrix3x2.CreateScale(sx, sy);
			_states[^1] = state;
		}

		public void RestoreState ()
		{
			if (_states.Count > 1)
			{
				_states.RemoveAt(_states.Count - 1);
			}
		}

		public IImage? ImageFromFile (string path)
		{
			return null;
		}

		public void FillPolygon (Polygon poly)
		{
			// TODO: Implement
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			// TODO: Implement
		}

		public void FillRect (float x, float y, float width, float height)
		{
			// TODO: Implement
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			// TODO: Implement
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			// TODO: Implement
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			// TODO: Implement
		}

		public void FillOval (float x, float y, float width, float height)
		{
			// TODO: Implement
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			// TODO: Implement
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			throw new NotImplementedException ();
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			throw new NotImplementedException ();
		}

		public void BeginLines (bool rounded)
		{
			// TODO: Implement
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			// TODO: Implement
		}

		public void EndLines ()
		{
			// TODO: Implement
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			// TODO: Implement
		}

		public void DrawString (string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			// TODO: Implement
		}

		public void DrawString (string s, float x, float y)
		{
			// TODO: Implement
		}
	}
}
