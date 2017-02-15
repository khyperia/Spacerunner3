using System;
using System.Linq;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using SDL2;

namespace Spacerunner3
{
    public static class Util
    {
        public static Random rand = new Random();
        public static void CheckSdl(this int retval)
        {
            if (retval != 0)
            {
                throw new Exception("SDL call failure: returned (" + retval + "): " + SDL.SDL_GetError());
            }
        }

        public static Vector2 MyVec(this Microsoft.Xna.Framework.Vector2 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }
    }

    class MyColor
    {
        public readonly byte r, g, b;

        public MyColor(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
    }

    public abstract class PhysicsObject : IObject, IDrawable
    {
        private static MyColor defaultPen = new MyColor(255, 255, 255);
        protected static World world;

        public abstract void Update(Scene scene, double dt);

        public abstract Body Body { get; }

        public virtual void Draw(Graphics graphics, Camera camera)
        {
            var body = Body;
            if (body == null)
                return;
            var pen = body.UserData as MyColor ?? defaultPen;
            foreach (var fixture in body.FixtureList)
            {
                var shape = fixture.Shape;
                var poly = shape as PolygonShape;
                var edge = shape as EdgeShape;
                if (poly != null)
                {
                    for (int i = 0; i < poly.Vertices.Count; i++)
                    {
                        var vert1 = body.GetWorldPoint(poly.Vertices[i]).MyVec();
                        var vert2 = body.GetWorldPoint(poly.Vertices[(i + 1) % poly.Vertices.Count]).MyVec();
                        vert1 = camera.Transform(vert1.X, vert1.Y);
                        vert2 = camera.Transform(vert2.X, vert2.Y);
                        graphics.Line(vert1.Point, vert2.Point, pen.r, pen.g, pen.b);
                    }
                }
                else if (edge != null)
                {
                    var vert1 = body.GetWorldPoint(edge.Vertex1).MyVec();
                    var vert2 = body.GetWorldPoint(edge.Vertex2).MyVec();
                    vert1 = camera.Transform(vert1.X, vert1.Y);
                    vert2 = camera.Transform(vert2.X, vert2.Y);
                    graphics.Line(vert1.Point, vert2.Point, pen.r, pen.g, pen.b);
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

    public class SceneClearer : PhysicsObject
    {
        public override Body Body => null;

        public override void Update(Scene scene, double dt)
        {
            foreach (var obj in scene.Objects)
                scene.Die(obj);
        }
    }

    public class PhysicsManager : PhysicsObject
    {
        public PhysicsManager()
        {
            if (world == null)
                world = new World(new Microsoft.Xna.Framework.Vector2(0, 0));
        }

        public override Body Body => null;

        public override void Update(Scene scene, double dt)
        {
            if (dt > 0)
            {
                world.Step((float)dt);
            }
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
                if ((asteroid.Body.Position - pos.Xna).LengthSquared() < roidRadius * roidRadius * Settings.Grab.AsteroidSpacing)
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
                if (vert.Length2 > size * size)
                {
                    i--;
                    continue;
                }
                verts.Add(vert.Xna);
            }
            verts = FarseerPhysics.Common.ConvexHull.GiftWrap.GetConvexHull(verts);
            body = BodyFactory.CreatePolygon(world, verts, 1, position.Xna);
            body.BodyType = BodyType.Dynamic;
            body.AngularDamping = 0;
            body.LinearDamping = 0;
            body.Restitution = Settings.Grab.ObjectRestitution;
            if (Settings.Grab.AsteroidInitialVel > 0)
            {
                Microsoft.Xna.Framework.Vector2 vel;
                do
                {
                    vel = new Microsoft.Xna.Framework.Vector2((float)(Util.rand.NextDouble() * 2 - 1), (float)((Util.rand.NextDouble() * 2 - 1)));
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
            var offset = body.Position - scene.Camera.Center.Xna;
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
        private readonly MyColor red = new MyColor(255, 0, 0);
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
            verts.Add(new Microsoft.Xna.Framework.Vector2(0, size));
            verts.Add(new Microsoft.Xna.Framework.Vector2((float)Math.Sin(angle) * -size, (float)Math.Cos(angle) * -size));
            verts.Add(new Microsoft.Xna.Framework.Vector2((float)Math.Sin(-angle) * -size, (float)Math.Cos(-angle) * -size));
            body = BodyFactory.CreatePolygon(world, verts, 1, new Vector2(0, 0));
            body.UserData = new MyColor(255, 255, 0);
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
                var points = new Vector2[3];
                points[0] = new Vector2((float)Math.Sin(angle) * -size, (float)Math.Cos(angle) * -size);
                points[1] = new Vector2((float)Math.Sin(-angle) * -size, (float)Math.Cos(-angle) * -size);
                points[2] = new Vector2(0, -size - size * (float)drawExhaust);
                for (int i = 0; i < points.Length; i++)
                {
                    var vec = body.GetWorldPoint(new Microsoft.Xna.Framework.Vector2((float)points[i].X, (float)points[i].Y));
                    points[i] = camera.Transform(vec.X, vec.Y);
                }
                for (int i = 0; i < points.Length; i++)
                {
                    graphics.Line(points[i].Point, points[(i + 1) % points.Length].Point, red.r, red.g, red.b);
                }
            }
            base.Draw(graphics, camera);
            graphics.Arc(new Vector2(50, 50), 50, 0, health * (2 * Math.PI), red.r, red.g, red.b);
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
                drawExhaust = joystick.Y;
                var force = body.GetWorldVector(new Vector2(0, thrust * joystick.Y).Xna);
                body.ApplyForce(force);
                body.ApplyTorque((float)(torque * joystick.X));
            }
            else
            {
                if (scene.PressedKeys.Contains(Settings.Grab.KeyThrust))
                {
                    var force = body.GetWorldVector(new Microsoft.Xna.Framework.Vector2(0, thrust));
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
            var targetCamera = (body.Position + body.LinearVelocity * 2).MyVec();
            var oldCenter = scene.Camera.Center;
            if (double.IsNaN(oldCenter.X))
                throw new Exception();
            scene.Camera.Center = (oldCenter * damp + targetCamera) * (1 / (damp + 1));
            scene.Camera.CenterVelocity = (scene.Camera.Center - oldCenter) * (1 / dt);

            if (scene.Camera.Center.Length2 > 100 * 100)
            {
                world.ShiftOrigin(new Microsoft.Xna.Framework.Vector2((float)scene.Camera.Center.X, (float)scene.Camera.Center.Y));
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

        private const int NumPoints = 10;
        private const float PointFrequency = 0.25f;

        public PlayerLineTrace(Player player, Camera camera, int vertexIndex)
        {
            this.vertexIndex = vertexIndex;
            this.player = player;
            subscribed = camera;
            subscribed.OnOriginShift += OnOriginShift;
            line = new Vector2[NumPoints];
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
                return player.Body.Position.MyVec();
            return player.Body.GetWorldPoint(((PolygonShape)player.Body.FixtureList[0].Shape).Vertices[vertexIndex]).MyVec();
        }

        private Vector2 GetFuturePos()
        {
            var pos = GetPos();
            var vel = player.Body.LinearVelocity.MyVec();
            return pos + vel * Settings.Grab.FuturePrediction;
        }

        public void Draw(Graphics graphics, Camera camera)
        {
            var points = new Vector2[line.Length + 2];
            for (var i = 0; i < line.Length; i++)
            {
                var point = line[i];
                points[i + 2] = camera.Transform(point.X, point.Y);
            }
            var current = GetPos();
            points[1] = camera.Transform(current.X, current.Y);
            var future = GetFuturePos();
            points[0] = camera.Transform(future.X, future.Y);
            for (int i = 0; i < points.Length - 1; i++)
            {
                graphics.Line(points[i].Point, points[i + 1].Point, 112, 128, 144);
            }
        }

        public void OnDie(Scene scene)
        {
            subscribed.OnOriginShift -= OnOriginShift;
        }

        public void Update(Scene scene, double dt)
        {
            counter += dt;
            if (counter > PointFrequency)
            {
                counter %= PointFrequency;
                for (var i = line.Length - 2; i >= 0; i--)
                    line[i + 1] = line[i];
                line[0] = GetPos();
            }
        }
    }
}
