using System;
using CrossGraphics;

namespace Clock
{
	/// <summary>
	/// This class demonstrates drawing with the CrossGraphics library.
	/// </summary>
	public class Clock
	{
		public float Width { get; set; }
		public float Height { get; set; }

		readonly Color BackColor = new Color (0xDD, 0xDD, 0xDD);
		readonly Color FaceColor = new Color (0xCC, 0xCC, 0xCC);
		readonly Color BorderColor = new Color (0x66, 0x66, 0x66);
		readonly Font LabelFont = Font.SystemFontOfSize (16);

		public void Draw (IGraphics g)
		{
			g.BeginEntity (this);

			var now = DateTime.Now;

			var r = Math.Min (Width / 2, Height / 2);
			var center = new System.Drawing.PointF (Width / 2, Height / 2);

			//
			// Draw the face
			//
			g.SetColor (BackColor);
			g.FillOval (center.X - r, center.Y - r, 2 * r, 2 * r);
			g.SetColor (BorderColor);
			g.DrawOval (center.X - r, center.Y - r, 2 * r, 2 * r, 8);

			g.SetColor (FaceColor);
			for (var i = 0; i < 12; i++) {
				var angle = (i / 12.0) * 2 * Math.PI;
				g.DrawLine (
					center.X + 0.75f * r * (float)Math.Cos (angle),
					center.Y + 0.75f * r * (float)Math.Sin (angle),
					center.X + 0.9f * r * (float)Math.Cos (angle),
					center.Y + 0.9f * r * (float)Math.Sin (angle),
					7);
			}
			g.SetFont (LabelFont);
			var textWidth = g.GetFontMetrics ().StringWidth ("Cross Graphics");
			g.DrawString ("Cross Graphics", center.X - textWidth / 2, center.Y + 0.25f * r);

			//
			// Draw the hour hand
			//
			g.SetColor (Colors.DarkGray);
			var h = now.Hour + now.Minute / 60.0;
			var hAngle = h > 12 ?
				((h - 12) / 12.0 * 2 * Math.PI - Math.PI /2) :
				(h / 12.0 * 2 * Math.PI - Math.PI / 2);
			g.DrawLine (
				center.X + -0.1f * r * (float)Math.Cos (hAngle),
				center.Y + -0.1f * r * (float)Math.Sin (hAngle),
				center.X + 0.65f * r * (float)Math.Cos (hAngle),
				center.Y + 0.65f * r * (float)Math.Sin (hAngle),
				7);

			//
			// Draw the minute hand
			//
			g.SetColor (Colors.DarkGray);
			var m = now.Minute + now.Second / 60.0;
			var mAngle = (m / 60.0) * 2 * Math.PI - Math.PI / 2;
			g.DrawLine (
				center.X + -0.15f * r * (float)Math.Cos (mAngle),
				center.Y + -0.15f * r * (float)Math.Sin (mAngle),
				center.X + 0.85f * r * (float)Math.Cos (mAngle),
				center.Y + 0.85f * r * (float)Math.Sin (mAngle),
				5);

			//
			// Draw the second hand
			//
			g.SetColor (Colors.Red);
			var sAngle = (now.Second / 60.0) * 2 * Math.PI - Math.PI / 2;
			g.DrawLine (
				center.X + -0.15f * r * (float)Math.Cos (sAngle),
				center.Y + -0.15f * r * (float)Math.Sin (sAngle),
				center.X + 0.85f * r * (float)Math.Cos (sAngle),
				center.Y + 0.85f * r * (float)Math.Sin (sAngle),
				1);

			//
			// Draw the pin
			//
			g.SetColor (Colors.Black);
			g.FillOval (center.X - 5, center.Y - 5, 10, 10);
		}
	}
}
