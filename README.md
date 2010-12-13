CrossGraphics Library
=====================

CrossGraphics aims to deliver a simple immediate mode interface for drawing graphics
on a variety of platforms running .NET.

Currently the following platforms are supported:

* MonoTouch using `UIKitGraphics.cs`
* MonoDroid using `DroidGraphics.cs`
* Silverlight and Windows Phone 7 using `SilverlightGraphics.cs`


In your cross-platform code
---------------------------

Add a reference to the cross-platform file `Graphics.cs`. This will expose the
interface `CrossGraphics.IGraphics` that is the recipient of all drawing commands.

Drawing commands include:

* **Rectangles** using `FillRect` and `DrawRect`
* **Rounded Rectangles** using `FillRoundedRect` and `DrawRoundedRect`
* **Ovals** using `FillOval` and `DrawOval`
* **Lines** using `DrawLine` and the option `BeginLine` and `EndLine` primitives
* **Images** using `DrawImage`
* **Text** using `DrawString` and the associated font functions


In your platform-specific code
------------------------------

Add a reference to the appropriate platform-specific implementation of `IGraphics`. 
Create the appropriate graphics context and pass it to your objects that expect
an `IGraphics` object.


License
-------

The code is copyright Frank A. Krueger and is released under the MIT license.
