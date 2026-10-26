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
	public override bool FinishedLaunching (UIApplication application, NSDictionary? launchOptions)
	{
		// The window is created by SceneDelegate; the tests don't depend on it
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

[Register ("SceneDelegate")]
public class SceneDelegate : UIResponder, IUIWindowSceneDelegate {
	[Export ("window")]
	public UIWindow? Window { get; set; }

	[Export ("scene:willConnectToSession:options:")]
	public void WillConnect (UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		if (scene is not UIWindowScene windowScene)
			return;

		var window = new UIWindow (windowScene);

		// create a UIViewController with a single UILabel
		var vc = new UIViewController ();
		if (vc.View is UIView view) {
			view.AddSubview (new UILabel (view.Bounds) {
				BackgroundColor = UIColor.SystemBackground,
				TextAlignment = UITextAlignment.Center,
				Text = "Hello, Mac Catalyst!",
				AutoresizingMask = UIViewAutoresizing.All,
			});
		}
		window.RootViewController = vc;

		// make the window visible
		window.MakeKeyAndVisible ();
		Window = window;
	}
}
#endif
