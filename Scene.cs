using System.Collections.Generic;
using System.Linq;
using SDL2;

namespace Spacerunner3
{
    public class Scene
    {
        private readonly List<IObject> _toSpawn;
        private readonly List<IObject> _toDie;

        public Scene(double screenSize)
        {
            Camera = new Camera(screenSize);
            Objects = new List<IObject>();
            _toSpawn = new List<IObject>();
            _toDie = new List<IObject>();
            PressedKeys = new HashSet<SDL.SDL_Scancode>();
        }

        public Camera Camera { get; }

        public List<IObject> Objects { get; }

        public IEnumerable<IDrawable> Drawables => Objects.OfType<IDrawable>();

        public ISet<SDL.SDL_Scancode> PressedKeys { get; }

        public void Die(IObject obj) => _toDie.Add(obj);

        public void Spawn(IObject obj) => _toSpawn.Add(obj);

        public void Update(double dt)
        {
            if (_toSpawn.Count > 0)
            {
                foreach (var spawn in _toSpawn)
                {
                    Objects.Add(spawn);
                }
                _toSpawn.Clear();
            }
            foreach (var obj in Objects)
            {
                obj.Update(this, dt);
            }
            if (_toDie.Count > 0)
            {
                foreach (var die in _toDie)
                {
                    Objects.Remove(die);
                    die.OnDie(this);
                }
                _toDie.Clear();
            }
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