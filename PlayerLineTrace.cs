using FarseerPhysics.Collision.Shapes;
using System;

namespace Spacerunner3
{
    public class PlayerLineTrace : IObject, IDrawable
    {
        private readonly Camera _subscribed;
        private readonly Player _player;
        private readonly Vector2[] _line;
        private readonly int _vertexIndex;
        private double _counter;

        private const int _numPoints = 10;
        private const float _pointFrequency = 0.25f;

        public PlayerLineTrace(Player player, Camera camera, int vertexIndex)
        {
            _vertexIndex = vertexIndex;
            _player = player;
            _subscribed = camera;
            _subscribed.OnOriginShift += OnOriginShift;
            _line = new Vector2[_numPoints];
            _counter = 0;
        }

        private void OnOriginShift(Vector2 shift)
        {
            for (var i = 0; i < _line.Length; i++)
            {
                _line[i] -= shift;
            }
        }

        private Vector2 GetPos()
        {
            if (_player.Body is null)
            {
                throw new Exception("Player.Body should be non-null");
            }

            if (_player.Body.FixtureList == null)
            {
                return _player.Body.Position.MyVec();
            }

            return _player.Body.GetWorldPoint(((PolygonShape)_player.Body.FixtureList[0].Shape).Vertices[_vertexIndex]).MyVec();
        }

        private Vector2 GetFuturePos()
        {
            if (_player.Body is null)
            {
                throw new Exception("Player.Body should be non-null");
            }

            var pos = GetPos();
            var vel = _player.Body.LinearVelocity.MyVec();
            return pos + vel * Settings.Grab.FuturePrediction;
        }

        public void Draw(Graphics graphics, Camera camera)
        {
            var points = new Vector2[_line.Length + 2];
            for (var i = 0; i < _line.Length; i++)
            {
                var point = _line[i];
                points[i + 2] = camera.Transform(point.X, point.Y);
            }
            var current = GetPos();
            points[1] = camera.Transform(current.X, current.Y);
            var future = GetFuturePos();
            points[0] = camera.Transform(future.X, future.Y);
            for (var i = 0; i < points.Length - 1; i++)
            {
                graphics.Line(points[i].Point, points[i + 1].Point, 112, 128, 144);
            }
        }

        public void OnDie(Scene scene) => _subscribed.OnOriginShift -= OnOriginShift;

        public void Update(Scene scene, double dt)
        {
            _counter += dt;
            if (_counter > _pointFrequency)
            {
                _counter %= _pointFrequency;
                for (var i = _line.Length - 2; i >= 0; i--)
                {
                    _line[i + 1] = _line[i];
                }

                _line[0] = GetPos();
            }
        }
    }
}
