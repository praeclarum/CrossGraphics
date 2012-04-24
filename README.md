# CrossGraphics Library

CrossGraphics aims to deliver a simple immediate mode interface for drawing graphics
on a variety of platforms running .NET.

Currently the following platforms are supported:

* MonoTouch using [CoreGraphicsGraphics.cs](https://github.com/praeclarum/CrossGraphics/blob/master/CoreGraphicsGraphics.cs)
* MonoDroid using [DroidGraphics.cs](https://github.com/praeclarum/CrossGraphics/blob/master/DroidGraphics.cs)
* Silverlight, Windows Phone 7, and Windows Metro (WinRT) using [SilverlightGraphics.cs](https://github.com/praeclarum/CrossGraphics/blob/master/SilverlightGraphics.cs)
* OpenGL using [OpenGLGraphics.cs](https://github.com/praeclarum/CrossGraphics/blob/master/OpenGLGraphics.cs)
* SVG using [SvgGraphics.cs](https://github.com/praeclarum/CrossGraphics/blob/master/SvgGraphics.cs)


## Using the library

CrossGraphics assumes that you have partitioned your project into parts that will be cross-platform
and parts that are platform-specific.

### In your cross-platform code

Add a reference to the cross-platform file `Graphics.cs`. This will expose the
interface `CrossGraphics.IGraphics` that is the recipient of all drawing commands.

You should now code your objects to be able to draw themselves by being passed and `IGraphics` object.

Drawing commands include:

* **Rectangles** using `FillRect` and `DrawRect`
* **Rounded Rectangles** using `FillRoundedRect` and `DrawRoundedRect`
* **Ovals** using `FillOval` and `DrawOval`
* **Lines** using `DrawLine` and the option `BeginLine` and `EndLine` primitives
* **Images** using `DrawImage`
* **Text** using `DrawString` and the associated font functions



### In your platform-specific code

Add a reference to the appropriate platform-specific implementation of `IGraphics`. 
Create the appropriate graphics context and pass it to your objects that expect
an `IGraphics` object.


## License

The code is copyright Frank A. Krueger and is released under the MIT license.
