//
// Copyright (c) 2010-2025 Frank A. Krueger
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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using CrossGraphics;

namespace CrossGraphics
{
	public interface ICanvas
	{
		CanvasContent? Content { get; set; }
	}

	public class DrawEventArgs : EventArgs
	{
		public IGraphics Graphics { get; }
		public RectangleF Frame { get; }

		public DrawEventArgs(IGraphics g, RectangleF frame)
		{
			Graphics = g;
			Frame = frame;
		}
	}

	public class TouchEventArgs : EventArgs
	{
		public TouchPhase Phase { get; }
		public CanvasTouch[] Touches { get; }
		public CanvasKeys Keys { get; }

		public TouchEventArgs(TouchPhase phase, CanvasTouch[] touches, CanvasKeys keys)
		{
			Phase = phase;
			Touches = touches;
			Keys = keys;
		}
	}

	public enum TouchPhase
	{
		Began,
		Moved,
		Ended,
		Cancelled,
	}

	[Flags]
	public enum CanvasKeys
	{
		None = 0,
		Command = 0x01,
		Shift = 0x02,
	}

    public class CanvasContent : INotifyPropertyChanged
    {
        public RectangleF Frame = new RectangleF(0, 0, 320, 480);

		public event EventHandler? NeedsDisplay;

		public virtual void SetNeedsDisplay ()
		{
			NeedsDisplay?.Invoke(this, EventArgs.Empty);
		}

		public bool DrawBackground { get; set; }

		public CanvasContent ()
		{
			DrawBackground = true;
		}

        public virtual void TouchesBegan(CanvasTouch[] touches, CanvasKeys keys)
        {
        }

        public virtual bool TouchesMoved(CanvasTouch[] touches)
        {
            return false;
        }

        public virtual void TouchesEnded(CanvasTouch[] touches)
        {
        }
		
		public virtual void TouchesCancelled(CanvasTouch[] touches)
        {
        }

        public virtual void Draw(IGraphics g)
        {
        }

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged (string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs (propertyName));
		}
	}

	public class CanvasTouch
	{
	    public IntPtr Handle;

	    public int TapCount;

	    public DateTime Time;
	    public DateTime PreviousTime;

	    public PointF CanvasLocation;
	    public PointF CanvasPreviousLocation;

	    public PointF SuperCanvasLocation;
	    public PointF SuperCanvasPreviousLocation;

        public PointF GetVelocity ()
        {
            var t = this;
			var d = new PointF (
				t.CanvasLocation.X - t.CanvasPreviousLocation.X,
				t.CanvasLocation.Y - t.CanvasPreviousLocation.Y);
			var dt = (t.Time - t.PreviousTime).TotalSeconds;
			if (dt > 0) {
				d.X /= (float)dt;
				d.Y /= (float)dt;
			}
			return d;
		}

		public PointF GetChange ()
		{
		    var t = this;
			var d = new PointF (
				t.CanvasLocation.X - t.CanvasPreviousLocation.X,
				t.CanvasLocation.Y - t.CanvasPreviousLocation.Y);
			return d;
		}
	}
	
	public class Button {
		public required Font Font;
		public required string Title;
		public System.Drawing.RectangleF Frame;
		public bool Checked;
		public object? Tag;
		public event EventHandler? Click;
		
		public float PaddingTop = 0;
		public float PaddingBottom = 0;
		
		enum InteractionState {
			None,
			Pressed,
		}
		InteractionState istate = InteractionState.None;
		
		public void Draw (IGraphics g) {
			g.SetFont (Font);
			g.SetColor (Colors.Gray);
			var border = Frame;
			border.Y += PaddingTop;
			border.Height -= PaddingTop + PaddingBottom;
			
			border.Inflate (-1, -1);
			g.DrawRoundedRect (border, 5, 1);
			
			var inner = border;
			inner.Inflate (-2, -2);
			
			if (Checked) {
				g.FillRoundedRect (inner, 3);
				g.SetColor (Colors.LightGray);
			}
			
			if (!string.IsNullOrEmpty (Title)) {
				var fm = g.GetFontMetrics ();
				var sw = fm.StringWidth (Title);
				
				g.DrawString (Title, inner.X + (inner.Width - sw)/2, inner.Y + (inner.Height - fm.Height)/2);
			}
		}
		public void TouchesBegan (CanvasTouch[] touches)
		{
			var touch = touches.First ();
			if (Frame.Contains (touch.CanvasLocation)) {
				istate = InteractionState.Pressed;
			}			
		}
		public void TouchesEnded (CanvasTouch[] touches)
		{
			if (istate == InteractionState.Pressed) {
				istate = InteractionState.None;
				Click?.Invoke (this, EventArgs.Empty);
			}
		}
	}
}
