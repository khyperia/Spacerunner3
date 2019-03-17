using System;
using System.Linq;

namespace Spacerunner3
{
    public class AsteroidManager : IObject
    {
        private void TrySpawnRoid(Scene scene, Vector2 pos)
        {
            var roidRadius = Settings.Grab.AsteroidRadius;
            var good = true;
            foreach (var asteroid in scene.Objects.OfType<Asteroid>())
            {
                if (asteroid.Body != null && (asteroid.Body.Position - pos.Xna).LengthSquared() < roidRadius * roidRadius * Settings.Grab.AsteroidSpacing)
                {
                    good = false;
                    break;
                }
            }
            if (good)
            {
                scene.Spawn(new Asteroid(roidRadius, pos));
            }
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
}
