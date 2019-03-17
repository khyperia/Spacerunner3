using System;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;

namespace Spacerunner3
{
    public class Player : PhysicsObject
    {
        private readonly MyColor _red = new MyColor(255, 0, 0);
        private readonly Body _body;
        private PlayerLineTrace[]? _traces;
        private bool _addTrace;
        private double _drawExhaust;
        private float _health = 1.0f;

        public Player()
        {
            var verts = new Vertices();
            var size = Settings.Grab.ShipSize;
            var angle = Settings.Grab.ShipShapeAngle;
            verts.Add(new Microsoft.Xna.Framework.Vector2(0, size));
            verts.Add(new Microsoft.Xna.Framework.Vector2((float)Math.Sin(angle) * -size, (float)Math.Cos(angle) * -size));
            verts.Add(new Microsoft.Xna.Framework.Vector2((float)Math.Sin(-angle) * -size, (float)Math.Cos(-angle) * -size));
            _body = BodyFactory.CreatePolygon(world, verts, 1, new Vector2(0, 0));
            _body.UserData = new MyColor(255, 255, 0);
            _body.BodyType = BodyType.Dynamic;
            _body.LinearDamping = 0;
            _body.AngularDamping = Settings.Grab.ShipAngularDamping;
            _body.Restitution = Settings.Grab.ObjectRestitution;
            foreach (var fixture in _body.FixtureList)
            {
                fixture.AfterCollision += PlayerOnCollision;
            }
        }

        private void PlayerOnCollision(Fixture player, Fixture other, Contact contact, ContactVelocityConstraint impulse)
        {
            var healthDeduction = impulse.points[0].normalImpulse;
            healthDeduction *= healthDeduction;
            healthDeduction /= Settings.Grab.ShipHealth;
            _health -= healthDeduction;
        }

        public override Body? Body => _body;

        public override void Draw(Graphics graphics, Camera camera)
        {
            if (_traces == null)
            {
                _traces = new[] {
                    new PlayerLineTrace(this, camera, 0),
                    new PlayerLineTrace(this, camera, 1),
                    new PlayerLineTrace(this, camera, 2)
                };
                _addTrace = true;
            }
            if (_drawExhaust > 0)
            {
                var size = Settings.Grab.ShipSize;
                var angle = Settings.Grab.ShipShapeAngle / 2;
                var points = new Vector2[3];
                points[0] = new Vector2((float)Math.Sin(angle) * -size, (float)Math.Cos(angle) * -size);
                points[1] = new Vector2((float)Math.Sin(-angle) * -size, (float)Math.Cos(-angle) * -size);
                points[2] = new Vector2(0, -size - size * (float)_drawExhaust);
                for (var i = 0; i < points.Length; i++)
                {
                    var vec = _body.GetWorldPoint(new Microsoft.Xna.Framework.Vector2((float)points[i].X, (float)points[i].Y));
                    points[i] = camera.Transform(vec.X, vec.Y);
                }
                for (var i = 0; i < points.Length; i++)
                {
                    graphics.Line(points[i].Point, points[(i + 1) % points.Length].Point, _red.r, _red.g, _red.b);
                }
            }
            base.Draw(graphics, camera);
            graphics.Arc(new Vector2(50, 50), 50, 0, _health * (2 * Math.PI), _red.r, _red.g, _red.b);
        }

        public override void Update(Scene scene, double dt)
        {
            if (_addTrace)
            {
                foreach (var trace in _traces!)
                {
                    scene.Spawn(trace);
                }

                _addTrace = false;
            }
            if (_health < 0)
            {
                scene.Die(this);
                return;
            }
            if (dt <= 0)
            {
                return;
            }
            var thrust = Settings.Grab.ShipThrust;
            var torque = Settings.Grab.ShipTorque;
            if (Settings.Grab.UseJoystick)
            {
                var joystick = JoystickManager.GetJoystick();
                joystick = new Vector2(joystick.X, Math.Max(0, joystick.Y));
                _drawExhaust = joystick.Y;
                var force = _body.GetWorldVector(new Vector2(0, thrust * joystick.Y).Xna);
                _body.ApplyForce(force);
                _body.ApplyTorque((float)(torque * joystick.X));
            }
            else
            {
                if (scene.PressedKeys.Contains(Settings.Grab.KeyThrust))
                {
                    var force = _body.GetWorldVector(new Microsoft.Xna.Framework.Vector2(0, thrust));
                    _body.ApplyForce(force);
                    _drawExhaust = 1;
                }
                else
                {
                    _drawExhaust = 0;
                }

                if (scene.PressedKeys.Contains(Settings.Grab.KeyTurnRight))
                {
                    _body.ApplyTorque(torque);
                }
                if (scene.PressedKeys.Contains(Settings.Grab.KeyTurnLeft))
                {
                    _body.ApplyTorque(-torque);
                }
            }
            var damp = 1 / (float)dt;
            var targetCamera = (_body.Position + _body.LinearVelocity * 2).MyVec();
            var oldCenter = scene.Camera.Center;
            if (double.IsNaN(oldCenter.X))
            {
                throw new Exception();
            }

            scene.Camera.Center = (oldCenter * damp + targetCamera) * (1 / (damp + 1));
            scene.Camera.CenterVelocity = (scene.Camera.Center - oldCenter) * (1 / dt);

            if (scene.Camera.Center.Length2 > 100 * 100)
            {
                world.ShiftOrigin(new Microsoft.Xna.Framework.Vector2((float)scene.Camera.Center.X, (float)scene.Camera.Center.Y));
                scene.Camera.OriginShift();
            }
        }
    }
}
