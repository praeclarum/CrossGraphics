//
// Copyright (c) 2010-2013 Frank A. Krueger
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

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;
using DispatcherTimerTickEventArgs = System.Object;
using NativeColors = Windows.UI.Colors;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DispatcherTimerTickEventArgs = System.EventArgs;
using NativeColors = System.Windows.Media.Colors;
#endif

namespace CrossGraphics
{
    public class XamlCanvas : Canvas, ICanvas
    {
        const int NativePointsPerInch = 160;

        XamlGraphics _graphics;

        int _fps = 20;
        readonly DispatcherTimer _drawTimer;

        const double CpuUtilization = 0.25;
        public int MinFps { get; set; }
        public int MaxFps { get; set; }
        static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(1.0);

        double _drawTime;
        int _drawCount;
        DateTime _lastThrottleTime = DateTime.Now;

		CanvasContent content;
		public CanvasContent Content
		{
			get
			{
				return content;
			}
			set
			{
				if (content != value) {
					if (content != null) {
						content.NeedsDisplay -= OnNeedsDisplay;
					}
					content = value;
					if (content != null) {
						content.NeedsDisplay += OnNeedsDisplay;
					}
				}
			}
		}

		public bool Continuous { get; set; }

        public XamlCanvas ()
        {
			Background = new SolidColorBrush (NativeColors.Transparent);

            MinFps = 4;
            MaxFps = 30;
			Continuous = true;

            _drawTimer = new DispatcherTimer();
            _drawTimer.Tick += DrawTick;

            Unloaded += HandleUnloaded;
            Loaded += HandleLoaded;
			LayoutUpdated += delegate { HandleLayoutUpdated (); };
        }

		double lastUpdateWidth = -1, lastUpdateHeight = -1;

		void HandleLayoutUpdated ()
		{
			if (Content != null && (Math.Abs (lastUpdateWidth - ActualWidth) > 0.5 || Math.Abs (lastUpdateHeight - ActualHeight) > 0.5)) {
				lastUpdateWidth = ActualWidth;
				lastUpdateHeight = ActualHeight;
#if NETFX_CORE
#pragma warning disable 4014
				Dispatcher.RunAsync (Windows.UI.Core.CoreDispatcherPriority.Normal, delegate {
					Content.SetNeedsDisplay ();
				});
#pragma warning restore 4014
#endif
			}
		}

        void HandleLoaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("LOADED {0}", Delegate);
            TouchEnabled = true;
        }

        void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("UNLOADED {0}", Delegate);
            TouchEnabled = false;
            Stop();
        }

		void OnNeedsDisplay (object sender, EventArgs e)
		{
			Draw ();
		}

        bool _touchEnabled = false;

        public bool TouchEnabled
        {
            get { return _touchEnabled; }
            set
            {
                if (value == _touchEnabled) return;

                _touchEnabled = value;
                _activeTouches.Clear();

                if (_touchEnabled) {
#if NETFX_CORE
					PointerPressed += SilverlightGraphicsCanvas_PointerPressed;
					PointerMoved += SilverlightGraphicsCanvas_PointerMoved;
					PointerReleased += SilverlightGraphicsCanvas_PointerReleased;
					PointerCanceled += SilverlightGraphicsCanvas_PointerCanceled;
					PointerExited += SilverlightGraphicsCanvas_PointerExited;
#elif SILVERLIGHT
                    Touch.FrameReported += HandleTouchFrameReported;
#else
					TouchDown += XamlCanvas_TouchDown;
					TouchEnter += XamlCanvas_TouchEnter;
					TouchLeave += XamlCanvas_TouchLeave;
					TouchMove += XamlCanvas_TouchMove;
					TouchUp += XamlCanvas_TouchUp;
					MouseDown += XamlCanvas_MouseDown;
					MouseMove += XamlCanvas_MouseMove;
					MouseUp += XamlCanvas_MouseUp;
					MouseWheel += XamlCanvas_MouseWheel;
#endif
                }
                else {
#if NETFX_CORE
					PointerPressed -= SilverlightGraphicsCanvas_PointerPressed;
					PointerMoved -= SilverlightGraphicsCanvas_PointerMoved;
					PointerReleased -= SilverlightGraphicsCanvas_PointerReleased;
#elif SILVERLIGHT
                    Touch.FrameReported -= HandleTouchFrameReported;
#else
					TouchDown -= XamlCanvas_TouchDown;
					TouchEnter -= XamlCanvas_TouchEnter;
					TouchLeave -= XamlCanvas_TouchLeave;
					TouchMove -= XamlCanvas_TouchMove;
					TouchUp -= XamlCanvas_TouchUp;
					MouseDown -= XamlCanvas_MouseDown;
					MouseMove -= XamlCanvas_MouseMove;
					MouseUp -= XamlCanvas_MouseUp;
					MouseWheel -= XamlCanvas_MouseWheel;
#endif
                }
            }
        }

        #region Drawing

        int _ppi = NativePointsPerInch;

        public int PointsPerInch
        {
            get { return _ppi; }
            set
            {
                if (_ppi == value || value <= 0) return;

                _ppi = value;

                var sc = _ppi / (double)NativePointsPerInch;
                var scaleTx = new ScaleTransform() {
                    ScaleX = sc,
                    ScaleY = sc,
                    CenterX = 0,
                    CenterY = 0
                };

                RenderTransform = scaleTx;
            }
        }

        public void Start()
        {
            _drawTimer.Interval = TimeSpan.FromSeconds(1.0 / _fps);

            if (Continuous && !_drawTimer.IsEnabled) {
                _drawTimer.Start();
            }
        }

        public void Stop()
        {
            _drawTimer.Stop();
        }

        public void ResetGraphics()
        {
            _graphics = null;
        }

        bool _paused = false;
        public bool Paused
        {
            get
            {
                return _paused;
            }
            set
            {
                if (_paused != value) {
                    _paused = value;
                    _lastThrottleTime = DateTime.Now;
                }
            }
        }

        public event EventHandler DrewFrame;

		double Draw ()
		{
			var del = Content;
			if (del == null) return 0;

			if (_graphics == null) {
				_graphics = new XamlGraphics (this);
			}

			var startT = DateTime.Now;

			_graphics.BeginDrawing ();
			_graphics.BeginEntity (del);

			//
			// Draw
			//
			var fr = new RectangleF (0, 0, (float)ActualWidth, (float)ActualHeight);
			if (_ppi != NativePointsPerInch) {
				fr.Width /= _ppi / (float)NativePointsPerInch;
				fr.Height /= _ppi / (float)NativePointsPerInch;
			}
			del.Frame = fr;
			try {
				del.Draw (_graphics);
			}
			catch (Exception) {
			}

			_graphics.EndDrawing ();

			var endT = DateTime.Now;
			return (endT - startT).TotalSeconds;
		}

		void DrawTick(object sender, DispatcherTimerTickEventArgs e)
        {
			//
			// Decide if we should draw
			//
            if (Paused) {
                _drawCount = 0;
                _drawTime = 0;
                return;
            }

			var del = Content;
			if (del == null) return;

            var good = ActualWidth > 0 && ActualHeight > 0;
            if (!good) return;
			
			//
			// Draw
			//
            _drawTime += Draw ();
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
        }

        int ClampUpdateFreq(int fps)
        {
            return Math.Min(MaxFps, Math.Max(MinFps, fps));
        }

        #endregion

        #region Touching

        readonly Dictionary<IntPtr, CanvasTouch> _activeTouches = new Dictionary<IntPtr, CanvasTouch>();

        readonly Dictionary<IntPtr, DateTime> _lastDownTime = new Dictionary<IntPtr, DateTime>();
        readonly Dictionary<IntPtr, PointF> _lastBeganPosition = new Dictionary<IntPtr, PointF>();

		const float DoubleClickMinDistance = 20;

#if NETFX_CORE

		PointF ToPointF(Windows.UI.Input.PointerPoint pt)
		{
			return new PointF((float)pt.Position.X, (float)pt.Position.Y);
		}

		class NetfxCoreTouch : CanvasTouch
		{
			public bool IsMoving;
		}

        void SilverlightGraphicsCanvas_PointerPressed(DispatcherTimerTickEventArgs sender, PointerRoutedEventArgs e)
		{
			var handle = new IntPtr(e.Pointer.PointerId);
			//Debug.WriteLine (string.Format ("{0} PRESSED {1}", DateTime.Now, handle));

			var now = DateTime.Now;
			var pos = ToPointF(e.GetCurrentPoint(this));
			
			//
			// Look for double taps
			//
			var tapCount = 1;

			if (_lastDownTime.ContainsKey(handle) &&
				_lastBeganPosition.ContainsKey(handle)) {
				var dt = now - _lastDownTime[handle];

				if (dt.TotalSeconds < 0.5 && pos.DistanceTo(_lastBeganPosition[handle]) < DoubleClickMinDistance) {
					tapCount++;
				}
			}

			//
			// TouchBegan
			//
			var touch = new NetfxCoreTouch {
				Handle = handle,
				Time = now,
				SuperCanvasLocation = ToPointF(e.GetCurrentPoint((UIElement)Parent)),
				CanvasLocation = pos,
				TapCount = tapCount,
				IsMoving = false,
			};
			touch.PreviousTime = touch.Time;
			touch.SuperCanvasPreviousLocation = touch.SuperCanvasLocation;
			touch.CanvasPreviousLocation = touch.CanvasLocation;

			_lastBeganPosition [handle] = pos;
			_lastDownTime[handle] = now;

			_activeTouches[handle] = touch;

			if (Content != null) {
				var keys = CanvasKeys.None;
				var ctrl = Windows.UI.Core.CoreWindow.GetForCurrentThread ().GetKeyState (Windows.System.VirtualKey.Control);
				var shift = Windows.UI.Core.CoreWindow.GetForCurrentThread ().GetKeyState (Windows.System.VirtualKey.Shift);
				if ((ctrl & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0) {
					keys = keys | CanvasKeys.Command;
				}
				if ((shift & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0) {
					keys = keys | CanvasKeys.Shift;
				}

				Content.TouchesBegan(new[] { touch }, keys);
			}
		}

        void SilverlightGraphicsCanvas_PointerMoved(DispatcherTimerTickEventArgs sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.IsInContact) {
				var handle = new IntPtr(e.Pointer.PointerId);
				//Debug.WriteLine (string.Format ("{0} MOVED {1}", DateTime.Now, handle));

				if (_activeTouches.ContainsKey(handle)) {
					var touch = (NetfxCoreTouch)_activeTouches[handle];

					var sloc = ToPointF (e.GetCurrentPoint ((UIElement)Parent));

					var dx = sloc.X - touch.SuperCanvasLocation.X;
					var dy = sloc.Y - touch.SuperCanvasLocation.Y;

					if (touch.IsMoving || (Math.Abs (dx) > 1) || (Math.Abs (dy) > 1)) {

						touch.IsMoving = true;

						// Rate limit
						var now = DateTime.Now;
						if ((now - touch.Time).TotalSeconds < (1.0 / 20)) return;

						var loc = ToPointF (e.GetCurrentPoint (this));						

						//Debug.WriteLine (string.Format ("MOVE:{0} {1} {2}", handle, sloc, loc));

						touch.SuperCanvasPreviousLocation = touch.SuperCanvasLocation;
						touch.CanvasPreviousLocation = touch.CanvasLocation;
						touch.PreviousTime = touch.Time;

						touch.SuperCanvasLocation = sloc;
						touch.CanvasLocation = loc;
						touch.Time = now;

						if (Content != null) {
							Content.TouchesMoved (new[] { touch });
						}
					}
				}			
			}
		}

        void SilverlightGraphicsCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			var handle = new IntPtr(e.Pointer.PointerId);
			//Debug.WriteLine (string.Format ("{0} RELEASED {1}", DateTime.Now, handle));

			if (_activeTouches.ContainsKey(handle)) {
				var touch = _activeTouches[handle];
				_activeTouches.Remove(handle);

				if (Content != null) {
					Content.TouchesEnded(new[] { touch });
				}
			}
		}

		void SilverlightGraphicsCanvas_PointerCanceled (DispatcherTimerTickEventArgs sender, PointerRoutedEventArgs e)
		{
			var handle = new IntPtr (e.Pointer.PointerId);
			//Debug.WriteLine (string.Format ("{0} CANCELED {1}", DateTime.Now, handle));

			if (_activeTouches.ContainsKey (handle)) {
				var touch = _activeTouches[handle];
				_activeTouches.Remove (handle);

				if (Content != null) {
					Content.TouchesCancelled (new[] { touch });
				}
			}
		}

		void SilverlightGraphicsCanvas_PointerExited (DispatcherTimerTickEventArgs sender, PointerRoutedEventArgs e)
		{
			var handle = new IntPtr (e.Pointer.PointerId);
			//Debug.WriteLine (string.Format ("{0} EXITED {1}", DateTime.Now, handle));

			if (_activeTouches.ContainsKey (handle)) {
				var touch = _activeTouches[handle];
				_activeTouches.Remove (handle);

				if (Content != null) {
					Content.TouchesCancelled (new[] { touch });
				}
			}
		}
#elif SILVERLIGHT
        void HandleTouchFrameReported(object sender, TouchFrameEventArgs e)
        {
            try {
                var pts = e.GetTouchPoints(this);
                var spts = e.GetTouchPoints((UIElement)Parent);

                var began = new List<CanvasTouch>();
                var ended = new List<CanvasTouch>();
                var moved = new List<CanvasTouch>();

                for (var i = 0; i < pts.Count; i++) {

                    var p = pts[i];
                    var handle = new IntPtr(p.TouchDevice.Id + 1);

                    var now = DateTime.UtcNow;

                    if (p.Action == TouchAction.Down) {

                        var pos = p.Position.ToPointF();

                        //
                        // Look for double taps
                        //
                        var tapCount = 1;

                        if (_lastDownTime.ContainsKey(handle) &&
                            _lastBeganPosition.ContainsKey(handle)) {
                            var dt = now - _lastDownTime[handle];
                            
                            if (dt.TotalSeconds < 0.5 && pos.DistanceTo(_lastBeganPosition[handle]) < DoubleClickMinDistance) {
                                tapCount++;
                            }
                        }

                        //
                        // TouchBegan
                        //
                        var t = new CanvasTouch {
                            Handle = handle,
                            TapCount = tapCount,
                            CanvasLocation = pos,
                            CanvasPreviousLocation = pos,
                            SuperCanvasLocation = spts[i].Position.ToPointF(),
                            SuperCanvasPreviousLocation = spts[i].Position.ToPointF(),
                            PreviousTime = now,
                            Time = now,
                        };
                        _activeTouches[t.Handle] = t;
                        _lastDownTime[t.Handle] = now;
                        _lastBeganPosition[t.Handle] = pos;
                        began.Add(t);
                    }
                    else if (_activeTouches.ContainsKey(handle)) {
                        var t = _activeTouches[handle];

                        t.CanvasPreviousLocation = t.CanvasLocation;
                        t.SuperCanvasPreviousLocation = t.SuperCanvasLocation;
                        t.PreviousTime = t.Time;

                        t.CanvasLocation = p.Position.ToPointF();
                        t.SuperCanvasLocation = spts[i].Position.ToPointF();
                        t.Time = now;

                        if (p.Action == TouchAction.Move) {
                            moved.Add(t);
                        }
                        else {
                            ended.Add(t);
                        }
                    }
                }

				var del = Content;
                if (del != null && _touchEnabled) {
                    if (began.Count > 0) {
						var keys = CanvasKeys.None;
						//if (Keyboard.IsKeyDown (Key.LeftCtrl) || Keyboard.IsKeyDown (Key.RightCtrl)) {
						//	keys = keys | CanvasKeys.Command;
						//}
						//if (Keyboard.IsKeyDown (Key.LeftShift) || Keyboard.IsKeyDown (Key.RightShift)) {
						//	keys = keys | CanvasKeys.Shift;
						//}
                        del.TouchesBegan(began.ToArray(), keys);
                    }
                    if (moved.Count > 0) {
                        del.TouchesMoved(moved.ToArray());
                    }
                    if (ended.Count > 0) {
                        del.TouchesEnded(ended.ToArray());
                    }
                }
            }
            catch (Exception err) {
                System.Diagnostics.Debug.WriteLine(err);
            }
        }
#else
		static readonly IntPtr MouseId = new IntPtr (42424242);

		void XamlCanvas_TouchDown (object sender, TouchEventArgs e)
		{
			var spt = e.GetTouchPoint ((FrameworkElement)Parent);
			var pt = e.GetTouchPoint (this);

			var t = new CanvasTouch {
				Handle = new IntPtr (e.TouchDevice.Id),
				CanvasLocation = new PointF ((float)pt.Position.X, (float)pt.Position.Y),
				SuperCanvasLocation = new PointF ((float)spt.Position.X, (float)spt.Position.Y),
				Time = DateTime.UtcNow,
			};
			t.PreviousTime = t.Time;
			t.CanvasPreviousLocation = t.CanvasLocation;
			t.SuperCanvasPreviousLocation = t.SuperCanvasLocation;

			_activeTouches[t.Handle] = t;

			if (Content != null) {
				Content.TouchesBegan (new[] { t }, CanvasKeys.None);
			}
		}

		void XamlCanvas_TouchMove (object sender, TouchEventArgs e)
		{
			var id = new IntPtr (e.TouchDevice.Id);
			CanvasTouch t;
			if (!_activeTouches.TryGetValue (id, out t)) return;

			t.SuperCanvasPreviousLocation = t.SuperCanvasLocation;
			t.CanvasPreviousLocation = t.CanvasLocation;
			t.PreviousTime = t.Time;

			var spt = e.GetTouchPoint ((FrameworkElement)Parent);
			var pt = e.GetTouchPoint (this);
			t.CanvasLocation = new PointF ((float)pt.Position.X, (float)pt.Position.Y);
			t.SuperCanvasLocation = new PointF ((float)spt.Position.X, (float)spt.Position.Y);
			t.Time = DateTime.UtcNow;

			if (Content != null) {
				Content.TouchesMoved (new[] { t });
			}
		}

		void XamlCanvas_TouchUp (object sender, TouchEventArgs e)
		{
			var id = new IntPtr (e.TouchDevice.Id);
			CanvasTouch t;
			if (!_activeTouches.TryGetValue (id, out t)) return;

			_activeTouches.Remove (id);

			if (Content != null) {
				Content.TouchesEnded (new[] { t });
			}
		}		

		void XamlCanvas_TouchLeave (object sender, TouchEventArgs e)
		{
			var id = new IntPtr (e.TouchDevice.Id);
			CanvasTouch t;
			if (!_activeTouches.TryGetValue (id, out t)) return;

			_activeTouches.Remove (id);

			if (Content != null) {
				Content.TouchesCancelled (new[] { t });
			}
		}

		void XamlCanvas_TouchEnter (object sender, TouchEventArgs e)
		{
		}

		void XamlCanvas_MouseDown (object sender, MouseButtonEventArgs e)
		{
			var spt = e.GetPosition ((FrameworkElement)Parent);
			var pt = e.GetPosition (this);

			var t = new CanvasTouch {
				Handle = MouseId,
				CanvasLocation = new PointF ((float)pt.X, (float)pt.Y),
				SuperCanvasLocation = new PointF ((float)spt.X, (float)spt.Y),
				Time = DateTime.UtcNow,
			};
			t.PreviousTime = t.Time;
			t.CanvasPreviousLocation = t.CanvasLocation;
			t.SuperCanvasPreviousLocation = t.SuperCanvasLocation;

			_activeTouches[t.Handle] = t;

			if (Content != null) {
				Content.TouchesBegan (new[] { t }, CanvasKeys.None);
			}
		}

		void XamlCanvas_MouseMove (object sender, MouseEventArgs e)
		{
			CanvasTouch t;
			if (!_activeTouches.TryGetValue (MouseId, out t)) return;

			t.SuperCanvasPreviousLocation = t.SuperCanvasLocation;
			t.CanvasPreviousLocation = t.CanvasLocation;
			t.PreviousTime = t.Time;

			var spt = e.GetPosition ((FrameworkElement)Parent);
			var pt = e.GetPosition (this);
			t.CanvasLocation = new PointF ((float)pt.X, (float)pt.Y);
			t.SuperCanvasLocation = new PointF ((float)spt.X, (float)spt.Y);
			t.Time = DateTime.UtcNow;

			if (Content != null) {
				Content.TouchesMoved (new[] { t });
			}
		}

		void XamlCanvas_MouseUp (object sender, MouseButtonEventArgs e)
		{
			CanvasTouch t;
			if (!_activeTouches.TryGetValue (MouseId, out t)) return;

			_activeTouches.Remove (MouseId);

			if (Content != null) {
				Content.TouchesEnded (new[] { t });
			}
		}

		void XamlCanvas_MouseWheel (object sender, MouseWheelEventArgs e)
		{
		}
#endif
		#endregion
	}
}
