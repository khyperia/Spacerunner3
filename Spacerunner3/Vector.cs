using System;

namespace Spacerunner3
{
    public struct Vector2
    {
        public double X { get; }
        public double Y { get; }

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator +(Vector2 l, Vector2 r) => new Vector2(l.X + r.X, l.Y + r.Y);

        public static Vector2 operator -(Vector2 l, Vector2 r) => new Vector2(l.X - r.X, l.Y - r.Y);

        public static Vector2 operator -(Vector2 r) => new Vector2(-r.X, -r.Y);

        public static Vector2 operator *(Vector2 l, double r) => new Vector2(l.X * r, l.Y * r);

        public static Vector2 operator *(double l, Vector2 r) => r * l;

        public double Length2 => X * X + Y * Y;

        public double Length => Math.Sqrt(Length2);

        public Point Point => new Point((int)Math.Floor(X), (int)Math.Floor(Y));

        public Microsoft.Xna.Framework.Vector2 Xna => new Microsoft.Xna.Framework.Vector2((float)X, (float)Y);
    }

    public struct Point : IEquatable<Point>
    {
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Point operator +(Point l, Point r) => new Point(l.X + r.X, l.Y + r.Y);

        public static Point operator -(Point l, Point r) => new Point(l.X - r.X, l.Y - r.Y);

        public static Point operator *(Point l, int r) => new Point(l.X * r, l.Y * r);

        public static Point operator *(int l, Point r) => r * l;

        public static bool operator ==(Point l, Point r) => l.X == r.X && l.Y == r.Y;

        public static bool operator !=(Point l, Point r) => !(l == r);

        public override bool Equals(object obj) => obj != null && obj is Point && this == (Point)obj;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

        public bool Equals(Point other) => this == other;

        public Vector2 Vector => new Vector2(X, Y);
    }
}