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

		readonly Buffers _buffers;
		readonly IMTLRenderCommandEncoder _renderEncoder;
		static readonly Lazy<IMTLRenderPipelineState?> _pipeline;

		static MetalGraphics ()
		{
			_pipeline = new Lazy<IMTLRenderPipelineState?> (() => {
				var device = MTLDevice.SystemDefault;
				return device != null ? CreatePipeline (device) : null;
			});
		}

		public MetalGraphics (IMTLDevice device, IMTLRenderCommandEncoder renderEncoder, Buffers buffers)
		{
			_renderEncoder = renderEncoder;
			_buffers = buffers;
		}

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

		public const int VertexPositionByteSize = 2 * sizeof (float);
		public const int VertexUvByteSize = 2 * sizeof (float);
		public const int VertexColorByteSize = 4 * sizeof (float);
		public const int VertexBBByteSize = 4 * sizeof (float);
		public const int VertexArgsByteSize = 4 * sizeof (float);
		public const int VertexOpByteSize = 1 * sizeof (uint);
		public const int VertexByteSize = VertexPositionByteSize + VertexUvByteSize + VertexColorByteSize + VertexBBByteSize + VertexArgsByteSize + VertexOpByteSize;

		public const int UniformModelToViewByteSize = 16 * sizeof (float);
		public const int UniformByteSize = UniformModelToViewByteSize;

		public class Buffer
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
			public Buffer (IMTLDevice device)
			{
				Device = device;
				VertexBuffer = Device.CreateBuffer ((nuint)(MaxVertices * VertexByteSize), MTLResourceOptions.CpuCacheModeDefault);
				IndexBuffer = Device.CreateBuffer ((nuint)(MaxIndices * sizeof (ushort)), MTLResourceOptions.CpuCacheModeDefault);
			}
			public void Reset ()
			{
				NumVertices = 0;
				NumIndices = 0;
			}
			public int AddVertex (float x, float y, float u, float v, ValueColor color, Vector4 bb, Vector4 args, DrawOp op)
			{
				var index = NumVertices;
				if (VertexBuffer is not null) {
					unsafe {
						var p = (float*)VertexBuffer.Contents;
						p += index * VertexByteSize / sizeof(float);
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
		Matrix4x4 _modelToViewport = Matrix4x4.Identity;

		public class Buffers
		{
			public IMTLDevice Device;
			readonly List<Buffer> buffers;
			public List<Buffer> All => buffers;
			int currentBufferIndex = 0;
			readonly IMTLBuffer? _uniformBuffer;
			public IMTLBuffer? Uniforms => _uniformBuffer;
			public Buffers (IMTLDevice device)
			{
				Device = device;
				buffers = new List<Buffer> ();
				buffers.Add (new Buffer (Device));
				_uniformBuffer = Device.CreateBuffer ((nuint)(UniformByteSize), MTLResourceOptions.CpuCacheModeDefault);
			}
			public Buffer GetBuffer (int numVertices, int numIndices)
			{
				var b = buffers[currentBufferIndex];
				if (b.RemainingVertices >= numVertices && b.RemainingIndices >= numIndices) {
					return b;
				}
				var buffer = new Buffer (Device);
				buffers.Add (buffer);
				currentBufferIndex++;
				return buffer;
			}
			public void Reset ()
			{
				foreach (var b in buffers) {
					b.Reset ();
				}
				currentBufferIndex = 0;
			}
			public void SetUniforms (Matrix4x4 modelToView)
			{
				if (_uniformBuffer is not null) {
					unsafe {
						var p = (float*)_uniformBuffer.Contents;
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
			override public string ToString ()
			{
				return $"BB({MinX}, {MinY}, {MaxX}, {MaxY})";
			}
		}

		void DoRect (float x, float y, float width, float height, float w, DrawOp op)
		{
			var buffer = _buffers.GetBuffer (4, 6);
			var bb = BoundingBox.FromRect (x, y, width, height, w);
			var bbv = new Vector4 (bb.MinX, bb.MinY, bb.MaxX, bb.MaxY);
			var args = new Vector4 (w, 0, 0, 0);
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
			DoRect (x, y, width, height, 0, DrawOp.FillRoundedRect);
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
			var buffer = _buffers.GetBuffer (4, 6);
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
			// TODO: Implement
		}

		public void DrawString (string s, float x, float y)
		{
			// TODO: Implement
		}

		public void EndDrawing ()
		{
			if (_pipeline.Value is {} pipeline) {
				_buffers.SetUniforms (_modelToViewport);
				if (_buffers.Uniforms is not null) {
					_renderEncoder.SetVertexBuffer (_buffers.Uniforms, 0, 1);
				}
				foreach (var buffer in _buffers.All) {
					if (buffer.NumIndices <= 0) {
						break;
					}

					if (buffer.VertexBuffer is null || buffer.IndexBuffer is null) {
						continue;
					}

					_renderEncoder.SetRenderPipelineState (pipeline);
					_renderEncoder.SetVertexBuffer (buffer.VertexBuffer, 0, 0);
					_renderEncoder.DrawIndexedPrimitives (MTLPrimitiveType.Triangle, (nuint)buffer.NumIndices, MTLIndexType.UInt16, buffer.IndexBuffer, 0);
				}
			}
			_buffers.Reset ();
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

fragment float4 fragmentShader(ColorInOut in [[stage_in]])
{
    uint op = in.op;
    float mask = 0.0;
	switch (op) {
	case 0: // FillRect
	case 2: // FillRoundedRect
	case 4: // FillOval
	case 6: // FillArc
		mask = fillRect(in);
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
}
