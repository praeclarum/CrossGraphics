using CrossGraphicsTests;

namespace atests;

[Register ("AppDelegate")]
public class AppDelegate : NSApplicationDelegate {
	public override void DidFinishLaunching (NSNotification notification)
	{
		RunTests ().ContinueWith (_ => {
			BeginInvokeOnMainThread (() => {
				NSApplication.SharedApplication.Terminate (this);
			});
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
		tests.Ovals ();
	}
}
