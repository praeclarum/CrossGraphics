#nullable enable

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
using Foundation;
using AppKit;

#if __MACOS__
using NativeColor = AppKit.NSColor;
#endif

namespace CrossGraphics.SceneKit
{
	public class SceneKitGraphics : IGraphics
	{
		public readonly List<State> states = new List<State> { new State { Transform = SCNMatrix4.Identity } };

		readonly SCNScene scene;
		readonly SCNNode rootNode;

		int currentNodeIndex = 0;
		EntityNode currentNode => entityNodes[currentNodeIndex];

		Color currentColor = Colors.Black;
		Font currentFont = Font.SystemFontOfSize (16);

		readonly List<SCNVector3> linePoints = new List<SCNVector3> ();
		bool inLines = false;
		float lineW = 1;

		readonly List<EntityNode> entityNodes = new List<EntityNode> { new EntityNode () };

		public SceneKitGraphics (SCNScene scene)
		{
			this.scene = scene;
			rootNode = scene.RootNode;
		}

		public void BeginFrame ()
		{
			currentNodeIndex = 0;
			states.Clear ();
			states.Add (new State { Transform = SCNMatrix4.Identity });
			foreach (var n in entityNodes)
				n.BeginFrame ();

			SCNTransaction.Begin ();
			SCNTransaction.DisableActions = true;
		}

		public void EndFrame ()
		{
			SCNTransaction.Commit ();
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
			//Console.WriteLine ($"ENTITY {entity}");
			currentNodeIndex++;
			if (currentNodeIndex >= entityNodes.Count) {
				var node = new EntityNode ();
				rootNode.Add (node);
				entityNodes.Add (node);
			}
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
		static SCNMaterial GetNativeMaterial (Color c)
		{
			if (c.Tag is SCNMaterial mat)
				return mat;
			mat = SCNMaterial.Create ();
			mat.WritesToDepthBuffer = false;
			mat.ReadsFromDepthBuffer = false;
			mat.Diffuse.ContentColor = GetNativeColor (c);
			c.Tag = mat;
			return mat;
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
			currentNode.Arc (cx, cy, radius, startAngle, endAngle, ref style);
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.Arc (cx, cy, radius, startAngle, endAngle, ref style);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var style = new Style {
				W = w,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.Oval (x, y, width, height, ref style);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.Oval (x, y, width, height, ref style);
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			var style = new Style {
				W = w,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.Polygon (poly, ref style);
		}

		public void FillPolygon (Polygon poly)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.Polygon (poly, ref style);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			var style = new Style {
				W = w,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.Rect (x, y, width, height, ref style);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.Rect (x, y, width, height, ref style);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			var style = new Style {
				W = w,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.RoundedRect (x, y, width, height, radius, ref style);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			var style = new Style {
				Fill = true,
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.RoundedRect (x, y, width, height, radius, ref style);
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
				currentNode.Line (sx, sy, ex, ey, ref style);
			}
		}

		public void EndLines ()
		{
			if (inLines) {
				inLines = false;
				var style = new Style {
					W = lineW,
					Color = currentColor,
					Transform = states[^1].Transform,
				};
				currentNode.Lines (linePoints, ref style);
			}
		}

		public void DrawString (string s, float x, float y)
		{
			var style = new Style {
				Color = currentColor,
				Font = currentFont,
				Transform = states[^1].Transform,
			};
			currentNode.String (s, x, y, ref style);
		}

		public void DrawString (string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			DrawString (s, x, y);
		}

		public IImage? ImageFromFile (string path)
		{
			return null;
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			var style = new Style {
				Transform = states[^1].Transform,
			};
			currentNode.Image (img, x, y, width, height, ref style);
		}

		public class State
		{
			public SCNMatrix4 Transform;
		}

		public struct Style
		{
			public SCNMatrix4 Transform;
			public Color Color;
			public Font Font;
			public float W;
			public bool Fill;
		}

		public class EntityNode : SCNNode
		{
			readonly List<PrimitiveNode> primitiveNodes;
			int primitiveIndex = 0;

			public EntityNode ()
			{
				primitiveNodes = new List<PrimitiveNode> ();
			}
			public EntityNode (IntPtr handle)
				: base (handle)
			{
				primitiveNodes = base.ChildNodes.OfType<PrimitiveNode> ().ToList ();
			}

			public void BeginFrame ()
			{
				primitiveIndex = 0;
			}

			T GetNodeType<T> () where T : PrimitiveNode, new()
			{
				if (primitiveIndex < primitiveNodes.Count) {
					if (primitiveNodes[primitiveIndex] is T t) {
						primitiveIndex++;
						return t;
					}
					else {
						for (var i = primitiveIndex; i < primitiveNodes.Count; i++) {
							primitiveNodes[i].RemoveFromParentNode ();
						}
						primitiveNodes.RemoveRange (primitiveIndex, primitiveNodes.Count - primitiveIndex);
					}
				}
				var node = new T ();
				primitiveNodes.Add (node);
				primitiveIndex++;
				AddChildNode (node);
				return node;
			}

			public void Arc (float cx, float cy, float radius, float startAngle, float endAngle, ref Style style)
			{
			}

			public void Image (IImage image, float x, float y, float width, float height, ref Style style)
			{
			}

			public void Line (float sx, float sy, float ex, float ey, ref Style style)
			{
				var n = GetNodeType<LineNode> ();
				n.Set (sx, sy, ex, ey, ref style);
			}

			public void Lines (List<SCNVector3> points, ref Style style)
			{
			}

			public void Oval (float x, float y, float width, float height, ref Style style)
			{
				if (style.Fill) {
					var n = GetNodeType<FilledOvalNode> ();
					n.Set (x, y, width, height, ref style);
				}
				else {
				}					
			}

			public void Polygon (Polygon poly, ref Style style)
			{
			}

			public void Rect (float x, float y, float width, float height, ref Style style)
			{
				if (style.Fill) {
					var n = GetNodeType<FilledRectNode> ();
					n.Set (x, y, width, height, ref style);
				}
				else {
				}
			}

			public void RoundedRect (float x, float y, float width, float height, float radius, ref Style style)
			{
			}

			public void String (string str, float x, float y, ref Style style)
			{
			}
		}
		public abstract class PrimitiveNode : SCNNode
		{
			protected Style style;

			public PrimitiveNode ()
			{
				style.Color = Colors.Black;
			}

			protected bool StyleChanged (ref Style other)
			{
				return style.Color.Red != other.Color.Red ||
					style.Color.Green != other.Color.Green ||
					style.Color.Blue != other.Color.Blue ||
					style.Color.Alpha != other.Color.Alpha ||
					style.Fill != other.Fill;
			}
		}
		public class FilledOvalNode : PrimitiveNode
		{
			float x, y, width, height;

			static SCNCylinder template = SCNCylinder.Create (10f, 10f);

			public FilledOvalNode ()
			{
				//Console.WriteLine ("NEW FILLED OVAL");
				Geometry = (SCNGeometry)template.Copy (NSZone.Default);
			}

			public void Set (float sx, float sy, float ex, float ey, ref Style style)
			{
				if (this.x == sx && this.y == sy && this.width == ex && this.height == ey)
					return;
				//Console.WriteLine ($"({this.sx} == {sx} && {this.sy} == {sy} && {this.ex} == {ex} && {this.ey == ey})");
				this.x = sx;
				this.y = sy;
				this.width = ex;
				this.height = ey;
				this.style = style;

				//Console.WriteLine (style.Transform);

				Geometry.FirstMaterial = GetNativeMaterial (style.Color);

				Transform =
					SCNMatrix4.Scale (width/20, 1, height/20)
					* SCNMatrix4.CreateRotationX ((float)(Math.PI / 2))
					* SCNMatrix4.CreateTranslation (x, y, 0)
					* style.Transform
					;

				//Transform =
				//	SCNMatrix4.Scale (style.W, length, style.W)
				//	//* SCNMatrix4.CreateTranslation (sx, sy, 0)
				//	* style.Transform;

			}
		}
		public class FilledRectNode : PrimitiveNode
		{
			float x, y, width, height;

			static SCNBox template = SCNBox.Create (10f, 10f, 1, 0);

			public FilledRectNode ()
			{
				//Console.WriteLine ("NEW FILLED OVAL");
				Geometry = (SCNGeometry)template.Copy (NSZone.Default);
			}

			public void Set (float sx, float sy, float ex, float ey, ref Style style)
			{
				if (this.x == sx && this.y == sy && this.width == ex && this.height == ey)
					return;
				//Console.WriteLine ($"({this.sx} == {sx} && {this.sy} == {sy} && {this.ex} == {ex} && {this.ey == ey})");
				this.x = sx;
				this.y = sy;
				this.width = ex;
				this.height = ey;
				this.style = style;

				//Console.WriteLine (style.Transform);

				Geometry.FirstMaterial = GetNativeMaterial (style.Color);

				Transform =
					SCNMatrix4.Scale (width / 10, height / 10, 1)
					* SCNMatrix4.CreateTranslation (x + width / 2, y + height / 2, 0)
					* style.Transform
					;

				//Transform =
				//	SCNMatrix4.Scale (style.W, length, style.W)
				//	//* SCNMatrix4.CreateTranslation (sx, sy, 0)
				//	* style.Transform;

			}
		}
		public class LineNode : PrimitiveNode
		{
			float sx, sy, ex, ey;

			static SCNBox template = SCNBox.Create (10f, 10f, 1f, 0);

			public LineNode ()
			{
				//Console.WriteLine ("NEW LINE");
				Geometry = (SCNGeometry)template.Copy (NSZone.Default);
				//Geometry.FirstMaterial.Diffuse.ContentColor = NSColor.Red;
			}

			public void Set (float sx, float sy, float ex, float ey, ref Style style)
			{
				if (this.sx == sx && this.sy == sy && this.ex == ex && this.ey == ey)
					return;
				//Console.WriteLine ($"({this.sx} == {sx} && {this.sy} == {sy} && {this.ex} == {ex} && {this.ey == ey})");
				this.sx = sx;
				this.sy = sy;
				this.ex = ex;
				this.ey = ey;
				this.style = style;

				var dx = ex - sx;
				var dy = ey - sy;
				var length = (float)Math.Sqrt (dx * dx + dy * dy);
				var angle = (float)(Math.Atan2 (dy, dx) + Math.PI / 2);

				//Console.WriteLine ($"Line: w={style.W}, dx={dx}, dy={dy}");

				var cx = sx + 0.5f * dx;
				var cy = sy + 0.5f * dy;

				//Console.WriteLine ((cx, cy));
				//Console.WriteLine (style.Transform);

				Geometry.FirstMaterial = GetNativeMaterial (style.Color);

				//Scale = new SCNVector3 (1, 100, 1);
				Transform =
					SCNMatrix4.Scale (style.W / 10, length / 10, 1)
					* SCNMatrix4.CreateRotationZ (angle)
					* SCNMatrix4.CreateTranslation (cx, cy, 0)
					* style.Transform
					;

				//Transform =
				//	SCNMatrix4.Scale (style.W, length, style.W)
				//	//* SCNMatrix4.CreateTranslation (sx, sy, 0)
				//	* style.Transform;

			}
		}
	}
}
