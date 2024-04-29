#nullable enable

using System;

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;

#if __IOS__ || __MACOS__ || __MACCATALYST__
#elif __ANDROID__
#endif

namespace CrossGraphics
{
	public interface IMauiCanvas : IView, IElement, ITransform
	{
		// SKSize CanvasSize { get; }

		bool IgnorePixelScaling { get; }

		bool EnableTouchEvents { get; }

		void InvalidateSurface();

		// void OnCanvasSizeChanged(SKSizeI size);

		// void OnPaintSurface(SKPaintSurfaceEventArgs e);

		// void OnTouch(SKTouchEventArgs e);
	}

	public class MauiCanvas : View, ICanvas
	{
		CanvasContent? _content = null;

		CanvasContent? ICanvas.Content {
			get => _content;
			set
			{
				_content = value;
				InvalidateCanvas ();
			}
		}

		public delegate void DrawDelegate (IGraphics g);

		public event EventHandler<DrawEventArgs>? Draw;

		// public CrossGraphics.Color ClearColor { get; set; } = CrossGraphics.Colors.Black;

		public void InvalidateCanvas ()
		{
			Draw?.Invoke (this, new DrawEventArgs (new NullGraphics ()));
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
	public class MauiCanvasView : CrossGraphics.Metal.MetalCanvas
	{
		public MauiCanvasView ()
		{
		}

		public void InvalidateCanvas ()
		{
		}
	}
	#elif __ANDROID__
	public class MauiCanvasView : SkiaSharp.Views.Android.SKCanvasView
	{
		public MauiCanvasView (global::Android.Content.Context context)
			: base(context)
		{
		}

		public void InvalidateCanvas ()
		{
		}
	}
	#endif

	public class MauiCanvasHandler : ViewHandler<IMauiCanvas, MauiCanvasView>
	{
		// private (int Width, int Height) lastCanvasSize = (0, 0);

		public static PropertyMapper<IMauiCanvas, MauiCanvasHandler>
			MauiCanvasMapper =
				new(ViewMapper) {
					["EnableTouchEvents"] =
						new Action<MauiCanvasHandler, IMauiCanvas> (MapEnableTouchEvents),
					["IgnorePixelScaling"] =
						new Action<MauiCanvasHandler, IMauiCanvas> (MapIgnorePixelScaling)
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
			// platformView.PaintSurface += OnPaintSurface;
			base.ConnectHandler (platformView);
		}

		protected override void DisconnectHandler (MauiCanvasView platformView)
		{
			// platformView.PaintSurface -= OnPaintSurface;
			base.DisconnectHandler (platformView);
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
