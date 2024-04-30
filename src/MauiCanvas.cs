#nullable enable

using System;

using CrossGraphics.Skia;

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

		public bool EnableTouchEvents {
			get;
			set;
		}

		public MauiCanvasView (global::Android.Content.Context context)
			: base(context)
		{
		}

		public void InvalidateCanvas ()
		{
		}

		protected override void OnPaintSurface (SkiaSharp.Views.Android.SKPaintSurfaceEventArgs e)
		{
			var g = new SkiaGraphics (e.Surface);
			Draw?.Invoke (this, new DrawEventArgs (g));
		}
	}
	#endif

	class MauiCanvasHandler : ViewHandler<IMauiCanvas, MauiCanvasView>
	{
		// private (int Width, int Height) lastCanvasSize = (0, 0);

		public static PropertyMapper<IMauiCanvas, MauiCanvasHandler>
			MauiCanvasMapper =
				new(ViewMapper) {
					["EnableTouchEvents"] = MapEnableTouchEvents,
					// ["IgnorePixelScaling"] = MapIgnorePixelScaling
				};

		public static CommandMapper<IMauiCanvas, MauiCanvasHandler>
			MauiCanvasCommandMapper =
				new () {
					["InvalidateCanvas"] = OnInvalidateCanvas
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

		private void OnDraw(object? sender, DrawEventArgs e)
		{
			// SKSizeI size = e.Info.Size;
			// if (this.lastCanvasSize != size)
			// {
			// 	this.lastCanvasSize = size;
			// 	this.VirtualView?.OnCanvasSizeChanged(size);
			// }
			this.VirtualView?.OnDraw(e);
		}

		public static void OnInvalidateCanvas (
			MauiCanvasHandler handler,
			IMauiCanvas canvasView,
			object? args)
		{
			handler.PlatformView?.InvalidateCanvas ();
		}

		public static void MapIgnorePixelScaling (MauiCanvasHandler handler,
			IMauiCanvas canvasView)
		{
			var platformView = handler.PlatformView;
			if (platformView == null)
				return;
		}

		public static void MapEnableTouchEvents (MauiCanvasHandler handler,
			IMauiCanvas canvasView)
		{
			if (handler.PlatformView == null)
				return;
			handler.PlatformView.EnableTouchEvents = canvasView.EnableTouchEvents;
		}

		// private void OnPaintSurface (object? sender, SkiaSharp.Views.Android.SKPaintSurfaceEventArgs e)
		// {
		// 	SKSizeI size = e.Info.Size;
		// 	if (this.lastCanvasSize != size) {
		// 		this.lastCanvasSize = size;
		// 		this.VirtualView?.OnCanvasSizeChanged (size);
		// 	}
		//
		// 	this.VirtualView?.OnPaintSurface (
		// 		new SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs (e.Surface, e.Info, e.RawInfo));
		// }

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
