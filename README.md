# CrossGraphics

CrossGraphics aims to deliver a simple immediate mode interface for drawing graphics
on a variety of platforms running .NET.

Currently the following platforms are supported:

* iOS and macOS using [CoreGraphicsGraphics.cs](https://github.com/praeclarum/CrossGraphics/blob/master/CoreGraphicsGraphics.cs)
* Android using [AndroidGraphics.cs](https://github.com/praeclarum/CrossGraphics/blob/master/AndroidGraphics.cs)
* Cross-platform SkiaSharp using [SkiaSharpGraphics.cs](https://github.com/praeclarum/CrossGraphics/blob/master/SkiaGraphics.cs)
* Cross-platform SVG using [SvgGraphics.cs](https://github.com/praeclarum/CrossGraphics/blob/master/SvgGraphics.cs)


## Installation

```bash
dotnet add package CrossGraphics
```


## Using the library

The interface `CrossGraphics.IGraphics` is the recipient of all drawing commands.

You should now code your objects to be able to draw themselves by being passed and `IGraphics` object.

Drawing commands include:

* **Rectangles** using `FillRect` and `DrawRect`
* **Rounded Rectangles** using `FillRoundedRect` and `DrawRoundedRect`
* **Ovals** using `FillOval` and `DrawOval`
* **Lines** using `DrawLine` and the option `BeginLine` and `EndLine` primitives
* **Images** using `DrawImage`
* **Text** using `DrawString` and the associated font functions

## Development

* **Run Acceptance Tests**

```bash
dotnet run --project atests/CrossGraphicsAcceptanceTests.csproj -- $(pwd)
```

## License

The code is copyright Frank A. Krueger and is released under the MIT license.
