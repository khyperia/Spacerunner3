using FarseerPhysics.Dynamics;

namespace Spacerunner3
{
    public class PhysicsManager : PhysicsObject
    {
        public PhysicsManager()
        {
            if (world == null)
            {
                world = new World(new Microsoft.Xna.Framework.Vector2(0, 0));
            }
        }

        public override Body? Body => null;

        public override void Update(Scene scene, double dt)
        {
            if (dt > 0)
            {
                world.Step((float)dt);
            }
        }
    }
}
