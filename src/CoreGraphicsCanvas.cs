//
// Copyright (c) 2024 Frank A. Krueger
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
using System.Drawing;
using System.Linq;

#if __IOS__ || __MACCATALYST__
using NativeView = UIKit.UIView;
#elif __MACOS__
using NativeView = AppKit.NSView;
#endif

namespace CrossGraphics.CoreGraphics
{
	public class CoreGraphicsCanvas : ICanvas
	{
		public CanvasContent Content { get; set; } = new CanvasContent ();

		public CoreGraphicsCanvas ()
		{
			Content.NeedsDisplay += (s, e) => Invalidate ();
		}

		public void Invalidate ()
		{
		}
	}

	public class CoreGraphicsTouchManager
	{
		readonly Dictionary<IntPtr, CanvasTouch> _activeTouches = new Dictionary<IntPtr, CanvasTouch> ();
#if __IOS__ || __MACCATALYST__
		public CanvasTouch GetCanvasTouch (TouchPhase phase, UIKit.UITouch touch, UIKit.UIView inView)
		{
			var now = DateTime.UtcNow;
			var id = touch.Handle;
			if (_activeTouches.TryGetValue (id, out var ct)) {
				ct.TapCount = (int)touch.TapCount;
				ct.CanvasPreviousLocation = ct.CanvasLocation;
				ct.PreviousTime = ct.Time;
				ct.CanvasLocation = touch.LocationInView (inView).ToPointF ();
				ct.Time = now;
				if (phase == TouchPhase.Ended || phase == TouchPhase.Cancelled) {
					_activeTouches.Remove (id);
				}
				return ct;
			}
			ct = new CanvasTouch {
				Handle = touch.Handle,
				TapCount = (int)touch.TapCount,
				CanvasLocation = touch.LocationInView (inView).ToPointF (),
				CanvasPreviousLocation = touch.PreviousLocationInView (inView).ToPointF (),
				Time = now,
				PreviousTime = now,
			};
			if (phase == TouchPhase.Began) {
				_activeTouches[id] = ct;
			}
			return ct;
		}
#endif
	}
}
