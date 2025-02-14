using CrossGraphicsTests;

namespace atests;

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
