namespace CrossGraphicsTests;

public class AcceptanceTests
{
    [SetUp]
    public void Setup()
    {
    }

    class DrawArgs {

    }

    class Drawing {
        public string Title { get; set; } = string.Empty;
        public Action<DrawArgs> Draw { get; set; } = _ => {};
    }

    void Accept(string name, params Drawing[] drawings)
    {
        var args = new DrawArgs();
        foreach (var drawing in drawings)
        {
            Console.WriteLine($"Drawing: {drawing.Title}");
            drawing.Draw(args);
        }
    }

    [Test]
    public void Ovals()
    {
	    Accept("Ovals",
            new Drawing {
                Title = "Oval 1",
                Draw = args => {
                    // Draw an oval
                    Console.WriteLine("Drawing an oval");
                }
            },
            new Drawing {
                Title = "Oval 2",
                Draw = args => {
                    // Draw another oval
                    Console.WriteLine("Drawing another oval");
                }
            }
        );
    }
}
