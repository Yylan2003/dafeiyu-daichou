using System;
using System.Collections.Generic;
using System.Drawing;

namespace SmokeSimulator
{
    // Shared by the desktop and settings preview. Color is captured at emission time.
    internal sealed class SmokeTrail
    {
        private sealed class Particle { public float Age, Offset, Volume; public Color Color; public PointF Mouth; public bool Vape; }
        private readonly List<Particle> particles = new List<Particle>();
        internal void Emit(Product product, string pose = "smoke")
        {
            bool vape = product != null && product.Vape;
            PointF mouth = vape ? new PointF(126, pose == "cough" ? 99 : 88) : new PointF(111, 87);
            for (int i = 0; i < (vape ? 21 : 7); i++) particles.Add(new Particle { Age = -i * .055f, Offset = (i % 5 - 2) * (vape ? 2 : 1), Volume = vape ? 1.6f : 1, Color = AssetArt.SmokeColor(product, i), Mouth = mouth, Vape = vape });
            if (particles.Count > 84) particles.RemoveRange(0, particles.Count - 84);
        }
        internal void Clear() { particles.Clear(); }
        internal void Frame(double seconds)
        {
            foreach (Particle p in particles) p.Age += (float)seconds;
            particles.RemoveAll(p => p.Age >= 1.6f);
        }
        internal void Draw(Graphics g)
        {
            foreach (Particle p in particles)
            {
                if (p.Age < 0) continue;
                float size = (4 + p.Age * 21) * p.Volume;
                // Small plume exits sideways and stays above the lowered hand, below the eyes.
                float x = p.Mouth.X - p.Age * 58, y = p.Mouth.Y + p.Offset * Math.Min(1, p.Age * 5) - p.Age * 8;
                using (Brush b = new SolidBrush(Color.FromArgb((int)(140 * (1 - p.Age / 1.6f)), p.Color)))
                    g.FillEllipse(b, x - size, y - size * .25f, size, size * .55f);
            }
        }
        internal void DrawMouth(Graphics g)
        {
            // Only the thin plume at the lips is in front; the larger cloud stays behind the body.
            foreach (Particle p in particles)
            {
                if (!p.Vape || p.Age < 0 || p.Age > .4f) continue;
                float length = Math.Min(60, 5 + p.Age * 170), fade = 1 - p.Age / .6f;
                using (Pen breath = new Pen(Color.FromArgb((int)(80 * fade), p.Color), 1.6f + p.Age * 8))
                {
                    breath.StartCap = breath.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawBezier(breath, p.Mouth, new PointF(p.Mouth.X - length * .3f, p.Mouth.Y + .4f), new PointF(p.Mouth.X - length * .7f, p.Mouth.Y), new PointF(p.Mouth.X - length, p.Mouth.Y - 3));
                }
            }
        }
    }
}
