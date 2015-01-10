using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using CrossGraphics;

namespace Clock.WP8
{
	public partial class MainPage : PhoneApplicationPage
	{
		Clock _clock;
		XamlGraphics _graphics;

		public MainPage ()
		{
			InitializeComponent ();

			_clock = new Clock ();
		}

		private void PhoneApplicationPage_Loaded (object sender, RoutedEventArgs e)
		{
			//
			// Initialize the graphics context
			//
            _graphics = new XamlGraphics(LayoutRoot);

			//
			// Create a timer to refresh the clock
			//
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
			_clock.Width = (float)LayoutRoot.ActualWidth;
			_clock.Height = (float)LayoutRoot.ActualHeight;

			_graphics.BeginDrawing ();

			_clock.Draw (_graphics);

			_graphics.EndDrawing ();
		}
	}
}
