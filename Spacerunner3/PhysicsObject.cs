using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using System;
using System.Drawing;
using FarseerPhysics.Collision.Shapes;
using System.Linq;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Collision;

namespace Spacerunner3
{
    public static class Util
    {
        public static Random rand = new Random();
        public static Font font = new Font(FontFamily.GenericMonospace, 12);
    }

    public abstract class PhysicsObject : IObject, IDrawable
    {
        private static Pen defaultPen = Pens.White;
        protected static World world;

        public abstract void Update(Scene scene, double dt);

        public abstract Body Body { get; }

        public virtual void Draw(Graphics graphics, Camera camera)
        {
            var body = Body;
            if (body == null)
                return;
            var pen = body.UserData as Pen ?? defaultPen;
            foreach (var fixture in body.FixtureList)
            {
                AABB aabb;
                Transform transform;
                body.GetTransform(out transform);
                var draw = false;
                for (var i = 0; i < fixture.ProxyCount; i++)
                {
                    fixture.Shape.ComputeAABB(out aabb, ref transform, i);
                    var topleft = aabb.Center - aabb.Extents;
                    var bottomright = aabb.Center + aabb.Extents;
                    var tl = camera.Transform(topleft.X, topleft.Y);
                    var br = camera.Transform(bottomright.X, bottomright.Y);
                    if (graphics.ClipBounds.IntersectsWith(new RectangleF(tl, new SizeF(br.X - tl.X, br.Y - tl.Y))))
                    {
                        draw = true;
                        break;
                    }
                }
                if (draw == false)
                    continue;

                var shape = fixture.Shape;
                var poly = shape as PolygonShape;
                var edge = shape as EdgeShape;
                var circle = shape as CircleShape;
                if (poly != null)
                {
                    var points = new PointF[poly.Vertices.Count];
                    for (int i = 0; i < poly.Vertices.Count; i++)
                    {
                        var vert = body.GetWorldPoint(poly.Vertices[i]);
                        points[i] = camera.Transform(vert.X, vert.Y);
                    }
                    graphics.DrawPolygon(pen, points);
                }
                else if (edge != null)
                {
                    var v1 = body.GetWorldPoint(edge.Vertex1);
                    var v2 = body.GetWorldPoint(edge.Vertex2);
                    graphics.DrawLine(pen, camera.Transform(v1.X, v1.Y), camera.Transform(v2.X, v2.Y));
                }
                else if (circle != null)
                {
                    var pos = body.GetWorldPoint(circle.Position);
                    var topLeft = pos - new Vector2(circle.Radius, circle.Radius);
                    var bottomRight = pos + new Vector2(circle.Radius, circle.Radius);
                    var tl = camera.Transform(topLeft.X, topLeft.Y);
                    var br = camera.Transform(bottomRight.X, bottomRight.Y);
                    var sz = new SizeF(br.X - tl.X, br.Y - tl.Y);
                    graphics.DrawEllipse(pen, new RectangleF(tl, sz));
                }
                else
                {
                    throw new Exception("Unknown shape type " + shape.GetType().Name);
                }
            }
        }

        public virtual void OnDie(Scene scene)
        {
            var body = Body;
            if (body != null)
                world.RemoveBody(body);
        }
    }

    public class PhysicsManager : PhysicsObject
    {
        public PhysicsManager()
        {
            if (world == null)
                world = new World(new Vector2(0, 0));
        }

        public override Body Body => null;

        public override void Update(Scene scene, double dt)
        {
            world.Step((float)dt);
        }
    }

    public class AsteroidManager : IObject
    {
        private void TrySpawnRoid(Scene scene, Vector2 pos)
        {
            var roidRadius = Settings.Grab.AsteroidRadius;
            var good = true;
            foreach (var asteroid in scene.Objects.OfType<Asteroid>())
            {
                if ((asteroid.Body.Position - pos).LengthSquared() < roidRadius * roidRadius * Settings.Grab.AsteroidSpacing)
                {
                    good = false;
                    break;
                }
            }
            if (good)
                scene.Spawn(new Asteroid(roidRadius, pos));
        }

        public void Update(Scene scene, double dt)
        {
            var prob = 1f;
            var probHoriz = Math.Abs(scene.Camera.CenterVelocity.X);
            if (Util.rand.NextDouble() < probHoriz * dt * prob)
            {
                var pos = scene.Camera.Center + 1.2f * new Vector2((float)scene.Camera.Size * Math.Sign(scene.Camera.CenterVelocity.X), (float)((Util.rand.NextDouble() * 2 - 1) * scene.Camera.Size));
                TrySpawnRoid(scene, pos);
            }

            var probVirt = Math.Abs(scene.Camera.CenterVelocity.Y);
            if (Util.rand.NextDouble() < probVirt * dt * prob)
            {
                var pos = scene.Camera.Center + 1.2f * new Vector2((float)((Util.rand.NextDouble() * 2 - 1) * scene.Camera.Size), (float)scene.Camera.Size * Math.Sign(scene.Camera.CenterVelocity.Y));
                TrySpawnRoid(scene, pos);
            }
        }

        public void OnDie(Scene scene)
        {
        }
    }

    public class Asteroid : PhysicsObject
    {
        private readonly double size;
        private readonly Body body;

        public Asteroid(double sizeParam, Vector2 position)
        {
            size = Math.Exp(Settings.Grab.AsteroidSizeVariety * (Util.rand.NextDouble() - 0.5)) * sizeParam;
            Vertices verts = new Vertices();
            var numVerts = Util.rand.Next(Settings.Grab.AsteroidMinVerts, Settings.Grab.AsteroidMaxVerts);
            for (int i = 0; i < numVerts; i++)
            {
                var vertx = (float)((Util.rand.NextDouble() * 2 - 1) * size);
                var verty = (float)((Util.rand.NextDouble() * 2 - 1) * size);
                var vert = new Vector2(vertx, verty);
                if (vert.LengthSquared() > size * size)
                {
                    i--;
                    continue;
                }
                verts.Add(vert);
            }
            verts = FarseerPhysics.Common.ConvexHull.GiftWrap.GetConvexHull(verts);
            body = BodyFactory.CreatePolygon(world, verts, 1, position);
            body.BodyType = BodyType.Dynamic;
            body.AngularDamping = 0;
            body.LinearDamping = 0;
            body.Restitution = Settings.Grab.ObjectRestitution;
            if (Settings.Grab.AsteroidInitialVel > 0)
            {
                Vector2 vel;
                do
                {
                    vel = new Vector2((float)(Util.rand.NextDouble() * 2 - 1), (float)((Util.rand.NextDouble() * 2 - 1)));
                } while (vel.LengthSquared() > 1);
                vel *= Settings.Grab.AsteroidInitialVel;
                body.LinearVelocity = vel;
            }
            if (Settings.Grab.AsteroidInitialRot > 0)
            {
                var vel = (float)Util.rand.NextDouble();
                vel *= vel;
                vel *= Settings.Grab.AsteroidInitialRot;
                body.AngularVelocity = vel;
            }
        }

        public override Body Body => body;

        public override void Update(Scene scene, double dt)
        {
            var offset = body.Position - scene.Camera.Center;
            offset /= (float)scene.Camera.FixedSize * 1.5f;
            if (Math.Abs(offset.X) > 1 || Math.Abs(offset.Y) > 1)
                scene.Die(this);
        }
    }

    public static class JoystickManager
    {
        private static IntPtr joystick = IntPtr.Zero;

        static JoystickManager()
        {
            if (SDL2.SDL.SDL_Init(SDL2.SDL.SDL_INIT_JOYSTICK) != 0)
            {
                throw new Exception("Couldn't init SDL_INIT_JOYSTICK");
            }
        }

        internal static Vector2 GetJoystick()
        {
            if (joystick == IntPtr.Zero)
            {
                for (int i = 0; i < SDL2.SDL.SDL_NumJoysticks(); i++)
                {
                    joystick = SDL2.SDL.SDL_JoystickOpen(i);
                    if (joystick != IntPtr.Zero)
                    {
                        break;
                    }
                    Console.WriteLine("Attempted to open joystick " + i + " and failed");
                }
            }
            if (joystick == IntPtr.Zero)
            {
                throw new Exception("Joystick not found");
            }
            SDL2.SDL.SDL_JoystickUpdate();
            short x = SDL2.SDL.SDL_JoystickGetAxis(joystick, Settings.Grab.JoystickAxisX);
            short y = SDL2.SDL.SDL_JoystickGetAxis(joystick, Settings.Grab.JoystickAxisY);
            float fx = (float)x / short.MaxValue * (Settings.Grab.JoystickInvertX ? -1 : 1);
            float fy = (float)y / short.MaxValue * (Settings.Grab.JoystickInvertY ? -1 : 1);
            return new Vector2(fx, fy);
        }
    }

    public class Player : PhysicsObject
    {
        private readonly Pen red = Pens.Red;
        private readonly Body body;
        private PlayerLineTrace[] traces;
        private bool addTrace;
        private double drawExhaust;
        private float health = 1.0f;

        public Player()
        {
            var verts = new Vertices();
            var size = Settings.Grab.ShipSize;
            var angle = Settings.Grab.ShipShapeAngle;
            verts.Add(new Vector2(0, size));
            verts.Add(new Vector2((float)Math.Sin(angle) * -size, (float)Math.Cos(angle) * -size));
            verts.Add(new Vector2((float)Math.Sin(-angle) * -size, (float)Math.Cos(-angle) * -size));
            body = BodyFactory.CreatePolygon(world, verts, 1, new Vector2(0, 0));
            body.UserData = Pens.Yellow;
            body.BodyType = BodyType.Dynamic;
            body.LinearDamping = 0;
            body.AngularDamping = Settings.Grab.ShipAngularDamping;
            body.Restitution = Settings.Grab.ObjectRestitution;
            foreach (var fixture in body.FixtureList)
            {
                fixture.AfterCollision += PlayerOnCollision;
            }
        }

        private void PlayerOnCollision(Fixture player, Fixture other, Contact contact, ContactVelocityConstraint impulse)
        {
            var healthDeduction = impulse.points[0].normalImpulse;
            healthDeduction *= healthDeduction;
            healthDeduction /= Settings.Grab.ShipHealth;
            health -= healthDeduction;
        }

        public override Body Body => body;

        public override void Draw(Graphics graphics, Camera camera)
        {
            if (traces == null)
            {
                traces = new[] {
                    new PlayerLineTrace(this, camera, 0),
                    new PlayerLineTrace(this, camera, 1),
                    new PlayerLineTrace(this, camera, 2)
                };
                addTrace = true;
            }
            if (drawExhaust > 0)
            {
                var size = Settings.Grab.ShipSize;
                var angle = Settings.Grab.ShipShapeAngle / 2;
                var points = new PointF[3];
                points[0] = new PointF((float)Math.Sin(angle) * -size, (float)Math.Cos(angle) * -size);
                points[1] = new PointF((float)Math.Sin(-angle) * -size, (float)Math.Cos(-angle) * -size);
                points[2] = new PointF(0, -size - size * (float)drawExhaust);
                for (int i = 0; i < points.Length; i++)
                {
                    var vec = body.GetWorldPoint(new Vector2(points[i].X, points[i].Y));
                    points[i] = camera.Transform(vec.X, vec.Y);
                }
                graphics.DrawPolygon(red, points);
            }
            base.Draw(graphics, camera);
            graphics.DrawArc(red, new Rectangle(0, 0, 100, 100), 0, health * 360);
            var healthStr = (health * 100) + "%";
            var strSize = graphics.MeasureString(healthStr, Util.font);
            graphics.DrawString(healthStr, Util.font, Brushes.Red, new PointF(50 - strSize.Width / 2, 50 - strSize.Height / 2));
        }

        public override void Update(Scene scene, double dt)
        {
            if (addTrace)
            {
                foreach (var trace in traces)
                    scene.Spawn(trace);
                addTrace = false;
            }
            if (health < 0)
            {
                scene.Die(this);
                return;
            }
            var thrust = Settings.Grab.ShipThrust;
            var torque = Settings.Grab.ShipTorque;
            if (Settings.Grab.UseJoystick)
            {
                var joystick = JoystickManager.GetJoystick();
                joystick.Y = Math.Max(0, joystick.Y);
                drawExhaust = joystick.Y;
                var force = body.GetWorldVector(new Vector2(0, thrust * joystick.Y));
                body.ApplyForce(force);
                body.ApplyTorque(torque * joystick.X);
            }
            else
            {
                if (scene.PressedKeys.Contains(Settings.Grab.KeyThrust))
                {
                    var force = body.GetWorldVector(new Vector2(0, thrust));
                    body.ApplyForce(force);
                    drawExhaust = 1;
                }
                else
                    drawExhaust = 0;
                if (scene.PressedKeys.Contains(Settings.Grab.KeyTurnRight))
                {
                    body.ApplyTorque(torque);
                }
                if (scene.PressedKeys.Contains(Settings.Grab.KeyTurnLeft))
                {
                    body.ApplyTorque(-torque);
                }
            }
            var damp = 1 / (float)dt;
            var targetCamera = body.Position + body.LinearVelocity * 2;
            var oldCenter = scene.Camera.Center;
            if (float.IsNaN(oldCenter.X))
                throw new Exception();
            scene.Camera.Center = (oldCenter * damp + targetCamera) / (damp + 1);
            scene.Camera.CenterVelocity = (scene.Camera.Center - oldCenter) / (float)dt;

            if (scene.Camera.Center.LengthSquared() > 100 * 100)
            {
                world.ShiftOrigin(scene.Camera.Center);
                scene.Camera.OriginShift();
            }
        }
    }

    public class PlayerLineTrace : IObject, IDrawable
    {
        private readonly Camera subscribed;
        private readonly Player player;
        private readonly Vector2[] line;
        private readonly int vertexIndex;
        private double counter;

        public PlayerLineTrace(Player player, Camera camera, int vertexIndex)
        {
            this.vertexIndex = vertexIndex;
            this.player = player;
            subscribed = camera;
            subscribed.OnOriginShift += OnOriginShift;
            line = new Vector2[50];
            counter = 0;
        }

        private void OnOriginShift(Vector2 shift)
        {
            for (var i = 0; i < line.Length; i++)
                line[i] -= shift;
        }

        private Vector2 GetPos()
        {
            if (player.Body.FixtureList == null)
                return player.Body.Position;
            return player.Body.GetWorldPoint(((PolygonShape)player.Body.FixtureList[0].Shape).Vertices[vertexIndex]);
        }

        private Vector2 GetFuturePos()
        {
            var pos = GetPos();
            var vel = player.Body.LinearVelocity;
            return pos + vel * Settings.Grab.FuturePrediction;
        }

        public void Draw(Graphics graphics, Camera camera)
        {
            var points = new PointF[line.Length + 2];
            for (var i = 0; i < line.Length; i++)
            {
                var point = line[i];
                points[i + 2] = camera.Transform(point.X, point.Y);
            }
            var current = GetPos();
            points[1] = camera.Transform(current.X, current.Y);
            var future = GetFuturePos();
            points[0] = camera.Transform(future.X, future.Y);
            graphics.DrawLines(Pens.SlateGray, points);
        }

        public void OnDie(Scene scene)
        {
            subscribed.OnOriginShift -= OnOriginShift;
        }

        public void Update(Scene scene, double dt)
        {
            var rollover = 0.125f;
            counter += dt;
            if (counter > rollover)
            {
                counter %= rollover;
                for (var i = line.Length - 2; i >= 0; i--)
                    line[i + 1] = line[i];
                line[0] = GetPos();
            }
        }
    }
}
