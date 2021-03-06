﻿using System;
using System.IO;

namespace Spacerunner3
{
    class Program
    {
        public static void Reset(Scene scene)
        {
            var step = 1.2;
            Console.WriteLine(step);
            Console.WriteLine(Math.Log(step, 2));
            Console.WriteLine(Math.Round(Math.Log(step, 2)));
            Console.WriteLine(Math.Pow(2, Math.Round(Math.Log(step, 2))));
            scene.Spawn(new SceneClearer());
            scene.Update(0);
            if (scene.Objects.Count != 0)
            {
                throw new System.Exception("Reset failed, objects remaining: " + string.Join(", ", scene.Objects));
            }

            scene.Camera.ResetOriginShift();
            scene.Spawn(new PhysicsManager());
            scene.Spawn(new AsteroidManager());
            scene.Spawn(new DistanceTracker(scene.Camera));
            scene.Spawn(new Player());
            scene.Update(0);
        }

        private static void InitSettings(string? filename)
        {
            if (filename == null)
            {
                filename = "settings.cfg";
            }
            Settings.Grab = File.Exists(filename) ? Settings.Load(filename) : new Settings();
        }

        private static void SaveSettings(string? filename)
        {
            if (filename == null)
            {
                filename = "settings.cfg";
            }
            if (!File.Exists(filename))
            {
                Settings.Grab.Save(filename);
            }
        }

        static void Main(string?[] args)
        {
            var arg = args.Length == 0 ? null : args[0];
            if (arg != null && arg.EndsWith(".srv3") == true)
            {
                new MovieWindow(arg).Run();
                return;
            }
            InitSettings(arg);
            var scene = new Scene(Settings.Grab.ScreenSize);
            Reset(scene);
            var display = new DisplayWindow(scene);
            display.Run();
            SaveSettings(arg);
        }
    }
}
