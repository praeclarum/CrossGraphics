using CrossGraphicsTests;

namespace atests;

#if __MACOS__
[Register ("AppDelegate")]
public class AppDelegate : NSApplicationDelegate {
	public override void DidFinishLaunching (NSNotification notification)
	{
		RunTests ().ContinueWith (_ => {
			Environment.Exit (0);
		});
	}

	public override void WillTerminate (NSNotification notification)
	{
		// Insert code here to tear down your application
	}

	async Task RunTests ()
	{
		await Task.Delay (1);
		Console.WriteLine ("Running tests...");
		var tests = new AcceptanceTests ();
		tests.Arcs ();
		tests.Ovals ();
	}
}

#elif __IOS__ || __MACCATALYST__
[Register ("AppDelegate")]
public class AppDelegate : UIApplicationDelegate {
	public override UIWindow? Window {
		get;
		set;
	}

	public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
	{
		// create a new window instance based on the screen size
		Window = new UIWindow (UIScreen.MainScreen.Bounds);

		// create a UIViewController with a single UILabel
		var vc = new UIViewController ();
		vc.View!.AddSubview (new UILabel (Window!.Frame) {
			BackgroundColor = UIColor.SystemBackground,
			TextAlignment = UITextAlignment.Center,
			Text = "Hello, Mac Catalyst!",
			AutoresizingMask = UIViewAutoresizing.All,
		});
		Window.RootViewController = vc;

		// make the window visible
		Window.MakeKeyAndVisible ();

		RunTests ().ContinueWith (_ => {
			Environment.Exit (0);
		});

		return true;
	}

	async Task RunTests ()
	{
		await Task.Delay (1);
		Console.WriteLine ("Running tests...");
		var tests = new AcceptanceTests ();
		tests.Arcs ();
		tests.Ovals ();
		tests.Rects ();
		tests.RoundedRects ();
		tests.Text ();
	}
}
#endif
