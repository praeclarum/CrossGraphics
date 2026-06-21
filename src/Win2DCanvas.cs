//
// Copyright (c) 2010-2026 Frank A. Krueger
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
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.System;
using Windows.UI.Core;

using DispatcherTimerTickEventArgs = System.Object;
using NativeColors = Microsoft.UI.Colors;

namespace CrossGraphics.Win2D
{
    public class Win2DCanvas : Grid, ICanvas
    {
		CanvasControl? canvasControl;
		CanvasAnimatedControl? animatedCanvasControl;

        int _fps = 30;

        const double CpuUtilization = 0.25;
		int _maxFps = 30;
		public int MaxFps {
			get => _maxFps;
			set {
				_maxFps = Math.Max (1, value);
				_fps = ClampUpdateFreq (_fps);
				ApplyFrameRate ();
			}
		}
        static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(1.0);

        double _drawTime;
        int _drawCount;
		readonly Stopwatch _drawStopwatch = new Stopwatch ();
		readonly Stopwatch _throttleStopwatch = Stopwatch.StartNew ();
		bool _isLoaded;

		float _drawWidth;
		float _drawHeight;
		double lastUpdateWidth = -1, lastUpdateHeight = -1;

		bool _continuous = true;
		public bool Continuous {
			get => _continuous;
			set {
				if (_continuous == value)
					return;
				_continuous = value;
				UpdateRenderingMode ();
			}
		}

		CanvasContent? content;
		public CanvasContent? Content
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

        public Win2DCanvas ()
        {
			Background = new SolidColorBrush (NativeColors.Transparent);

            Unloaded += HandleUnloaded;
            Loaded += HandleLoaded;
			SizeChanged += Win2DCanvas_SizeChanged;
		}

		async void Win2DCanvas_SizeChanged (object sender, SizeChangedEventArgs e)
		{
			var width = e.NewSize.Width;
			var height = e.NewSize.Height;
			_drawWidth = (float)width;
			_drawHeight = (float)height;

			if (Content is not null && (Math.Abs (lastUpdateWidth - width) > 0.5 || Math.Abs (lastUpdateHeight - height) > 0.5)) {
				lastUpdateWidth = width;
				lastUpdateHeight = height;
				if (Dispatcher is { } dispatcher) {
					await dispatcher.RunAsync (Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
					{
						Content?.SetNeedsDisplay ();
					});
				}
			}
		}

        void HandleLoaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("LOADED {0}", Delegate);
            TouchEnabled = true;
			_drawWidth = (float)ActualWidth;
			_drawHeight = (float)ActualHeight;
			_isLoaded = true;
			UpdateRenderingMode ();
        }

        void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("UNLOADED {0}", Delegate);
            TouchEnabled = false;
			_isLoaded = false;
			DestroyDrawingControl ();
			SizeChanged -= Win2DCanvas_SizeChanged;
        }

		public void SetNeedsDisplay ()
		{
			if (!_isLoaded)
				return;

			if (Continuous) {
				EnsureAnimatedCanvasControl ().Invalidate ();
			}
			else {
				EnsureCanvasControl ().Invalidate (); 
			}
		}

		void OnNeedsDisplay (object? sender, EventArgs e)
		{
			SetNeedsDisplay ();
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
					PointerPressed += HandlePointerPressed;
					PointerMoved += HandlePointerMoved;
					PointerReleased += HandlePointerReleased;
					PointerCanceled += HandlePointerCanceled;
					PointerExited += HandlePointerExited;
                }
                else {
					PointerPressed -= HandlePointerPressed;
					PointerMoved -= HandlePointerMoved;
					PointerReleased -= HandlePointerReleased;
					PointerCanceled -= HandlePointerCanceled;
					PointerExited -= HandlePointerExited;
				}
            }
        }

        #region Drawing

        public void ResetGraphics()
        {
        }

        public event EventHandler? DrewFrame;

		void Draw (CanvasControl sender, CanvasDrawEventArgs args)
		{
			DrawFrame (args.DrawingSession);
		}

		void DrawAnimated (ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
		{
			var elapsed = DrawFrame (args.DrawingSession);
			if (elapsed <= 0)
				return;

			_drawTime += elapsed;
			_drawCount++;
			ThrottleContinuousRendering ();
		}

		double DrawFrame (CanvasDrawingSession drawingSession)
		{
			var del = Content;
			if (del is null) return 0;

			var width = _drawWidth;
			var height = _drawHeight;
            var good = width > 0 && height > 0;
            if (!good) return 0;

			var _graphics = new Win2DGraphics (drawingSession);

			_drawStopwatch.Restart ();

			_graphics.BeginEntity (del);

			//
			// Draw
			//
			var fr = new RectangleF (0, 0, width, height);
			del.Frame = fr;
			try {
				del.Draw (_graphics);
			}
			catch (Exception) {
			}

			_drawStopwatch.Stop ();
			DrewFrame?.Invoke (this, EventArgs.Empty);
			return _drawStopwatch.Elapsed.TotalSeconds;
		}

		void ThrottleContinuousRendering ()
		{
            //
            // Throttle
            //
            if (_drawCount > 2 && _throttleStopwatch.Elapsed >= ThrottleInterval) {

				_throttleStopwatch.Restart ();

                var maxfps = 1.0 / (_drawTime / _drawCount);
                _drawTime = 0;
                _drawCount = 0;

                var fps = ClampUpdateFreq((int)(maxfps * CpuUtilization));

                if (Math.Abs(fps - _fps) > 1) {
                    _fps = fps;
                    ApplyFrameRate ();
                }
            }
		}

		void ApplyFrameRate ()
		{
			if (animatedCanvasControl is not null) {
				animatedCanvasControl.TargetElapsedTime = TimeSpan.FromSeconds (1.0 / ClampUpdateFreq (_fps));
			}
		}

		void UpdateRenderingMode ()
		{
			if (!_isLoaded)
				return;

			if (Continuous) {
				DestroyCanvasControl ();
				EnsureAnimatedCanvasControl ().Paused = false;
			}
			else {
				DestroyAnimatedCanvasControl ();
				EnsureCanvasControl ().Invalidate ();
			}
			if (!Continuous) {
				_drawCount = 0;
				_drawTime = 0;
			}
		}

		CanvasControl EnsureCanvasControl ()
		{
			if (canvasControl is not null)
				return canvasControl;

			DestroyAnimatedCanvasControl ();

			canvasControl = new CanvasControl ();
			canvasControl.Draw += Draw;
			SetRow (canvasControl, 0);
			SetColumn (canvasControl, 0);
			Children.Add (canvasControl);
			return canvasControl;
		}

		CanvasAnimatedControl EnsureAnimatedCanvasControl ()
		{
			if (animatedCanvasControl is not null)
				return animatedCanvasControl;

			DestroyCanvasControl ();

			animatedCanvasControl = new CanvasAnimatedControl ();
			animatedCanvasControl.Draw += DrawAnimated;
			SetRow (animatedCanvasControl, 0);
			SetColumn (animatedCanvasControl, 0);
			Children.Add (animatedCanvasControl);
			ApplyFrameRate ();
			return animatedCanvasControl;
		}

		void DestroyDrawingControl ()
		{
			DestroyCanvasControl ();
			DestroyAnimatedCanvasControl ();
		}

		void DestroyCanvasControl ()
		{
			if (canvasControl is null)
				return;

			canvasControl.Draw -= Draw;
			canvasControl.RemoveFromVisualTree ();
			Children.Remove (canvasControl);
			canvasControl = null;
		}

		void DestroyAnimatedCanvasControl ()
		{
			if (animatedCanvasControl is null)
				return;

			animatedCanvasControl.Paused = true;
			animatedCanvasControl.Draw -= DrawAnimated;
			animatedCanvasControl.RemoveFromVisualTree ();
			Children.Remove (animatedCanvasControl);
			animatedCanvasControl = null;
		}

        int ClampUpdateFreq(int fps)
        {
            return Math.Min(MaxFps, Math.Max(1, fps));
        }

        #endregion

        #region Touching

        readonly Dictionary<IntPtr, CanvasTouch> _activeTouches = new Dictionary<IntPtr, CanvasTouch>();

        readonly Dictionary<IntPtr, DateTime> _lastDownTime = new Dictionary<IntPtr, DateTime>();
        readonly Dictionary<IntPtr, PointF> _lastBeganPosition = new Dictionary<IntPtr, PointF>();

		const float DoubleClickMinDistance = 20;

		PointF ToPointF (Microsoft.UI.Input.PointerPoint pt)
		{
			return new PointF((float)pt.Position.X, (float)pt.Position.Y);
		}

		class NetfxCoreTouch : CanvasTouch
		{
			public bool IsMoving;
		}

		readonly HashSet<Windows.System.VirtualKey> _pressedKeys = new HashSet<Windows.System.VirtualKey> ();

		void HandlePointerPressed(DispatcherTimerTickEventArgs sender, PointerRoutedEventArgs e)
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
				var lastPos = _lastBeganPosition[handle];
				var dx = pos.X - lastPos.X;
				var dy = pos.Y - lastPos.Y;

				if (dt.TotalSeconds < 0.5 && MathF.Sqrt(dx*dx + dy*dy) < DoubleClickMinDistance) {
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
				if (InputKeyboardSource.GetKeyStateForCurrentThread (VirtualKey.Control).HasFlag (CoreVirtualKeyStates.Down)) {
					keys = keys | CanvasKeys.Command;
				}
				if (InputKeyboardSource.GetKeyStateForCurrentThread (VirtualKey.Shift).HasFlag (CoreVirtualKeyStates.Down)) {
					keys = keys | CanvasKeys.Shift;
				}
				Content.TouchesBegan(new[] { touch }, keys);
			}
		}

        void HandlePointerMoved(DispatcherTimerTickEventArgs sender, PointerRoutedEventArgs e)
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

        void HandlePointerReleased(object sender, PointerRoutedEventArgs e)
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

		void HandlePointerCanceled (DispatcherTimerTickEventArgs sender, PointerRoutedEventArgs e)
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

		void HandlePointerExited (DispatcherTimerTickEventArgs sender, PointerRoutedEventArgs e)
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
		#endregion
	}
}
