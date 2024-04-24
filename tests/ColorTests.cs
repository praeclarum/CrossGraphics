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
}
