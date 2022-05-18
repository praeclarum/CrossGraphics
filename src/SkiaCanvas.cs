#nullable enable

using System;

using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace CrossGraphics.Skia
{
	public class SkiaCanvas : SKCanvasView, ICanvas
	{
		float renderedCanvasFromLayoutScale = 1.0f;

		CanvasContent? content = null;
		CanvasContent? ICanvas.Content {
			get => content;
			set {
				content = value;
				InvalidateSurface ();
			}
		}

		public SkiaCanvas ()
		{
			PaintSurface += RenderView_PaintSurface;
			Touch += RenderView_Touch;
			EnableTouchEvents = true;
		}

		protected virtual CrossGraphics.Color GetClearColor ()
		{
			return CrossGraphics.Colors.Black;
		}

		public void InvalidateCanvas ()
		{
			InvalidateSurface ();
		}

		CanvasTouch GetCanvasTouch (SKTouchEventArgs e)
		{
			return new CanvasTouch {
				Handle = new IntPtr (e.Id),
				CanvasLocation = new System.Drawing.PointF (e.Location.X, e.Location.Y),
			};
		}

		private void RenderView_Touch (object? sender, SKTouchEventArgs e)
		{
			Console.WriteLine ($"{e.ActionType}");
			switch (e.ActionType) {
				case SKTouchAction.Entered:
					break;
				case SKTouchAction.Exited:
					break;
				case SKTouchAction.WheelChanged:
					break;
				case SKTouchAction.Pressed:
					content?.TouchesBegan (new[] { GetCanvasTouch (e) }, CanvasKeys.None);
					break;
				case SKTouchAction.Moved:
					content?.TouchesMoved (new[] { GetCanvasTouch (e) });
					break;
				case SKTouchAction.Released:
					content?.TouchesEnded (new[] { GetCanvasTouch (e) });
					break;
				case SKTouchAction.Cancelled:
					content?.TouchesCancelled (new[] { GetCanvasTouch (e) });
					break;
			}
		}

		void RenderView_PaintSurface (object? sender, SKPaintSurfaceEventArgs e)
		{
			var c = e.Surface.Canvas;
			var g = new SkiaGraphics (c);
			c.Clear (GetClearColor ().ToSkiaColor ());
			var w = Width;
			var h = Height;
			if (w > 0 && h > 0 && content is CanvasContent co) {
				renderedCanvasFromLayoutScale = CanvasSize.Width / (float)w;
				g.Scale (renderedCanvasFromLayoutScale, renderedCanvasFromLayoutScale);
				co.Frame = new System.Drawing.RectangleF (0, 0, (float)w, (float)h);
				co.Draw (g);
			}
		}
	}
}
