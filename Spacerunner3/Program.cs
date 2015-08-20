using System.IO;

namespace Spacerunner3
{
    class Program
    {
        public static void Reset(Scene scene)
        {
            foreach (var obj in scene.Objects)
                scene.Die(obj);
            scene.Update(0, false);
            if (scene.Objects.Count != 0)
                throw new System.Exception("Reset failed, objects remaining: " + string.Join(", ", scene.Objects));
            scene.Camera.ResetOriginShift();
            scene.Spawn(new PhysicsManager());
            scene.Spawn(new AsteroidManager());
            scene.Spawn(new DistanceTracker(scene.Camera));
            scene.Spawn(new Player());
            scene.Update(0, false);
        }

        private static void InitSettings(string filename)
        {
            filename = filename ?? "settings.xml";
            Settings.Grab = File.Exists(filename) ? Settings.Load(filename) : new Settings();
        }

        private static void SaveSettings(string filename)
        {
            filename = filename ?? "settings.xml";
            Settings.Grab.Save(filename);
        }

        static void Main(string[] args)
        {
            InitSettings(args.Length == 0 ? null : args[0]);
            var scene = new Scene(Settings.Grab.ScreenSize);
            Reset(scene);
            var display = new DisplayWindow(scene);
            display.Run();
            SaveSettings(args.Length == 0 ? null : args[0]);
        }
    }
}
