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

		struct State
		{
			public Matrix3x2 Transform;
		}

		readonly List<State> _states = new List<State> () { new State () { Transform = Matrix3x2.Identity } };

		readonly MetalGraphicsBuffers _buffers;
		readonly IMTLRenderCommandEncoder _renderEncoder;
		static readonly Lazy<IMTLRenderPipelineState?> _pipeline;

		Matrix4x4 _modelToViewport = Matrix4x4.Identity;

		static MetalGraphics ()
		{
			_pipeline = new Lazy<IMTLRenderPipelineState?> (() => {
				var device = MTLDevice.SystemDefault;
				return device != null ? CreatePipeline (device) : null;
			});
		}

		public MetalGraphics (IMTLRenderCommandEncoder renderEncoder, MetalGraphicsBuffers buffers)
		{
			_renderEncoder = renderEncoder ?? throw new ArgumentNullException (nameof(renderEncoder));
			_buffers = buffers ?? throw new ArgumentNullException (nameof(buffers));
		}

		public void SetViewport (float viewWidth, float viewHeight, float modelToViewScale, float modelToViewTranslationX, float modelToViewTranslationY)
		{
			var t = Matrix4x4.CreateTranslation (modelToViewTranslationX, modelToViewTranslationY, 0);
			var s = Matrix4x4.CreateScale (modelToViewScale, modelToViewScale, 1);
			var modelToView = s * t;
			// Now calculate the viewport transform
			// View is (0,0) to (viewWidth, viewHeight)
			// Viewport is (-1,-1) to (1,1)
			var viewToViewport = Matrix4x4.CreateScale (2 / viewWidth, -2 / viewHeight, 1) * Matrix4x4.CreateTranslation (-1, 1, 0);
			_modelToViewport = modelToView * viewToViewport;
		}

		public void BeginEntity (object entity)
		{
		}

		public IFontMetrics GetFontMetrics ()
		{
			return new NullGraphicsFontMetrics (_currentFont.Size, isBold: _currentFont.IsBold);
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

		void DoRect (float x, float y, float width, float height, float w, DrawOp op, float argy = 0)
		{
			var buffer = _buffers.GetPrimitivesBuffer(numVertices: 4, numIndices: 6);
			var bb = BoundingBox.FromRect (x, y, width, height, w);
			var bbv = new Vector4 (bb.MinX, bb.MinY, bb.MaxX, bb.MaxY);
			var args = new Vector4 (w, argy, 0, 0);
			var v0 = buffer.AddVertex(bb.MinX, bb.MinY, 0, 0, _currentColor, bb: bbv, args: args, op: op);
			var v1 = buffer.AddVertex(bb.MaxX, bb.MinY, 1, 0, _currentColor, bb: bbv, args: args, op: op);
			var v2 = buffer.AddVertex(bb.MaxX, bb.MaxY, 1, 1, _currentColor, bb: bbv, args: args, op: op);
			var v3 = buffer.AddVertex(bb.MinX, bb.MaxY, 0, 1, _currentColor, bb: bbv, args: args, op: op);
			buffer.AddTriangle(v0, v1, v2);
			buffer.AddTriangle(v2, v3, v0);
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
			DoRect (x, y, width, height, w, DrawOp.StrokeRoundedRect);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			DoRect (x, y, width, height, 0, DrawOp.FillOval);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			DoRect (x, y, width, height, w, DrawOp.StrokeOval);
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			DoRect (cx - radius, cy - radius, radius * 2, radius * 2, 0, DrawOp.FillArc);
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			DoRect (cx - radius, cy - radius, radius * 2, radius * 2, w, DrawOp.StrokeArc);
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
			var nx = dy / len;
			var ny = -dx / len;
			var w2 = w / 2;
			var bbv = new Vector4 (Math.Min (sx, ex) - w2, Math.Min (sy, ey) - w2, Math.Max (sx, ex) + w2, Math.Max (sy, ey) + w2);
			var args = new Vector4 (w, 0, 0, 0);
			var v0 = buffer.AddVertex (sx - nx * w2, sy - ny * w2, 0, 0, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			var v1 = buffer.AddVertex (sx + nx * w2, sy + ny * w2, 1, 0, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			var v2 = buffer.AddVertex (ex + nx * w2, ey + ny * w2, 1, 1, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
			var v3 = buffer.AddVertex (ex - nx * w2, ey - ny * w2, 0, 1, _currentColor, bb: bbv, args: args, op: DrawOp.DrawLine);
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
			var font = _currentFont.FontFamily;
			var regionO = _buffers.FindExistingSdfTextureRegion (s, font);

			var renderFontSize = (nfloat)16.0;

			if (regionO is null) {
				const double maxLength = 2048.0;
				const int maxTries = 3;
				nfloat ascent = 0;
				nfloat descent = 0;
				nfloat leading = 0;
				double len = 0;
				CTLine? drawLine = null;
				for (var tri = 0; tri < maxTries; tri++) {
					using var atext = new NSMutableAttributedString (s, new CTStringAttributes {
						ForegroundColorFromContext = true,
						// StrokeColor = _whiteCGColor,
						// ForegroundColor = _whiteCGColor,
						Font = new CTFont (_currentFont.FontFamily, renderFontSize),
					});
					using var l = new CTLine (atext);
					len = l.GetTypographicBounds (out ascent, out descent, out leading);
					if (len > maxLength) {
						renderFontSize *= (nfloat)(maxLength / len * 0.98);
					}
					else {
						drawLine = l;
						break;
					}
				}
				if (drawLine is null) {
					return;
				}

				var drawWidth = (float)len;
				var drawHeight = (float)renderFontSize;
				regionO = _buffers.DrawSdfTextureRegion (s, font, drawWidth, drawHeight, (cgContext) => {
					// cgContext.SetFillColor ((nfloat)0.5, (nfloat)0.5, (nfloat)0.5, 1);
					// if (s.Length > 5) {
					// 	cgContext.FillEllipseInRect (new CGRect (0, 0, drawWidth, drawHeight));
					// }
					// else {
					// 	cgContext.FillRect (new CGRect (0, 0, drawWidth, drawHeight));
					// }
					cgContext.SetFillColor (1, 1, 1, 1);
					// cgContext.SetStrokeColor (1, 1, 1, 1);
					// cgContext.SetLineWidth (1);
					// cgContext.SetTextDrawingMode (CGTextDrawingMode.Fill);
					cgContext.TextPosition = new CGPoint (0, renderFontSize * 0.15);
					cgContext.SelectFont ("Helvetica", renderFontSize, CGTextEncoding.MacRoman);
					cgContext.ShowText (s);
					// drawLine.Draw (cgContext);
				});
			}

			if (regionO is SdfTextureRegion region) {
				var fontScale = (float)(_currentFont.Size / renderFontSize);
				var width = region.DrawSize.X * fontScale;
				var height = region.DrawSize.Y * fontScale;
				var buffer = _buffers.GetPrimitivesBuffer(numVertices: 4, numIndices: 6);
				var bb = BoundingBox.FromSafeRect (x, y, width, height);
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
			if (_pipeline.Value is {} pipeline) {
				_renderEncoder.SetRenderPipelineState (pipeline);

				_buffers.SetUniforms (_modelToViewport);
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
	float2 center = (bbMin + bbMax) / 2;
	float2 outerRadius = (bbMax - bbMin) / 2 - float2(2.0*w, 2.0*w);
	float2 innerRadius = outerRadius - float2(w, w);
	float2 d = (p - center);
	float r = length(d);
	bool onedge = r >= length(innerRadius) && r <= length(outerRadius);
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

float drawString(ColorInOut in, texture2d<float> sdf)
{
    float2 uv = in.texCoord;
	float mask = sdf.sample(sdfSampler, uv).r;
	return mask;
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

fragment float4 fragmentShader(
	ColorInOut in [[stage_in]],
	texture2d<float> sdf0 [[ texture(0) ]])
{
    uint op = in.op;
    float mask = 0.0;
	switch (op) {
	case 0: // FillRect
		mask = fillRect(in);
		break;
	case 2: // FillRoundedRect
		mask = fillRoundedRect(in);
		break;
	case 4: // FillOval
		mask = fillOval(in);
		break;
	case 5: // StrokeOval
		mask = strokeOval(in);
		break;
	case 6: // FillArc
		mask = fillRect(in);
		break;
	case 13: // DrawString
		mask = drawString(in, sdf0);
		break;
	case 14: // DrawLine
		mask = fillRect(in);
		break;
	default:
		mask = strokeRect(in);
		break;
	}
	return float4(in.color.xyz, in.color.w * mask);
}
";
	}

	public class MetalPrimitivesBuffer
	{
		public readonly IMTLDevice Device;
		public readonly IMTLBuffer? VertexBuffer;
		public readonly IMTLBuffer? IndexBuffer;
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
			IndexBuffer = Device.CreateBuffer ((nuint)(MaxIndices * sizeof (ushort)), MTLResourceOptions.CpuCacheModeDefault);
		}
		public void Reset ()
		{
			NumVertices = 0;
			NumIndices = 0;
		}
		public int AddVertex (float x, float y, float u, float v, ValueColor color, Vector4 bb, Vector4 args, MetalGraphics.DrawOp op)
		{
			var index = NumVertices;
			if (VertexBuffer is not null) {
				unsafe {
					var p = (float*)VertexBuffer.Contents;
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
			if (IndexBuffer is not null) {
				unsafe {
					var p = (ushort*)IndexBuffer.Contents;
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

		class SubRect
		{
			public readonly int X;
			public readonly int Y;
			public readonly int Width;
			public readonly int Height;
			public SubRect (int x, int y, int width, int height)
			{
				X = x;
				Y = y;
				Width = width;
				Height = height;
			}
			public Vector4 UVBoundingBox => new Vector4 (X / (float)MaxWidth, Y / (float)MaxHeight, (X + Width) / (float)MaxWidth, (Y + Height) / (float)MaxHeight);
		}
		readonly List<SubRect> _free = new List<SubRect> { new SubRect (0, 0, MaxWidth, MaxHeight) };

		public IMTLTexture? Texture { get; }

		// private const MTLPixelFormat TexturePixelFormat = MTLPixelFormat.R32Float;
		private const MTLPixelFormat TexturePixelFormat = MTLPixelFormat.R8Unorm;

		public MetalSdfTexture (IMTLDevice device)
		{
			var tdesc = MTLTextureDescriptor.CreateTexture2DDescriptor (TexturePixelFormat, (nuint)MaxWidth,
				(nuint)MaxHeight, mipmapped: false);
			Texture = device.CreateTexture (tdesc);
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
					if (freeRect.Height > idesiredHeight) {
						_free.Insert (0, new SubRect (freeRect.X, freeRect.Y + idesiredHeight, freeRect.Width, freeRect.Height - idesiredHeight));
					}
					if (freeRect.Width > idesiredWidth) {
						_free.Insert (0, new SubRect (freeRect.X + idesiredWidth, freeRect.Y, freeRect.Width - idesiredWidth, idesiredHeight));
					}
					return new SubRect (freeRect.X, freeRect.Y, idesiredWidth, idesiredHeight);
				}
			}
			return null;
		}

		public Vector4? AllocAndDraw (float width, float height, Action<CGContext> draw)
		{
			if (Texture is null) {
				return null;
			}
			var subRect = Alloc (width, height);
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
			using var cs = CGColorSpace.CreateDeviceGray ();
			using var cgContext = new CGBitmapContext (null, subRect.Width, subRect.Height, bitsPerComponent, bytesPerRow, cs, bitmapFlags);
			// cgContext.TranslateCTM (0, subRect.Height);
			// cgContext.ScaleCTM (1, -1);
			draw (cgContext);
			cgContext.Flush ();
			var data = cgContext.Data;
			if (data != IntPtr.Zero) {
				var region = MTLRegion.Create2D ((nuint)subRect.X, (nuint)subRect.Y, (nuint)subRect.Width,
					(nuint)subRect.Height);
				Texture.ReplaceRegion (region: region, level: 0, pixelBytes: data, bytesPerRow: (nuint)bytesPerRow);
			}
			return subRect.UVBoundingBox;
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

	public readonly struct SdfTextureRegion
	{
		public readonly int TextureIndex;
		public readonly Vector4 UVBoundingBox;
		public readonly Vector2 DrawSize;
		public SdfTextureRegion (int textureIndex, Vector4 uv, Vector2 drawSize)
		{
			TextureIndex = textureIndex;
			UVBoundingBox = uv;
			DrawSize = drawSize;
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
		public IMTLBuffer? Uniforms => _uniformsBuffer;

		public MetalGraphicsBuffers (IMTLDevice device)
		{
			Device = device;
			_primitiveBuffers = new List<MetalPrimitivesBuffer> { new MetalPrimitivesBuffer (Device) };
			_sdfTextures = new List<MetalSdfTexture> { new MetalSdfTexture (Device) };
			_uniformsBuffer = Device.CreateBuffer ((nuint)(MetalGraphics.UniformsByteSize), MTLResourceOptions.CpuCacheModeDefault);
		}

		public void Reset ()
		{
			foreach (var b in _primitiveBuffers) {
				b.Reset ();
			}
			_currentPrimitiveBufferIndex = 0;
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
				return value;
			}
			return null;
		}

		public SdfTextureRegion? DrawSdfTextureRegion (string text, string font, float width, float height, Action<CGContext> draw)
		{
			var key = new SdfKey (text, font);
			var textureIndex = 0;
			var texture = _sdfTextures[textureIndex];
			var uvO = texture.AllocAndDraw (width, height, draw);
			if (uvO is Vector4 uv) {
				var region = new SdfTextureRegion (textureIndex, uv, new Vector2 (width, height));
				_sdfValues[key] = region;
				return region;
			}
			return null;
		}

		public void SetUniforms (Matrix4x4 modelToView)
		{
			if (_uniformsBuffer is not null) {
				unsafe {
					var p = (float*)_uniformsBuffer.Contents;
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
	}
}
