using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SDL2;

namespace Spacerunner3
{
    public class Graphics
    {
        private readonly IntPtr _renderer;
        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader? _reader;
        private int _width, _height;

        public Graphics(IntPtr renderer, BinaryReader? readerOpt)
        {
            _renderer = renderer;
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
            _reader = readerOpt;
        }

        // 1gb max
        private bool Recording => _stream.Position < 1024 * 1024 * 1024 && _reader == null;

        public bool DrawRecordedFrame()
        {
            if (_reader is null)
            {
                throw new Exception("Cannot use DrawRecordedFrame without a reader");
            }

            SDL.SDL_GetRendererOutputSize(_renderer, out _width, out _height);
            while (_reader.BaseStream.Position < _reader.BaseStream.Length)
            {
                var r = _reader.ReadByte();
                var g = _reader.ReadByte();
                var b = _reader.ReadByte();
                var p1x = _reader.ReadSingle();
                var p1y = _reader.ReadSingle();
                var p2x = _reader.ReadSingle();
                var p2y = _reader.ReadSingle();
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
                    var p1 = new Vector2(p1x * _width, p1y * _height);
                    var p2 = new Vector2(p2x * _width, p2y * _height);
                    Line(p1.Point, p2.Point, r, g, b, false);
                }
            }
            return true;
        }

        private void Record(byte r, byte g, byte b, int p1x, int p1y, int p2x, int p2y)
        {
            _writer.Write(r);
            _writer.Write(g);
            _writer.Write(b);
            _writer.Write((float)p1x / _width);
            _writer.Write((float)p1y / _height);
            _writer.Write((float)p2x / _width);
            _writer.Write((float)p2y / _height);
        }

        public void Clear(byte r, byte g, byte b)
        {
            SDL.SDL_GetRendererOutputSize(_renderer, out _width, out _height);
            SDL.SDL_SetRenderDrawColor(_renderer, r, g, b, 255);
            SDL.SDL_RenderClear(_renderer);

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
            var draw = p1.X >= 0 && p1.X < _width && p1.Y >= 0 && p1.Y < _height ||
                       p2.X >= 0 && p2.X < _width && p2.Y >= 0 && p2.Y < _height;
            if (!draw)
            {
                var left = FarseerPhysics.Common.LineTools.LineIntersect(p1.Vector.Xna, p2.Vector.Xna,
                    new Microsoft.Xna.Framework.Vector2(0, 0), new Microsoft.Xna.Framework.Vector2(0, _height), true,
                    true, out var point);
                var right = FarseerPhysics.Common.LineTools.LineIntersect(p1.Vector.Xna, p2.Vector.Xna,
                    new Microsoft.Xna.Framework.Vector2(_width, 0), new Microsoft.Xna.Framework.Vector2(_width, _height),
                    true, true, out point);
                var up = FarseerPhysics.Common.LineTools.LineIntersect(p1.Vector.Xna, p2.Vector.Xna,
                    new Microsoft.Xna.Framework.Vector2(0, 0), new Microsoft.Xna.Framework.Vector2(_width, 0), true, true,
                    out point);
                var down = FarseerPhysics.Common.LineTools.LineIntersect(p1.Vector.Xna, p2.Vector.Xna,
                    new Microsoft.Xna.Framework.Vector2(0, _height), new Microsoft.Xna.Framework.Vector2(_width, _height),
                    true, true, out point);
                draw = left | right | up | down;
            }
            if (draw)
            {
                SDL.SDL_SetRenderDrawColor(_renderer, r, g, b, 255);
                SDL.SDL_RenderDrawLine(_renderer, p1.X, p1.Y, p2.X, p2.Y);
                if (record && Recording)
                {
                    Record(r, g, b, p1.X, p1.Y, p2.X, p2.Y);
                }
            }
        }

        public void Line(Point p1, Point p2, byte r, byte g, byte b) => Line(p1, p2, r, g, b, true);

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
            var length = _stream.Position;
            _stream.Position = 0;
            string filename;
            var fileIndex = 0;
            do
            {
                filename = name + (fileIndex == 0 ? "" : "_" + fileIndex) + ".srv3";
                fileIndex++;
            } while (File.Exists(filename));
            using (var fileStream = File.OpenWrite(filename))
            {
                _stream.CopyTo(fileStream);
            }
            _stream.Position = length;
            Console.WriteLine("Saved " + filename);
        }

        public void ResetVideo() => _stream.SetLength(0);
    }

    public class MovieWindow
    {
        private readonly IntPtr _window;
        private readonly IntPtr _renderer;
        private readonly Graphics _graphics;
        public MovieWindow(string filename)
        {
            _window = SDL.SDL_CreateWindow("Spacerunner 3", 300, 300, 1000, 800, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            if (_window == IntPtr.Zero)
            {
                (-1).CheckSdl();
            }
            _renderer = SDL.SDL_CreateRenderer(_window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            if (_renderer == IntPtr.Zero)
            {
                (-1).CheckSdl();
            }
            var reader = new BinaryReader(File.OpenRead(filename));
            _graphics = new Graphics(_renderer, reader);
        }

        public void Run()
        {
            var going = false;
            while (true)
            {
                while (SDL.SDL_PollEvent(out var evnt) != 0)
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
                    if (_graphics.DrawRecordedFrame())
                    {
                        break;
                    }
                }
                SDL.SDL_RenderPresent(_renderer);
            }
        }
    }

    public class DisplayWindow
    {
        private readonly IntPtr _window;
        private readonly IntPtr _renderer;
        private readonly Scene _scene;
        private readonly Graphics _graphics;
        private int _thing;
        private Stopwatch? _fps;
        private double _fpsCounter;
        private bool _paused;

        public DisplayWindow(Scene scene)
        {
            _window = SDL.SDL_CreateWindow("Spacerunner 3", 300, 300, 1000, 800, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            if (_window == IntPtr.Zero)
            {
                (-1).CheckSdl();
            }
            _renderer = SDL.SDL_CreateRenderer(_window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            if (_renderer == IntPtr.Zero)
            {
                (-1).CheckSdl();
            }
            _graphics = new Graphics(_renderer, null);
            _scene = scene;
        }

        private void SaveVideo()
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var score = _scene.Objects.OfType<DistanceTracker>().First().Describe();
            var name = now + "_" + score;
            _graphics.SaveVideo(name);
        }

        public void Run()
        {
            while (true)
            {
                if (SDL.SDL_PollEvent(out var evnt) != 0)
                {
                    if (evnt.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        break;
                    }
                    else if (evnt.type == SDL.SDL_EventType.SDL_KEYDOWN)
                    {
                        if (evnt.key.keysym.scancode == Settings.Grab.KeyReset)
                        {
                            Program.Reset(_scene);
                            _graphics.ResetVideo();
                        }

                        if (evnt.key.keysym.scancode == Settings.Grab.KeyPause)
                        {
                            _paused = !_paused;
                        }

                        if (evnt.key.keysym.scancode == Settings.Grab.KeySaveVideo)
                        {
                            SaveVideo();
                        }

                        _scene.PressedKeys.Add(evnt.key.keysym.scancode);
                    }
                    else if (evnt.type == SDL.SDL_EventType.SDL_KEYUP)
                    {
                        _scene.PressedKeys.Remove(evnt.key.keysym.scancode);
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
            SDL.SDL_GetRendererOutputSize(_renderer, out var width, out var height).CheckSdl();
            _scene.Camera.ScreenScale = new Vector2(width, height);
            if (_fps == null)
            {
                _fps = Stopwatch.StartNew();
            }
            else
            {
                var elapsed = _fps.Elapsed.TotalSeconds;
                _fps.Restart();
                _fpsCounter = (_fpsCounter * 20 + 1 / elapsed) / 21;
                if (!_paused)
                {
                    _scene.Update(elapsed);
                }
            }

            _graphics.Clear(40, 79, 79);

            var camera = _scene.Camera;
            foreach (var drawable in _scene.Drawables)
            {
                drawable.Draw(_graphics, camera);
            }

            const int wrap = 100;
            _graphics.Arc(new Vector2(width - 50, 50), 48, 0, (_thing = (_thing + 1) % wrap) * (2 * Math.PI / wrap), 255,
                140, 0);

            if (_thing == 0)
            {
                var score = _scene.Objects.OfType<DistanceTracker>().First().Describe();
                SDL.SDL_SetWindowTitle(_window, $"Spacerunner 3 - {_fpsCounter}fps{(_paused ? " - PAUSED" : "")} - {score}");
            }

            _graphics.MarkEndOfFrame();
            SDL.SDL_RenderPresent(_renderer);
        }
    }
}