//
// Copyright (c) 2012 Frank A. Krueger
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;


namespace CrossGraphics.Android
{
	public class AndroidGraphicsCanvas : global::Android.Views.View, ICanvas
	{
		int _fps;
		global::Android.OS.Handler _handler;

		const double CpuUtilization = 0.25;
		public int MinFps { get; set; }
		public int MaxFps { get; set; }
		static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(1.0);

		double _drawTime;
		int _drawCount;
		DateTime _lastThrottleTime = DateTime.Now;

		CanvasContent _content;
		public CanvasContent Content
		{
			get { return _content; }
			set
			{
				if (_content != value) {
					if (_content != null) {
						_content.NeedsDisplay -= HandleNeedsDisplay;
					}
					_content = value;
					if (_content != null) {
						_content.NeedsDisplay += HandleNeedsDisplay;
					}
				}
			}
		}

		public AndroidGraphicsCanvas (global::Android.Content.Context context, global::Android.Util.IAttributeSet attrs)
			: base (context, attrs)
		{
			Initialize ();
		}

		public AndroidGraphicsCanvas (global::Android.Content.Context context)
			: base (context)
		{
			Initialize ();
		}

		void Initialize ()
		{
			_touchMan = new AndroidCanvasTouchManager (0);

			MinFps = 4;
			MaxFps = 30;
			_fps = (MinFps + MaxFps) / 2;
			_handler = new global::Android.OS.Handler ();
		}

		#region Touching

		AndroidCanvasTouchManager _touchMan;

		public override bool OnTouchEvent (global::Android.Views.MotionEvent e)
		{
			var del = Content;
			if (del != null) {
				_touchMan.OnTouchEvent (e, Content);
				return true;
			}
			else {
				return false;
			}
		}

		#endregion

		#region Drawing

		void HandleNeedsDisplay (object sender, EventArgs e)
		{
			Invalidate ();
		}

		void HandleDrawTimerElapsed ()
		{
			Invalidate ();
		}

		bool _running = false;

		public void Start()
		{
			_running = true;
			_drawCount = 0;
			_drawTime = 0;
			_lastThrottleTime = DateTime.Now;
			Invalidate ();
		}

		public void Stop()
		{
			_running = false;
		}

		public event EventHandler DrewFrame;

		public override void Draw (global::Android.Graphics.Canvas canvas)
		{
			//
			// Start drawing
			//
			var del = Content;
			if (del == null) return;

			var startT = DateTime.Now;

			var _graphics = new AndroidGraphics (canvas);

			//
			// Draw
			//
			del.Frame = new RectangleF (0, 0, Width, Height);
			try {
				del.Draw (_graphics);
			}
			catch (Exception) {
			}

			var endT = DateTime.Now;

			_drawTime += (endT - startT).TotalSeconds;
			_drawCount++;

			//
			// Throttle
			//
			if (_drawCount > 2 && (DateTime.Now - _lastThrottleTime) >= ThrottleInterval) {

				_lastThrottleTime = DateTime.Now;

				var maxfps = 1.0 / (_drawTime / _drawCount);
				_drawTime = 0;
				_drawCount = 0;

				var fps = ClampUpdateFreq((int)(maxfps * CpuUtilization));

				if (Math.Abs(fps - _fps) > 1) {
					_fps = fps;
					Start();
				}
			}

			//
			// Notify
			//
			var df = DrewFrame;
			if (df != null) {
				df(this, EventArgs.Empty);
			}

			if (_running) {
				_handler.PostDelayed (HandleDrawTimerElapsed, 1000 / _fps);
			}
		}

		int ClampUpdateFreq(int fps)
		{
			return Math.Min(MaxFps, Math.Max(MinFps, fps));
		}

		#endregion
	}

	public class AndroidCanvasTouchManager
	{
		const int MaxTouchId = 10;
		AndroidTouch[] _activeTouches = new AndroidTouch[MaxTouchId];

		float _initialMoveResolution;

		class AndroidTouch : CanvasTouch
		{
			public bool IsMoving;
		}

		public AndroidCanvasTouchManager (float initialMoveResolution = 4)
		{
			_initialMoveResolution = initialMoveResolution;
		}

		public Func<PointF, PointF> LocationFromViewLocationFunc { get; set; }

		PointF LocationFromView (PointF viewLocation)
		{
			var f = LocationFromViewLocationFunc;
			if (f != null) {
				return f (viewLocation);
			}
			else {
				return viewLocation;
			}
		}

		public void OnTouchEvent (global::Android.Views.MotionEvent e, CanvasContent content)
		{
			if (e == null) throw new ArgumentNullException ("e");
			if (content == null) throw new ArgumentNullException ("del");

			var began = new List<CanvasTouch> ();
			var moved = new List<CanvasTouch> ();
			var ended = new List<CanvasTouch> ();
			var cancelled = new List<CanvasTouch> ();

			var pointerCount = e.PointerCount;
			var actionMasked = e.Action & global::Android.Views.MotionEventActions.Mask;
			var actionIndex = (int)(e.Action & global::Android.Views.MotionEventActions.PointerIdMask) >> (int)global::Android.Views.MotionEventActions.PointerIdShift;
			var actionId = e.GetPointerId (actionIndex);

			//Log.WriteLine("a = {0}, index = {1}, id = {2}, c = {3}", actionMasked, actionIndex, actionId, pointerCount);

			switch (actionMasked) {
			case global::Android.Views.MotionEventActions.Move:
				for (var index = 0; index < pointerCount; index++) {
					var id = e.GetPointerId (index);
					if (id < MaxTouchId) {
						var t = _activeTouches[id];
						if (t != null) {
							var curSuperLoc = t.SuperCanvasLocation;
							var newSuperLoc = new System.Drawing.PointF (e.GetX (index), e.GetY (index));
							if (t.IsMoving ||
								(Math.Abs (curSuperLoc.X - newSuperLoc.X) > _initialMoveResolution ||
								Math.Abs (curSuperLoc.Y - newSuperLoc.Y) > _initialMoveResolution)) {
								t.SuperCanvasPreviousLocation = t.SuperCanvasLocation;
								t.CanvasPreviousLocation = t.CanvasLocation;
								t.PreviousTime = t.Time;
								t.IsMoving = true;

								t.SuperCanvasLocation = newSuperLoc;
								t.CanvasLocation = LocationFromView (t.SuperCanvasLocation);
								t.Time = DateTime.UtcNow;

								moved.Add (t);
							}
						}
					}
				}
				break;
			case global::Android.Views.MotionEventActions.Down:
			case global::Android.Views.MotionEventActions.PointerDown:
				if (actionId < MaxTouchId) {
					var t = new AndroidTouch {
						Handle = new IntPtr (actionId + 1), // +1 because IntPtr=0 is special
						SuperCanvasLocation = new System.Drawing.PointF (e.GetX (actionIndex), e.GetY (actionIndex)),
						Time = DateTime.UtcNow,
					};
					t.CanvasLocation = LocationFromView (t.SuperCanvasLocation);
					t.CanvasPreviousLocation = t.CanvasLocation;
					t.SuperCanvasPreviousLocation = t.SuperCanvasLocation;
					t.PreviousTime = t.Time;
					began.Add (t);
					_activeTouches[actionId] = t;
				}
				break;
			case global::Android.Views.MotionEventActions.Up:
			case global::Android.Views.MotionEventActions.PointerUp:
				if (actionId < MaxTouchId) {
					var t = _activeTouches[actionId];
					if (t != null) {
						t.Time = DateTime.UtcNow;
						_activeTouches[actionId] = null;
						ended.Add (t);
					}
				}
				break;
			}

			if (began.Count > 0) {
				content.TouchesBegan (began.ToArray ());
			}
			if (moved.Count > 0) {
				content.TouchesMoved (moved.ToArray ());
			}
			if (ended.Count > 0) {
				content.TouchesEnded (ended.ToArray ());
			}
			if (cancelled.Count > 0) {
				content.TouchesCancelled (cancelled.ToArray ());
			}
		}
	}
}
