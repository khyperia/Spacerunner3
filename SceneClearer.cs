using FarseerPhysics.Dynamics;

namespace Spacerunner3
{
    public class SceneClearer : PhysicsObject
    {
        public override Body? Body => null;

        public override void Update(Scene scene, double dt)
        {
            foreach (var obj in scene.Objects)
            {
                scene.Die(obj);
            }
        }
    }
}
