using System;

namespace Spacerunner3
{
    public static class JoystickManager
    {
        private static IntPtr _joystick = IntPtr.Zero;

        static JoystickManager()
        {
            if (SDL2.SDL.SDL_Init(SDL2.SDL.SDL_INIT_JOYSTICK) != 0)
            {
                throw new Exception("Couldn't init SDL_INIT_JOYSTICK");
            }
        }

        internal static Vector2 GetJoystick()
        {
            if (_joystick == IntPtr.Zero)
            {
                for (var i = 0; i < SDL2.SDL.SDL_NumJoysticks(); i++)
                {
                    _joystick = SDL2.SDL.SDL_JoystickOpen(i);
                    if (_joystick != IntPtr.Zero)
                    {
                        break;
                    }
                    Console.WriteLine("Attempted to open joystick " + i + " and failed");
                }
            }
            if (_joystick == IntPtr.Zero)
            {
                throw new Exception("Joystick not found");
            }
            SDL2.SDL.SDL_JoystickUpdate();
            var x = SDL2.SDL.SDL_JoystickGetAxis(_joystick, Settings.Grab.JoystickAxisX);
            var y = SDL2.SDL.SDL_JoystickGetAxis(_joystick, Settings.Grab.JoystickAxisY);
            var fx = (float)x / short.MaxValue * (Settings.Grab.JoystickInvertX ? -1 : 1);
            var fy = (float)y / short.MaxValue * (Settings.Grab.JoystickInvertY ? -1 : 1);
            return new Vector2(fx, fy);
        }
    }
}
