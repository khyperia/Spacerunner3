using System;
using System.Collections.Generic;
using System.Linq;
using SDL2;

namespace Spacerunner3
{
    public static class Util
    {
        public static Random rand = new Random();
        public static void CheckSdl(this int retval)
        {
            if (retval != 0)
            {
                throw new Exception("SDL call failure: returned (" + retval + "): " + SDL.SDL_GetError());
            }
        }

        public static Vector2 MyVec(this Microsoft.Xna.Framework.Vector2 vec) => new Vector2(vec.X, vec.Y);

        public static IEnumerable<double> RoundRange(double start, double end, int count)
        {
            var step = (end - start) / count;
            var stepFixed = Math.Pow(2, Math.Round(Math.Log(step, 2)));
            //Console.WriteLine(stepFixed + " " + Math.Round(Math.Log(step, 2)) + " " + Math.Log(step, 2) + " " + step);
            var startFixed = Math.Floor(start / stepFixed) * stepFixed;
            return Enumerable.Range(0, int.MaxValue).Select(i => stepFixed * i + startFixed).TakeWhile(v => v < end);
        }
    }
}
