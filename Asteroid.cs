using System;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;

namespace Spacerunner3
{
    public class Asteroid : PhysicsObject
    {
        private readonly double _size;
        private readonly Body _body;

        public Asteroid(double sizeParam, Vector2 position)
        {
            _size = Math.Exp(Settings.Grab.AsteroidSizeVariety * (Util.rand.NextDouble() - 0.5)) * sizeParam;
            var verts = new Vertices();
            var numVerts = Util.rand.Next(Settings.Grab.AsteroidMinVerts, Settings.Grab.AsteroidMaxVerts);
            for (var i = 0; i < numVerts; i++)
            {
                var vertx = (float)((Util.rand.NextDouble() * 2 - 1) * _size);
                var verty = (float)((Util.rand.NextDouble() * 2 - 1) * _size);
                var vert = new Vector2(vertx, verty);
                if (vert.Length2 > _size * _size)
                {
                    i--;
                    continue;
                }
                verts.Add(vert.Xna);
            }
            verts = FarseerPhysics.Common.ConvexHull.GiftWrap.GetConvexHull(verts);
            _body = BodyFactory.CreatePolygon(world, verts, 1, position.Xna);
            _body.BodyType = BodyType.Dynamic;
            _body.AngularDamping = 0;
            _body.LinearDamping = 0;
            _body.Restitution = Settings.Grab.ObjectRestitution;
            if (Settings.Grab.AsteroidInitialVel > 0)
            {
                Microsoft.Xna.Framework.Vector2 vel;
                do
                {
                    vel = new Microsoft.Xna.Framework.Vector2((float)(Util.rand.NextDouble() * 2 - 1), (float)((Util.rand.NextDouble() * 2 - 1)));
                } while (vel.LengthSquared() > 1);
                vel *= Settings.Grab.AsteroidInitialVel;
                _body.LinearVelocity = vel;
            }
            if (Settings.Grab.AsteroidInitialRot > 0)
            {
                var vel = (float)Util.rand.NextDouble();
                vel *= vel;
                vel *= Settings.Grab.AsteroidInitialRot;
                _body.AngularVelocity = vel;
            }
        }

        public override Body? Body => _body;

        public override void Update(Scene scene, double dt)
        {
            var offset = _body.Position - scene.Camera.Center.Xna;
            offset /= (float)scene.Camera.FixedSize * 1.5f;
            if (Math.Abs(offset.X) > 1 || Math.Abs(offset.Y) > 1)
            {
                scene.Die(this);
            }
        }
    }
}
