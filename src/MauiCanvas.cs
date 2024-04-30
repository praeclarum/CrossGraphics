#nullable enable

using System;

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;

#if __IOS__ || __MACOS__ || __MACCATALYST__
#elif __ANDROID__
#endif

namespace CrossGraphics
{
	public interface IMauiCanvas : IView, IElement, ITransform
	{
		// SKSize CanvasSize { get; }

		bool EnableTouchEvents { get; }

		void InvalidateCanvas();

		// void OnCanvasSizeChanged(SKSizeI size);

		void OnDraw(DrawEventArgs e);

		void OnTouch(TouchEventArgs e);
	}

	public interface IMauiCanvasController : IViewController
	{
		event EventHandler CanvasInvalidated;

		// event EventHandler<GetPropertyValueEventArgs<SKSize>> GetCanvasSize;

		void OnDraw(DrawEventArgs e);

		void OnTouch(TouchEventArgs e);
	}

	public class MauiCanvas : View, IMauiCanvas, IMauiCanvasController
	{
		// public static readonly BindableProperty IgnorePixelScalingProperty = BindableProperty.Create(nameof (IgnorePixelScaling), typeof (bool), typeof (SKCanvasView), (object) false);
		public static readonly BindableProperty EnableTouchEventsProperty = BindableProperty.Create(nameof (EnableTouchEvents), typeof (bool), typeof (MauiCanvas), (object) false);

		// CanvasContent? _content = null;
		//
		// CanvasContent? ICanvas.Content {
		// 	get => _content;
		// 	set
		// 	{
		// 		_content = value;
		// 		InvalidateCanvas ();
		// 	}
		// }

		public bool EnableTouchEvents
		{
			get => (bool) this.GetValue(EnableTouchEventsProperty);
			set => this.SetValue(EnableTouchEventsProperty, value);
		}

		public event EventHandler? CanvasInvalidated;
		public event EventHandler<DrawEventArgs>? Draw;
		public event EventHandler<TouchEventArgs>? Touch;

		public void InvalidateCanvas ()
		{
			CanvasInvalidated?.Invoke (this, EventArgs.Empty);
			Handler?.Invoke(nameof(CanvasInvalidated), null);
		}

		protected virtual void OnDraw (DrawEventArgs e)
		{
			Draw?.Invoke (this, e);
		}

		protected virtual void OnTouch (TouchEventArgs e)
		{
			Touch?.Invoke (this, e);
		}

		void IMauiCanvas.OnDraw (DrawEventArgs e) => OnDraw (e);
		void IMauiCanvas.OnTouch (TouchEventArgs e) => OnTouch (e);
		void IMauiCanvasController.OnDraw (DrawEventArgs e) => OnDraw (e);
		void IMauiCanvasController.OnTouch (TouchEventArgs e) => OnTouch (e);

		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			return new SizeRequest(new Size(40.0, 40.0));
		}

		protected override void OnSizeAllocated (double width, double height)
		{
			base.OnSizeAllocated (width, height);
			Handler?.Invoke(nameof(SizeChanged), new Size (width, height));
		}
	}

	public class DrawEventArgs (IGraphics g) : EventArgs
	{
		public IGraphics Graphics { get; } = g;
	}

	public static class AppHostBuilderExtensions
	{
		public static MauiAppBuilder UseCrossGraphics (this MauiAppBuilder builder)
		{
			return builder.ConfigureMauiHandlers ((Action<IMauiHandlersCollection>)(handlers =>
				handlers.AddHandler<MauiCanvas, MauiCanvasHandler> ()));
		}
	}

	#if __IOS__ || __MACCATALYST__ || __MACOS__
	class MauiCanvasView : CrossGraphics.Metal.MetalCanvas
	{
		bool _enableTouchEvents;
		public new event EventHandler<DrawEventArgs>? Draw;
#pragma warning disable CS0067 // Event is never used
		public event EventHandler<TouchEventArgs>? Touch;
#pragma warning restore CS0067 // Event is never used

		public bool EnableTouchEvents {
			get => _enableTouchEvents;
			set
			{
				if (_enableTouchEvents == value)
					return;
				_enableTouchEvents = value;
#if __IOS__
				this.MultipleTouchEnabled = value;
				this.UserInteractionEnabled = value;
#endif
			}
		}

		Color _backgroundColor = Colors.Clear;
		public Color CanvasBackgroundColor {
			get => _backgroundColor;
			set
			{
				_backgroundColor = value;
				OnBackgroundColorChanged ();
			}
		}

		public MauiCanvasView ()
		{
			OnBackgroundColorChanged ();
		}

		void OnBackgroundColorChanged ()
		{
			ClearColor = new global::Metal.MTLClearColor (_backgroundColor.RedValue, _backgroundColor.GreenValue, _backgroundColor.BlueValue, _backgroundColor.AlphaValue);
#if __IOS__ || __MACCATALYST__
			this.BackgroundColor = UIKit.UIColor.FromRGBA (_backgroundColor.Red, _backgroundColor.Green, _backgroundColor.Blue, _backgroundColor.Alpha);
#else
#endif
			InvalidateCanvas ();
		}

		public void InvalidateCanvas ()
		{
			// Metal is always rendering
		}

		public override void DrawMetalGraphics (CrossGraphics.Metal.MetalGraphics g)
		{
			var bounds = Bounds;
			g.SetViewport ((float)bounds.Width, (float)bounds.Height, 1, 0, 0);
			Draw?.Invoke (this, new DrawEventArgs (g));
			g.EndDrawing ();
		}

		public void SetVirtualViewSize (Size size)
		{
		}

#if __IOS__
		public override void TouchesBegan (Foundation.NSSet touches, UIKit.UIEvent? evt)
		{
			base.TouchesBegan (touches, evt);
			if (EnableTouchEvents) {
				Touch?.Invoke (this, new TouchEventArgs ());
			}
		}
#endif
	}
	#elif __ANDROID__
	class MauiCanvasView : SkiaSharp.Views.Android.SKCanvasView
	{
		public new event EventHandler<DrawEventArgs>? Draw;

		private Size virtualViewSize = new Size (40, 40);

		public bool EnableTouchEvents {
			get;
			set;
		}

		Color _backgroundColor = Colors.Clear;
		public Color CanvasBackgroundColor {
			get => _backgroundColor;
			set {
				_backgroundColor = value;
				Invalidate ();
			}
		}

		public MauiCanvasView (global::Android.Content.Context context)
			: base(context)
		{
			IgnorePixelScaling = false;
		}

		public void InvalidateCanvas ()
		{
			this.Invalidate ();
		}

		protected override void OnPaintSurface (SkiaSharp.Views.Android.SKPaintSurfaceEventArgs e)
		{
			var c = e.Surface.Canvas;
			var g = new Skia.SkiaGraphics (c);
			c.Clear (Skia.Conversions.ToSkiaColor (_backgroundColor));
			var w = virtualViewSize.Width;
			var h = virtualViewSize.Height;
			if (w > 0 && h > 0) {
				var renderedCanvasFromLayoutScale = CanvasSize.Width / (float)w;
				g.Scale (renderedCanvasFromLayoutScale, renderedCanvasFromLayoutScale);
				Draw?.Invoke (this, new DrawEventArgs (g));
			}
		}

		public void SetVirtualViewSize (Size size)
		{
			virtualViewSize = size;
			Invalidate ();
		}
	}
	#endif

	class MauiCanvasHandler : ViewHandler<IMauiCanvas, MauiCanvasView>
	{
		public static PropertyMapper<IMauiCanvas, MauiCanvasHandler>
			MauiCanvasMapper =
				new(ViewMapper) {
					[nameof(IMauiCanvas.EnableTouchEvents)] = MapEnableTouchEvents,
					[nameof(IMauiCanvas.Background)] = MapBackground,
				};

		public static CommandMapper<IMauiCanvas, MauiCanvasHandler>
			MauiCanvasCommandMapper =
				new () {
					["CanvasInvalidated"] = OnCanvasInvalidated,
					["SizeChanged"] = OnSizeChanged
				};

		#if __ANDROID__
		protected override MauiCanvasView CreatePlatformView () => new MauiCanvasView (this.Context);
		#else
		protected override MauiCanvasView CreatePlatformView () => new MauiCanvasView ();
		#endif

		protected override void ConnectHandler (MauiCanvasView platformView)
		{
			platformView.Draw += OnDraw;
			base.ConnectHandler (platformView);
		}

		protected override void DisconnectHandler (MauiCanvasView platformView)
		{
			platformView.Draw -= OnDraw;
			base.DisconnectHandler (platformView);
		}

		void OnDraw(object? sender, DrawEventArgs e)
		{
			this.VirtualView?.OnDraw(e);
		}

		static void OnCanvasInvalidated (
			MauiCanvasHandler handler,
			IMauiCanvas canvasView,
			object? args)
		{
			handler.PlatformView?.InvalidateCanvas ();
		}

		static void OnSizeChanged (
			MauiCanvasHandler handler,
			IMauiCanvas canvasView,
			object? args)
		{
			if (args is Size size && handler.PlatformView is {} platformView) {
				platformView.SetVirtualViewSize (size);
			}
		}

		static void MapEnableTouchEvents (MauiCanvasHandler handler,
			IMauiCanvas canvasView)
		{
			if (handler.PlatformView == null)
				return;
			handler.PlatformView.EnableTouchEvents = canvasView.EnableTouchEvents;
		}

		static void MapBackground (MauiCanvasHandler handler,
			IMauiCanvas canvasView)
		{
			var back = canvasView.Background;
			if (handler.PlatformView is { } platformView && back is SolidPaint solid) {
				solid.Color.ToRgba (out var r, out var g, out var b, out var a);
				platformView.CanvasBackgroundColor = new Color (r, g, b, a);
			}
		}

		public MauiCanvasHandler ()
			: base (MauiCanvasMapper, MauiCanvasCommandMapper)
		{
		}

		public MauiCanvasHandler (PropertyMapper? mapper, CommandMapper? commands)
			: base (
				mapper ?? MauiCanvasMapper,
				commands ?? MauiCanvasCommandMapper)
		{
		}
	}
}
