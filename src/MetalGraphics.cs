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
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using CoreGraphics;

using CoreText;

using Foundation;

using Metal;

namespace CrossGraphics.Metal
{
	public class MetalGraphics : IGraphics
	{
		public const MTLPixelFormat DefaultPixelFormat = MTLPixelFormat.BGRA8Unorm;

		Font _currentFont = CrossGraphics.Font.SystemFontOfSize (16);
		ValueColor _currentColor = new ValueColor (0, 0, 0, 255);

		ValueColor _clearColor = new ValueColor (0, 0, 0, 0);
		public ValueColor ClearColor => _clearColor;

		//struct State
		//{
		//	public Matrix4x4 ModelToView;
		//}
		//readonly List<State> _states = new List<State> () { new State () { ModelToView = Matrix4x4.Identity } };

		readonly MetalGraphicsBuffers _buffers;
		readonly IMTLRenderCommandEncoder _renderEncoder;
		static readonly Lazy<IMTLRenderPipelineState?> _pipeline;

		Matrix4x4 _modelToView = Matrix4x4.Identity;
		readonly Matrix4x4 _projection;

		static MetalGraphics ()
		{
			_pipeline = new Lazy<IMTLRenderPipelineState?> (() => {
				var device = MTLDevice.SystemDefault;
				return device != null ? CreatePipeline (device) : null;
			});
		}

		public MetalGraphics (IMTLRenderCommandEncoder renderEncoder, float viewWidth, float viewHeight, MetalGraphicsBuffers buffers)
		{
			_renderEncoder = renderEncoder ?? throw new ArgumentNullException (nameof(renderEncoder));
			_buffers = buffers ?? throw new ArgumentNullException (nameof(buffers));
			// View is (0,0) to (viewWidth, viewHeight)
			// Viewport is (-1,-1) to (1,1)
			_projection = Matrix4x4.CreateScale (2 / viewWidth, -2 / viewHeight, 1) * Matrix4x4.CreateTranslation (-1, 1, 0);
		}

		public void BeginEntity (object entity)
		{
		}

		public IFontMetrics GetFontMetrics ()
		{
			return new NullGraphicsFontMetrics (_currentFont.Size, isBold: _currentFont.IsBold, isMonospace: _currentFont.IsMonospace);
		}

		public void SetFont (Font? f)
		{
			if (f is not null) {
				_currentFont = f;
			}
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
			//_states.Add(_states[^1]);
		}

		public void SetClippingRect (float x, float y, float width, float height)
		{
		}

		public void Translate (float dx, float dy)
		{
			_modelToView *= Matrix4x4.CreateTranslation(dx, dy, 0);
		}

		public void Scale (float sx, float sy)
		{
			_modelToView *= Matrix4x4.CreateScale(sx, sy, 1);
		}

		public void RestoreState ()
		{
			//if (_states.Count > 1)
			//{
			//	_states.RemoveAt(_states.Count - 1);
			//}
		}

		public IImage? ImageFromFile (string path)
		{
			return null;
		}

		struct BoundingBox {
			public float MinX, MinY, MaxX, MaxY;
			public static BoundingBox FromRect (float x, float y, float width, float height, float w)
			{
				var left = x;
				var right = x + width;
				var top = y;
				var bottom = y + height;
				var r = w / 2;
				var minX = Math.Min (left, right) - r;
				var minY = Math.Min (top, bottom) - r;
				var maxX = Math.Max (left, right) + r;
				var maxY = Math.Max (top, bottom) + r;
				return new BoundingBox { MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY };
			}

			public static BoundingBox FromSafeRect (float x, float y, float width, float height)
			{
				var minX = x;
				var minY = y;
				var maxX = x + width;
				var maxY = y + height;
				return new BoundingBox { MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY };
			}
		}

		void DoRect (float x, float y, float width, float height, float w, DrawOp op, float argy = 0, float argz = 0)
		{
			var buffer = _buffers.GetPrimitivesBuffer(numVertices: 4, numIndices: 6);
			var bb = BoundingBox.FromRect (x, y, width, height, w);
			var bbv = new Vector4 (bb.MinX, bb.MinY, bb.MaxX, bb.MaxY);
			var args = new Vector4 (w, argy, argz, 0);
			var v0 = buffer.AddVertex(bb.MinX, bb.MinY, 0, 0, _currentColor, bb: bbv, args: args, op: op);
			var v1 = buffer.AddVertex(bb.MaxX, bb.MinY, 1, 0, _currentColor, bb: bbv, args: args, op: op);
			var v2 = buffer.AddVertex(bb.MaxX, bb.MaxY, 1, 1, _currentColor, bb: bbv, args: args, op: op);
			var v3 = buffer.AddVertex(bb.MinX, bb.MaxY, 0, 1, _currentColor, bb: bbv, args: args, op: op);
			buffer.AddTriangle(v0, v1, v2);
			buffer.AddTriangle(v2, v3, v0);
		}

		public void FillPolygon (Polygon poly)
		{
			if (poly.Points.Count < 3)
				return;
			var buffer = _buffers.GetPrimitivesBuffer(numVertices: poly.Points.Count, numIndices: (poly.Points.Count - 2) * 3);
			var bbv = Vector4.Zero;
			var args = Vector4.Zero;
			var op = DrawOp.FillPolygon;
			var v0 = buffer.AddVertex (poly.Points[0].X, poly.Points[0].Y, 0, 0, _currentColor, bb: bbv, args: args, op: op);
			for (var i = 1; i < poly.Points.Count - 1; i++) {
				var v1 = buffer.AddVertex (poly.Points[i].X, poly.Points[i].Y, 0, 0, _currentColor, bb: bbv, args: args, op: op);
				var v2 = buffer.AddVertex (poly.Points[i + 1].X, poly.Points[i + 1].Y, 0, 0, _currentColor, bb: bbv, args: args, op: op);
				buffer.AddTriangle (v0, v1, v2);
			}
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			BeginLines (true);
			for (var i = 0; i < poly.Points.Count; i++) {
				var j = (i + 1) % poly.Points.Count;
				DrawLine (poly.Points[i].X, poly.Points[i].Y, poly.Points[j].X, poly.Points[j].Y, w);
			}
			EndLines ();
		}

		public void FillRect (float x, float y, float width, float height)
		{
			DoRect (x, y, width, height, 0, DrawOp.FillRect);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			DoRect (x, y, width, height, w, DrawOp.StrokeRect);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			DoRect (x, y, width, height, 0, DrawOp.FillRoundedRect, argy: radius);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			DoRect (x, y, width, height, w, DrawOp.StrokeRoundedRect, argy: radius);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			DoRect (x, y, width, height, 0, DrawOp.FillOval);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			DoRect (x, y, width, height, w, DrawOp.StrokeOval);
		}

		/// <summary>
		/// Yes, this function looks buggy. Yes, it probably is. But it works for determining circles.
		/// </summary>
		static float PositiveAngle (float a)
		{
			var twoPi = MathF.PI * 2.0f;
			var na = a % twoPi;
			if (na < 0) {
				a += twoPi;
			}
			return a;
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			var isCircle = Math.Abs(PositiveAngle (endAngle - startAngle)) >= MathF.PI * 2.0f - 1.0e-6f;
			if (isCircle) {
				FillOval (cx - radius, cy - radius, radius * 2, radius * 2);
				return;
			}
			DoRect (cx - radius, cy - radius, radius * 2, radius * 2, 0, DrawOp.FillArc, argy: startAngle, argz: endAngle);
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			var isCircle = Math.Abs(PositiveAngle (endAngle - startAngle)) >= MathF.PI * 2.0f - 1.0e-6f;
			if (isCircle) {
				DrawOval (cx - radius, cy - radius, radius * 2, radius * 2, w);
				return;
			}
			DoRect (cx - radius, cy - radius, radius * 2, radius * 2, w, DrawOp.StrokeArc, argy: startAngle, argz: endAngle);
		}

		public void BeginLines (bool rounded)
		{
			// TODO: Implement
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			var buffer = _buffers.GetPrimitivesBuffer(numVertices: 4, numIndices: 6);
			var dx = ex - sx;
			var dy = ey - sy;
			var len = MathF.Sqrt (dx * dx + dy * dy);
			if (len <= 1e-6f) {
				return;
			}
			var w2 = w / 2;
			var bbv = new Vector4 (sx, sy, ex, ey);
			var args = new Vector4 (w, 0, 0, 0);
			var ox = dx / len * w2;
			var oy = dy / len * w2;
			var nx = oy;
			var ny = -ox;
			var v0 = buffer.AddVertex (sx - ox - nx, sy - oy - ny, 0, 0, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			var v1 = buffer.AddVertex (sx - ox  + nx, sy - oy + ny, 1, 0, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			var v2 = buffer.AddVertex (ex + ox + nx, ey + oy + ny, 1, 1, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			var v3 = buffer.AddVertex (ex + ox - nx, ey + oy - ny, 0, 1, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			buffer.AddTriangle (v0, v1, v2);
			buffer.AddTriangle (v2, v3, v0);
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
			if (_currentFont == null)
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

		readonly CGColor _whiteCGColor = new CGColor (1, 1, 1, 1);

		public void DrawString (string s, float x, float y)
		{
			var font = CrossGraphics.CoreGraphics.CoreGraphicsGraphics.GetFontName (_currentFont);
			var regionO = _buffers.FindExistingSdfTextureRegion (s, font);

			var renderFontSize = (nfloat)32.0;

			var hpadding = renderFontSize * 0.1;
			var vpadding = renderFontSize * 0.2;

			if (regionO is null) {
				using var atext = new NSMutableAttributedString (s, _buffers.GetCTStringAttributes(font, renderFontSize));
				using var drawLine = new CTLine (atext);
				var len = drawLine.GetTypographicBounds (out var ascent, out var descent, out var leading);

				var drawWidth = (float)(len + 2.0 * hpadding);
				var drawHeight = (float)(renderFontSize + 2.0 * vpadding);
				regionO = _buffers.DrawSdfTextureRegion (s, font, drawWidth, drawHeight, (cgContext) => {
					// cgContext.SetFillColor ((nfloat)0.5, (nfloat)0.5, (nfloat)0.5, 1);
					// if (s.Length > 5) {
					// 	cgContext.FillEllipseInRect (new CGRect (0, 0, drawWidth, drawHeight));
					// }
					// else {
					// 	cgContext.FillRect (new CGRect (0, 0, drawWidth, drawHeight));
					// }
					cgContext.SetFillColor (1, 1, 1, 1);
					cgContext.TextMatrix = CGAffineTransform.MakeScale (1, 1);
					cgContext.TranslateCTM ((nfloat)hpadding, (nfloat)(renderFontSize * 0.15 + vpadding));
					drawLine.Draw (cgContext);
				});
			}

			if (regionO is SdfTextureRegion region) {
				var fontScale = (float)(_currentFont.Size / renderFontSize);
				var width = region.DrawSize.X * fontScale;
				var height = region.DrawSize.Y * fontScale;
				var buffer = _buffers.GetPrimitivesBuffer(numVertices: 4, numIndices: 6);
				var bb = BoundingBox.FromSafeRect (x - (float)hpadding * fontScale, y - (float)vpadding * fontScale, width, height);
				var bbv = new Vector4 (bb.MinX, bb.MinY, bb.MaxX, bb.MaxY);
				var args = new Vector4 (0, 0, 0, 0);
				var uMin = region.UVBoundingBox.X;
				var vMin = region.UVBoundingBox.Y;
				var uMax = region.UVBoundingBox.Z;
				var vMax = region.UVBoundingBox.W;
				var op = DrawOp.DrawString;
				var v0 = buffer.AddVertex(bb.MinX, bb.MinY, uMin, vMin, _currentColor, bb: bbv, args: args, op: op);
				var v1 = buffer.AddVertex(bb.MaxX, bb.MinY, uMax, vMin, _currentColor, bb: bbv, args: args, op: op);
				var v2 = buffer.AddVertex(bb.MaxX, bb.MaxY, uMax, vMax, _currentColor, bb: bbv, args: args, op: op);
				var v3 = buffer.AddVertex(bb.MinX, bb.MaxY, uMin, vMax, _currentColor, bb: bbv, args: args, op: op);
				buffer.AddTriangle(v0, v1, v2);
				buffer.AddTriangle(v2, v3, v0);
			}
		}

		const int MaxSdfTextures = 1;

		public void EndDrawing ()
		{
			var modelToViewport = _modelToView * _projection;
			if (_pipeline.Value is {} pipeline) {
				_renderEncoder.SetRenderPipelineState (pipeline);

				_buffers.SetUniforms (modelToViewport);
				if (_buffers.Uniforms is {} u) {
					_renderEncoder.SetVertexBuffer (buffer: u, offset: 0, index: 1);
				}

				for (var textureIndex = 0; textureIndex < Math.Min (MaxSdfTextures, _buffers.SdfTextures.Count); textureIndex++) {
					var texture = _buffers.SdfTextures[textureIndex].Texture;
					if (texture is null) {
						continue;
					}
					_renderEncoder.SetFragmentTexture (texture, index: (nuint)textureIndex);
				}

				foreach (var buffer in _buffers.Primitives) {
					if (buffer.NumIndices <= 0) {
						break;
					}
					if (buffer.VertexBuffer is null || buffer.IndexBuffer is null) {
						continue;
					}
					_renderEncoder.SetVertexBuffer (buffer: buffer.VertexBuffer, offset: 0, index: 0);
					_renderEncoder.DrawIndexedPrimitives (MTLPrimitiveType.Triangle, (nuint)buffer.NumIndices, MTLIndexType.UInt16, buffer.IndexBuffer, 0);
				}
			}
			_buffers.Reset ();
		}

		const int VertexPositionByteSize = 2 * sizeof (float);
		const int VertexUvByteSize = 2 * sizeof (float);
		const int VertexColorByteSize = 4 * sizeof (float);
		const int VertexBBByteSize = 4 * sizeof (float);
		const int VertexArgsByteSize = 4 * sizeof (float);
		const int VertexOpByteSize = 1 * sizeof (uint);
		public const int VertexByteSize = VertexPositionByteSize + VertexUvByteSize + VertexColorByteSize + VertexBBByteSize + VertexArgsByteSize + VertexOpByteSize;

		const int UniformModelToViewByteSize = 16 * sizeof (float);
		public const int UniformsByteSize = UniformModelToViewByteSize;

		static IMTLRenderPipelineState CreatePipeline (IMTLDevice device)
		{
			IMTLLibrary? library = device.CreateLibrary (source: MetalCode, options: new MTLCompileOptions(), error: out Foundation.NSError? error);
			if (library is null) {
				if (error is not null) {
					throw new NSErrorException (error);
				}
				else {
					throw new Exception ("Could not create library");
				}
			}
			var vertexFunction = library?.CreateFunction ("vertexShader");
			var fragmentFunction = library?.CreateFunction ("fragmentShader");
			if (vertexFunction is null || fragmentFunction is null) {
				throw new Exception ("Could not create vertex or fragment function");
			}
			var pipelineDescriptor = new MTLRenderPipelineDescriptor {
				VertexFunction = vertexFunction,
				FragmentFunction = fragmentFunction,
				// RasterSampleCount = 4,
			};
			pipelineDescriptor.ColorAttachments[0] = new MTLRenderPipelineColorAttachmentDescriptor {
				PixelFormat = DefaultPixelFormat,
				BlendingEnabled = true,
				SourceRgbBlendFactor = MTLBlendFactor.SourceAlpha,
				DestinationRgbBlendFactor = MTLBlendFactor.OneMinusSourceAlpha,
				RgbBlendOperation = MTLBlendOperation.Add,
				SourceAlphaBlendFactor = MTLBlendFactor.SourceAlpha,
				DestinationAlphaBlendFactor = MTLBlendFactor.OneMinusSourceAlpha,
				AlphaBlendOperation = MTLBlendOperation.Add,
			};
			pipelineDescriptor.VertexBuffers[0] = new MTLPipelineBufferDescriptor() {
				Mutability = MTLMutability.Immutable,
			};
			var vdesc = new MTLVertexDescriptor ();
			vdesc.Layouts[0] = new MTLVertexBufferLayoutDescriptor {
				Stride = VertexByteSize,
			};
			vdesc.Attributes[0] = new MTLVertexAttributeDescriptor {
				BufferIndex = 0, Format = MTLVertexFormat.Float2, Offset = 0,
			};
			vdesc.Attributes[1] = new MTLVertexAttributeDescriptor {
				BufferIndex = 0, Format = MTLVertexFormat.Float2, Offset = 2 * sizeof (float),
			};
			vdesc.Attributes[2] = new MTLVertexAttributeDescriptor {
				BufferIndex = 0, Format = MTLVertexFormat.Float4, Offset = 4 * sizeof (float),
			};
			vdesc.Attributes[3] = new MTLVertexAttributeDescriptor {
				BufferIndex = 0, Format = MTLVertexFormat.Float4, Offset = 8 * sizeof (float),
			};
			vdesc.Attributes[4] = new MTLVertexAttributeDescriptor {
				BufferIndex = 0, Format = MTLVertexFormat.Float4, Offset = 12 * sizeof (float),
			};
			vdesc.Attributes[5] = new MTLVertexAttributeDescriptor {
				BufferIndex = 0, Format = MTLVertexFormat.UInt, Offset = 16 * sizeof (float),
			};
			pipelineDescriptor.VertexDescriptor = vdesc;
			var pipeline = device.CreateRenderPipelineState (pipelineDescriptor, error: out error);
			if (error is not null) {
				throw new NSErrorException (error);
			}
			return pipeline;
		}

		public enum DrawOp
		{
			FillRect = 0,
			StrokeRect = 1,
			FillRoundedRect = 2,
			StrokeRoundedRect = 3,
			FillOval = 4,
			StrokeOval = 5,
			FillArc = 6,
			StrokeArc = 7,
			FillPolygon = 8,
			StrokePolygon = 9,
			FillPath = 10,
			StrokePath = 11,
			DrawImage = 12,
			DrawString = 13,
			DrawLine = 14,
		}

		const string MetalCode = @"
using namespace metal;

constexpr sampler sdfSampler(coord::normalized, address::clamp_to_edge, filter::linear);

typedef struct
{
    float4x4 modelViewProjectionMatrix;
} Uniforms;

typedef struct
{
    float2 position [[attribute(0)]];
    float2 texCoord [[attribute(1)]];
	float4 color [[attribute(2)]];
	float4 bb [[attribute(3)]];
	float4 args [[attribute(4)]];
	uint op [[attribute(5)]];
} Vertex;

typedef struct
{
    float4 projectedPosition [[position]];
    float2 modelPosition;
    float2 texCoord;
    float4 color;
	float4 bb;
	float4 args;
	uint op;
} ColorInOut;

float fillRect(ColorInOut in)
{
	return 1.0;
}

float strokeRect(ColorInOut in)
{
	float2 p = in.modelPosition;
	float2 bbMin = in.bb.xy;
	float2 bbMax = in.bb.zw;
	float w = in.args.x;
	bool onedge = p.x < bbMin.x + w || p.x > bbMax.x - w || p.y < bbMin.y + w || p.y > bbMax.y - w;
	return onedge ? 1.0 : 0.0;
}

float fillRoundedRect(ColorInOut in)
{
	float2 p = in.modelPosition;
	float2 bbMin = in.bb.xy;
	float2 bbMax = in.bb.zw;
	float w = in.args.x;
	float r = in.args.y;
	if (p.x < bbMin.x + r && p.y < bbMin.y + r) {
		float pr = length(p - bbMin - float2(r, r));
		return pr <= r ? 1.0 : 0.0;
	}
	else if (p.x > bbMax.x - r && p.y < bbMin.y + r) {
		float pr = length(p - float2(bbMax.x, bbMin.y) - float2(-r, r));
		return pr <= r ? 1.0 : 0.0;
	}
	else if (p.x > bbMax.x - r && p.y > bbMax.y - r) {
		float pr = length(p - bbMax - float2(-r, -r));
		return pr <= r ? 1.0 : 0.0;
	}
	else if (p.x < bbMin.x + r && p.y > bbMax.y - r) {
		float pr = length(p - float2(bbMin.x, bbMax.y) - float2(r, -r));
		return pr <= r ? 1.0 : 0.0;
	}
	else {
		return 1.0;
	}
}

float strokeRoundedRect(ColorInOut in)
{
	float2 p = in.modelPosition;
	float w = in.args.x;
	float r = in.args.y;
	float w2 = w / 2;
	float2 bbMin = in.bb.xy;// + float2(w2, w2);
	float2 bbMax = in.bb.zw;// - float2(w2, w2);
	float rw2 = r + w2;
	bool onedge = false;
	if (p.x < bbMin.x + rw2 && p.y < bbMin.y + rw2) {
		float pr = length(p - (bbMin + float2(rw2, rw2)));
		onedge = abs(pr - r) <= w2;
	}
	else if (p.x > bbMax.x - rw2 && p.y < bbMin.y + rw2) {
		float pr = length(p - float2(bbMax.x, bbMin.y) - float2(-rw2, rw2));
		onedge = abs(pr - r) <= w2;
	}
	else if (p.x > bbMax.x - rw2 && p.y > bbMax.y - rw2) {
		float pr = length(p - bbMax - float2(-rw2, -rw2));
		onedge = abs(pr - r) <= w2;
	}
	else if (p.x < bbMin.x + rw2 && p.y > bbMax.y - rw2) {
		float pr = length(p - float2(bbMin.x, bbMax.y) - float2(rw2, -rw2));
		onedge = abs(pr - r) <= w2;
	}
	else {
		onedge = p.x < bbMin.x + w || p.x > bbMax.x - w || p.y < bbMin.y + w || p.y > bbMax.y - w;
	}
	return onedge ? 1.0 : 0.0;
}

float fillOval(ColorInOut in)
{
	float2 p = in.modelPosition;
	float2 bbMin = in.bb.xy;
	float2 bbMax = in.bb.zw;
	float2 center = (bbMin + bbMax) / 2;
	float2 radius = (bbMax - bbMin) / 2;
	float2 d = (p - center) / radius;
	float r = length(d);
	return r <= 1.0 ? 1.0 : 0.0;
}

float strokeOval(ColorInOut in)
{
	float2 p = in.modelPosition;
	float2 bbMin = in.bb.xy;
	float2 bbMax = in.bb.zw;
	float w = in.args.x;
	float w2 = w / 2;
	float2 center = (bbMin + bbMax) / 2;
	float2 radius = (bbMax - bbMin) / 2 - w2;
	float2 d = (p - center);
	float r = length(d / radius);
	float nw2 = w2 / min(radius.x, radius.y);
	bool onedge = r >= 1.0 - nw2 && r <= 1.0 + nw2;
	return onedge ? 1.0 : 0.0;
}

float drawString(ColorInOut in, texture2d<float> sdf)
{
    float2 uv = in.texCoord;
	float mask = sdf.sample(sdfSampler, uv).r;
	return mask;
}

float drawLine(ColorInOut in)
{
	float2 p3 = in.modelPosition;
	float2 p1 = in.bb.xy;
	float2 p2 = in.bb.zw;
	float w = in.args.x;
	float w2 = w / 2.0;
	float2 d21 = p2 - p1;
	float denom = dot(d21, d21);
	if (denom < 1e-6) {
		return 0.0;
	}
	float2 d31 = p3 - p1;
	float t = dot(d31, d21) / denom;
	if (t < 0) {
		float dist = length(p3 - p1);
		return dist < w2 ? 1.0 : 0.0;
	}
	else if (t > 1) {
		float dist = length(p3 - p2);
		return dist < w2 ? 1.0 : 0.0;
	}
	else {
		return 1.0;
	}
}

float fillArc(ColorInOut in)
{
	float2 p = in.modelPosition;
	float2 bbMin = in.bb.xy;
	float2 bbMax = in.bb.zw;
	float2 center = (bbMin + bbMax) / 2;
	float startAngle = in.args.y;
	float endAngle = in.args.z;
	float radius = bbMax.x - center.x;
	float2 dir = p - center;
	float distance = length(dir);
	bool inside = distance <= radius;
	if (inside) {
		float2 startNorm = float2(cos(startAngle), -sin(startAngle));
		float2 endNorm = float2(cos(endAngle), -sin(endAngle));
		float2 p1 = center + startNorm * radius;
		float2 p2 = center + endNorm * radius;
		float dx = (p2.x - p1.x);
		float dy = (p2.y - p1.y);
		if (abs(dx) < 1e-6 && abs(dy) < 1e-6) {
			return 0.0;
		}
		float sideOfLine = dx * (p.y - p1.y) - dy * (p.x - p1.x);
		if (sideOfLine > 0) {
			return 1.0;
		}
	}
	return 0.0;
}

float strokeArc(ColorInOut in)
{
	float2 p = in.modelPosition;
	float2 bbMin = in.bb.xy;
	float2 bbMax = in.bb.zw;
	float2 center = (bbMin + bbMax) / 2;
	float w = in.args.x;
	float startAngle = in.args.y;
	float endAngle = in.args.z;
	float w2 = w / 2;
	float radius = bbMax.x - center.x - w2;
	float2 dir = p - center;
	float distance = length(dir);
	bool onedge = distance >= radius - w2 && distance <= radius + w2;
	if (onedge) {
		float2 startNorm = float2(cos(startAngle), -sin(startAngle));
		float2 endNorm = float2(cos(endAngle), -sin(endAngle));
		startAngle = -atan2(startNorm.y, startNorm.x);
		endAngle = -atan2(endNorm.y, endNorm.x);
		if (abs(startAngle - endAngle) < 1e-6) {
			return 0.0;
		}
		float angle = -atan2(dir.y, dir.x);
		if (endAngle < startAngle) {
			endAngle += 2.0 * 3.14159265359;
		}
		if (angle < startAngle) {
			angle += 2.0 * 3.14159265359;
		}
		if (angle >= startAngle && angle <= endAngle) {
			return 1.0;
		}
		float2 startPoint = center + startNorm * radius;
		if (length(p - startPoint) <= w2) {
			return 1.0;
		}
		float2 endPoint = center + endNorm * radius;
		if (length(p - endPoint) <= w2) {
			return 1.0;
		}
	}
	return 0.0;
}

vertex ColorInOut vertexShader(Vertex in [[ stage_in ]],
                               constant Uniforms &uniforms [[ buffer(1) ]])
{
	float4 modelPosition = float4(in.position, 0.0, 1.0);
	float4 projectedPosition = uniforms.modelViewProjectionMatrix * modelPosition;
	ColorInOut out;
	out.projectedPosition = projectedPosition;
	out.modelPosition = in.position;
	out.texCoord = in.texCoord;
	out.color = in.color;
	out.bb = in.bb;
	out.args = in.args;
	out.op = in.op;
	return out;
}

constant float2 aaOffsets[8] = {
	float2(-0.25, -0.25),
	float2(-0.25, 0.25),
	float2(0.25, -0.25),
	float2(0.25, 0.25),
	float2(0, -0.375),
	float2(0, 0.375),
	float2(-0.375, 0),
	float2(0.375, 0),
};

fragment float4 fragmentShader(
	ColorInOut in [[stage_in]],
	texture2d<float> sdf0 [[ texture(0) ]])
{
	uint op = in.op;
	// Calculate derivatives for supersampling
    float2 dx = dfdx(in.modelPosition);
    float2 dy = dfdy(in.modelPosition);
    
    float mask = 0.0;
	for (int i = 0; i < 8; i++) {
        float2 offset = aaOffsets[i].x * dx + aaOffsets[i].y * dy;
        float2 samplePos = in.modelPosition + offset;
        
        ColorInOut sample = in;
        sample.modelPosition = samplePos;
		switch (op) {
		case 0: // FillRect
			mask += fillRect(sample);
			break;
		case 2: // FillRoundedRect
			mask += fillRoundedRect(sample);
			break;
		case 3: // StrokeRoundedRect
			mask += strokeRoundedRect(sample);
			break;
		case 4: // FillOval
			mask += fillOval(sample);
			break;
		case 5: // StrokeOval
			mask += strokeOval(sample);
			break;
		case 6: // FillArc
			mask += fillArc(sample);
			break;
		case 7: // StrokeArc
			mask += strokeArc(sample);
			break;
		case 8: // FillPolygon
			mask += fillRect(sample);
			break;
		case 13: // DrawString
			mask += drawString(sample, sdf0);
			break;
		case 14: // DrawLine
			mask += drawLine(sample);
			break;
		default:
			mask += strokeRect(sample);
			break;
		}
	}
	mask = clamp(mask * 0.125, 0.0, 1.0);
	if (mask < 0.01) {
		discard_fragment();
	}
	const float alpha = mask * in.color.w;
	return float4(in.color.xyz * alpha, alpha);
}
";
	}

	public class MetalPrimitivesBuffer
	{
		public readonly IMTLDevice Device;
		public readonly IMTLBuffer? VertexBuffer;
		private readonly IntPtr _vertexBufferPointer;
		public readonly IMTLBuffer? IndexBuffer;
		private readonly IntPtr _indexBufferPointer;
		public const int MaxVertices = 0x10000;
		public const int MaxIndices = 0x10000;

		public int NumVertices = 0;
		public int NumIndices = 0;
		public int RemainingVertices => MaxVertices - NumVertices;
		public int RemainingIndices => MaxIndices - NumIndices;
		public MetalPrimitivesBuffer (IMTLDevice device)
		{
			Device = device;
			VertexBuffer = Device.CreateBuffer ((nuint)(MaxVertices * MetalGraphics.VertexByteSize), MTLResourceOptions.CpuCacheModeDefault);
			_vertexBufferPointer = VertexBuffer?.Contents ?? IntPtr.Zero;
			IndexBuffer = Device.CreateBuffer ((nuint)(MaxIndices * sizeof (ushort)), MTLResourceOptions.CpuCacheModeDefault);
			_indexBufferPointer = IndexBuffer?.Contents ?? IntPtr.Zero;
		}
		public void Reset ()
		{
			NumVertices = 0;
			NumIndices = 0;
		}
		public int AddVertex (float x, float y, float u, float v, ValueColor color, Vector4 bb, Vector4 args, MetalGraphics.DrawOp op)
		{
			var index = NumVertices;
			if (_vertexBufferPointer != IntPtr.Zero) {
				unsafe {
					var p = (float*)_vertexBufferPointer;
					p += index * MetalGraphics.VertexByteSize / sizeof(float);
					p[0] = x;
					p[1] = y;
					p[2] = u;
					p[3] = v;
					p[4] = color.Red / 255f;
					p[5] = color.Green / 255f;
					p[6] = color.Blue / 255f;
					p[7] = color.Alpha / 255f;
					p[8] = bb.X;
					p[9] = bb.Y;
					p[10] = bb.Z;
					p[11] = bb.W;
					p[12] = args.X;
					p[13] = args.Y;
					p[14] = args.Z;
					p[15] = args.W;
					*((uint*)(p + 16)) = (uint)op;
				}
			}
			NumVertices++;
			return index;
		}
		public void AddTriangle (int v0, int v1, int v2)
		{
			var index = NumIndices;
			if (_indexBufferPointer != IntPtr.Zero) {
				unsafe {
					var p = (ushort*)_indexBufferPointer;
					p += index;
					p[0] = (ushort)v0;
					p[1] = (ushort)v1;
					p[2] = (ushort)v2;
				}
			}
			NumIndices += 3;
		}
	}

	public class MetalSdfTexture
	{
		const int MaxWidth = 2048;
		const int MaxHeight = 2048;

		public class SubRect
		{
			public readonly int X;
			public readonly int Y;
			public readonly int Width;
			public readonly int Height;
			public override string ToString () => $"({X}, {Y}, {Width}, {Height})";
			public int Left => X;
			public int Right => X + Width;
			public int Top => Y;
			public int Bottom => Y + Height;
			public SubRect (int x, int y, int width, int height)
			{
				X = x;
				Y = y;
				Width = width;
				Height = height;
			}
			public Vector4 UVBoundingBox => new Vector4 (X / (float)MaxWidth, Y / (float)MaxHeight, (X + Width) / (float)MaxWidth, (Y + Height) / (float)MaxHeight);
		}
		readonly List<SubRect> _free = new ();
		readonly Dictionary<int, List<SubRect>> _freeLeft = new ();
		readonly Dictionary<int, List<SubRect>> _freeRight = new ();
		readonly Dictionary<int, List<SubRect>> _freeTop = new ();
		readonly Dictionary<int, List<SubRect>> _freeBottom = new ();

		public IMTLTexture? Texture { get; }

		// private const MTLPixelFormat TexturePixelFormat = MTLPixelFormat.R32Float;
		private const MTLPixelFormat TexturePixelFormat = MTLPixelFormat.R8Unorm;

		public readonly int TextureIndex;

		public MetalSdfTexture (IMTLDevice device, int textureIndex)
		{
			var tdesc = MTLTextureDescriptor.CreateTexture2DDescriptor (TexturePixelFormat, (nuint)MaxWidth,
				(nuint)MaxHeight, mipmapped: false);
			Texture = device.CreateTexture (tdesc);
			TextureIndex = textureIndex;
			Dealloc (new SubRect (0, 0, MaxWidth, MaxHeight));
		}

		SubRect? Alloc (float desiredWidth, float desiredHeight)
		{
			var idesiredWidth = (int)MathF.Ceiling(desiredWidth);
			var idesiredHeight = (int)MathF.Ceiling(desiredHeight);
			if (idesiredWidth > MaxWidth / 4) {
				var nidesiredWidth = MaxWidth / 4;
				idesiredHeight = (int)MathF.Ceiling (idesiredHeight * (float)nidesiredWidth / desiredWidth);
				idesiredWidth = nidesiredWidth;
			}
			if (idesiredHeight > MaxHeight / 4) {
				var nidesiredHeight = MaxHeight / 4;
				idesiredWidth = (int)MathF.Ceiling (idesiredWidth * (float)nidesiredHeight / desiredHeight);
				idesiredHeight = nidesiredHeight;
			}
			for (var freeIndex = 0; freeIndex < _free.Count; freeIndex++) {
				var freeRect = _free[freeIndex];
				if (freeRect.Width >= idesiredWidth && freeRect.Height >= idesiredHeight) {
					_free.RemoveAt (freeIndex);
					RemoveFromFreeList (freeRect, freeRect.Left, _freeLeft);
					RemoveFromFreeList (freeRect, freeRect.Right, _freeRight);
					RemoveFromFreeList (freeRect, freeRect.Top, _freeTop);
					RemoveFromFreeList (freeRect, freeRect.Bottom, _freeBottom);
					if (freeRect.Height > idesiredHeight) {
						Dealloc (new SubRect (freeRect.X, freeRect.Y + idesiredHeight, freeRect.Width, freeRect.Height - idesiredHeight));
					}
					if (freeRect.Width > idesiredWidth) {
						Dealloc (new SubRect (freeRect.X + idesiredWidth, freeRect.Y, freeRect.Width - idesiredWidth, idesiredHeight));
					}
					return new SubRect (freeRect.X, freeRect.Y, idesiredWidth, idesiredHeight);
				}
			}
			return null;
		}

		void AddToFreeList(SubRect rect, int key, Dictionary<int, List<SubRect>> list)
		{
			if (!list.TryGetValue(key, out var l)) {
				l = new List<SubRect> { rect };
				list[key] = l;
			}
			else {
				l.Add (rect);
			}
		}

		void RemoveFromFreeList(SubRect rect, int key, Dictionary<int, List<SubRect>> list)
		{
			if (list.TryGetValue(key, out var l)) {
				l.Remove (rect);
				if (l.Count == 0) {
					list.Remove (key);
				}
			}
		}

		void RemoveFromFreeLists (SubRect alreadyFree)
		{
			_free.Remove (alreadyFree);
			RemoveFromFreeList (alreadyFree, alreadyFree.Left, _freeLeft);
			RemoveFromFreeList (alreadyFree, alreadyFree.Right, _freeRight);
			RemoveFromFreeList (alreadyFree, alreadyFree.Top, _freeTop);
			RemoveFromFreeList (alreadyFree, alreadyFree.Bottom, _freeBottom);
		}

		List<SubRect>? GetFreeList(int key, Dictionary<int, List<SubRect>> list)
		{
			if (list.TryGetValue(key, out var l)) {
				return l;
			}
			return null;
		}

		public void Dealloc (SubRect allocatedRect)
		{
			var mergedRect = allocatedRect;
			var merged = true;
			while (merged) {
				merged = false;
				//
				// Look for free rects that are to the left
				//
				if (GetFreeList (mergedRect.Left, _freeRight) is {} leftList) {
					foreach (var freeRect in leftList) {
						if (freeRect.Top == mergedRect.Top && freeRect.Bottom == mergedRect.Bottom) {
							var newRect = new SubRect (freeRect.X, mergedRect.Y, freeRect.Width + mergedRect.Width, mergedRect.Height);
							RemoveFromFreeLists (freeRect);
							mergedRect = newRect;
							merged = true;
							break;
						}
					}
				}
				//
				// Look for free rects that are above
				//
				if (GetFreeList (mergedRect.Top, _freeBottom) is {} topList) {
					foreach (var freeRect in topList) {
						if (freeRect.Left == mergedRect.Left && freeRect.Right == mergedRect.Right) {
							var newRect = new SubRect (mergedRect.X, freeRect.Y, mergedRect.Width, freeRect.Height + mergedRect.Height);
							RemoveFromFreeLists (freeRect);
							mergedRect = newRect;
							merged = true;
							break;
						}
					}
				}
				//
				// Look for free rects that are to the right
				//
				if (GetFreeList (mergedRect.Right, _freeLeft) is {} rightList) {
					foreach (var freeRect in rightList) {
						if (freeRect.Top == mergedRect.Top && freeRect.Bottom == mergedRect.Bottom) {
							var newRect = new SubRect (mergedRect.X, mergedRect.Y, mergedRect.Width + freeRect.Width, mergedRect.Height);
							RemoveFromFreeLists (freeRect);
							mergedRect = newRect;
							merged = true;
							break;
						}
					}
				}
				//
				// Look for free rects that are below
				//
				if (GetFreeList (mergedRect.Bottom, _freeTop) is {} bottomList) {
					foreach (var freeRect in bottomList) {
						if (freeRect.Left == mergedRect.Left && freeRect.Right == mergedRect.Right) {
							var newRect = new SubRect (mergedRect.X, mergedRect.Y, mergedRect.Width, mergedRect.Height + freeRect.Height);
							RemoveFromFreeLists (freeRect);
							mergedRect = newRect;
							merged = true;
							break;
						}
					}
				}
			}

			_free.Add (mergedRect);
			AddToFreeList (mergedRect, mergedRect.Left, _freeLeft);
			AddToFreeList (mergedRect, mergedRect.Right, _freeRight);
			AddToFreeList (mergedRect, mergedRect.Top, _freeTop);
			AddToFreeList (mergedRect, mergedRect.Bottom, _freeBottom);
		}

		const int padding = 4;
		IntPtr _drawingData = IntPtr.Zero;
		int _drawingDataSize = 0;

		~MetalSdfTexture ()
		{
			if (_drawingData != IntPtr.Zero) {
				Marshal.FreeHGlobal (_drawingData);
			}
		}

		public SdfTextureRegion? AllocAndDraw (float width, float height, Action<CGContext> draw)
		{
			if (Texture is null) {
				return null;
			}
			var paddedWidth = width + padding * 2;
			var paddedHeight = height + padding * 2;
			var subRect = Alloc (paddedWidth, paddedHeight);
			if (subRect is null) {
				return null;
			}
			var bitsPerComponent = 32;
			var bytesPerRow = (subRect.Width * bitsPerComponent) / 8;
			var bitmapFlags = CGBitmapFlags.FloatComponents;
			if (TexturePixelFormat == MTLPixelFormat.R8Unorm) {
				bitsPerComponent = 8;
				bytesPerRow = subRect.Width;
				bitmapFlags = CGBitmapFlags.None;
			}
			var dataSize = bytesPerRow * subRect.Height;
			if (dataSize > _drawingDataSize) {
				if (_drawingData != IntPtr.Zero) {
					_drawingData = Marshal.ReAllocHGlobal (_drawingData, (IntPtr)dataSize);
				}
				else {
					_drawingData = Marshal.AllocHGlobal (dataSize);
				}
				_drawingDataSize = dataSize;
			}
			using var cs = CGColorSpace.CreateDeviceGray ();
			using var cgContext = new CGBitmapContext (_drawingData, subRect.Width, subRect.Height, bitsPerComponent, bytesPerRow, cs, bitmapFlags);
			cgContext.ClearRect (new CGRect (0, 0, subRect.Width, subRect.Height));
			cgContext.TranslateCTM (padding, padding);
			// cgContext.ScaleCTM (1, -1);
			draw (cgContext);
			cgContext.Flush ();
			var data = cgContext.Data;
			if (data != IntPtr.Zero) {
				var region = MTLRegion.Create2D ((nuint)subRect.X, (nuint)subRect.Y, (nuint)subRect.Width,
					(nuint)subRect.Height);
				Texture.ReplaceRegion (region: region, level: 0, pixelBytes: data, bytesPerRow: (nuint)bytesPerRow);
			}
			var unPaddedSubRect = new SubRect (subRect.X + padding, subRect.Y + padding, subRect.Width - padding * 2, subRect.Height - padding * 2);
			return new SdfTextureRegion (TextureIndex, unPaddedSubRect.UVBoundingBox, new Vector2 (width, height), subRect);
		}
	}

	readonly struct SdfKey
	{
		public readonly string Text;
		public readonly string Font;
		public SdfKey (string text, string font)
		{
			Text = text;
			Font = font;
		}
		public override bool Equals (object? obj)
		{
			if (obj is SdfKey key) {
				return key.Text == Text && key.Font == Font;
			}
			return false;
		}
		public override int GetHashCode ()
		{
			return HashCode.Combine (Text, Font);
		}
	}

	class SdfKeyComparer : IEqualityComparer<SdfKey>
	{
		public static readonly SdfKeyComparer Shared = new SdfKeyComparer ();
		public bool Equals (SdfKey x, SdfKey y)
		{
			return x.Text == y.Text && x.Font == y.Font;
		}
		public int GetHashCode (SdfKey obj)
		{
			return HashCode.Combine (obj.Text, obj.Font);
		}
	}

	public class SdfTextureRegion
	{
		public readonly int TextureIndex;
		public readonly Vector4 UVBoundingBox;
		public readonly Vector2 DrawSize;
		public readonly MetalSdfTexture.SubRect AllocatedRect;
		public int LastUsedFrame = 0;
		public SdfTextureRegion (int textureIndex, Vector4 uv, Vector2 drawSize, MetalSdfTexture.SubRect allocatedRect)
		{
			TextureIndex = textureIndex;
			UVBoundingBox = uv;
			DrawSize = drawSize;
			AllocatedRect = allocatedRect;
		}
	}

	public class MetalGraphicsBuffers
	{
		public readonly IMTLDevice Device;

		int _currentPrimitiveBufferIndex = 0;
		readonly List<MetalPrimitivesBuffer> _primitiveBuffers;
		readonly List<MetalSdfTexture> _sdfTextures;

		public List<MetalPrimitivesBuffer> Primitives => _primitiveBuffers;
		public List<MetalSdfTexture> SdfTextures => _sdfTextures;
		private readonly Dictionary<SdfKey, SdfTextureRegion> _sdfValues = new(comparer: SdfKeyComparer.Shared);

		readonly IMTLBuffer? _uniformsBuffer;
		readonly IntPtr _uniformsBufferPointer;
		public IMTLBuffer? Uniforms => _uniformsBuffer;

		readonly Dictionary<string, CTStringAttributes> _cachedStringAttributes = new ();

		int _frame = 0;

		public MetalGraphicsBuffers (IMTLDevice device)
		{
			Device = device;
			_primitiveBuffers = new List<MetalPrimitivesBuffer> { new MetalPrimitivesBuffer (Device) };
			_sdfTextures = new List<MetalSdfTexture> { new MetalSdfTexture (Device, textureIndex: 0) };
			_uniformsBuffer = Device.CreateBuffer ((nuint)(MetalGraphics.UniformsByteSize), MTLResourceOptions.CpuCacheModeDefault);
			_uniformsBufferPointer = _uniformsBuffer?.Contents ?? IntPtr.Zero;
		}

		public void Reset ()
		{
			foreach (var b in _primitiveBuffers) {
				b.Reset ();
			}
			_currentPrimitiveBufferIndex = 0;
			_frame++;
		}

		public MetalPrimitivesBuffer GetPrimitivesBuffer (int numVertices, int numIndices)
		{
			var b = _primitiveBuffers[_currentPrimitiveBufferIndex];
			if (b.RemainingVertices >= numVertices && b.RemainingIndices >= numIndices) {
				return b;
			}
			var buffer = new MetalPrimitivesBuffer (Device);
			_primitiveBuffers.Add (buffer);
			_currentPrimitiveBufferIndex++;
			return buffer;
		}

		public SdfTextureRegion? FindExistingSdfTextureRegion (string text, string font)
		{
			var key = new SdfKey (text, font);
			if (_sdfValues.TryGetValue (key, out var value)) {
				value.LastUsedFrame = _frame;
				return value;
			}
			return null;
		}

		public SdfTextureRegion? DrawSdfTextureRegion (string text, string font, float width, float height, Action<CGContext> draw)
		{
			var key = new SdfKey (text, font);
			var textureIndex = 0;
			var texture = _sdfTextures[textureIndex];
			var numTries = 2;
			for (var tri = 0; tri < numTries; tri++) {
				var regionO = texture.AllocAndDraw (width, height, draw);
				if (regionO is SdfTextureRegion region) {
					region.LastUsedFrame = _frame;
					_sdfValues[key] = region;
					return region;
				}
				Console.WriteLine ($"Could not allocate SDF texture region for {width}x{height}");
				FreePastFrameSdfTextureRegions ();
			}
			return null;
		}

		void FreePastFrameSdfTextureRegions ()
		{
			var vals = _sdfValues.ToArray ();
			foreach (var (key, region) in vals) {
				if (region.LastUsedFrame < _frame - 2) {
					SdfTextures[region.TextureIndex].Dealloc (region.AllocatedRect);
					_sdfValues.Remove (key);
				}
			}
		}

		public void SetUniforms (Matrix4x4 modelToView)
		{
			if (_uniformsBufferPointer != IntPtr.Zero) {
				unsafe {
					var p = (float*)_uniformsBufferPointer;
					p[0] = modelToView.M11;
					p[1] = modelToView.M12;
					p[2] = modelToView.M13;
					p[3] = modelToView.M14;
					p[4] = modelToView.M21;
					p[5] = modelToView.M22;
					p[6] = modelToView.M23;
					p[7] = modelToView.M24;
					p[8] = modelToView.M31;
					p[9] = modelToView.M32;
					p[10] = modelToView.M33;
					p[11] = modelToView.M34;
					p[12] = modelToView.M41;
					p[13] = modelToView.M42;
					p[14] = modelToView.M43;
					p[15] = modelToView.M44;
				}
			}
		}

		public CTStringAttributes GetCTStringAttributes(string font, nfloat renderFontSize)
		{
			if (_cachedStringAttributes.TryGetValue (font, out var attrs)) {
				return attrs;
			}
			attrs = new CTStringAttributes {
				ForegroundColorFromContext = true,
				Font = new CTFont (font, renderFontSize),
			};
			_cachedStringAttributes[font] = attrs;
			return attrs;
		}
	}
}
