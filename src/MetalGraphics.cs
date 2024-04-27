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
				BlendingEnabled = false,
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
			pipelineDescriptor.VertexDescriptor = vdesc;
			// 	new MTLVertexAttributeDescriptor {
			// 		BufferIndex = 0,
			// 		Format = MTLVertexFormat.Float2,
			// 		Offset = 2 * sizeof (float),
			// 	},
			// 	new MTLVertexAttributeDescriptor {
			// 		BufferIndex = 0,
			// 		Format = MTLVertexFormat.Float4,
			// 		Offset = 4 * sizeof (float),
			// 	},
			// };
			var pipeline = device.CreateRenderPipelineState (pipelineDescriptor, error: out error);
			if (error is not null) {
				throw new NSErrorException (error);
			}
			return pipeline;
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
			DoRect (x, y, width, height, 0, true);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			DoRect (x, y, width, height, w, false);
		}

		public const int VertexPositionByteSize = 2 * sizeof (float);
		public const int VertexUvByteSize = 2 * sizeof (float);
		public const int VertexColorByteSize = 4 * sizeof (float);
		public const int VertexByteSize = VertexPositionByteSize + VertexUvByteSize + VertexColorByteSize;

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
			public int AddVertex (float x, float y, float u, float v, ValueColor color)
			{
				var index = NumVertices;
				if (VertexBuffer is not null) {
					unsafe {
						var p = (float*)VertexBuffer.Contents;
						p += index * VertexByteSize / sizeof(float);
						p[0] = x * 5.0e-4f;
						p[1] = y * 5.0e-4f;
						p[2] = u;
						p[3] = v;
						p[4] = color.Red / 255f;
						p[5] = color.Green / 255f;
						p[6] = color.Blue / 255f;
						p[7] = color.Alpha / 255f;
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

		public class Buffers
		{
			public IMTLDevice Device;
			readonly List<Buffer> buffers;
			public List<Buffer> All => buffers;
			int currentBufferIndex = 0;
			public Buffers (IMTLDevice device)
			{
				Device = device;
				buffers = new List<Buffer> ();
				buffers.Add (new Buffer (Device));
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

		public void DoRect (float x, float y, float width, float height, float w, bool fill)
		{
			var buffer = _buffers.GetBuffer (4, 6);
			var bb = BoundingBox.FromRect (x, y, width, height, w);
			var v0 = buffer.AddVertex(bb.MinX, bb.MinY, 0, 0, _currentColor);
			var v1 = buffer.AddVertex(bb.MaxX, bb.MinY, 1, 0, _currentColor);
			var v2 = buffer.AddVertex(bb.MaxX, bb.MaxY, 1, 1, _currentColor);
			var v3 = buffer.AddVertex(bb.MinX, bb.MaxY, 0, 1, _currentColor);
			buffer.AddTriangle(v0, v1, v2);
			buffer.AddTriangle(v2, v3, v0);
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

		public void EndDrawing ()
		{
			if (_pipeline.Value is {} pipeline) {
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

		const string MetalCode = @"
typedef struct
{
    float2 position [[attribute(0)]];
    float2 texCoord [[attribute(1)]];
	float4 color [[attribute(2)]];
} Vertex;

typedef struct
{
    float4 position [[position]];
    float2 texCoord;
    float4 color;
} ColorInOut;

vertex ColorInOut vertexShader(Vertex in [[stage_in]])
{
	ColorInOut out;
	out.position = float4(in.position, 1.0);
	out.texCoord = in.texCoord;
	out.color = in.color;
	return out;
}

fragment float4 fragmentShader(ColorInOut in [[stage_in]])
{
	return in.color;
}
";
	}
}
