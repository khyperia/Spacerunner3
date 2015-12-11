using System;
using System.Diagnostics;
using SDL2;

namespace Spacerunner3
{
    public class Graphics
    {
        IntPtr renderer;

        public Graphics(IntPtr renderer)
        {
            this.renderer = renderer;
        }

        public void Clear(byte r, byte g, byte b)
        {
            SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 255);
            SDL.SDL_RenderClear(renderer);
        }

        public void Line(Point p1, Point p2, byte r, byte g, byte b)
        {
            SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 255);
            SDL.SDL_RenderDrawLine(renderer, p1.X, p1.Y, p2.X, p2.Y);
        }

        public void Arc(Vector2 center, double radius, double radStart, double radAmount, byte r, byte g, byte b)
        {
            double rot = 0;
            const double inc = Math.PI / 64;
            for (; rot + inc < radAmount; rot += inc)
            {
                var one = center + radius * new Vector2(Math.Cos(radStart + rot), Math.Sin(radStart + rot));
                var two = center + radius * new Vector2(Math.Cos(radStart + rot + inc), Math.Sin(radStart + rot + inc));
                Line(one.Point, two.Point, r, g, b);
            }
            var onef = center + radius * new Vector2(Math.Cos(radStart + rot), Math.Sin(radStart + rot));
            var twof = center + radius * new Vector2(Math.Cos(radStart + radAmount), Math.Sin(radStart + radAmount));
            Line(onef.Point, twof.Point, r, g, b);
        }
    }

    public class DisplayWindow
    {
        private readonly IntPtr window;
        private readonly IntPtr renderer;
        private readonly Scene scene;
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
            this.scene = scene;
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
                            Program.Reset(scene);
                        if (evnt.key.keysym.scancode == Settings.Grab.KeyPause)
                            paused = !paused;
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

            var graphics = new Graphics(renderer);
            graphics.Clear(40, 79, 79);

            var camera = scene.Camera;
            foreach (var drawable in scene.Drawables)
            {
                drawable.Draw(graphics, camera);
            }

            const int wrap = 100;
            graphics.Arc(new Vector2(width - 50, 50), 48, 0, (thing = (thing + 1) % wrap) * (2 * Math.PI / wrap), 255, 140, 0);

            if (thing == 0)
            {
                SDL.SDL_SetWindowTitle(window, "Spacerunner 3 - " + fpsCounter + "fps" + (paused ? " - PAUSED" : ""));
            }

            SDL.SDL_RenderPresent(renderer);
        }
    }
}
