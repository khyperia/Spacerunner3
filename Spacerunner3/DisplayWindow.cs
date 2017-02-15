using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SDL2;

namespace Spacerunner3
{
    public class Graphics
    {
        IntPtr renderer;
        int width, height;
        MemoryStream stream;
        BinaryWriter writer;
        BinaryReader reader;

        public Graphics(IntPtr renderer, BinaryReader readerOpt)
        {
            this.renderer = renderer;
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            reader = readerOpt;
        }

        // 1gb max
        private bool Recording => stream.Position < 1024 * 1024 * 1024 && reader == null;

        public bool DrawRecordedFrame()
        {
            SDL.SDL_GetRendererOutputSize(renderer, out width, out height);
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var r = reader.ReadByte();
                var g = reader.ReadByte();
                var b = reader.ReadByte();
                var p1x = reader.ReadSingle();
                var p1y = reader.ReadSingle();
                var p2x = reader.ReadSingle();
                var p2y = reader.ReadSingle();
                if (p1x == 0 && p1y == 0 && p2x == 0 && p2y == 0)
                {
                    if (r == 0 && g == 0 && b == 0)
                    {
                        return false;
                    }
                    else
                    {
                        Clear(r, g, b);
                    }
                }
                else
                {
                    var p1 = new Vector2(p1x * width, p1y * height);
                    var p2 = new Vector2(p2x * width, p2y * height);
                    Line(p1.Point, p2.Point, r, g, b, false);
                }
            }
            return true;
        }

        private void Record(byte r, byte g, byte b, int p1x, int p1y, int p2x, int p2y)
        {
            writer.Write(r);
            writer.Write(g);
            writer.Write(b);
            writer.Write((float)p1x / width);
            writer.Write((float)p1y / height);
            writer.Write((float)p2x / width);
            writer.Write((float)p2y / height);
        }

        public void Clear(byte r, byte g, byte b)
        {
            SDL.SDL_GetRendererOutputSize(renderer, out width, out height);
            SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 255);
            SDL.SDL_RenderClear(renderer);

            if (Recording)
            {
                Record(r, g, b, 0, 0, 0, 0);
            }
        }

        public void MarkEndOfFrame()
        {
            if (Recording)
            {
                Record(0, 0, 0, 0, 0, 0, 0);
            }
        }

        private void Line(Point p1, Point p2, byte r, byte g, byte b, bool record)
        {
            var draw = p1.X >= 0 && p1.X < width && p1.Y >= 0 && p1.Y < height ||
                       p2.X >= 0 && p2.X < width && p2.Y >= 0 && p2.Y < height;
            if (!draw)
            {
                Microsoft.Xna.Framework.Vector2 point;
                var left = FarseerPhysics.Common.LineTools.LineIntersect(p1.Vector.Xna, p2.Vector.Xna,
                    new Microsoft.Xna.Framework.Vector2(0, 0), new Microsoft.Xna.Framework.Vector2(0, height), true,
                    true, out point);
                var right = FarseerPhysics.Common.LineTools.LineIntersect(p1.Vector.Xna, p2.Vector.Xna,
                    new Microsoft.Xna.Framework.Vector2(width, 0), new Microsoft.Xna.Framework.Vector2(width, height),
                    true, true, out point);
                var up = FarseerPhysics.Common.LineTools.LineIntersect(p1.Vector.Xna, p2.Vector.Xna,
                    new Microsoft.Xna.Framework.Vector2(0, 0), new Microsoft.Xna.Framework.Vector2(width, 0), true, true,
                    out point);
                var down = FarseerPhysics.Common.LineTools.LineIntersect(p1.Vector.Xna, p2.Vector.Xna,
                    new Microsoft.Xna.Framework.Vector2(0, height), new Microsoft.Xna.Framework.Vector2(width, height),
                    true, true, out point);
                draw = left | right | up | down;
            }
            if (draw)
            {
                SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 255);
                SDL.SDL_RenderDrawLine(renderer, p1.X, p1.Y, p2.X, p2.Y);
                if (record && Recording)
                {
                    Record(r, g, b, p1.X, p1.Y, p2.X, p2.Y);
                }
            }
        }

        public void Line(Point p1, Point p2, byte r, byte g, byte b)
        {
            Line(p1, p2, r, g, b, true);
        }

        public void Arc(Vector2 center, double radius, double radStart, double radAmount, byte r, byte g, byte b)
        {
            double rot = 0;
            const double inc = Math.PI / 64;
            for (; rot + inc < radAmount; rot += inc)
            {
                var one = center + radius * new Vector2(Math.Cos(radStart + rot), Math.Sin(radStart + rot));
                var two = center + radius * new Vector2(Math.Cos(radStart + rot + inc), Math.Sin(radStart + rot + inc));
                Line(one.Point, two.Point, r, g, b, false);
            }
            var onef = center + radius * new Vector2(Math.Cos(radStart + rot), Math.Sin(radStart + rot));
            var twof = center + radius * new Vector2(Math.Cos(radStart + radAmount), Math.Sin(radStart + radAmount));
            Line(onef.Point, twof.Point, r, g, b, false);
        }

        public void SaveVideo(string name)
        {
            var length = stream.Position;
            stream.Position = 0;
            string filename;
            var fileIndex = 0;
            do
            {
                filename = name + (fileIndex == 0 ? "" : "_" + fileIndex) + ".srv3";
                fileIndex++;
            } while (File.Exists(filename));
            using (var fileStream = File.OpenWrite(filename))
            {
                stream.CopyTo(fileStream);
            }
            stream.Position = length;
            Console.WriteLine("Saved " + filename);
        }

        public void ResetVideo()
        {
            stream.SetLength(0);
        }
    }

    public class MovieWindow
    {
        private readonly IntPtr window;
        private readonly IntPtr renderer;
        private readonly Graphics graphics;
        public MovieWindow(string filename)
        {
            window = SDL.SDL_CreateWindow("Spacerunner 3", 300, 300, 1000, 800, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            if (window == IntPtr.Zero)
            {
                (-1).CheckSdl();
            }
            renderer = SDL.SDL_CreateRenderer(window, -1, (uint)SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            if (renderer == IntPtr.Zero)
            {
                (-1).CheckSdl();
            }
            var reader = new BinaryReader(File.OpenRead(filename));
            graphics = new Graphics(renderer, reader);
        }

        public void Run()
        {
            var going = false;
            while (true)
            {
                SDL.SDL_Event evnt;
                while (SDL.SDL_PollEvent(out evnt) != 0)
                {
                    if (evnt.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        return;
                    }
                    if (evnt.type == SDL.SDL_EventType.SDL_KEYDOWN &&
                        evnt.key.keysym.scancode == SDL.SDL_Scancode.SDL_SCANCODE_SPACE)
                    {
                        going = !going;
                    }
                }
                if (going)
                {
                    if (graphics.DrawRecordedFrame())
                    {
                        break;
                    }
                }
                SDL.SDL_RenderPresent(renderer);
            }
        }
    }

    public class DisplayWindow
    {
        private readonly IntPtr window;
        private readonly IntPtr renderer;
        private readonly Scene scene;
        private readonly Graphics graphics;
        private int thing;
        private Stopwatch fps;
        private double fpsCounter;
        private bool paused;

        public DisplayWindow(Scene scene)
        {
            window = SDL.SDL_CreateWindow("Spacerunner 3", 300, 300, 1000, 800, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            if (window == IntPtr.Zero)
            {
                (-1).CheckSdl();
            }
            renderer = SDL.SDL_CreateRenderer(window, -1, (uint)SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            if (renderer == IntPtr.Zero)
            {
                (-1).CheckSdl();
            }
            graphics = new Graphics(renderer, null);
            this.scene = scene;
        }

        private void SaveVideo()
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var score = scene.Objects.OfType<DistanceTracker>().First().Describe();
            var name = now + "_" + score;
            graphics.SaveVideo(name);
        }

        public void Run()
        {
            while (true)
            {
                SDL.SDL_Event evnt;
                if (SDL.SDL_PollEvent(out evnt) != 0)
                {
                    if (evnt.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        break;
                    }
                    else if (evnt.type == SDL.SDL_EventType.SDL_KEYDOWN)
                    {
                        if (evnt.key.keysym.scancode == Settings.Grab.KeyReset)
                        {
                            Program.Reset(scene);
                            graphics.ResetVideo();
                        }
                        if (evnt.key.keysym.scancode == Settings.Grab.KeyPause)
                            paused = !paused;
                        if (evnt.key.keysym.scancode == Settings.Grab.KeySaveVideo)
                            SaveVideo();
                        scene.PressedKeys.Add(evnt.key.keysym.scancode);
                    }
                    else if (evnt.type == SDL.SDL_EventType.SDL_KEYUP)
                    {
                        scene.PressedKeys.Remove(evnt.key.keysym.scancode);
                    }
                }
                else
                {
                    FormPaint();
                }
            }
        }

        private void FormPaint()
        {
            int width, height;
            SDL.SDL_GetRendererOutputSize(renderer, out width, out height).CheckSdl();
            scene.Camera.ScreenScale = new Vector2(width, height);
            if (fps == null)
                fps = Stopwatch.StartNew();
            else
            {
                var elapsed = fps.Elapsed.TotalSeconds;
                fps.Restart();
                fpsCounter = (fpsCounter * 20 + 1 / elapsed) / 21;
                if (!paused)
                    scene.Update(elapsed);
            }

            graphics.Clear(40, 79, 79);

            var camera = scene.Camera;
            foreach (var drawable in scene.Drawables)
            {
                drawable.Draw(graphics, camera);
            }

            const int wrap = 100;
            graphics.Arc(new Vector2(width - 50, 50), 48, 0, (thing = (thing + 1) % wrap) * (2 * Math.PI / wrap), 255,
                140, 0);

            if (thing == 0)
            {
                var score = scene.Objects.OfType<DistanceTracker>().First().Describe();
                SDL.SDL_SetWindowTitle(window, $"Spacerunner 3 - {fpsCounter}fps{(paused ? " - PAUSED" : "")} - {score}");
            }

            graphics.MarkEndOfFrame();
            SDL.SDL_RenderPresent(renderer);
        }
    }
}