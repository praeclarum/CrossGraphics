#if __IOS__ || __MACCATALYST__ || __MACOS__
global using Foundation;
#endif
#if __MACOS__
global using AppKit;
#endif
#if __IOS__ || __MACCATALYST__
global using UIKit;
#endif
