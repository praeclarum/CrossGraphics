using atests;

// This is the main entry point of the application.
// NSApplication.Init ();
// NSApplication.Main (args);

#if __IOS__ || __MACCATALYST__
UIApplication.Main (args, null, typeof (AppDelegate));
#else
System.Console.WriteLine ("Acceptance tests running on unsupported platform.");
#endif
