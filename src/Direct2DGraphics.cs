//
// Copyright (c) 2013 Frank A. Krueger
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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SharpDX.Direct2D1;
using SharpDX;

namespace CrossGraphics
{
	class WindowsDrawTimer
	{
		int _fps = 20;
		readonly DispatcherTimer _drawTimer;

		const double CpuUtilization = 0.25;
		public int MinFps { get; set; }
		public int MaxFps { get; set; }
		static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds (1.0);

		double _drawTime;
		int _drawCount;
		DateTime _lastThrottleTime = DateTime.Now;

		public bool Continuous { get; set; }

		public WindowsDrawTimer ()
		{
			MinFps = 4;
			MaxFps = 30;
			Continuous = true;

			_drawTimer = new DispatcherTimer ();
			_drawTimer.Tick += DrawTick;
		}

		public void Start ()
		{
			_drawTimer.Interval = TimeSpan.FromSeconds (1.0 / _fps);

			if (Continuous && !_drawTimer.IsEnabled) {
				_drawTimer.Start ();
			}
		}

		bool _paused = false;
		public bool Paused
		{
			get
			{
				return _paused;
			}
			set
			{
				if (_paused != value) {
					_paused = value;
					_lastThrottleTime = DateTime.Now;
				}
			}
		}

		public void Stop ()
		{
			_drawTimer.Stop ();
		}

		public Func<bool> ShouldDrawFunc { get; set; }
		public Func<double> DrawFunc { get; set; }

		void DrawTick (object sender, object e)
		{
			//
			// Decide if we should draw
			//
			if (Paused) {
				_drawCount = 0;
				_drawTime = 0;
				return;
			}

			var sd = ShouldDrawFunc;
			if (sd != null) {
				if (!sd ()) {
					return;
				}
			}

			//
			// Draw
			//
			var d = DrawFunc;
			if (d != null) {
				var dt = d ();
				_drawTime += dt;
				_drawCount++;
			}

			//
			// Throttle
			//
			if (_drawCount > 2 && (DateTime.Now - _lastThrottleTime) >= ThrottleInterval) {

				_lastThrottleTime = DateTime.Now;

				var maxfps = 1.0 / (_drawTime / _drawCount);
				_drawTime = 0;
				_drawCount = 0;

				var fps = ClampUpdateFreq ((int)(maxfps * CpuUtilization));

				if (Math.Abs (fps - _fps) > 1) {
					_fps = fps;
					Start ();
				}
			}
		}

		int ClampUpdateFreq (int fps)
		{
			return Math.Min (MaxFps, Math.Max (MinFps, fps));
		}
	}

	public class Direct2DGraphics : IGraphics
	{
		const int MaxFontSize = 120;
		Direct2DGraphicsFontMetrics[] _fontMetrics;
		int _fontSize = 10;

		Color lastColor;

		Factory d2dFactory;
		SharpDX.DirectWrite.Factory dwFactory;

		RenderTarget dc;

		SharpDX.DXGI.Surface surface;
		SharpDX.WIC.Bitmap bitmap;

		StrokeStyle strokeStyle;
		SharpDX.DirectWrite.TextFormat textFormat;

		class State
		{
			public Matrix3x2 Transform;
		}
		Stack<State> states;

		Direct2DGraphics ()
		{
			d2dFactory = DisposeLater (new Factory (FactoryType.MultiThreaded));
			dwFactory = DisposeLater (new SharpDX.DirectWrite.Factory ());
		}

		public Direct2DGraphics (SharpDX.DXGI.SwapChain swapChain)
			: this ()
		{
			surface = DisposeLater (SharpDX.DXGI.Surface.FromSwapChain (swapChain, 0));
			dc = DisposeLater (new RenderTarget (
				d2dFactory,
				surface,
				new RenderTargetProperties (
					RenderTargetType.Default,
					new SharpDX.Direct2D1.PixelFormat (SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
					d2dFactory.DesktopDpi.Width, d2dFactory.DesktopDpi.Height,
					RenderTargetUsage.None,
					FeatureLevel.Level_DEFAULT)));

			Initialize ();
		}

		public Direct2DGraphics (int width, int height)
			: this ()
		{
			var wicFactory = new SharpDX.WIC.ImagingFactory2 ();
			bitmap = DisposeLater (new SharpDX.WIC.Bitmap (
				wicFactory,
				width, height,
				SharpDX.WIC.PixelFormat.Format32bppPBGRA,
				SharpDX.WIC.BitmapCreateCacheOption.CacheOnDemand));
			dc = DisposeLater (new WicRenderTarget (
				d2dFactory,
				bitmap,
				new RenderTargetProperties (
					RenderTargetType.Default,
					new SharpDX.Direct2D1.PixelFormat (SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
					d2dFactory.DesktopDpi.Width, d2dFactory.DesktopDpi.Height,
					RenderTargetUsage.None,
					FeatureLevel.Level_DEFAULT)));

			Initialize ();
		}

		void Initialize ()
		{
			strokeStyle = DisposeLater (new StrokeStyle (d2dFactory, new StrokeStyleProperties {
				EndCap = CapStyle.Round,
				StartCap = CapStyle.Round,
			}));

			states = new Stack<State> ();
			states.Push (new State {
				Transform = Matrix3x2.Identity,
			});

			lastColor = Colors.Black;

			SetFont (Font.SystemFontOfSize (16));
		}

		~Direct2DGraphics ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		List<IDisposable> toDispose;

		T DisposeLater<T> (T d) where T : IDisposable
		{
			if (toDispose == null) toDispose = new List<IDisposable> ();
			toDispose.Add (d);
			return d;
		}

		protected virtual void Dispose (bool disposing)
		{
			if (toDispose == null) return;
			toDispose.Reverse ();
			foreach (var d in toDispose) {
				if (d != null) {
					d.Dispose ();
				}
			}
			toDispose.Clear ();
			toDispose = null;

			if (textFormat != null) {
				textFormat.Dispose ();
				textFormat = null;
			}
		}

		public byte[] GetPixels ()
		{
			if (bitmap == null) {
				throw new InvalidOperationException ("Cannot GetPixels on SwapChain graphics");
			}

			var width = bitmap.Size.Width;
			var numBytes = width * bitmap.Size.Height * 4;
			var bytes = new byte[numBytes];
			unsafe {
				fixed (byte* pb = bytes) {
					bitmap.CopyPixels (width * 4, new IntPtr (pb), numBytes);
				}
			}
			return bytes;
		}

		public void BeginDrawing ()
		{
			dc.BeginDraw ();
		}

		public void Clear (Color clearColor)
		{
			dc.Clear (new Color4 (clearColor.RedValue, clearColor.GreenValue, clearColor.BlueValue, clearColor.AlphaValue));
		}

		public void EndDrawing ()
		{
			dc.EndDraw ();
		}

		public void SetFont (Font f)
		{
			if (textFormat != null) {
				textFormat.Dispose ();
			}
			textFormat = new SharpDX.DirectWrite.TextFormat (dwFactory, "Segoe", f.Size);
			_fontSize = f.Size;
		}

		public void SetColor (Color c)
		{
			lastColor = c;
		}

		public void FillPolygon (Polygon poly)
		{
			var g = poly.GetGeometry (d2dFactory);
			dc.FillGeometry (g, lastColor.GetBrush (dc));
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			var g = poly.GetGeometry (d2dFactory);
			dc.DrawGeometry (g, lastColor.GetBrush (dc), w);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			dc.FillRoundedRectangle (new RoundedRectangle {
				Rect = new RectangleF (x, y, x + width, y + height),
				RadiusX = radius,
				RadiusY = radius,
			}, lastColor.GetBrush (dc));
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			dc.DrawRoundedRectangle (new RoundedRectangle {
				Rect = new RectangleF (x, y, x + width, y + height),
				RadiusX = radius,
				RadiusY = radius,
			}, lastColor.GetBrush (dc), w);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			dc.FillRectangle (new RectangleF (x, y, x + width, y + height), lastColor.GetBrush (dc));
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			dc.DrawRectangle (new RectangleF (x, y, x + width, y + height), lastColor.GetBrush (dc), w);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var rx = width / 2;
			var ry = height / 2;
			dc.FillEllipse (new Ellipse (new DrawingPointF (x + rx, y + ry), rx, ry), lastColor.GetBrush (dc));
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var rx = width / 2;
			var ry = height / 2;
			dc.DrawEllipse (new Ellipse (new DrawingPointF (x + rx, y + ry), rx, ry), lastColor.GetBrush (dc), w);
		}

		public void BeginLines (bool rounded)
		{
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			dc.DrawLine (new DrawingPointF (sx, sy), new DrawingPointF (ex, ey), lastColor.GetBrush (dc), w, strokeStyle);
		}

		public void EndLines ()
		{
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
		}

		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
		}

		const float FontOffsetScale = 0.25f;

		public void DrawString (string s, float x, float y)
		{
			var yy = y - FontOffsetScale * _fontSize;

			dc.DrawText (
				s,
				textFormat,
				new RectangleF (x, yy, x + 1000, yy + 1000),
				lastColor.GetBrush (dc));
		}

		public void DrawString (string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			var yy = y - FontOffsetScale * _fontSize;

			dc.DrawText (
				s,
				textFormat,
				new RectangleF (x, yy, x + width, yy + height),
				lastColor.GetBrush (dc));
		}

		public IFontMetrics GetFontMetrics ()
		{
			if (_fontMetrics == null) {
				_fontMetrics = new Direct2DGraphicsFontMetrics[MaxFontSize + 1];
			}
			var i = Math.Min (_fontMetrics.Length, _fontSize);
			if (_fontMetrics[i] == null) {
				_fontMetrics[i] = new Direct2DGraphicsFontMetrics (i);
			}
			return _fontMetrics[i];
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
		}

		public void SaveState ()
		{
			var s = states.Peek ();
			var n = new State {
				Transform = s.Transform,
			};
			states.Push (n);
			dc.Transform = n.Transform;
		}

		public void SetClippingRect (float x, float y, float width, float height)
		{
		}

		public void Translate (float dx, float dy)
		{
			dc.Transform = Matrix3x2.Multiply (Matrix3x2.Translation (dx, dy), dc.Transform);
		}

		public void Scale (float sx, float sy)
		{
			dc.Transform = Matrix3x2.Multiply (Matrix3x2.Scaling (sx, sy), dc.Transform);
		}

		public Transform2D Transform
		{
			get
			{
				var dt = dc.Transform;
				var t = new Transform2D ();
				t.M11 = dt.M11;
				t.M21 = dt.M12;
				t.M12 = dt.M21;
				t.M22 = dt.M22;
				t.M13 = dt.M31;
				t.M23 = dt.M32;
				return t;
			}
			set
			{
				var dt = new Matrix3x2 ();
				dt.M11 = value.M11;
				dt.M12 = value.M21;
				dt.M21 = value.M12;
				dt.M22 = value.M22;
				dt.M31 = value.M13;
				dt.M32 = value.M23;
				dc.Transform = dt;
			}
		}

		public void RestoreState ()
		{
			if (states.Count > 1) {
				states.Pop ();
				var s = states.Peek ();
				dc.Transform = s.Transform;
			}
		}

		public IImage ImageFromFile (string filename)
		{
			return null;
		}

		public void BeginEntity (object entity)
		{
		}
	}

	class Direct2DGraphicsFontMetrics : IFontMetrics
	{
		int _height;
		int _charWidth;

		public Direct2DGraphicsFontMetrics (int size)
		{
			_height = size;
			_charWidth = (855 * size) / 1600;
		}

		public int StringWidth (string str, int startIndex, int length)
		{
			return length * _charWidth;
		}

		public int Height
		{
			get
			{
				return _height;
			}
		}

		public int Ascent
		{
			get
			{
				return Height;
			}
		}

		public int Descent
		{
			get
			{
				return 0;
			}
		}
	}

	public static class PolygonEx
	{
		public static Geometry GetGeometry (this Polygon poly, Factory factory)
		{
			var g = poly.Tag as Geometry;
			if (g == null) {
				var pg = new PathGeometry (factory);
				using (var gs = pg.Open ()) {
					gs.BeginFigure (new DrawingPointF (poly.Points[0].X, poly.Points[0].Y), FigureBegin.Filled);
					gs.AddLines (poly.Points.Select (p => new DrawingPointF (p.X, p.Y)).ToArray ());
					gs.EndFigure (FigureEnd.Closed);
					gs.Close ();
				}

				g = pg;
				//poly.Tag = g;
			}
			return g;
		}
	}

	public static partial class ColorEx
	{
		class ColorInfo
		{
			public RenderTarget Target;
			public SolidColorBrush Brush;
		}

		public static SolidColorBrush GetBrush (this Color color, RenderTarget renderTarget)
		{
			var ci = color.Tag as ColorInfo;

			if (ci != null) {
				if (ci.Target != renderTarget) {
					ci = null;
				}
			}

			if (ci == null) {
				ci = new ColorInfo {
					Target = renderTarget,
					Brush = new SolidColorBrush (
						renderTarget,
						new Color4 (color.RedValue, color.GreenValue, color.BlueValue, color.AlphaValue)),
				};
				color.Tag = ci;
			}

			return ci.Brush;
		}
	}

	public class Direct2DCanvas : SwapChainBackgroundPanel, ICanvas
	{
		SharpDX.DXGI.SwapChain1 swapChain;
		Direct2DGraphics g;
		WindowsDrawTimer _drawTimer;

		CanvasContent content;
		public CanvasContent Content
		{
			get
			{
				return content;
			}
			set
			{
				if (content != value) {
					if (content != null) {
						content.NeedsDisplay -= OnNeedsDisplay;
					}
					content = value;
					if (content != null) {
						content.NeedsDisplay += OnNeedsDisplay;
					}
				}
			}
		}

		public Direct2DCanvas ()
		{
			int width = 1024;
			int height = 1024;

			SharpDX.Direct3D11.Device defaultDevice = new SharpDX.Direct3D11.Device (
				SharpDX.Direct3D.DriverType.Hardware,
				SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport);

			// Query the default device for the supported device and context interfaces.
			var device = defaultDevice.QueryInterface<SharpDX.Direct3D11.Device1> ();

			// Query for the adapter and more advanced DXGI objects.
			SharpDX.DXGI.Device2 dxgiDevice2 = device.QueryInterface<SharpDX.DXGI.Device2> ();
			SharpDX.DXGI.Adapter dxgiAdapter = dxgiDevice2.Adapter;
			SharpDX.DXGI.Factory2 dxgiFactory2 = dxgiAdapter.GetParent<SharpDX.DXGI.Factory2> ();

			var swapChainDescription = new SharpDX.DXGI.SwapChainDescription1 {
				Width = width,
				Height = height,
				Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
				Stereo = false,
				Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
				BufferCount = 2,
				Scaling = SharpDX.DXGI.Scaling.Stretch,
				SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
				Flags = SharpDX.DXGI.SwapChainFlags.None,
			};
			swapChainDescription.SampleDescription.Count = 1;
			swapChainDescription.SampleDescription.Quality = 0;

			swapChain = dxgiFactory2.CreateSwapChainForComposition (dxgiDevice2, ref swapChainDescription, null);
			swapChain.BackgroundColor = new Color4 (1, 0, 0, 0);

			var native = SharpDX.ComObject.QueryInterface<SharpDX.DXGI.ISwapChainBackgroundPanelNative> (this);
			native.SwapChain = swapChain;

			g = new Direct2DGraphics (swapChain);

			_drawTimer = new WindowsDrawTimer {
				Continuous = true,
				ShouldDrawFunc = () =>
					Content != null && ActualWidth > 0 && ActualHeight > 0,
				DrawFunc = Draw,
			};

			Unloaded += HandleUnloaded;
			Loaded += HandleLoaded;
			LayoutUpdated += HandleLayoutUpdated;
		}

		double lastUpdateWidth = -1, lastUpdateHeight = -1;

		void HandleLayoutUpdated (object sender, object e)
		{
			if (Content != null && (Math.Abs (lastUpdateWidth - ActualWidth) > 0.5 || Math.Abs (lastUpdateHeight - ActualHeight) > 0.5)) {
				lastUpdateWidth = ActualWidth;
				lastUpdateHeight = ActualHeight;

				// TODO: Resize the SwapChain

#if NETFX_CORE
#pragma warning disable 4014
				Dispatcher.RunAsync (Windows.UI.Core.CoreDispatcherPriority.Normal, delegate {
					Content.SetNeedsDisplay ();
				});
#pragma warning restore 4014
#endif
			}
		}

		void HandleLoaded (object sender, RoutedEventArgs e)
		{
			//Debug.WriteLine("LOADED {0}", Delegate);
			//TouchEnabled = true;
			if (_drawTimer.Continuous) {
				Start ();
			}
		}

		void HandleUnloaded (object sender, RoutedEventArgs e)
		{
			//Debug.WriteLine("UNLOADED {0}", Delegate);
			//TouchEnabled = false;
			Stop ();
		}

		void OnNeedsDisplay (object sender, EventArgs e)
		{
			Draw ();
		}

		public void Start ()
		{
			_drawTimer.Start ();
		}

		public void Stop ()
		{
			_drawTimer.Stop ();
		}

		public Transform2D ContentTransform
		{
			get
			{
				return g.Transform;
			}
			set
			{
				g.Transform = value;
				SetNeedsDisplay ();
			}
		}

		void SetNeedsDisplay ()
		{
			Draw ();
		}

		double Draw ()
		{
			var del = Content;
			if (del == null) return 0;

			var startT = DateTime.Now;

			g.BeginDrawing ();
			g.BeginEntity (del);

			//
			// Draw
			//
			var fr = new System.Drawing.RectangleF (0, 0, (float)ActualWidth, (float)ActualHeight);
			//if (_ppi != NativePointsPerInch) {
			//	fr.Width /= _ppi / (float)NativePointsPerInch;
			//	fr.Height /= _ppi / (float)NativePointsPerInch;
			//}
			del.Frame = fr;
			try {
				Console.WriteLine (g.Transform);
				//g.Transform = transform;
				g.Clear (Colors.Red);
				del.Draw (g);
			}
			catch (Exception) {
			}

			g.EndDrawing ();

			swapChain.Present (1, SharpDX.DXGI.PresentFlags.None);

			var endT = DateTime.Now;
			return (endT - startT).TotalSeconds;
		}
	}
}

