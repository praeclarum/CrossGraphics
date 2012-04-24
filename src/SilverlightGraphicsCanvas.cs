//
// Copyright (c) 2010-2012 Frank A. Krueger
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
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DispatcherTimerTickEventArgs = System.EventArgs;
#endif

namespace CrossGraphics.SilverlightGraphics
{
    public class SilverlightGraphicsCanvas : Canvas
    {
        const int NativePointsPerInch = 160;

        SilverlightGraphics _graphics;

        int _fps = 20;
        readonly DispatcherTimer _drawTimer;

        const double CpuUtilization = 0.25;
        public int MinFps { get; set; }
        public int MaxFps { get; set; }
        static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(1.0);

        double _drawTime;
        int _drawCount;
        DateTime _lastThrottleTime = DateTime.Now;

        public GraphicsCanvasDelegate Delegate { get; set; }

        public SilverlightGraphicsCanvas()
        {
            MinFps = 4;
            MaxFps = 30;

            _drawTimer = new DispatcherTimer();
            _drawTimer.Tick += DrawTick;

            Unloaded += HandleUnloaded;
            Loaded += HandleLoaded;
        }

        void HandleLoaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("LOADED {0}", Delegate);
            TouchEnabled = true;
            _drawTimer.Start();
        }

        void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("UNLOADED {0}", Delegate);
            TouchEnabled = false;
            _drawTimer.Stop();
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
#else
                    Touch.FrameReported += HandleTouchFrameReported;
#endif
                }
                else {
#if NETFX_CORE
					PointerPressed -= SilverlightGraphicsCanvas_PointerPressed;
					PointerMoved -= SilverlightGraphicsCanvas_PointerMoved;
					PointerReleased -= SilverlightGraphicsCanvas_PointerReleased;
#else
                    Touch.FrameReported -= HandleTouchFrameReported;
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

            if (!_drawTimer.IsEnabled) {
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

		void DrawTick(object sender, DispatcherTimerTickEventArgs e)
        {
            if (Paused) {
                _drawCount = 0;
                _drawTime = 0;
                return;
            }

            var del = Delegate;
            if (del == null) return;

            if (_graphics == null) {
                _graphics = new SilverlightGraphics(this);
            }

            var good = ActualWidth > 0 && ActualHeight > 0;
            if (!good) return;

            //
            // Start drawing
            //
            var startT = DateTime.Now;
            _graphics.BeginDrawing();

            //
            // Draw
            //
            var fr = new RectangleF(0, 0, (float)ActualWidth, (float)ActualHeight);
            if (_ppi != NativePointsPerInch) {
                fr.Width /= _ppi/(float) NativePointsPerInch;
                fr.Height /= _ppi / (float)NativePointsPerInch;
            }
            del.Frame = fr;
            try {
                del.Draw (_graphics);
            }
            catch (Exception) {
            }

            //
            // End drawing
            //
            _graphics.EndDrawing();

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

#if NETFX_CORE

		PointF ToPointF(Windows.UI.Input.PointerPoint pt)
		{
			return new PointF((float)pt.Position.X, (float)pt.Position.Y);
		}

		const float DoubleClickMinDistance = 10;

		void SilverlightGraphicsCanvas_PointerPressed(DispatcherTimerTickEventArgs sender, PointerEventArgs e)
		{
			var handle = new IntPtr(e.Pointer.PointerId);

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
			var touch = new CanvasTouch {
				Handle = handle,
				Time = now,
				SuperCanvasLocation = ToPointF(e.GetCurrentPoint((UIElement)Parent)),
				CanvasLocation = pos,
				TapCount = tapCount,
			};
			touch.PreviousTime = touch.Time;
			touch.SuperCanvasPreviousLocation = touch.SuperCanvasLocation;
			touch.CanvasPreviousLocation = touch.CanvasLocation;

			_lastBeganPosition [handle] = pos;
			_lastDownTime[handle] = now;

			_activeTouches[handle] = touch;

			if (Delegate != null) {
				Delegate.TouchesBegan(new[] { touch });
			}
		}

		void SilverlightGraphicsCanvas_PointerMoved(DispatcherTimerTickEventArgs sender, PointerEventArgs e)
		{
			if (e.Pointer.IsInContact) {
				var handle = new IntPtr(e.Pointer.PointerId);

				if (_activeTouches.ContainsKey(handle)) {
					var touch = _activeTouches[handle];

					var sloc = ToPointF (e.GetCurrentPoint ((UIElement)Parent));
					var loc = ToPointF (e.GetCurrentPoint (this));

					touch.SuperCanvasPreviousLocation = touch.SuperCanvasLocation;
					touch.CanvasPreviousLocation = touch.CanvasLocation;
					touch.PreviousTime = touch.Time;

					touch.SuperCanvasLocation = sloc;
					touch.CanvasLocation = loc;
					touch.Time = DateTime.Now;

					if (Delegate != null) {
						Delegate.TouchesMoved(new[] { touch });
					}
				}			
			}
		}

		void SilverlightGraphicsCanvas_PointerReleased(object sender, PointerEventArgs e)
		{
			var handle = new IntPtr(e.Pointer.PointerId);

			if (_activeTouches.ContainsKey(handle)) {
				var touch = _activeTouches[handle];
				_activeTouches.Remove(handle);

				if (Delegate != null) {
					Delegate.TouchesEnded(new[] { touch });
				}
			}
		}
#else
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

                var del = Delegate;
                if (del != null && _touchEnabled) {
                    if (began.Count > 0) {
                        del.TouchesBegan(began.ToArray());
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
#endif
		#endregion
	}
}
