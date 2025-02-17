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

		void DoRect (float x, float y, float width, float height, float w, DrawOp op, float argy = 0, float argz = 0, float argw = 0)
		{
			var buffer = _buffers.GetPrimitivesBuffer(numVertices: 4, numIndices: 6);
			var bb = BoundingBox.FromRect (x, y, width, height, w);
			var bbv = new Vector4 (bb.MinX, bb.MinY, bb.MaxX, bb.MaxY);
			var args = new Vector4 (w, argy, argz, argw);
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
			var pad = ArcPad (0);
			DoRect (x-pad, y-pad, width+2*pad, height+2*pad, 0, DrawOp.FillRect, argz: width, argw: height);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			var pad = ArcPad (0);
			DoRect (x-pad, y-pad, width+2*pad, height+2*pad, w, DrawOp.StrokeRect, argz: width, argw: height);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			var pad = ArcPad (radius);
			DoRect (x-pad, y-pad, width+2*pad, height+2*pad, 0, DrawOp.FillRoundedRect, argy: radius, argz: width, argw: height);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			var pad = ArcPad (radius);
			DoRect (x-pad, y-pad, width+2*pad, height+2*pad, w, DrawOp.StrokeRoundedRect, argy: radius, argz: width, argw: height);
		}

		float ArcPad(float radius)
		{
			return 3;
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var pad = ArcPad (MathF.Max (width, height));
			DoRect (x - pad, y - pad, width + 2*pad, height + 2*pad, 0, DrawOp.FillOval, argz: width, argw: height);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var pad = ArcPad (MathF.Max (width, height));
			DoRect (x - pad, y - pad, width + 2*pad, height + 2*pad, w, DrawOp.StrokeOval, argz: width, argw: height);
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
			var pad = ArcPad (radius);
			DoRect (cx - radius - pad, cy - radius - pad, radius * 2 + pad * 2, radius * 2 + pad * 2, 0, DrawOp.FillArc, argy: startAngle, argz: endAngle, argw: radius);
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			var isCircle = Math.Abs(PositiveAngle (endAngle - startAngle)) >= MathF.PI * 2.0f - 1.0e-6f;
			if (isCircle) {
				DrawOval (cx - radius, cy - radius, radius * 2, radius * 2, w);
				return;
			}
			var pad = ArcPad (radius);
			DoRect (cx - radius - pad, cy - radius - pad, radius * 2 + pad * 2, radius * 2 + pad * 2, w, DrawOp.StrokeArc, argy: startAngle, argz: endAngle, argw: radius);
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
			var args = new Vector4 (sx, sy, 0, w);
			var ox = dx / len * w2 * 4.0f;
			var oy = dy / len * w2 * 4.0f;
			var nx = oy;
			var ny = -ox;
			var v0 = buffer.AddVertex (sx - ox - nx, sy - oy - ny, ex, ey, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			var v1 = buffer.AddVertex (sx - ox  + nx, sy - oy + ny, ex, ey, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			var v2 = buffer.AddVertex (ex + ox + nx, ey + oy + ny, ex, ey, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			var v3 = buffer.AddVertex (ex + ox - nx, ey + oy - ny, ex, ey, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
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

		static int GetRenderFontSize (int fontSize) => 32;//Math.Min(96, Math.Max(1, ((fontSize + 15) / 16) * 16 * 2));

		public void DrawString (string s, float x, float y)
		{
			var font = CrossGraphics.CoreGraphics.CoreGraphicsGraphics.GetFontName (_currentFont);
			var fontSize = _currentFont.Size;
			var renderFontSize = GetRenderFontSize (fontSize);
			var regionO = _buffers.FindExistingSdfTextureRegion (s, font, renderFontSize);

			var hpadding = MathF.Max (2.0f, renderFontSize * 0.1f);
			var vpadding = MathF.Max (2.0f, renderFontSize * 0.2f);

			if (regionO is null) {
				using var atext = new NSMutableAttributedString (s, _buffers.GetCTStringAttributes(font, renderFontSize));
				using var drawLine = new CTLine (atext);
				var len = drawLine.GetTypographicBounds (out var ascent, out var descent, out var leading);

				var drawWidth = (float)(len + 2.0 * hpadding);
				var drawHeight = (float)(renderFontSize + 2.0 * vpadding);
				regionO = _buffers.DrawSdfTextureRegion (s, font, renderFontSize, drawWidth, drawHeight, (cgContext) => {
					// cgContext.SetFillColor ((nfloat)0.5, (nfloat)0.5, (nfloat)0.5, 1);
					// if (s.Length > 5) {
					// 	cgContext.FillEllipseInRect (new CGRect (0, 0, drawWidth, drawHeight));
					// }
					// else {
					// 	cgContext.FillRect (new CGRect (0, 0, drawWidth, drawHeight));
					// }
					// cgContext.SetFillColor (1, 1, 0, 0.25f);
					// cgContext.FillRect (new CGRect (0, 0, drawWidth, drawHeight));
					cgContext.SetFillColor (1, 1, 1, 1);
					// cgContext.TextMatrix = CGAffineTransform.MakeScale (1, -1);
					cgContext.TranslateCTM ((nfloat)hpadding, drawHeight - vpadding - (nfloat)(renderFontSize * 0.8333));
					drawLine.Draw (cgContext);
				});
			}

			if (regionO is SdfTextureRegion region) {
				var fontScale = fontSize / (float)renderFontSize;
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
	float2 p = in.modelPosition;
	float2 center = (in.bb.xy + in.bb.zw) / 2;
	float width = in.args.z;
	float height = in.args.w;
	float2 bbMin = center - float2(width / 2, height / 2);
	float2 bbMax = center + float2(width / 2, height / 2);
	return (bbMin.x <= p.x && p.x < bbMax.x && bbMin.y <= p.y && p.y < bbMax.y) ? 1.0 : 0.0;
}

float strokeRect(ColorInOut in)
{
	float2 p = in.modelPosition;
	float w = in.args.x;
	float rw2 = w / 2;
	float2 center = (in.bb.xy + in.bb.zw) / 2;
	float width = in.args.z;
	float height = in.args.w;
	float2 bbMin = center - float2(width / 2 + rw2, height / 2 + rw2);
	float2 bbMax = center + float2(width / 2 + rw2, height / 2 + rw2);
	bool onedge = (bbMin.x <= p.x && p.x < bbMin.x + w && bbMin.y <= p.y && p.y < bbMax.y) ||
                 (bbMax.x - w <= p.x && p.x < bbMax.x && bbMin.y <= p.y && p.y < bbMax.y) ||
				(bbMin.y <= p.y && p.y < bbMin.y + w && bbMin.x + rw2 <= p.x && p.x < bbMax.x - rw2) ||
				(bbMax.y - w <= p.y && p.y < bbMax.y && bbMin.x + rw2 <= p.x && p.x < bbMax.x - rw2);
	return onedge ? 1.0 : 0.0;
}

float fillRoundedRect(ColorInOut in)
{
	float2 p = in.modelPosition;
	float2 center = (in.bb.xy + in.bb.zw) / 2;
	float width = in.args.z;
	float height = in.args.w;
	float2 bbMin = center - float2(width / 2, height / 2);
	float2 bbMax = center + float2(width / 2, height / 2);
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
		return (bbMin.x < p.x && p.x < bbMax.x && bbMin.y < p.y && p.y < bbMax.y) ? 1.0 : 0.0;
	}
}

float strokeRoundedRect(ColorInOut in)
{
	float2 p = in.modelPosition;
	float w = in.args.x;
	float r = in.args.y;
	float w2 = w / 2;
	float width = in.args.z;
	float height = in.args.w;
	float2 center = (in.bb.xy + in.bb.zw) / 2;
	float2 bbMin = center - float2(width / 2 + w2, height / 2 + w2);
	float2 bbMax = center + float2(width / 2 + w2, height / 2 + w2);
	float rw2 = r + w2;
	bool onedge = false;
	if (p.x <= bbMin.x + rw2 && p.y <= bbMin.y + rw2) {
		float pr = length(p - (bbMin + float2(rw2, rw2)));
		onedge = abs(pr - r) <= w2;
	}
	else if (p.x >= bbMax.x - rw2 && p.y <= bbMin.y + rw2) {
		float pr = length(p - float2(bbMax.x, bbMin.y) - float2(-rw2, rw2));
		onedge = abs(pr - r) <= w2;
	}
	else if (p.x >= bbMax.x - rw2 && p.y >= bbMax.y - rw2) {
		float pr = length(p - bbMax - float2(-rw2, -rw2));
		onedge = abs(pr - r) <= w2;
	}
	else if (p.x <= bbMin.x + rw2 && p.y >= bbMax.y - rw2) {
		float pr = length(p - float2(bbMin.x, bbMax.y) - float2(rw2, -rw2));
		onedge = abs(pr - r) <= w2;
	}
	else {
		onedge = (bbMin.x < p.x && p.x < bbMin.x + w && bbMin.y + rw2 < p.y && p.y < bbMax.y - rw2) ||
                 (bbMax.x - w < p.x && p.x < bbMax.x && bbMin.y + rw2 < p.y && p.y < bbMax.y - rw2) ||
				(bbMin.y < p.y && p.y < bbMin.y + w && bbMin.x + rw2 < p.x && p.x < bbMax.x - rw2) ||
				(bbMax.y - w < p.y && p.y < bbMax.y && bbMin.x + rw2 < p.x && p.x < bbMax.x - rw2);
	}
	return onedge ? 1.0 : 0.0;
}

float fillOval(ColorInOut in)
{
	float2 p = in.modelPosition;
	float2 bbMin = in.bb.xy;
	float2 bbMax = in.bb.zw;
	float2 center = (bbMin + bbMax) / 2;
	float2 radius = in.args.zw / 2;
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
	float2 radius = in.args.zw / 2;
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
	float2 p1 = in.args.xy;
	float2 p2 = in.texCoord;
	float w = in.args.w;
	float w2 = w / 2.0;
	float2 d21 = p2 - p1;
	float denom = dot(d21, d21);
	if (denom < 1e-6) {
		return 0.0;
	}
	float2 d31 = p3 - p1;
	float t = dot(d31, d21) / denom;
	float dist = 0.0f;
	if (t < 0) {
		dist = length(p3 - p1);
	}
	else if (t > 1) {
		dist = length(p3 - p2);
	}
	else {
		dist = length(p3 - (p1 + t * d21));
	}
	return dist < w2 ? 1.0 : 0.0;
}

float calculateThickLineAABBIntersectionArea(
    float2 p1,           // Start point of line segment
    float2 p2,           // End point of line segment
    float width,         // Width of the line
    float2 boxMin,       // Min corner of AABB
    float2 boxMax        // Max corner of AABB
) {
    // 1. Transform line segment to have p1 at origin and be aligned with x-axis
    float2 dir = p2 - p1;
    float lineLength = length(dir);
    float2 unit_dir = dir / lineLength;
    
    // 2. Transform corners to line space
    float2x2 inv_rotation = float2x2(unit_dir.x, -unit_dir.y, unit_dir.y, unit_dir.x);
	float2 corner0 = inv_rotation * (boxMin - p1);
	float2 corner1 = inv_rotation * (float2(boxMax.x, boxMin.y) - p1);
	float2 corner2 = inv_rotation * (boxMax - p1);
	float2 corner3 = inv_rotation * (float2(boxMin.x, boxMax.y) - p1);
	
	// 3. Calculate the bounding box of the transformed box
	float2 boxBBMin = min(min(min(corner0, corner1), corner2), corner3);
	float2 boxBBMax = max(max(max(corner0, corner1), corner2), corner3);
	
	// 4. Calculate the bounding box of the line
	float half_width = width * 0.5;
    float2 line_bounds_min = float2(0, -half_width);
    float2 line_bounds_max = float2(lineLength, half_width);
	
	// 4. Calculate the intersection of the two bounding boxes
	float2 intersectionMin = max(boxBBMin, line_bounds_min);
	float2 intersectionMax = min(boxBBMax, line_bounds_max);

	// 5. Calculate the area of the intersection
	float2 intersectionSize = max(intersectionMax - intersectionMin, float2(0, 0));

	return intersectionSize.x * intersectionSize.y;
}

float calculateRoundedThickLineAABBIntersectionArea(
    float2 p1,           // Start point of line segment
    float2 p2,           // End point of line segment
    float width,         // Width of the line
    float2 boxMin,       // Min corner of AABB
    float2 boxMax        // Max corner of AABB
) {
    // 1. Transform line segment to have p1 at origin and be aligned with x-axis
    float2 dir = p2 - p1;
    float lineLength = length(dir);
    float2 unit_dir = dir / lineLength;
    
    // 2. Transform corners to line space
    float2x2 inv_rotation = float2x2(unit_dir.x, -unit_dir.y, unit_dir.y, unit_dir.x);
	float2 corner0 = inv_rotation * (boxMin - p1);
	float2 corner1 = inv_rotation * (float2(boxMax.x, boxMin.y) - p1);
	float2 corner2 = inv_rotation * (boxMax - p1);
	float2 corner3 = inv_rotation * (float2(boxMin.x, boxMax.y) - p1);
	
	// 3. Calculate the bounding box of the transformed box
	float2 boxBBMin = min(min(min(corner0, corner1), corner2), corner3);
	float2 boxBBMax = max(max(max(corner0, corner1), corner2), corner3);
	
	// 4. Calculate the bounding box of the line
	float half_width = width * 0.5;
    float2 line_bounds_min = float2(-half_width, -half_width);
    float2 line_bounds_max = float2(lineLength + half_width, half_width);
	
	// 4. Calculate the intersection of the two bounding boxes
	float2 intersectionMin = max(boxBBMin, line_bounds_min);
	float2 intersectionMax = min(boxBBMax, line_bounds_max);

	// 4a. Handle rounded ends
	if (intersectionMax.x < 0 || intersectionMin.x > lineLength) {
		float x = (intersectionMin.x + intersectionMax.x) * 0.5;
		float dx = x < 0 ? -x : x - lineLength;
		float r = half_width;
		float end_half_width = sqrt(r * r - dx * dx); 
		intersectionMin = max(intersectionMin, float2(-half_width, -end_half_width));
		intersectionMax = min(intersectionMax, float2(lineLength + half_width, end_half_width));
	}

	// 5. Calculate the area of the intersection
	float2 intersectionSize = max(intersectionMax - intersectionMin, float2(0, 0));

	return intersectionSize.x * intersectionSize.y;
}

float fillArc(ColorInOut in)
{
	float2 p = in.modelPosition;
	float2 bbMin = in.bb.xy;
	float2 bbMax = in.bb.zw;
	float2 center = (bbMin + bbMax) / 2;
	float startAngle = in.args.y;
	float endAngle = in.args.z;
	float radius = in.args.w;
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
	float radius = in.args.w;
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

constant float2 aaOffsets4[4] = {
	float2(-0.25, -0.25),
	float2(-0.25, 0.25),
	float2(0.25, -0.25),
	float2(0.25, 0.25),
};

constant float2 aaOffsets16[16] = {
	float2(-0.25, -0.25) + float2(-0.125, -0.125),
	float2(-0.25, -0.25) + float2(-0.125, 0.125),
	float2(-0.25, -0.25) + float2(0.125, -0.125),
	float2(-0.25, -0.25) + float2(0.125, 0.125),
	float2(-0.25, 0.25) + float2(-0.125, -0.125),
	float2(-0.25, 0.25) + float2(-0.125, 0.125),
	float2(-0.25, 0.25) + float2(0.125, -0.125),
	float2(-0.25, 0.25) + float2(0.125, 0.125),
	float2(0.25, -0.25) + float2(-0.125, -0.125),
	float2(0.25, -0.25) + float2(-0.125, 0.125),
	float2(0.25, -0.25) + float2(0.125, -0.125),
	float2(0.25, -0.25) + float2(0.125, 0.125),
	float2(0.25, 0.25) + float2(-0.125, -0.125),
	float2(0.25, 0.25) + float2(-0.125, 0.125),
	float2(0.25, 0.25) + float2(0.125, -0.125),
	float2(0.25, 0.25) + float2(0.125, 0.125),
};

fragment float4 fragmentShader(
	ColorInOut in [[stage_in]],
	texture2d<float> sdf0 [[ texture(0) ]])
{
	uint op = in.op;
	// Calculate derivatives for supersampling
    float2 dx = dfdx(in.modelPosition);
    float2 dy = dfdy(in.modelPosition);
    
	float2 p3 = in.modelPosition;
	float2 pixelMin = p3 - 0.5*dx - 0.5*dy;
	float2 pixelMax = p3 + 0.5*dx + 0.5*dy;
	float2 pixelD = pixelMax - pixelMin;
	float pixelArea = pixelD.x * pixelD.y;

    float mask = 0.0;
	if (op == 0) { // FillRect
		float2 center = (in.bb.xy + in.bb.zw) / 2;
		float width = in.args.z;
		float height = in.args.w;
		float2 bbMin = center - float2(width / 2, height / 2);
		float2 bbMax = center + float2(width / 2, height / 2);
		float2 intersectionMin = max(bbMin, pixelMin);
		float2 intersectionMax = min(bbMax, pixelMax);
		float2 intersectionSize = max(intersectionMax - intersectionMin, float2(0, 0));
		float intersectArea = intersectionSize.x * intersectionSize.y;
		mask = intersectArea / pixelArea;
	}
	else if (op == 1) { // StrokeRect
		float2 center = (in.bb.xy + in.bb.zw) / 2;
		float w = in.args.x;
		float w2 = w / 2.0;
		float width = in.args.z;
		float height = in.args.w;
		float2 bbMin = center - float2(width / 2, height / 2);
		float2 bbMax = center + float2(width / 2, height / 2);
		float leftArea = calculateThickLineAABBIntersectionArea(float2(bbMin.x, bbMin.y - w2), float2(bbMin.x, bbMax.y + w2), w, pixelMin, pixelMax);
		float rightArea = calculateThickLineAABBIntersectionArea(float2(bbMax.x, bbMin.y - w2), float2(bbMax.x, bbMax.y + w2), w, pixelMin, pixelMax);
		float topArea = calculateThickLineAABBIntersectionArea(float2(bbMin.x - w2, bbMax.y), float2(bbMax.x + w2, bbMax.y), w, pixelMin, pixelMax);
		float bottomArea = calculateThickLineAABBIntersectionArea(float2(bbMin.x - w2, bbMin.y), float2(bbMax.x + w2, bbMin.y), w, pixelMin, pixelMax);
		float intersectArea = max(max(max(leftArea, rightArea), topArea), bottomArea);
		mask = intersectArea / pixelArea;
	}
	else if (op == 5) { // StrokeOval
		float2 center = (in.bb.xy + in.bb.zw) / 2;
		float2 radius = in.args.zw / 2;
		float w = in.args.x;
		float w2 = w / 2;
		float2 d = (p3 - center);
		float angle = atan2(d.y/radius.y, d.x/radius.x);
		float2 linearizedEdgeCenter = float2(cos(angle), sin(angle)) * radius;
		float2 linearizedEdgeTangent = normalize(float2(cos(angle+0.001), sin(angle+0.001)) * radius - linearizedEdgeCenter);
		float2 linearizedEdgeP1 = center + linearizedEdgeCenter - linearizedEdgeTangent * pixelArea * 10.0;
		float2 linearizedEdgeP2 = center + linearizedEdgeCenter + linearizedEdgeTangent * pixelArea * 10.0;
		float intersectArea = calculateThickLineAABBIntersectionArea(linearizedEdgeP1, linearizedEdgeP2, w, pixelMin, pixelMax);
		mask = intersectArea / pixelArea;
	}
	else if (op == 14) { // DrawLine
		float2 p1 = in.args.xy;
		float2 p2 = in.texCoord;
		float w = in.args.w;
		float intersectArea = calculateRoundedThickLineAABBIntersectionArea(p1, p2, w, pixelMin, pixelMax);
		mask = intersectArea / pixelArea;
	}
	else {
		for (int i = 0; i < 4; i++) {
	        float2 offset = aaOffsets4[i].x * dx + aaOffsets4[i].y * dy;
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
		mask = mask / 4.0;
	}
	mask = max(0.0, min(1.0, sqrt(mask)));
	if (mask < 0.004) {
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
			var newPadding = 2;
			var paddedWidth = width + newPadding * 2;
			var paddedHeight = height + newPadding * 2;
			var subRect = Alloc (paddedWidth, paddedHeight);
			if (subRect is null) {
				return null;
			}
			var xscale = subRect.Width / paddedWidth;
			var yscale = subRect.Height / paddedHeight;
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
			cgContext.ScaleCTM (xscale, yscale);
			cgContext.TranslateCTM (newPadding, newPadding);
			draw (cgContext);
			cgContext.Flush ();
			var data = cgContext.Data;
			if (data != IntPtr.Zero) {
				var region = MTLRegion.Create2D ((nuint)subRect.X, (nuint)subRect.Y, (nuint)subRect.Width,
					(nuint)subRect.Height);
				Texture.ReplaceRegion (region: region, level: 0, pixelBytes: data, bytesPerRow: (nuint)bytesPerRow);
			}
			var uMin = (subRect.X + newPadding * xscale) / MaxWidth;
			var vMin = (subRect.Y + newPadding * yscale) / MaxHeight;
			var du = (subRect.Width - 2 * newPadding * xscale) / MaxWidth;
			var dv = (subRect.Height - 2 * newPadding * yscale) / MaxHeight;
			var uvbb = new Vector4 (uMin, vMin, uMin + du, vMin + dv);
			return new SdfTextureRegion (TextureIndex, uvbb, new Vector2 (width, height), subRect);
		}
	}

	readonly struct SdfKey
	{
		public readonly string Text;
		public readonly string Font;
		public readonly int RenderSize;
		public SdfKey (string text, string font, int renderSize)
		{
			Text = text;
			Font = font;
			RenderSize = renderSize;
		}
		public override bool Equals (object? obj)
		{
			if (obj is SdfKey key) {
				return key.Text == Text && key.Font == Font && key.RenderSize == RenderSize;
			}
			return false;
		}
		public override int GetHashCode ()
		{
			return HashCode.Combine (Text, Font, RenderSize);
		}
	}

	class SdfKeyComparer : IEqualityComparer<SdfKey>
	{
		public static readonly SdfKeyComparer Shared = new SdfKeyComparer ();
		public bool Equals (SdfKey x, SdfKey y)
		{
			return x.Text == y.Text && x.Font == y.Font && x.RenderSize == y.RenderSize;
		}
		public int GetHashCode (SdfKey obj)
		{
			return HashCode.Combine (obj.Text, obj.Font, obj.RenderSize);
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

		readonly Dictionary<string, Dictionary<int, CTStringAttributes>> _cachedStringAttributes = new ();

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
		
		public SdfTextureRegion? FindExistingSdfTextureRegion (string text, string font, int renderFontSize)
		{
			var key = new SdfKey (text, font, renderFontSize);
			if (_sdfValues.TryGetValue (key, out var value)) {
				value.LastUsedFrame = _frame;
				return value;
			}
			return null;
		}

		public SdfTextureRegion? DrawSdfTextureRegion (string text, string font, int renderFontSize, float width, float height, Action<CGContext> draw)
		{
			var key = new SdfKey (text, font, renderFontSize);
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

		public CTStringAttributes GetCTStringAttributes(string font, int renderFontSize)
		{
			if (!_cachedStringAttributes.TryGetValue (font, out var attrs)) {
				attrs = new Dictionary<int, CTStringAttributes> ();
				_cachedStringAttributes[font] = attrs;
			}
			if (attrs.TryGetValue (renderFontSize, out var a)) {
				return a;
			}
			a = new CTStringAttributes {
				ForegroundColorFromContext = true,
				Font = new CTFont (font, renderFontSize),
			};
			attrs[renderFontSize] = a;
			return a;
		}
	}
}
