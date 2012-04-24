using System;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using CrossGraphics.Droid;

namespace Clock.Android
{
	[Activity (Label = "ClockAndroid", MainLauncher = true, Icon = "@drawable/icon")]
	public class Activity1 : Activity
	{
		ClockView _view;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			_view = new ClockView (this);
			SetContentView (_view);

			var timer = new Timer (1);
			timer.Elapsed += delegate {
				RunOnUiThread (delegate {
					_view.Invalidate ();
				});
			};
			timer.Start ();
		}

		class ClockView : View
		{
			Clock _clock = new Clock ();

			public ClockView (Context context)
				: base (context)
			{
			}

			public override void Draw (global::Android.Graphics.Canvas canvas)
			{
				base.Draw (canvas);

				var g = new DroidGraphics (canvas);

				_clock.Width = Width;
				_clock.Height = Height;

				_clock.Draw (g);
			}
		}
	}
}
