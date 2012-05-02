//
// Copyright (c) 2012 Frank A. Krueger
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
using Android.Graphics;
using CrossGraphics.OpenGL;

namespace CrossGraphics.Android
{
	public class AndroidShapeStore : OpenGLShapeStore
	{
		string _packageName;
		string _storeName;

		public AndroidShapeStore (string packageName, string storeName)
		{
			_packageName = packageName;
			_storeName = storeName;
		}

		class BitmapTexture : OpenGLTexture
		{
			Bitmap _bmp;

			public BitmapTexture (string path)
				: base (0, 0)
			{
				using (var ibmp = BitmapFactory.DecodeFile (path)) {
					_bmp = ibmp.Copy (Bitmap.Config.Alpha8, true);
				}
				Width = _bmp.Width;
				Height = _bmp.Height;
			}

			public BitmapTexture (int width, int height)
				: base (width, height)
			{
				_bmp = Bitmap.CreateBitmap (width, height, Bitmap.Config.Alpha8);
			}

			protected override void CallTexImage2D ()
			{
				using (var dst = Java.Nio.ByteBuffer.AllocateDirect (Width * Height)) {
					_bmp.CopyPixelsToBuffer (dst);
					var ptr = dst.GetDirectBufferAddress ();
					TexImage2D (ptr);
				}
			}

			public void WritePng (string path)
			{
				using (var bigBmp = _bmp.Copy (Bitmap.Config.Argb8888, false)) {
					using (var s = System.IO.File.Create (path)) {
						bigBmp.Compress (Bitmap.CompressFormat.Png, 100, s);
					}
				}
			}

			public override IGraphics BeginRendering ()
			{
				return new AndroidGraphics (new Canvas (_bmp));
			}

			public override void EndRendering (IGraphics g)
			{
				base.EndRendering (g);
				var dg = g as AndroidGraphics;
				if (dg != null) {
					dg.Canvas.Dispose ();
				}
			}
		}

		protected override int MaxTextureSize { get { return 1024; } }

		protected override OpenGLTexture CreateTexture (int width, int height)
		{
			return new BitmapTexture (width, height);
		}

		string _shapeStorePath;
		protected override string ShapeStorePath
		{
			get {
				if (_shapeStorePath == null) {
					var externalPath = global::Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
					_shapeStorePath = System.IO.Path.Combine (
						externalPath,
						"Android",
						"data",
						_packageName,
						"cache",
						_storeName);
				}
				return _shapeStorePath;
			}
		}

		protected override OpenGLTexture ReadTexture (int id)
		{
			var path = System.IO.Path.Combine (ShapeStorePath, "Texture" + id + ".png");
			if (System.IO.File.Exists (path)) {
				return new BitmapTexture (path);
			}
			else {
				return null;
			}
		}

		protected override void WriteTexture (int id, OpenGLTexture texture)
		{
			var bmp = (BitmapTexture)texture;
			var path = System.IO.Path.Combine (ShapeStorePath, "Texture" + id + ".png");
			bmp.WritePng (path);
		}
	}
}
