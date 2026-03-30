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

	public override bool FinishedLaunching (UIApplication application, NSDictionary? launchOptions)
	{
		// create a new window instance based on the screen size
		#pragma warning disable CA1422 // Validate platform compatibility
		Window = new UIWindow (UIScreen.MainScreen.Bounds);
		#pragma warning restore CA1422 // Validate platform compatibility

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

		RunTests ().ContinueWith (t => {
			Environment.Exit (t.Result);
		});

		return true;
	}

	async Task<int> RunTests ()
	{
		await Task.Delay (1);
		try {
			Console.WriteLine ("Running tests...");
			var tests = new AcceptanceTests ();
			var result = tests.Run ();
			Console.WriteLine ("Finished running tests.");
			return result;
		} catch (Exception ex) {
			Console.WriteLine ($"Error running tests: {ex}");
			return 1;
		}
	}
}
#endif
