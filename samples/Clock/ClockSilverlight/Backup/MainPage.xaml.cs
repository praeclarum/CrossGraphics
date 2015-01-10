using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CrossGraphics.SilverlightGraphics;
using System.Windows.Threading;

namespace Clock.Silverlight
{
	public partial class MainPage : UserControl
	{
		Clock _clock;
		SilverlightGraphics _graphics;

		public MainPage ()
		{
			InitializeComponent ();
			_clock = new Clock ();
		}

		private void UserControl_Loaded (object sender, RoutedEventArgs e)
		{
			_graphics = new SilverlightGraphics (LayoutRoot);

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
