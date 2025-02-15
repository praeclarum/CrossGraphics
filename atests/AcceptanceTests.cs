using System.Drawing;

using CoreGraphics;

using CrossGraphics;

using Font = Microsoft.Maui.Font;
using LineBreakMode = CrossGraphics.LineBreakMode;
using TextAlignment = CrossGraphics.TextAlignment;

namespace CrossGraphicsTests;

public class AcceptanceTests
{
    static readonly string OutputPath = GetOutputPath ();
    static readonly string AcceptedPath = Path.Combine(OutputPath, "AcceptedTests");
    static readonly string PendingPath = Path.Combine(OutputPath, "PendingTests");
    static string GetOutputPath ()
    {
	    var dir = Environment.GetCommandLineArgs ()[^1];
	    if (Path.GetFileName (dir) != "CrossGraphics") {
		    dir = Path.GetTempPath ();
	    }
	    return dir;
    }
    static AcceptanceTests()
    {
        if (!Directory.Exists(AcceptedPath))
            Directory.CreateDirectory(AcceptedPath);
        if (!Directory.Exists(PendingPath))
            Directory.CreateDirectory(PendingPath);
    }
    public void Setup()
    {
	    #if __MACOS__
	    AppKit.NSApplication.Init ();
	    #endif
    }

    record DrawArgs(IGraphics Graphics, int Width, int Height)
    {
    }

    class Drawing {
        public string Title { get; set; } = string.Empty;
        public Action<DrawArgs> Draw { get; set; } = _ => {};
    }

    abstract class Platform {
        public abstract string Name { get; }
        public abstract (IGraphics, object?) BeginDrawing(int width, int height);
        public abstract string SaveDrawing(IGraphics graphics, object context, string dir, string name);
    }

    class SvgPlatform : Platform
    {
        public override string Name => "SVG";
        public override (IGraphics, object?) BeginDrawing(int width, int height)
        {
            var w = new StringWriter();
            var g = new SvgGraphics(w, new Rectangle(0, 0, width, height));
            g.BeginDrawing();
            return (g, w);
        }
        public override string SaveDrawing(IGraphics graphics, object context, string dir, string name)
        {
            var fullName = name + ".svg";
            if (graphics is SvgGraphics svgGraphics && context is StringWriter writer)
            {
                svgGraphics.EndDrawing();
                var svg = writer.ToString();
                File.WriteAllText(Path.Combine(dir, fullName), svg);
            }
            return fullName;
        }
    }

    #if __MACOS__ || __IOS__ || __MACCATALYST__
    class CoreGraphicsPlatform : Platform
    {
        public override string Name => "CoreGraphics";
        public override (IGraphics, object?) BeginDrawing(int width, int height)
        {
            var cgContext = new CoreGraphics.CGBitmapContext(null, width, height, 8, width * 4, CoreGraphics.CGColorSpace.CreateDeviceRGB(), CoreGraphics.CGBitmapFlags.PremultipliedLast);
            var g = new CrossGraphics.CoreGraphics.CoreGraphicsGraphics(cgContext, highQuality: true, flipText: false);
            return (g, cgContext);
        }
        public override string SaveDrawing(IGraphics graphics, object context, string dir, string name)
        {
            var fullName = name + ".png";
            var url = Foundation.NSUrl.FromFilename (Path.Combine (dir, fullName));
            if (graphics is not CrossGraphics.CoreGraphics.CoreGraphicsGraphics coreGraphics ||
                context is not CoreGraphics.CGBitmapContext cgContext || cgContext.ToImage () is not { } cgImage ||
                ImageIO.CGImageDestination.Create (url, "public.png", 1) is not { } d) {
	            return fullName;
            }
            d.AddImage (cgImage, options: null);
            d.Close ();
            return fullName;
        }
    }
    class CoreGraphicsFlippedPlatform : Platform
    {
        public override string Name => "CoreGraphicsFlipped";
        public override (IGraphics, object?) BeginDrawing(int width, int height)
        {
            var cgContext = new CoreGraphics.CGBitmapContext(null, width, height, 8, width * 4, CoreGraphics.CGColorSpace.CreateDeviceRGB(), CoreGraphics.CGBitmapFlags.PremultipliedLast);
            cgContext.TranslateCTM (0, height);
            cgContext.ScaleCTM (1, -1);
            var g = new CrossGraphics.CoreGraphics.CoreGraphicsGraphics(cgContext, highQuality: true, flipText: true);
            return (g, cgContext);
        }
        public override string SaveDrawing(IGraphics graphics, object context, string dir, string name)
        {
            var fullName = name + ".png";
            var url = Foundation.NSUrl.FromFilename (Path.Combine (dir, fullName));
            if (graphics is not CrossGraphics.CoreGraphics.CoreGraphicsGraphics coreGraphics ||
                context is not CoreGraphics.CGBitmapContext cgContext || cgContext.ToImage () is not { } cgImage ||
                ImageIO.CGImageDestination.Create (url, "public.png", 1) is not { } d) {
	            return fullName;
            }
            d.AddImage (cgImage, options: null);
            d.Close ();
            return fullName;
        }
    }
    #endif
    #if __IOS__ || __MACCATALYST__
    class UIGraphicsPlatform : Platform {
	    public override string Name => "UIGraphics";
	    public override (IGraphics, object?) BeginDrawing (int width, int height)
	    {
		    UIGraphics.BeginImageContextWithOptions(new CGSize (width, height), false, 1);
		    var graphics = new CrossGraphics.CoreGraphics.UIKitGraphics (highQuality: true);
		    return (graphics, null);
	    }
	    public override string SaveDrawing (IGraphics graphics, object context, string dir, string name)
	    {
		    var uiImage = UIGraphics.GetImageFromCurrentImageContext ();
		    UIGraphics.EndImageContext ();
		    var fullName = name + ".png";
		    uiImage.AsPNG ()?.Save (Path.Join (dir, fullName), atomically: true);
		    return fullName;
	    }
    }
    #endif
	class SkiaPlatform : Platform
	{
		public override string Name => "Skia";
		public override (IGraphics, object?) BeginDrawing (int width, int height)
		{
			var bitmap = new SkiaSharp.SKBitmap (width: width, height: height, isOpaque: false);
			var canvas = new SkiaSharp.SKCanvas (bitmap);
			var graphics = new CrossGraphics.Skia.SkiaGraphics (canvas);
			return (graphics, bitmap);
		}

		public override string SaveDrawing (IGraphics graphics, object context, string dir, string name)
		{
			var fullName = name + ".png";
			if (graphics is CrossGraphics.Skia.SkiaGraphics sg && context is SkiaSharp.SKBitmap bitmap) {
				sg.Canvas.Flush ();
				using var image = SkiaSharp.SKImage.FromBitmap (bitmap);
				using var data = image.Encode (SkiaSharp.SKEncodedImageFormat.Png, 100);
				using var stream = File.OpenWrite (Path.Combine (dir, fullName));
				data.SaveTo (stream);
			}

			return fullName;
		}
	}

	static readonly Platform[] Platforms = {
        new SvgPlatform(),
        #if __MACOS__ || __IOS__ || __MACCATALYST__
        // new CoreGraphicsPlatform(),
        new CoreGraphicsFlippedPlatform (),
        #endif
        #if __IOS__ || __MACCATALYST__
        new UIGraphicsPlatform(),
        #endif
		new SkiaPlatform(),
    };

    void Accept(string name, params Drawing[] drawings)
    {
        var w = new StringWriter();
        w.WriteLine($"<html><head><title>{name} - CrossGraphics Test</title>");
        w.WriteLine($"<style>");
        w.WriteLine($"html {{ font-family: sans-serif; background-color: #333; color: #fff; }}");
        w.WriteLine($"img {{ background-color: #fff; }}");
        w.WriteLine($"</style>");
        w.WriteLine($"</head><body>");
        w.WriteLine($"<h1>{name}</h1>");
        w.WriteLine($"<table border=\"0\" cellspacing=\"8\">");
        w.Write($"<tr><th>Drawing</th>");
        foreach (var platform in Platforms)
        {
	        w.Write ($"<th>{platform.Name}</th>");
        }
        w.WriteLine($"</tr>");

        var width = 100;
        var height = 100;
        
        foreach (var drawing in drawings) {
            w.Write($"<tr><th>{drawing.Title}</th>");
            foreach (var platform in Platforms) {
                var (graphics, context) = platform.BeginDrawing(width, height);
                drawing.Draw(new DrawArgs(graphics, width, height));
                var filename = platform.SaveDrawing(graphics, context, PendingPath, drawing.Title + "_" + platform.Name);
                var irender = filename.EndsWith (".svg") ? "smooth" : "crisp-edges";
                w.Write($"<td><img src=\"{filename}\" alt=\"{drawing.Title} on {platform.Name}\" width=\"{width*3}\" height=\"{height*3}\" image-rendering=\"{irender}\" /></td>");
            }
            w.WriteLine("</tr>");
        }
    
        w.WriteLine("</table>");
        w.WriteLine("</body></html>");
        var pendingHTML = w.ToString();
        File.WriteAllText(Path.Combine(PendingPath, name + ".html"), pendingHTML);
    }

    public void Arcs()
    {
        Drawing Make(float startAngle, float endAngle, float w=5) {
            return new Drawing {
                Title = $"Arc_S{startAngle*180.0f/MathF.PI:F2}_E{endAngle*180.0f/MathF.PI:F2}",
                Draw = args => {
                    args.Graphics.SetRgba(0, 0, 128, 255);
                    var radius = MathF.Min(args.Width, args.Height) * 0.2f;
                    var cxs = args.Width / 2 - radius - w/2;
                    var cxf = args.Width / 2 + radius + w/2;
                    var cy = args.Height / 2;
                    args.Graphics.DrawArc(cxs, cy, radius, startAngle, endAngle, w);
                    args.Graphics.FillArc(cxf, cy, radius, startAngle, endAngle);
                }
            };
        }
	    Accept("Arcs",
		    Make (0, MathF.PI * 2.00f),
		    Make (0, MathF.PI * 2.25f),
		    Make (-MathF.PI * 0.25f, MathF.PI * 2.25f),
		    Make (MathF.PI * 1.25f, -MathF.PI / 180.0f * 120.0f),
		    Make (MathF.PI * 1.25f, -MathF.PI / 180.0f * 135.0f),
		    Make (MathF.PI * 1.25f, -MathF.PI / 180.0f * 150.0f),
		    Make (MathF.PI * 1.25f, -MathF.PI / 180.0f * 179.0f),
		    Make (MathF.PI * 1.25f, -MathF.PI / 180.0f * 180.0f),
		    Make (MathF.PI * 1.25f, -MathF.PI / 180.0f * 181.0f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 1.25f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 2.50f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 2.75f),
		    Make (0, MathF.PI * 1.75f),
		    Make (0, MathF.PI * 1.50f),
		    Make (0, MathF.PI * 1.25f),
		    Make (0, MathF.PI * 1.00f),
		    Make (0, MathF.PI * 0.75f),
		    Make (0, MathF.PI * 0.50f),
		    Make (0, MathF.PI * 0.25f),
		    Make (0, MathF.PI * 0.00f),
		    Make (MathF.PI * 1.25f, MathF.PI * 2.00f),
		    Make (MathF.PI * 1.25f, MathF.PI * 1.75f),
		    Make (MathF.PI * 1.25f, MathF.PI * 1.50f),
		    Make (MathF.PI * 1.25f, MathF.PI * 1.25f),
		    Make (MathF.PI * 1.25f, MathF.PI * 1.00f),
		    Make (MathF.PI * 1.25f, MathF.PI * 0.75f),
		    Make (MathF.PI * 1.25f, MathF.PI * 0.50f),
		    Make (MathF.PI * 1.25f, MathF.PI * 0.25f),
		    Make (MathF.PI * 1.25f, MathF.PI * 0.00f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 2.00f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 1.75f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 1.50f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 1.25f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 1.00f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 0.75f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 0.50f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 0.25f),
		    Make (MathF.PI * 1.25f, -MathF.PI * 0.00f),
		    Make (-MathF.PI * 1.25f, MathF.PI * 2.00f),
		    Make (-MathF.PI * 1.25f, MathF.PI * 1.75f),
		    Make (-MathF.PI * 1.25f, MathF.PI * 1.50f),
		    Make (-MathF.PI * 1.25f, MathF.PI * 1.25f),
		    Make (-MathF.PI * 1.25f, MathF.PI * 1.00f),
		    Make (-MathF.PI * 1.25f, MathF.PI * 0.75f),
		    Make (-MathF.PI * 1.25f, MathF.PI * 0.50f),
		    Make (-MathF.PI * 1.25f, MathF.PI * 0.25f),
		    Make (-MathF.PI * 1.25f, MathF.PI * 0.00f),
		    Make (-MathF.PI * 1.25f, -MathF.PI * 2.00f),
		    Make (-MathF.PI * 1.25f, -MathF.PI * 1.75f),
		    Make (-MathF.PI * 1.25f, -MathF.PI * 1.50f),
		    Make (-MathF.PI * 1.25f, -MathF.PI * 1.25f),
		    Make (-MathF.PI * 1.25f, -MathF.PI * 1.00f),
		    Make (-MathF.PI * 1.25f, -MathF.PI * 0.75f),
		    Make (-MathF.PI * 1.25f, -MathF.PI * 0.50f),
		    Make (-MathF.PI * 1.25f, -MathF.PI * 0.25f),
		    Make (-MathF.PI * 1.25f, -MathF.PI * 0.00f)
        );
    }

    public void Ovals()
    {
        Drawing Make(float width, float height, float w) {
            return new Drawing {
                Title = $"Oval_W{width:F2}_H{height:F2}_L{w:F2}",
                Draw = args => {
                    args.Graphics.SetRgba(0, 0, 128, 255);
                    var x = args.Width / 2 - width / 2;
                    var y = args.Height / 2 - height / 2;
                    args.Graphics.DrawOval(new RectangleF(x, y, width, height), w);
                }
            };
        }
	    Accept("Ovals",
            Make(50, 50, 1),
            Make(50, 50, 10),
            Make(50, 50, 50),
            Make(50, 100, 1),
            Make(50, 100, 10),
            Make(50, 100, 50),
            Make(100, 100, 1),
            Make(100, 100, 10),
            Make(100, 100, 50)
        );
    }

    public void Text()
    {
	    string singleLine = "A single line of text.";
        Drawing MakeRect(string s, string? fontFamily, int fontSize, LineBreakMode lineBreakMode, TextAlignment align) {
            return new Drawing {
                Title = $"Rect_{s.Length}_F{fontFamily}_S{fontSize}_{lineBreakMode}_{align}",
                Draw = args => {
                    args.Graphics.SetRgba(0, 0, 0, 128);
                    var pad = 4;
                    args.Graphics.DrawRect (pad, pad, args.Width - pad * 2, args.Height - pad * 2, 1);
                    var font = fontFamily is {} fn ? CrossGraphics.Font.BoldUserFixedPitchFontOfSize (fontSize) : CrossGraphics.Font.SystemFontOfSize (fontSize);
                    args.Graphics.SetFont (font);
                    var fm = args.Graphics.GetFontMetrics ();
                    args.Graphics.SetRgba(0, 255, 0, 128);
                    args.Graphics.DrawLine (pad, pad + fm.Ascent, args.Width - pad, pad + fm.Ascent, 1);
                    args.Graphics.SetRgba(255, 0, 0, 128);
                    args.Graphics.DrawLine (pad, pad + fm.Ascent + fm.Descent, args.Width - pad, pad + fm.Ascent + fm.Descent, 1);
                    args.Graphics.SetRgba(0, 0, 128, 255);
                    args.Graphics.DrawString (s, pad, pad, args.Width-2*pad, args.Height-2*pad,lineBreakMode, align);
                }
            };
        }
        var otherFam = "BoldUserFixedPitch";
	    Accept("Text",
		    MakeRect (singleLine, null, 14, LineBreakMode.None, TextAlignment.Left),
		    MakeRect (singleLine, null, 14, LineBreakMode.None, TextAlignment.Center),
		    MakeRect (singleLine, null, 14, LineBreakMode.None, TextAlignment.Right),
		    MakeRect (singleLine, null, 22, LineBreakMode.None, TextAlignment.Left),
		    MakeRect (singleLine, null, 22, LineBreakMode.None, TextAlignment.Center),
		    MakeRect (singleLine, null, 22, LineBreakMode.None, TextAlignment.Right),
		    MakeRect (singleLine, otherFam, 14, LineBreakMode.None, TextAlignment.Left),
		    MakeRect (singleLine, otherFam, 14, LineBreakMode.None, TextAlignment.Center),
		    MakeRect (singleLine, otherFam, 14, LineBreakMode.None, TextAlignment.Right),
		    MakeRect (singleLine, otherFam, 22, LineBreakMode.None, TextAlignment.Left),
		    MakeRect (singleLine, otherFam, 22, LineBreakMode.None, TextAlignment.Center),
		    MakeRect (singleLine, otherFam, 22, LineBreakMode.None, TextAlignment.Right)
        );
    }
}
