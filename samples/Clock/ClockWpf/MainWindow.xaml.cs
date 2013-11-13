using System;
using System.Windows;
using System.Windows.Threading;
using CrossGraphics;

namespace ClockWpf
{
	public partial class MainWindow : Window
	{
		Clock.Clock _clock;
        XamlGraphics _graphics;

		public MainWindow ()
		{
			InitializeComponent ();
			_clock = new Clock.Clock ();
		}

		private void Window_Loaded (object sender, RoutedEventArgs e)
		{
			_graphics = new XamlGraphics (LayoutRoot);

			var timer = new DispatcherTimer {
				Interval = TimeSpan.FromSeconds (1),
			};
			timer.Tick += delegate {
				Draw ();
			};
			timer.Start ();
		}

		void Draw ()
		{
			_graphics.BeginDrawing ();

			_clock.Width = (float)LayoutRoot.ActualWidth;
			_clock.Height = (float)LayoutRoot.ActualHeight;
			_clock.Draw (_graphics);

			_graphics.EndDrawing ();
		}
	}
}
