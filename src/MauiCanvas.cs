#nullable enable

using System;
using System.Drawing;
using System.Linq;

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;

using Size = Microsoft.Maui.Graphics.Size;

#if __IOS__ || __MACCATALYST__ || __MACOS__
#elif __ANDROID__
#endif

namespace CrossGraphics.Maui
{
	public interface IMauiCanvas : IView, ICanvas
	{
		// SKSize CanvasSize { get; }

		bool EnableTouchEvents { get; }
		bool DrawsContinuously { get; }

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
		public static readonly BindableProperty EnableTouchEventsProperty = BindableProperty.Create(nameof (EnableTouchEvents), typeof (bool), typeof (MauiCanvas), false);
		public static readonly BindableProperty DrawsContinuouslyProperty = BindableProperty.Create(nameof (DrawsContinuously), typeof (bool), typeof (MauiCanvas), false);
		public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof (Content), typeof (CanvasContent), typeof (MauiCanvas), null);

		public CanvasContent? Content
		{
			get => (CanvasContent?) this.GetValue(ContentProperty);
			set => this.SetValue(ContentProperty, value);
		}

		public bool EnableTouchEvents
		{
			get => (bool) this.GetValue(EnableTouchEventsProperty);
			set => this.SetValue(EnableTouchEventsProperty, value);
		}

		public bool DrawsContinuously
		{
			get => (bool) this.GetValue(DrawsContinuouslyProperty);
			set => this.SetValue(DrawsContinuouslyProperty, value);
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
			if (Content is {} content) {
				content.Frame = e.Frame;
				content.Draw (e.Graphics);
			}
			Draw?.Invoke (this, e);
		}

		protected virtual void OnTouch (TouchEventArgs e)
		{
			if (Content is {} content) {
				switch (e.Phase) {
					case TouchPhase.Began:
						content.TouchesBegan (e.Touches, e.Keys);
						break;
					case TouchPhase.Moved:
						content.TouchesMoved (e.Touches);
						break;
					case TouchPhase.Ended:
						content.TouchesEnded (e.Touches);
						break;
					case TouchPhase.Cancelled:
						content.TouchesCancelled (e.Touches);
						break;
				}
			}
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
		public event EventHandler<CrossGraphics.TouchEventArgs>? Touch;
#pragma warning restore CS0067 // Event is never used

		public bool EnableTouchEvents {
			get => _enableTouchEvents;
			set
			{
				if (_enableTouchEvents == value)
					return;
				_enableTouchEvents = value;
#if __IOS__ || __MACCATALYST__
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
			#if __IOS__ || __MACCATALYST__
			SetNeedsDisplay ();
			#else
			SetNeedsDisplayInRect (Bounds);
			#endif
		}

		public override void DrawMetalGraphics (CrossGraphics.Metal.MetalGraphics g)
		{
			var bounds = Bounds;
			var w = (float)bounds.Width;
			var h = (float)bounds.Height;
			Draw?.Invoke (this, new DrawEventArgs (g, new RectangleF (0, 0, w, h)));
			g.EndDrawing ();
		}

		public void SetVirtualViewSize (Size size)
		{
		}

#if __IOS__ || __MACCATALYST__
		readonly CoreGraphics.CoreGraphicsTouchManager _touchManager = new CoreGraphics.CoreGraphicsTouchManager ();
		private void OnTouches(TouchPhase phase, Foundation.NSSet touches)
		{
			if (!EnableTouchEvents) {
				return;
			}
			var canvasTouches =
				touches
					.OfType<UIKit.UITouch> ()
					.Select (t => _touchManager.GetCanvasTouch (phase, t, this))
					.ToArray ();
			Touch?.Invoke (this, new TouchEventArgs (phase, canvasTouches, CanvasKeys.None));
		}

		public override void TouchesBegan (Foundation.NSSet touches, UIKit.UIEvent? evt)
		{
			base.TouchesBegan (touches, evt);
			OnTouches(TouchPhase.Began, touches);
		}

		public override void TouchesMoved (Foundation.NSSet touches, UIKit.UIEvent? evt)
		{
			base.TouchesMoved (touches, evt);
			OnTouches(TouchPhase.Moved, touches);
		}

		public override void TouchesEnded (Foundation.NSSet touches, UIKit.UIEvent? evt)
		{
			base.TouchesEnded (touches, evt);
			OnTouches(TouchPhase.Ended, touches);
		}

		public override void TouchesCancelled (Foundation.NSSet touches, UIKit.UIEvent? evt)
		{
			base.TouchesCancelled (touches, evt);
			OnTouches(TouchPhase.Cancelled, touches);
		}
#endif
	}
	#elif __ANDROID__
	class MauiCanvasView : SkiaSharp.Views.Android.SKCanvasView
	{
		public new event EventHandler<DrawEventArgs>? Draw;
#pragma warning disable CS0067 // Event is never used
		public new event EventHandler<CrossGraphics.TouchEventArgs>? Touch;
#pragma warning restore CS0067 // Event is never used

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

		private bool _drawsContinuously = false;
		public bool DrawsContinuously {
			get => _drawsContinuously;
			set
			{
				_drawsContinuously = value;
				OnDrawsContinuouslyChanged();
			}
		}

		public MauiCanvasView (global::Android.Content.Context context)
			: base(context)
		{
			IgnorePixelScaling = false;
		}

		global::Android.Animation.ValueAnimator? _animator;
		void OnDrawsContinuouslyChanged()
		{
			if (DrawsContinuously) {
				if (_animator is not null) {
					return;
				}
				var anim = new global::Android.Animation.ValueAnimator ();
				anim.SetFloatValues (0.0f, 1.0f);
				anim.RepeatMode = global::Android.Animation.ValueAnimatorRepeatMode.Restart;
				anim.SetDuration (1000);
				anim.RepeatCount = global::Android.Animation.ValueAnimator.Infinite;
				anim.Update += (sender, e) => {
					Invalidate ();
				};
				_animator = anim;
				anim.Start ();
			}
			else {
				_animator?.Cancel ();
				_animator = null;
			}
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
			var w = (float)virtualViewSize.Width;
			var h = (float)virtualViewSize.Height;
			if (w > 0 && h > 0) {
				var renderedCanvasFromLayoutScale = CanvasSize.Width / w;
				g.Scale (renderedCanvasFromLayoutScale, renderedCanvasFromLayoutScale);
				Draw?.Invoke (this, new DrawEventArgs (g, new RectangleF (0, 0, w, h)));
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
					[nameof(IMauiCanvas.DrawsContinuously)] = MapDrawsContinuously,
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
			platformView.Touch += OnTouch;
			base.ConnectHandler (platformView);
		}

		protected override void DisconnectHandler (MauiCanvasView platformView)
		{
			platformView.Draw -= OnDraw;
			platformView.Touch -= OnTouch;
			base.DisconnectHandler (platformView);
		}

		void OnDraw(object? sender, CrossGraphics.DrawEventArgs e)
		{
			this.VirtualView?.OnDraw(e);
		}

		void OnTouch(object? sender, CrossGraphics.TouchEventArgs e)
		{
			this.VirtualView?.OnTouch(e);
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

		static void MapDrawsContinuously (MauiCanvasHandler handler,
			IMauiCanvas canvasView)
		{
			if (handler.PlatformView == null)
				return;
			handler.PlatformView.DrawsContinuously = canvasView.DrawsContinuously;
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
