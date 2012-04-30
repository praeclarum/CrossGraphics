# CrossGraphics Clock Sample

This sample shows an animated clock running on a variety of platforms.

The clock code is contained in [Clock.cs](https://github.com/praeclarum/CrossGraphics/blob/master/samples/Clock/Clock/Clock.cs) and is just a very simple rendering of an analog clock (see screenshots below).

## iOS

`CoreGraphicsGraphics` is used to render the clock in a custom `UIView` called `ClockView` in [AppDelegate.cs](https://github.com/praeclarum/CrossGraphics/blob/master/samples/Clock/ClockiOS/AppDelegate.cs).

<a href="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/iOS.png"><img title="iOS Screenshot" src="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/iOS.png" width="200" /></a>

## Mac

`CoreGraphicsGraphics` is used to render the clock in a custom `NSView` called `ClockView` in [ClockView.cs](https://github.com/praeclarum/CrossGraphics/blob/master/samples/Clock/ClockMac/ClockView.cs).

<a href="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Mac.png"><img title="Mac Screenshot" src="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Mac.png" width="300" /></a>

## Android

`DroidGraphics` is used to render the clock in a custom `View` called `ClockView` in [Activity1.cs](https://github.com/praeclarum/CrossGraphics/blob/master/samples/Clock/ClockAndroid/Activity1.cs).

<a href="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Android.png"><img title="Android Screenshot" src="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Android.png" width="200" /></a>

## Windows Phone 7

`SilverlightGraphics` is used to render the clock on a `Canvas` in [MainPage.xaml.cs](https://github.com/praeclarum/CrossGraphics/blob/master/samples/Clock/ClockWP7/MainPage.xaml.cs).

<a href="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/WP7.png"><img title="WP7 Screenshot" src="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/WP7.png" width="200" /></a>

## Silverlight

`SilverlightGraphics` is used to render the clock on a `Canvas` in [MainPage.xaml.cs](https://github.com/praeclarum/CrossGraphics/blob/master/samples/Clock/ClockSilverlight/MainPage.xaml.cs).

<a href="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Silverlight.png"><img title="Silverlight Screenshot" src="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Silverlight.png" width="300" /></a>

## WPF

`SilverlightGraphics` is used to render the clock on a `Canvas` in [MainWindow.xaml.cs](https://github.com/praeclarum/CrossGraphics/blob/master/samples/Clock/ClockWpf/MainWindow.xaml.cs).

<a href="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Wpf.png"><img title="WPF Screenshot" src="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Wpf.png" width="300" /></a>

## SVG

`SvgGraphics` is used to render the clock into an svg element on an HTML5 page in [Default.aspx](https://github.com/praeclarum/CrossGraphics/blob/master/samples/Clock/ClockSvg/Default.aspx).

<a href="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Svg.png"><img title="SVG Screenshot" src="https://github.com/praeclarum/CrossGraphics/raw/master/samples/Clock/Screenshots/Svg.png" width="300" /></a>

