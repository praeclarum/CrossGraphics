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
using System.Runtime.InteropServices;
using Metal;
using ObjCRuntime;

#if __MACOS__
using NativeColor = AppKit.NSColor;
#endif

namespace CrossGraphics.SceneKit
{
	public class SceneKitGraphics : IGraphics
	{
		public readonly List<State> states = new List<State> { new State { Transform = SCNMatrix4.Identity } };

		readonly SCNScene scene;
		readonly SCNNode graphicsNode = new SCNNode { Name = "Graphics" };

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
			scene.RootNode.AddChildNode (graphicsNode);
			graphicsNode.AddChildNode (entityNodes[0]);
			graphicsNode.Scale = new SCNVector3 (1, -1, 1);
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
			entityNodes[currentNodeIndex].EndFrame ();
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
			var renderingOrder = 1;
			if (currentNodeIndex > 0) {
				entityNodes[currentNodeIndex].EndFrame ();
				renderingOrder = entityNodes[currentNodeIndex].NextRenderingOrder;
				renderingOrder = 4 * ((renderingOrder + 3) / 4);
			}
			//Console.WriteLine ($"ENTITY {entity}");
			currentNodeIndex++;
			if (currentNodeIndex >= entityNodes.Count) {
				var entityNode = new EntityNode ();
				entityNodes.Add (entityNode);
				graphicsNode.Add (entityNode);
			}
			currentNode.InitialRenderingOrder = renderingOrder;
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
				if (linePoints.Count == 2)
					currentNode.Line ((float)linePoints[0].X, (float)linePoints[0].Y, (float)linePoints[1].X, (float)linePoints[1].Y, ref style);
				else
					currentNode.Lines (linePoints, ref style);
			}
		}

		public void DrawString (string s, float x, float y)
		{
			var style = new Style {
				Color = currentColor,
				Transform = states[^1].Transform,
			};
			currentNode.String (s, x, y, currentFont, ref style);
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
			public float W;
			public bool Fill;
		}

		public class EntityNode : SCNNode
		{
			readonly List<PrimitiveNode> primitiveNodes;
			int primitiveIndex = 0;

			public int InitialRenderingOrder;
			public int NextRenderingOrder => InitialRenderingOrder + primitiveIndex;

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

			public void EndFrame ()
			{
				if (primitiveIndex < primitiveNodes.Count) {
					for (var i = primitiveIndex; i < primitiveNodes.Count; i++) {
						primitiveNodes[i].RemoveFromParentNode ();
					}
					primitiveNodes.RemoveRange (primitiveIndex, primitiveNodes.Count - primitiveIndex);
				}
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
				node.RenderingOrder = InitialRenderingOrder + primitiveIndex;
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
				var n = GetNodeType<LinesNode> ();
				n.Set (points, ref style);
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
				if (style.Fill) {
					var n = GetNodeType<FilledPolygonNode> ();
					n.Set (poly, ref style);
				}
				else {
				}
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
				if (style.Fill) {
					var n = GetNodeType<FilledRoundedRectNode> ();
					n.Set (x, y, width, height, radius, ref style);
				}
				else {
				}
			}

			public void String (string str, float x, float y, Font font, ref Style style)
			{
				var n = GetNodeType<StringNode> ();
				n.Set (str, x, y, font, ref style);
			}
		}

		public abstract class PrimitiveNode : SCNNode
		{
			protected Style style;

			public PrimitiveNode ()
			{
				style.Color = Colors.Black;
			}

			protected bool ColorChanged (ref Style other)
			{
				return style.Color.Red != other.Color.Red ||
					style.Color.Green != other.Color.Green ||
					style.Color.Blue != other.Color.Blue ||
					style.Color.Alpha != other.Color.Alpha;
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
				if (this.x != sx || this.y != sy || this.width != ex || this.height != ey) {
					//Console.WriteLine ($"({this.sx} == {sx} && {this.sy} == {sy} && {this.ex} == {ex} && {this.ey == ey})");
					this.x = sx;
					this.y = sy;
					this.width = ex;
					this.height = ey;
					Transform =
						SCNMatrix4.Scale (width / 20, 1, height / 20)
						* SCNMatrix4.CreateRotationX ((float)(Math.PI / 2))
						* SCNMatrix4.CreateTranslation (x + width / 2, y + height / 2, 0)
						* style.Transform
						;
				}
				if (ColorChanged (ref style)) {
					this.style.Color = style.Color;
					Geometry.FirstMaterial = GetNativeMaterial (style.Color);
				}
			}
		}
		public class FilledPolygonNode : PrimitiveNode
		{
			Polygon? poly;

			public FilledPolygonNode ()
			{
			}

			public void Set (Polygon poly, ref Style style)
			{
				if (this.poly != poly) {
					//Console.WriteLine ($"({this.sx} == {sx} && {this.sy} == {sy} && {this.ex} == {ex} && {this.ey == ey})");
					this.poly = poly;

					//Console.WriteLine (style.Transform);
					var ps = poly.Points;
					var n = ps.Count;
					if (n > 1) {
						var path = new NSBezierPath ();
						path.MoveTo (new CGPoint (ps[0].X, ps[0].Y));
						for (var i = 1; i < n; i++) {
							path.LineTo (new CGPoint (ps[i].X, ps[i].Y));
						}
						path.ClosePath ();
						var g = SCNShape.Create (path, 1);
						g.FirstMaterial = GetNativeMaterial (style.Color);
						Geometry = g;
					}
					else {
						Geometry = null;
					}

					Transform =
						style.Transform
						;
				}
				if (ColorChanged (ref style)) {
					this.style.Color = style.Color;
					var g = Geometry;
					if (g is object) {
						g.FirstMaterial = GetNativeMaterial (style.Color);
					}
				}
			}
		}
		public class FilledRectNode : PrimitiveNode
		{
			float x, y, width, height;

			static SCNBox template = SCNBox.Create (10f, 10f, 1, 0);

			static FilledRectNode ()
			{
				template.FirstMaterial = GetNativeMaterial (Colors.Black);
			}

			public FilledRectNode ()
			{
				//Console.WriteLine ("NEW FILLED OVAL");
				Geometry = (SCNGeometry)template.Copy (NSZone.Default);
				Geometry.FirstMaterial = GetNativeMaterial (style.Color);
			}

			public void Set (float sx, float sy, float ex, float ey, ref Style style)
			{
				if (this.x != sx || this.y != sy || this.width != ex || this.height != ey) {
					//Console.WriteLine ($"({this.sx} == {sx} && {this.sy} == {sy} && {this.ex} == {ex} && {this.ey == ey})");
					this.x = sx;
					this.y = sy;
					this.width = ex;
					this.height = ey;
					Transform =
						SCNMatrix4.Scale (width / 10, height / 10, 1)
						* SCNMatrix4.CreateTranslation (x + width / 2, y + height / 2, 0)
						* style.Transform
						;
				}
				if (ColorChanged (ref style)) {
					this.style.Color = style.Color;
					Geometry.FirstMaterial = GetNativeMaterial (style.Color);
				}
			}
		}
		public class FilledRoundedRectNode : PrimitiveNode
		{
			float x, y, width, height, radius;

			public FilledRoundedRectNode ()
			{
			}

			public void Set (float x, float y, float width, float height, float radius, ref Style style)
			{
				if (this.x != x || this.y != y || this.width != width || this.height != height || this.radius != radius) {
					//Console.WriteLine ($"({this.sx} == {sx} && {this.sy} == {sy} && {this.ex} == {ex} && {this.ey == ey})");
					this.x = x;
					this.y = y;
					this.width = width;
					this.height = height;
					this.radius = radius;

					//Console.WriteLine (style.Transform);

					var g = SCNBox.Create (this.width, this.height, radius * 2, radius);
					g.FirstMaterial = GetNativeMaterial (style.Color);
					Geometry = g;

					base.Transform =
						SCNMatrix4.CreateTranslation (this.x + this.width / 2, this.y + this.height / 2, 0)
						* style.Transform
						;
				}
				if (ColorChanged (ref style)) {
					this.style.Color = style.Color;
					Geometry.FirstMaterial = GetNativeMaterial (style.Color);
				}
			}
		}
		public class LineNode : PrimitiveNode
		{
			float sx, sy, ex, ey;

			static SCNBox template = SCNBox.Create (10f, 10f, 1f, 0);

			public LineNode ()
			{
				Geometry = (SCNGeometry)template.Copy (NSZone.Default);
				Geometry.FirstMaterial = GetNativeMaterial (style.Color);
			}

			public void Set (float sx, float sy, float ex, float ey, ref Style style)
			{
				if (this.sx != sx || this.sy != sy || this.ex != ex || this.ey != ey) {
					//Console.WriteLine ($"({this.sx} == {sx} && {this.sy} == {sy} && {this.ex} == {ex} && {this.ey == ey})");
					this.sx = sx;
					this.sy = sy;
					this.ex = ex;
					this.ey = ey;

					var dx = ex - sx;
					var dy = ey - sy;
					var length = (float)Math.Sqrt (dx * dx + dy * dy);
					var angle = (float)(Math.Atan2 (dy, dx) - Math.PI / 2);

					//Console.WriteLine ($"Line: w={style.W}, dx={dx}, dy={dy}");

					var cx = sx + 0.5f * dx;
					var cy = sy + 0.5f * dy;

					//Console.WriteLine ((cx, cy));
					//Console.WriteLine (style.Transform);

					Transform =
						SCNMatrix4.Scale (style.W / 10, length / 10, 1)
						* SCNMatrix4.CreateRotationZ (angle)
						* SCNMatrix4.CreateTranslation (cx, cy, 0)
						* style.Transform
						;
				}
				if (ColorChanged (ref style)) {
					this.style.Color = style.Color;
					Geometry.FirstMaterial = GetNativeMaterial (style.Color);
				}
			}
		}

		public class LinesNode : PrimitiveNode
		{
			SCNVector3[] points = Array.Empty<SCNVector3> ();
			readonly List<Segment> segments = new List<Segment> ();

			class Segment
			{
				public readonly SCNNode Node = SCNNode.Create ();
				public SCNVector3 Point;
				public double Length = 1;
				public SCNNode DotNode;
				public SCNNode LineNode;
				static readonly SCNGeometry Dot = SCNCylinder.Create (0.5f, 1);
				static readonly SCNGeometry Line = SCNBox.Create (1, 1, 1, 0);
				public Segment ()
				{
					DotNode = SCNNode.FromGeometry ((SCNGeometry)Dot.Copy (NSZone.Default));
					DotNode.LocalRotate (SCNQuaternion.FromAxisAngle (SCNVector3.UnitX, (float)(Math.PI / 2)));
					LineNode = SCNNode.FromGeometry ((SCNGeometry)Line.Copy (NSZone.Default));
					Node.AddChildNode (DotNode);
					Node.AddChildNode (LineNode);
				}
				public void SetStyle (ref Style style)
				{
					var mat = GetNativeMaterial (style.Color);
					DotNode.Geometry.FirstMaterial = mat;
					LineNode.Geometry.FirstMaterial = mat;
					var scale = (float)style.W;
					DotNode.Scale = new SCNVector3 (scale, scale, scale);
					LineNode.Scale = new SCNVector3 (scale, (float)Length, scale);
				}
			}

			public LinesNode ()
			{
			}

			public override nint RenderingOrder {
				get => base.RenderingOrder; set {
					base.RenderingOrder = value;

					foreach (var s in segments) {
						s.DotNode.RenderingOrder = value;
						s.LineNode.RenderingOrder = value;
					}
				}
			}

			public void Set (List<SCNVector3> points, ref Style style)
			{
				var n = points.Count;
				var nodesChanged = false;

				//
				// Check for points added and removed
				//
				while (n > segments.Count) {
					var s = new Segment ();
					var ro = RenderingOrder;
					//s.Node.RenderingOrder = ro;
					s.DotNode.RenderingOrder = ro;
					s.LineNode.RenderingOrder = ro;
					s.SetStyle (ref style);
					segments.Add (s);
					AddChildNode (s.Node);
					nodesChanged = true;
				}
				while (segments.Count > n) {
					var i = segments.Count - 1;
					var s = segments[i];
					s.Node.RemoveFromParentNode ();
					segments.RemoveAt (i);
					nodesChanged = true;
				}

				//
				// Check for position changes
				//
				if (!nodesChanged) {
					for (var i = 0; i < n && !nodesChanged; i++) {
						if (segments[i].Point != points[i])
							nodesChanged = true;
					}
				}

				//
				// If anything changed, reposition
				//
				if (nodesChanged) {
					for (var i = 0; i < n; i++) {

						var spt = points[i];
						var ept = i + 1 < n ? points[i + 1] : points[i];
						var d = ept - spt;
						var len = d.Length;
						var s = segments[i];
						s.Point = spt;
						s.Length = len;
						s.Node.Position = spt;
						if (len > 0) {
							var angle = Math.Atan2 (d.Y, d.X) + Math.PI / 2;
							s.LineNode.Transform = SCNMatrix4.CreateTranslation (0, -len / 2, 0) * SCNMatrix4.CreateRotationZ ((float)angle);
						}
						else {
							s.LineNode.Transform = SCNMatrix4.Scale (1e-3f, 1e-3f, 1e-3f);
						}
					}
				}

				if (ColorChanged (ref style)) {
					foreach (var s in segments) {
						s.SetStyle (ref style);
					}
					this.style.Color = style.Color;
				}
			}
		}

		public class StringNode : PrimitiveNode
		{
			string str = "";
			float x, y;

			public StringNode ()
			{
				//Console.WriteLine ("NEW STRING");
			}

			public void Set (string str, float x, float y, Font font, ref Style style)
			{
				if (this.str != str || this.x != x || this.y != y) {
					//Console.WriteLine ($"({this.sx} == {sx} && {this.sy} == {sy} && {this.ex} == {ex} && {this.ey == ey})");
					this.str = str;
					this.x = x;
					this.y = y;

					var g = SCNText.Create (str, 1);
					g.Font = font.FontFamily == "SystemFont" ? NSFont.SystemFontOfSize (font.Size) :
						NSFont.FromFontName (font.FontFamily, font.Size);
					g.FirstMaterial = GetNativeMaterial (style.Color);

					Geometry = g;

					//Scale = new SCNVector3 (1, 100, 1);
					Transform =
						SCNMatrix4.Scale (1, -1, 1)
						* SCNMatrix4.CreateTranslation (x, y + font.Size, 0)
						* style.Transform
						;
				}
				else if (ColorChanged (ref style)) {
					this.style.Color = style.Color;
					Geometry.FirstMaterial = GetNativeMaterial (style.Color);
				}
			}
		}
	}
}
