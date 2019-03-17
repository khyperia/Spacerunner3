using System.Linq;
using System;

namespace Spacerunner3
{
    public class DistanceTracker : IObject, IDrawable
    {
        private Vector2 _originShift;
        private Vector2 _player;
        private double _distance;
        private double _time;

        public DistanceTracker(Camera camera)
        {
            _originShift = new Vector2(0, 0);
            camera.OnOriginShift += shift => _originShift += shift;
        }

        public void OnDie(Scene scene)
        {
            Console.WriteLine("distance:  " + _distance);
            Console.WriteLine("dist/time: " + _distance / _time);
            Console.WriteLine("time:      " + _time);
            Console.WriteLine();
        }

        public void Update(Scene scene, double dt)
        {
            var player = scene.Objects.OfType<Player>().FirstOrDefault();
            if (player != null && player.Body != null)
            {
                _player = player.Body.Position.MyVec() + _originShift;
                _distance = _player.Length;
                _time += dt;
            }
        }

        public string Describe() => _distance.ToString("F2") + "@" + (_distance / _time).ToString("F2");

        public void Draw(Graphics graphics, Camera camera)
        {
            // var size = 200;
            // var min = Math.Max((_distance - size) / _time, 1);
            // var max = (_distance + size) / _time;
            // foreach (var speed in Util.RoundRange(min, max, 10))
            // {
            //     var distance = speed * _time;

            //     var direction = _player * (1 / Math.Max(_player.Length, 0.01));
            //     var left = direction * distance + new Vector2(direction.Y, -direction.X) * size;
            //     var right = direction * distance + new Vector2(-direction.Y, direction.X) * size;

            //     left -= _originShift;
            //     right -= _originShift;

            //     graphics.Line(camera.Transform(left.X, left.Y).Point, camera.Transform(right.X, right.Y).Point, (byte)(40 * 0.8), (byte)(79 * 0.8), (byte)(79 * 0.8));
            // }

            /*
            var str = "distance:  " + distance;
            var measure = graphics.MeasureString(str, Util.font);
            graphics.DrawString(str, Util.font, Brushes.Yellow, new PointF(110, 25 - measure.Height / 2));
            str = "time:      " + time;
            measure = graphics.MeasureString(str, Util.font);
            graphics.DrawString(str, Util.font, Brushes.Yellow, new PointF(110, 75 - measure.Height / 2));
            str = "dist/time: " + distance / time;
            measure = graphics.MeasureString(str, Util.font);
            graphics.DrawString(str, Util.font, Brushes.Yellow, new PointF(110, 50 - measure.Height / 2));
            */
        }
    }
}