#nullable enable

using System;
using System.Collections.Generic;

using CrossGraphics.Skia;

using Microsoft.Maui.Controls;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace CrossGraphics.Maui
{
	public class MauiSkiaCanvas : SKCanvasView, ICanvas
	{
		public static readonly BindableProperty DrawsContinuouslyProperty = BindableProperty.Create(nameof (DrawsContinuously), typeof (bool), typeof (MauiSkiaCanvas), false);

		float renderedCanvasFromLayoutScale = 1.0f;
		readonly Dictionary<IntPtr, CanvasTouch> activeTouches = new ();

		CanvasContent? content = null;
		public CanvasContent? Content {
			get => content;
			set {
				content = value;
				InvalidateSurface ();
			}
		}

		public bool DrawsContinuously {
			get => (bool)this.GetValue (DrawsContinuouslyProperty);
			set
			{
				this.SetValue (DrawsContinuouslyProperty, value);
				OnDrawsContinuouslyChanged ();
			}
		}

		public delegate void DrawDelegate (IGraphics g);

		public event EventHandler<DrawEventArgs>? Draw;

		public CrossGraphics.Color ClearColor { get; set; } = CrossGraphics.Colors.Black;

		public MauiSkiaCanvas ()
		{
			PaintSurface += RenderView_PaintSurface;
			Touch += RenderView_Touch;
			EnableTouchEvents = true;
		}

		public void InvalidateCanvas ()
		{
			InvalidateSurface ();
		}

		CanvasTouch GetCanvasTouch (SKTouchEventArgs e)
		{
			var handle = new IntPtr (e.Id);
			var location = new System.Drawing.PointF (e.Location.X / renderedCanvasFromLayoutScale, e.Location.Y / renderedCanvasFromLayoutScale);
			var now = DateTime.UtcNow;
			if (activeTouches.TryGetValue (handle, out var previous)) {
				previous.CanvasPreviousLocation = previous.CanvasLocation;
				previous.SuperCanvasPreviousLocation = previous.SuperCanvasLocation;
				previous.PreviousTime = previous.Time;
				previous.CanvasLocation = location;
				previous.SuperCanvasLocation = location;
				previous.Time = now;
				return previous;
			}

			return new CanvasTouch {
				Handle = handle,
				CanvasLocation = location,
				CanvasPreviousLocation = location,
				SuperCanvasLocation = location,
				SuperCanvasPreviousLocation = location,
				Time = now,
				PreviousTime = now,
			};
		}

		private void RenderView_Touch (object? sender, SKTouchEventArgs e)
		{
			switch (e.ActionType) {
				case SKTouchAction.Entered:
					break;
				case SKTouchAction.Exited:
					break;
				case SKTouchAction.WheelChanged:
					OnWheelChanged (e);
					break;
				case SKTouchAction.Pressed:
					var pressedTouch = GetCanvasTouch (e);
					activeTouches[pressedTouch.Handle] = pressedTouch;
					content?.TouchesBegan (new[] { pressedTouch }, CanvasKeys.None);
					break;
				case SKTouchAction.Moved:
					content?.TouchesMoved (new[] { GetCanvasTouch (e) });
					break;
				case SKTouchAction.Released:
					var releasedTouch = GetCanvasTouch (e);
					activeTouches.Remove (releasedTouch.Handle);
					content?.TouchesEnded (new[] { releasedTouch });
					break;
				case SKTouchAction.Cancelled:
					var cancelledTouch = GetCanvasTouch (e);
					activeTouches.Remove (cancelledTouch.Handle);
					content?.TouchesCancelled (new[] { cancelledTouch });
					break;
			}
			e.Handled = true;
		}

		protected virtual void OnWheelChanged (SKTouchEventArgs e)
		{
		}

		void RenderView_PaintSurface (object? sender, SKPaintSurfaceEventArgs e)
		{
			var c = e.Surface.Canvas;
			var g = new SkiaGraphics (c);
			c.Clear (ClearColor.ToSkiaColor ());
			var w = (float)Width;
			var h = (float)Height;
			var frame = new System.Drawing.RectangleF (0, 0, w, h);
			if (w > 0 && h > 0) {
				renderedCanvasFromLayoutScale = CanvasSize.Width / (float)w;
				g.Scale (renderedCanvasFromLayoutScale, renderedCanvasFromLayoutScale);
				if (content is CanvasContent co) {
					co.Frame = frame;
					co.Draw (g);
				}
				Draw?.Invoke (this, new DrawEventArgs (g, frame));
			}
		}
		private Microsoft.Maui.Dispatching.IDispatcherTimer? _drawTimer;
		private bool _drawsContinuouslyRunning;

		void OnDrawsContinuouslyChanged ()
		{
			if (DrawsContinuously) {
				if (_drawsContinuouslyRunning)
					return;

				var timer = Dispatcher?.CreateTimer ();
				if (timer is null)
					return;

				timer.Interval = TimeSpan.FromMilliseconds (16);
				timer.IsRepeating = true;
				timer.Tick += (_, _) => InvalidateSurface ();
				timer.Start ();

				_drawTimer = timer;
				_drawsContinuouslyRunning = true;
			}
			else {
				if (_drawTimer is {} timer) {
					timer.Stop ();
				}
				_drawTimer = null;
				_drawsContinuouslyRunning = false;
			}
		}
	}
}
