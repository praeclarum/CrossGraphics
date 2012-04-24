using MonoTouch.Foundation;
using MonoTouch.UIKit;
using CrossGraphics.CoreGraphics;

namespace Clock.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		UIWindow _window;
		UIViewController _vc;
		NSTimer _timer;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			_window = new UIWindow (UIScreen.MainScreen.Bounds);
			
			_vc = new UIViewController ();
			_vc.View = new ClockView (_vc.View.Frame);
			
			_window.RootViewController = _vc;
			
			_window.MakeKeyAndVisible ();
			
			_timer = NSTimer.CreateRepeatingScheduledTimer (1, delegate {
				_vc.View.SetNeedsDisplay ();
			});
			
			return true;
		}
		
		class ClockView : UIView
		{
			Clock _clock;
			
			public ClockView (System.Drawing.RectangleF frame)
				: base (frame)
			{
				_clock = new Clock ();
			}
			
			public override void Draw (System.Drawing.RectangleF rect)
			{
				base.Draw (rect);
				
				var c = UIGraphics.GetCurrentContext ();
				var g = new CoreGraphicsGraphics (c, true);
				
				var bounds = Bounds;
				_clock.Width = bounds.Width;
				_clock.Height = bounds.Height;
				_clock.Draw (g);
			}
		}
	}
}

