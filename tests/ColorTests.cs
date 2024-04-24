using System.Drawing;

using Color = CrossGraphics.Color;

namespace CrossGraphicsTests;

public class ColorTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ColorIsNotValueType()
    {
	    Assert.That (!typeof(Color).IsValueType);
    }
    
    [Test]
    public void ValueColorIsValueType()
	{
	    Assert.That (typeof(ValueColor).IsValueType);
	}
    
	[Test]
	public void ValueColorToColor()
	{
		var vc = new ValueColor (0x11, 0x33, 0x55, 0xAA);
		var c = vc.GetColor ();
		Assert.Multiple (() => {
			Assert.That (c.Red, Is.EqualTo (0x11));
			Assert.That (c.Green, Is.EqualTo (0x33));
			Assert.That (c.Blue, Is.EqualTo (0x55));
			Assert.That (c.Alpha, Is.EqualTo (0xAA));
		});
	}

	[Test]
	public void CanSetValueColorOnGraphics ()
	{
		var w = new StringWriter ();
		var g = new SvgGraphics (w, new RectangleF (0, 0, 320, 240));
		var vc = new ValueColor (0x11, 0x33, 0x55, 0xAA);
		g.BeginDrawing ();
		g.SetColor (vc);
		g.FillRect (10, 20, 100, 150);
		g.EndDrawing ();
		var s = w.ToString ();
		Assert.That (s, Does.Contain ("fill=\"#113355\""));
		Assert.That (s, Does.Contain ("fill-opacity=\"0.66"));
	}

	[Test]
	public void ColorCacheColorsWork ()
	{
		var cc = new ColorCache ();
		var c1 = cc.GetColor (0x11, 0x22, 0x33, 0x44);
		Assert.Multiple (() => {
			Assert.That (c1.Red, Is.EqualTo (0x11));
			Assert.That (c1.Green, Is.EqualTo (0x22));
			Assert.That (c1.Blue, Is.EqualTo (0x33));
			Assert.That (c1.Alpha, Is.EqualTo (0x44));
		});
	}

	[Test]
	public void ColorCacheColorsAreTheSameObject ()
	{
		var cc = new ColorCache ();
		var c1 = cc.GetColor (0x11, 0x22, 0x33, 0x44);
		var c2 = cc.GetColor (0x11, 0x22, 0x33, 0x44);
		Assert.That (ReferenceEquals (c1, c2));
	}
	
	[Test]
	public void ColorCacheDifferentColorsAreDifferentObjects ()
	{
		var cc = new ColorCache ();
		var c1 = cc.GetColor (0x11, 0x22, 0x33, 0x44);
		var c2 = cc.GetColor (0x10, 0x22, 0x33, 0x44);
		Assert.That (!ReferenceEquals (c1, c2));
	}
}
