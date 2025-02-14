using System.Drawing;
using CrossGraphics;

namespace CrossGraphicsTests;

public class AcceptanceTests
{
    static string OutputPath = GetOutputPath ();
    static string AcceptedPath = Path.Combine(OutputPath, "AcceptedTests");
    static string PendingPath = Path.Combine(OutputPath, "PendingTests");
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
        public abstract (IGraphics, object) BeginDrawing(int width, int height);
        public abstract string SaveDrawing(IGraphics graphics, object context, string dir, string name);
    }

    class SvgPlatform : Platform
    {
        public override string Name => "SVG";
        public override (IGraphics, object) BeginDrawing(int width, int height)
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
        public override (IGraphics, object) BeginDrawing(int width, int height)
        {
            var cgContext = new CoreGraphics.CGBitmapContext(null, width, height, 8, width * 4, CoreGraphics.CGColorSpace.CreateDeviceRGB(), CoreGraphics.CGBitmapFlags.PremultipliedLast);
            var g = new CrossGraphics.CoreGraphics.CoreGraphicsGraphics(cgContext, highQuality: true);
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

    static readonly Platform[] Platforms = {
        new SvgPlatform(),
        #if __MACOS__ || __IOS__ || __MACCATALYST__
        new CoreGraphicsPlatform(),
        #endif
    };

    void Accept(string name, params Drawing[] drawings)
    {
        var w = new StringWriter();
        w.WriteLine($"<html><head><title>{name} - CrossGraphics Test</title></head><body>");
        w.WriteLine($"<h1>{name}</h1>");
        w.WriteLine($"<table>");
        w.WriteLine($"<tr><th>Drawing</th></tr>");

        var width = 100;
        var height = 100;
        
        foreach (var drawing in drawings) {
            w.Write($"<tr><th>{drawing.Title}</th>");
            foreach (var platform in Platforms) {
                var (graphics, context) = platform.BeginDrawing(width, height);
                drawing.Draw(new DrawArgs(graphics, width, height));
                var filename = platform.SaveDrawing(graphics, context, PendingPath, drawing.Title + "_" + platform.Name);
                w.Write($"<td><img src=\"{filename}\" alt=\"{drawing.Title} on {platform.Name}\" width=\"{width}\" height=\"{height}\" /></td>");
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
        Drawing Make(float startAngle, float endAngle, float w) {
            return new Drawing {
                Title = $"Arc_S{startAngle:F2}_E{endAngle:F2}_L{w:F2}",
                Draw = args => {
                    args.Graphics.SetRgba(0, 0, 128, 255);
                    var cx = args.Width / 2;
                    var cy = args.Height / 2;
                    var radius = Math.Min(args.Width, args.Height) / 3;
                    args.Graphics.DrawArc(cx, cy, radius, startAngle, endAngle, w);
                }
            };
        }
	    Accept("Arcs",
            Make(0, MathF.PI*2.00f, 10),
            Make(0, MathF.PI*1.75f, 10),
            Make(0, MathF.PI*1.50f, 10),
            Make(0, MathF.PI*1.25f, 10),
            Make(0, MathF.PI*1.00f, 10),
            Make(0, MathF.PI*0.75f, 10),
            Make(0, MathF.PI*0.50f, 10),
            Make(0, MathF.PI*0.25f, 10),
            Make(0, MathF.PI*0.00f, 10),
            Make(MathF.PI*1.25f, MathF.PI*2.00f, 10),
            Make(MathF.PI*1.25f, MathF.PI*1.75f, 10),
            Make(MathF.PI*1.25f, MathF.PI*1.50f, 10),
            Make(MathF.PI*1.25f, MathF.PI*1.25f, 10),
            Make(MathF.PI*1.25f, MathF.PI*1.00f, 10),
            Make(MathF.PI*1.25f, MathF.PI*0.75f, 10),
            Make(MathF.PI*1.25f, MathF.PI*0.50f, 10),
            Make(MathF.PI*1.25f, MathF.PI*0.25f, 10),
            Make(MathF.PI*1.25f, MathF.PI*0.00f, 10),
            Make(MathF.PI*1.25f, -MathF.PI*2.00f, 10),
            Make(MathF.PI*1.25f, -MathF.PI*1.75f, 10),
            Make(MathF.PI*1.25f, -MathF.PI*1.50f, 10),
            Make(MathF.PI*1.25f, -MathF.PI*1.25f, 10),
            Make(MathF.PI*1.25f, -MathF.PI*1.00f, 10),
            Make(MathF.PI*1.25f, -MathF.PI*0.75f, 10),
            Make(MathF.PI*1.25f, -MathF.PI*0.50f, 10),
            Make(MathF.PI*1.25f, -MathF.PI*0.25f, 10),
            Make(MathF.PI*1.25f, -MathF.PI*0.00f, 10),
            Make(-MathF.PI*1.25f, MathF.PI*2.00f, 10),
            Make(-MathF.PI*1.25f, MathF.PI*1.75f, 10),
            Make(-MathF.PI*1.25f, MathF.PI*1.50f, 10),
            Make(-MathF.PI*1.25f, MathF.PI*1.25f, 10),
            Make(-MathF.PI*1.25f, MathF.PI*1.00f, 10),
            Make(-MathF.PI*1.25f, MathF.PI*0.75f, 10),
            Make(-MathF.PI*1.25f, MathF.PI*0.50f, 10),
            Make(-MathF.PI*1.25f, MathF.PI*0.25f, 10),
            Make(-MathF.PI*1.25f, MathF.PI*0.00f, 10),
            Make(-MathF.PI*1.25f, -MathF.PI*2.00f, 10),
            Make(-MathF.PI*1.25f, -MathF.PI*1.75f, 10),
            Make(-MathF.PI*1.25f, -MathF.PI*1.50f, 10),
            Make(-MathF.PI*1.25f, -MathF.PI*1.25f, 10),
            Make(-MathF.PI*1.25f, -MathF.PI*1.00f, 10),
            Make(-MathF.PI*1.25f, -MathF.PI*0.75f, 10),
            Make(-MathF.PI*1.25f, -MathF.PI*0.50f, 10),
            Make(-MathF.PI*1.25f, -MathF.PI*0.25f, 10),
            Make(-MathF.PI*1.25f, -MathF.PI*0.00f, 10)
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
}
