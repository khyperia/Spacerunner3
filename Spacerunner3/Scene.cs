using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System;

namespace Spacerunner3
{
    public class Scene
    {
        private List<IObject> objects;
        private List<IObject> toSpawn;
        private List<IObject> toDie;
        private ISet<Keys> pressedKeys;

        public Scene(double screenSize)
        {
            this.Camera = new Camera(screenSize);
            objects = new List<IObject>();
            toSpawn = new List<IObject>();
            toDie = new List<IObject>();
            pressedKeys = new HashSet<Keys>();
        }

        public Camera Camera { get; }

        public List<IObject> Objects => objects;

        public IEnumerable<IDrawable> Drawables => objects.OfType<IDrawable>();

        public ISet<Keys> PressedKeys => pressedKeys;

        public void Die(IObject obj)
        {
            toDie.Add(obj);
        }

        public void Spawn(IObject obj)
        {
            toSpawn.Add(obj);
        }

        public void Update(double dt, bool physics)
        {
            if (toSpawn.Count > 0)
            {
                foreach (var spawn in toSpawn)
                {
                    objects.Add(spawn);
                }
                toSpawn.Clear();
            }
            if (physics)
            {
                foreach (var obj in objects)
                {
                    obj.Update(this, dt);
                }
            }
            if (toDie.Count > 0)
            {
                foreach (var die in toDie)
                {
                    objects.Remove(die);
                    die.OnDie(this);
                }
                toDie.Clear();
            }
        }
    }

    public class Camera
    {
        public Camera(double screenSize)
        {
            Center = new Vector2(0, 0);
            SizeMultiplier = 1;
            FixedSize = screenSize;
        }

        public Vector2 CenterVelocity { get; set; }
        public Vector2 Center { get; set; }
        public double Size => FixedSize * SizeMultiplier;
        public double FixedSize { get; }
        public double SizeMultiplier { get; set; }
        public Size ScreenScale { get; set; }

        public event Action<Vector2> OnOriginShift;

        public void OriginShift()
        {
            OnOriginShift?.Invoke(Center);
            Center = new Vector2(0, 0);
        }

        public PointF Transform(double x, double y)
        {
            var point = new Vector2((float)x, (float)y);
            point -= Center;
            point /= (float)Size;
            point = (point * 0.5f + new Vector2(0.5f, (float)ScreenScale.Height / (2 * ScreenScale.Width))) * ScreenScale.Width;
            return new PointF(point.X, point.Y);
        }

        internal void ResetOriginShift()
        {
            Center = new Vector2(0, 0);
            OnOriginShift = null;
        }
    }

    public class DistanceTracker : IObject, IDrawable
    {
        private Vector2 originShift;
        private float distance;
        private double time;

        public DistanceTracker(Camera camera)
        {
            originShift = new Vector2(0, 0);
            camera.OnOriginShift += shift => originShift += shift;
        }

        public void OnDie(Scene scene)
        {
        }

        public void Update(Scene scene, double dt)
        {
            var player = scene.Objects.OfType<Player>().FirstOrDefault();
            if (player != null)
            {
                distance = (player.Body.Position + originShift).Length();
                time += dt;
            }
        }

        public void Draw(Graphics graphics, Camera camera)
        {
            var str = "distance:  " + distance;
            var measure = graphics.MeasureString(str, Util.font);
            graphics.DrawString(str, Util.font, Brushes.Yellow, new PointF(110, 25 - measure.Height / 2));
            str = "time:      " + time;
            measure = graphics.MeasureString(str, Util.font);
            graphics.DrawString(str, Util.font, Brushes.Yellow, new PointF(110, 75 - measure.Height / 2));
            str = "dist/time: " + distance / time;
            measure = graphics.MeasureString(str, Util.font);
            graphics.DrawString(str, Util.font, Brushes.Yellow, new PointF(110, 50 - measure.Height / 2));
        }
    }

    public interface IObject
    {
        void Update(Scene scene, double dt);
        void OnDie(Scene scene);
    }

    public interface IDrawable : IObject
    {
        void Draw(Graphics graphics, Camera camera);
    }
}