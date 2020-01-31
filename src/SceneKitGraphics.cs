//
// Copyright (c) 2010-2020 Frank A. Krueger
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
using System.Linq;

using CoreGraphics;
using SceneKit;

#if __MACOS__
using NativeColor = AppKit.NSColor;
#endif

namespace CrossGraphics.SceneKit
{
	public class SceneKitGraphics : IGraphics
	{
		public readonly List<State> states = new List<State> { new State () };

		readonly SCNScene scene;
		readonly SCNNode rootNode;

		readonly EntityNode[] initialEntityNodes;

		int nextInitialEntityNode;
		EntityNode Node => null;

		Color currentColor = Colors.Black;
		Font currentFont = Font.SystemFontOfSize (16);

		readonly List<SCNVector3> linePoints = new List<SCNVector3> ();
		bool inLines = false;
		float lineW = 1;

		public SceneKitGraphics (SCNScene scene)
		{
			this.scene = scene;
			rootNode = scene.RootNode;
			initialEntityNodes = rootNode.ChildNodes.OfType<EntityNode> ().ToArray ();
			Console.WriteLine (nameof (SceneKitGraphics) + $" Init: {initialEntityNodes.Length} nodes, {scene}");
		}

		public void SaveState ()
		{
			var os = states[^1];
			var ns = new State {
				Transform = os.Transform,
			};
			states.Add (ns);
		}

		public void RestoreState ()
		{
			if (states.Count > 1) {
				states.RemoveAt (states.Count - 1);
			}
		}

		public void Scale (float sx, float sy)
		{
			var s = states[^1];
			s.Transform = SCNMatrix4.Scale (sx, sy, 1) * s.Transform;
		}

		public void Translate (float dx, float dy)
		{
			var s = states[^1];
			s.Transform = SCNMatrix4.CreateTranslation (dx, dy, 0) * s.Transform;
		}

		public void BeginEntity (object entity)
		{
			throw new NotImplementedException ();
		}

		public void SetColor (Color c)
		{
			currentColor = c;
		}

		static NativeColor GetNativeColor (Color c)
		{
#if __MACOS__
			if (c.Tag is global::AppKit.NSColor nsc)
				return nsc;
			nsc = global::AppKit.NSColor.FromRgba ((byte)c.Red, (byte)c.Green, (byte)c.Blue, (byte)c.Alpha);
			c.Tag = nsc;
			return nsc;
#endif
		}

		public IFontMetrics GetFontMetrics ()
		{
			return new NullGraphicsFontMetrics (currentFont.Size, isBold: currentFont.IsBold);
		}

		public void SetFont (Font f)
		{
			currentFont = f;
		}

		public void SetClippingRect (float x, float y, float width, float height)
		{
		}

		public void Clear (Color c)
		{
			scene.Background.ContentColor = GetNativeColor (c);
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			var style = new Style {
				W = w,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.Arc (cx, cy, radius, startAngle, endAngle, ref style);
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.Arc (cx, cy, radius, startAngle, endAngle, ref style);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var style = new Style {
				W = w,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.Oval (x, y, width, height, ref style);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.Oval (x, y, width, height, ref style);
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			var style = new Style {
				W = w,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.Polygon (poly, ref style);
		}

		public void FillPolygon (Polygon poly)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.Polygon (poly, ref style);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			var style = new Style {
				W = w,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.Rect (x, y, width, height, ref style);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.Rect (x, y, width, height, ref style);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			var style = new Style {
				W = w,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.RoundedRect (x, y, width, height, radius, ref style);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			Node.RoundedRect (x, y, width, height, radius, ref style);
		}

		public void BeginLines (bool rounded)
		{
			inLines = true;
			linePoints.Clear ();
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (inLines) {
				lineW = w;
				if (linePoints.Count == 0)
					linePoints.Add (new SCNVector3 (sx, sy, 0));
				linePoints.Add (new SCNVector3 (ex, ey, 0));
			}
			else {
				var style = new Style {
					W = w,
					Color = currentColor,
					Transform = states[^1].Transform,
				};
				Node.Line (sx, sy, ex, ey, ref style);
			}
		}

		public void EndLines ()
		{
			if (inLines) {
				var style = new Style {
					W = lineW,
					Color = currentColor,
					Transform = states[^1].Transform,
				};
				Node.Lines (linePoints, ref style);
			}
		}

		public void DrawString (string s, float x, float y)
		{
		}

		public void DrawString (string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
		}

		public IImage ImageFromFile (string path)
		{
			return null;
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
		}

		public class State
		{
			public SCNMatrix4 Transform;
		}

		struct Style
		{
			public SCNMatrix4 Transform;
			public Color Color;
			public float W;
			public bool Fill;
		}

		class EntityNode : SCNNode
		{
			public void Arc (float cx, float cy, float radius, float startAngle, float endAngle, ref Style style)
			{
			}

			public void Line (float sx, float sy, float ex, float ey, ref Style style)
			{
			}

			public void Lines (List<SCNVector3> points, ref Style style)
			{
			}

			public void Oval (float x, float y, float width, float height, ref Style style)
			{
			}

			public void Polygon (Polygon poly, ref Style style)
			{
			}

			public void Rect (float x, float y, float width, float height, ref Style style)
			{
			}

			public void RoundedRect (float x, float y, float width, float height, float radius, ref Style style)
			{
			}
		}
	}
}
