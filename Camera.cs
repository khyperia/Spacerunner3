using System;

namespace Spacerunner3
{
    public class Camera
    {
        public Camera(double screenSize)
        {
            Center = new Vector2(0, 0);
            SizeMultiplier = 1;
            FixedSize = screenSize;
        }

        public Vector2 CenterVelocity { get; set; }
        public Vector2 Center { get; set; }
        public double Size => FixedSize * SizeMultiplier;
        public double FixedSize { get; }
        public double SizeMultiplier { get; set; }
        public Vector2 ScreenScale { get; set; }

        public event Action<Vector2>? OnOriginShift;

        public void OriginShift()
        {
            OnOriginShift?.Invoke(Center);
            Center = new Vector2(0, 0);
        }

        public Vector2 Transform(double x, double y)
        {
            var point = new Vector2(x, y);
            point -= Center;
            point *= 1 / Size;
            point = (point * 0.5f + new Vector2(0.5f, ScreenScale.Y / (2 * ScreenScale.X))) * ScreenScale.X;
            return new Vector2(point.X, point.Y);
        }

        public double Scale(double value) => value / Size * ((ScreenScale.X)) / 2;

        internal void ResetOriginShift()
        {
            Center = new Vector2(0, 0);
            OnOriginShift = null;
        }
    }
}