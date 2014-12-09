using System;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using CrossGraphics.Android;
using Android.Util;

namespace Clock.Android
{
	[Activity (Label = "ClockAndroid", MainLauncher = true, Icon = "@drawable/icon")]
	public class Activity1 : Activity
	{
		ClockView _view;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			System.Diagnostics.Debug.WriteLine ("Density: " + Resources.DisplayMetrics.Density + ", DensityDpi: " + Resources.DisplayMetrics.DensityDpi + ", HeightPixels: " + Resources.DisplayMetrics.HeightPixels + ", ScaledDensity: " + Resources.DisplayMetrics.ScaledDensity + ", WidthPixels: " + Resources.DisplayMetrics.WidthPixels + ", Xdpi: " + Resources.DisplayMetrics.Xdpi + ", Ydpi: " + Resources.DisplayMetrics.Ydpi);

			_view = new ClockView (this, 2.5f);
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

			public ClockView (Context context, float scaledDensity) : base (context)
			{
			
			}

			public ClockView (Context context, float scaledDensity, IAttributeSet attrs) : base (context, attrs)
			{

			}

			public ClockView (Context context, float scaledDensity, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
			{

			}


        //protected View(IntPtr javaReference, JniHandleOwnership transfer);
        //[Register(".ctor", "(Landroid/content/Context;Landroid/util/AttributeSet;I)V", "")]



			public override void Draw (global::Android.Graphics.Canvas canvas)
			{
				base.Draw (canvas);

				var g = new AndroidGraphics (canvas, Context);
				_clock.Width = Width / Resources.DisplayMetrics.ScaledDensity;
				_clock.Height = Height / Resources.DisplayMetrics.ScaledDensity;

				_clock.Draw (g);
			}
		}
	}
}
