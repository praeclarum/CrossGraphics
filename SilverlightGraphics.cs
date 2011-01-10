//
// Copyright (c) 2010 Frank A. Krueger
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media;

namespace CrossGraphics.SilverlightGraphics
{
	public class SilverlightGraphics : IGraphics
	{
		Dictionary<object, EntityShapes> _shapes = new Dictionary<object, EntityShapes> ();
		Dictionary<object, EntityShapes> _drawnShapes = new Dictionary<object, EntityShapes> ();

		EntityShapes _eshape = null;

		Canvas _canvas;

		public SilverlightGraphics (Canvas canvas)
		{
			if (canvas == null) throw new ArgumentNullException ("canvas");
			_canvas = canvas;
		}

		public void BeginDrawing ()
		{
			_drawnShapes.Clear ();
		}

		public void EndDrawing ()
		{
			if (_eshape != null) {
				_eshape.End ();
				_eshape = null;
			}

			var toRemove = new List<object> ();
			foreach (var k in _shapes.Keys) {
				if (!_drawnShapes.ContainsKey (k)) {
					toRemove.Add (k);
				}
			}
			foreach (var k in toRemove) {
				// Log.println ("Clearing " + k);
				var s = _shapes[k];
				s.Clear ();
				_shapes.Remove (k);
			}
		}

		public void BeginEntity (object entity)
		{
			if (_eshape != null) {
				_eshape.End ();
				_eshape = null;
			}

			EntityShapes eshape = null;
			if (!_shapes.TryGetValue (entity, out eshape)) {
				eshape = new EntityShapes (entity, _canvas);
				_shapes[entity] = eshape;
			}
			_eshape = eshape;
			_drawnShapes.Add (entity, _eshape);
			_eshape.Begin ();
		}

		public void SetFont (Font font)
		{
			_eshape.SetFont (font);
		}

		public void SetColor (Color color)
		{
			_eshape.SetColor (color);
		}

		public void FillPolygon (Polygon poly)
		{
			_eshape.FillPolygon (poly);
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			_eshape.DrawPolygon (poly, w);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			_eshape.FillRoundedRect (x, y, width, height, radius);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			_eshape.DrawRoundedRect (x, y, width, height, radius, w);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			_eshape.FillRect (x, y, width, height);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			_eshape.DrawRect (x, y, width, height, w);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			_eshape.FillOval (x, y, width, height);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			_eshape.DrawOval (x, y, width, height, w);
		}

		public void BeginLines ()
		{
			_eshape.BeginLines ();
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			_eshape.DrawLine (sx, sy, ex, ey, w);
		}

		public void EndLines ()
		{
			_eshape.EndLines ();
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			_eshape.DrawImage (img, x, y, width, height);
		}

		public void DrawString (string s, float x, float y)
		{
			_eshape.DrawString (s, x, y);
		}

		FontMetrics _metrics;

		public IFontMetrics GetFontMetrics ()
		{
			if (_metrics == null) {
				_metrics = new FontMetrics ();
			}
			return _metrics;
		}

		public IImage ImageFromFile (string path)
		{
			return new SilverlightImage (path);
		}
	}

	public class SilverlightImage : IImage
	{
		public BitmapSource Bitmap { get; private set; }
		public SilverlightImage (string path)
		{
			Bitmap = new BitmapImage (new Uri (path, UriKind.RelativeOrAbsolute));
		}
	}

	public class FontMetrics : IFontMetrics
	{
		public int StringWidth (string s)
		{
			return 9 * s.Length;
		}

		public int Height
		{
			get
			{
				return 10;
			}
		}

		public int Ascent
		{
			get
			{
				return 10;
			}
		}

		public int Descent
		{
			get
			{
				return 0;
			}
		}
	}

	class EntityShapes
	{
		Canvas _canvas;

		Color _currentColor = null;

		List<UIElement> _shapes = new List<UIElement> ();

		int _shapeIndex = 0;

		object _entity;

		public EntityShapes (object entity, Canvas canvas)
		{
			_canvas = canvas;
			_entity = entity;
		}

		UIElement GetNextElement (string typeId)
		{
			UIElement s = null;

			if (_shapeIndex >= _shapes.Count) {
				//Log.println ("Missing shape! " + typeId + " for " + _entity);
				s = ConstructAndAddShape (typeId);
			}
			else if (!ShapeHasTypeId (_shapes[_shapeIndex], typeId)) {
				//Log.println ("Bad shape " + _shapeIndex + "! Wanted " + typeId + " got " + _shapes[_shapeIndex] + " for " + _entity);
				TrimShapes ();
				s = ConstructAndAddShape (typeId);
			}
			else {
				s = _shapes[_shapeIndex];
			}

			_shapeIndex++;

			return s;
		}

		private bool ShapeHasTypeId (UIElement shape, string typeId)
		{
			if (typeId == "Line") return shape is Line;
			else if (typeId == "Text") return shape is TextBlock;
			else if (typeId == "Oval") return shape is Ellipse;
			else if (typeId == "RoundedRect") return shape is Rectangle;
			else if (typeId == "Rect") return shape is Rectangle;
			else if (typeId == "Image") return shape is Image;
            else if (typeId == "Polygon") return shape is System.Windows.Shapes.Polygon;
			else throw new NotSupportedException ("Don't know: " + typeId);
		}

		UIElement ConstructAndAddShape (string typeId)
		{
			UIElement s = null;

			if (typeId == "Line") {
				var line = new Line ();
				line.StrokeEndLineCap = System.Windows.Media.PenLineCap.Round;
				s = line;
			}
			else if (typeId == "Text") {
				s = new TextBlock ();
			}
			else if (typeId == "Oval") {
				s = new Ellipse ();
			}
			else if (typeId == "RoundedRect") {
				s = new Rectangle ();
			}
			else if (typeId == "Rect") {
				s = new Rectangle ();
			}
			else if (typeId == "Image") {
				s = new Image ();
			}
            else if (typeId == "Polygon")
            {
                s = new System.Windows.Shapes.Polygon();
            }
			else {
				throw new NotSupportedException ("Don't know how to construct: " + typeId);
			}

			//Log.println ("Add " + s + " for " + _entity);

			_shapes.Add (s);
			_canvas.Children.Add (s);

			return s;
		}

		void TrimShapes ()
		{
			if (_shapeIndex < _shapes.Count) {
				var n = _shapes.Count - _shapeIndex;
				for (var i = 0; i < n; i++) {
					_canvas.Children.Remove (_shapes[_shapeIndex + i]);
				}
				_shapes.RemoveRange (_shapeIndex, n);
			}
		}

		public void Clear ()
		{
			_shapeIndex = 0;
			TrimShapes ();
		}

		public void Begin ()
		{
			_currentColor = Colors.Black;
			_shapeIndex = 0;
		}

		public void End ()
		{
			TrimShapes ();
		}

		public void SetFont (Font font)
		{
			//throw new NotImplementedException ();
		}

		public void SetColor (Color color)
		{
			_currentColor = color;
		}

		public void FillPolygon (Polygon poly)
		{
            var e = GetNextElement("Polygon") as System.Windows.Shapes.Polygon;

            if (e.Points.Count != poly.Points.Count)
            {
                var ps = new PointCollection();
                ps.Clear();
                var n = poly.Points.Count;
                for (var i = 0 ; i < n; i++) {
                    ps.Add(new Point(poly.Points[i].X, poly.Points[i].Y));
                }
                e.Points = ps;
            }

            e.Fill = _currentColor.GetBrush();
            e.Stroke = null;
		}

		public void DrawPolygon (Polygon poly, float w)
		{
            var e = GetNextElement("Polygon") as System.Windows.Shapes.Polygon;

            if (e.Points.Count != poly.Points.Count)
            {
                var ps = new PointCollection();
                ps.Clear();
                var n = poly.Points.Count;
                for (var i = 0; i < n; i++)
                {
                    ps.Add(new Point(poly.Points[i].X, poly.Points[i].Y));
                }
                e.Points = ps;
            }

            e.Stroke = _currentColor.GetBrush();
            e.StrokeThickness = w;
            e.Fill = null;
        }

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			var e = GetNextElement ("RoundedRect") as Rectangle;
			e.Width = width;
			e.Height = height;
			e.Fill = _currentColor.GetBrush ();
			e.Stroke = null;
            e.RadiusX = radius;
            e.RadiusY = radius;
			Canvas.SetLeft (e, x);
			Canvas.SetTop (e, y);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			var e = GetNextElement ("RoundedRect") as Rectangle;
			e.Width = width;
			e.Height = height;
			e.StrokeThickness = w;
			e.Stroke = _currentColor.GetBrush ();
			e.Fill = null;
            e.RadiusX = radius;
            e.RadiusY = radius;
			Canvas.SetLeft (e, x);
			Canvas.SetTop (e, y);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			var e = GetNextElement ("Rect") as Rectangle;
			e.Width = width;
			e.Height = height;
			e.Fill = _currentColor.GetBrush ();
			e.Stroke = null;
			Canvas.SetLeft (e, x);
			Canvas.SetTop (e, y);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			var e = GetNextElement ("Rect") as Rectangle;
			e.Width = width;
			e.Height = height;
			e.StrokeThickness = w;
			e.Stroke = _currentColor.GetBrush ();
			e.Fill = null;
			Canvas.SetLeft (e, x);
			Canvas.SetTop (e, y);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			var e = GetNextElement ("Oval") as Ellipse;
			e.Width = width;
			e.Height = height;
			e.Fill = _currentColor.GetBrush ();
			e.Stroke = null;
			Canvas.SetLeft (e, x);
			Canvas.SetTop (e, y);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			var e = GetNextElement ("Oval") as Ellipse;
			e.Width = width;
			e.Height = height;
			e.StrokeThickness = w;
			e.Stroke = _currentColor.GetBrush ();
			e.Fill = null;
			Canvas.SetLeft (e, x);
			Canvas.SetTop (e, y);
		}

		Path _linePath = null;

		public void BeginLines ()
		{
			//_linePath = GetNextElement ("LinePath") as Path;
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_linePath != null) {

				//_linePath.D

			}
			else {
				var line = GetNextElement ("Line") as Line;
				line.X1 = sx;
				line.Y1 = sy;
				line.X2 = ex;
				line.Y2 = ey;
				line.Stroke = _currentColor.GetBrush ();
				line.StrokeThickness = w;
			}
		}

		public void EndLines ()
		{
			if (_linePath != null) {
			}
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			var simg = img as SilverlightImage;
			if (simg != null) {
				var bmp = simg.Bitmap;
				var e = GetNextElement ("Image") as Image;
				e.Source = bmp;
				e.Width = width;
				e.Height = height;
				Canvas.SetLeft (e, x);
				Canvas.SetTop (e, y);
			}
		}

		public void DrawString (string s, float x, float y)
		{
			var text = GetNextElement ("Text") as TextBlock;

			text.Text = s;
			Canvas.SetLeft (text, x);
			Canvas.SetTop (text, y);
		}
	}

	public static class ColorEx
	{
		public static System.Windows.Media.SolidColorBrush GetBrush (this Color color)
		{
			var b = color.Tag as System.Windows.Media.SolidColorBrush;
			if (b == null) {
				b = new System.Windows.Media.SolidColorBrush (
					System.Windows.Media.Color.FromArgb (
					(byte)color.Alpha,
					(byte)color.Red,
					(byte)color.Green,
					(byte)color.Blue));
				color.Tag = b;
			}
			return b;
		}
	}
}
