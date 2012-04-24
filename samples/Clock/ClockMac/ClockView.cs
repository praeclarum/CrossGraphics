using System;
using MonoMac.Foundation;
using MonoMac.AppKit;
using CrossGraphics.CoreGraphics;

namespace Clock.Mac
{
	[Register ("ClockView")]
	public class ClockView : NSView
	{
		Clock _clock;
		NSTimer _timer;
		
		public ClockView (IntPtr handle)
			: base (handle)
		{
			_clock = new Clock ();
			
			_timer = NSTimer.CreateRepeatingScheduledTimer (1, delegate {
				SetNeedsDisplayInRect (Bounds);				
			});
		}
		
		public override bool IsFlipped {
			get {
				return true;
			}
		}
		
		public override void DrawRect (System.Drawing.RectangleF dirtyRect)
		{
			base.DrawRect (dirtyRect);
			
			var g = new CoreGraphicsGraphics (NSGraphicsContext.CurrentContext.GraphicsPort, true);
			
			var bounds = Bounds;
			_clock.Width = bounds.Width;
			_clock.Height = bounds.Height;
			
			_clock.Draw (g);
		}
		
	}
}

