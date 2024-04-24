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
}
