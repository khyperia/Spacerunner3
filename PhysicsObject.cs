using System;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;

namespace Spacerunner3
{
    public abstract class PhysicsObject : IObject, IDrawable
    {
        private static readonly MyColor _defaultPen = new MyColor(255, 255, 255);
        protected static World world;

        public abstract void Update(Scene scene, double dt);

        public abstract Body? Body { get; }

        public virtual void Draw(Graphics graphics, Camera camera)
        {
            var body = Body;
            if (body == null)
            {
                return;
            }

            var pen = body.UserData as MyColor ?? _defaultPen;
            foreach (var fixture in body.FixtureList)
            {
                var shape = fixture.Shape;
                if (shape is PolygonShape poly)
                {
                    for (var i = 0; i < poly.Vertices.Count; i++)
                    {
                        var vert1 = body.GetWorldPoint(poly.Vertices[i]).MyVec();
                        var vert2 = body.GetWorldPoint(poly.Vertices[(i + 1) % poly.Vertices.Count]).MyVec();
                        vert1 = camera.Transform(vert1.X, vert1.Y);
                        vert2 = camera.Transform(vert2.X, vert2.Y);
                        graphics.Line(vert1.Point, vert2.Point, pen.r, pen.g, pen.b);
                    }
                }
                else if (shape is EdgeShape edge)
                {
                    var vert1 = body.GetWorldPoint(edge.Vertex1).MyVec();
                    var vert2 = body.GetWorldPoint(edge.Vertex2).MyVec();
                    vert1 = camera.Transform(vert1.X, vert1.Y);
                    vert2 = camera.Transform(vert2.X, vert2.Y);
                    graphics.Line(vert1.Point, vert2.Point, pen.r, pen.g, pen.b);
                }
                else if (shape is null)
                {
                    continue;
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
            {
                world.RemoveBody(body);
            }
        }
    }
}
