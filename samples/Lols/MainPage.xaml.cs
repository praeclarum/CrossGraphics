using System.Diagnostics;
using System.Globalization;

using CrossGraphics;
using CrossGraphics.Skia;

namespace Lols;

public partial class MainPage : ContentPage
{
	const int Max = 500;
	int count = 0;

	record Lol (string Text, ValueColor TextColor, float X, float Y);
	List<Lol> lols = new List<Lol> ();

	readonly System.Timers.Timer timer = new System.Timers.Timer(500);
	readonly Stopwatch stopwatch = new Stopwatch();

	public MainPage ()
	{
		InitializeComponent ();
		timer.Elapsed += OnTimer;
		StartTest ();
	}

	void OnTimer(object sender, System.Timers.ElapsedEventArgs e)
	{
		double avg = count / stopwatch.Elapsed.TotalSeconds;
		string text = avg.ToString("0.00", CultureInfo.InvariantCulture);
		Dispatcher.Dispatch(() => UpdateText(text));
	}

	void UpdateText(string text) => LolsPerSecondLabel.Text = text;

	private void OnCounterClicked(object sender, EventArgs e)
	{
		if (!timer.Enabled)
			StartTest ();
	}

	private void OnDraw (object? sender, DrawEventArgs e)
	{
		var g = e.Graphics;
		g.FillRect (0, 0, (float)Width, (float)Height);
		foreach (var lol in lols) {
			g.SetColor (lol.TextColor);
			g.DrawString (lol.Text, lol.X, lol.Y);
		}
	}

	void StartTest()
	{
		count = 0;
		timer.Start();
		stopwatch.Restart();
		_ = Task.Factory.StartNew(RunTest, TaskCreationOptions.LongRunning);
	}

	void RunTest ()
	{
		var random = Random.Shared;

		while (count < 5_000)
		{
			var lol = new Lol
			(
				Text: "lol?",
				TextColor: new ValueColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)),
				X: (float)random.NextDouble() * 200,
				Y: (float)random.NextDouble() * 200
			);
			Dispatcher.Dispatch(() =>
			{
				if (lols.Count >= Max)
					lols.RemoveAt(0);
				lols.Add(lol);
				count++;
				Canvas.InvalidateCanvas ();
			});
			//NOTE: plain Android we could put 1
			Thread.Sleep(1);
		}

		stopwatch.Stop();
		timer.Stop();
	}
}

