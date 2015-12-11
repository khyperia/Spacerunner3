using System;
using System.IO;
using System.Linq;
using SDL2;

namespace Spacerunner3
{
    [Serializable]
    public class Settings
    {
        public static Settings Grab;

        public SDL.SDL_Scancode KeyThrust;
        public SDL.SDL_Scancode KeyTurnLeft;
        public SDL.SDL_Scancode KeyTurnRight;
        public SDL.SDL_Scancode KeyPause;
        public SDL.SDL_Scancode KeyReset;
        public bool UseJoystick;
        public int JoystickAxisX;
        public int JoystickAxisY;
        public bool JoystickInvertX;
        public bool JoystickInvertY;
        public double ScreenSize;
        public double AsteroidRadius;
        public double AsteroidSpacing;
        public double AsteroidSizeVariety;
        public float AsteroidInitialVel;
        public float AsteroidInitialRot;
        public int AsteroidMinVerts;
        public int AsteroidMaxVerts;
        public float ObjectRestitution;
        public float ShipSize;
        public float ShipShapeAngle;
        public float ShipHealth;
        public float ShipAngularDamping;
        public float ShipThrust;
        public float ShipTorque;
        public float FuturePrediction;

        public Settings()
        {
            KeyThrust = SDL.SDL_Scancode.SDL_SCANCODE_W;
            KeyTurnLeft = SDL.SDL_Scancode.SDL_SCANCODE_A;
            KeyTurnRight = SDL.SDL_Scancode.SDL_SCANCODE_D;
            KeyPause = SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE;
            KeyReset = SDL.SDL_Scancode.SDL_SCANCODE_SPACE;
            UseJoystick = false;
            JoystickAxisX = 0;
            JoystickAxisY = 1;
            JoystickInvertX = false;
            JoystickInvertY = true;
            ScreenSize = 150;
            AsteroidRadius = 20;
            AsteroidSpacing = 3;
            AsteroidSizeVariety = 0.5;
            AsteroidInitialVel = 0;
            AsteroidInitialRot = 0;
            AsteroidMinVerts = 4;
            AsteroidMaxVerts = 8;
            ObjectRestitution = 0.2f;
            ShipSize = 2;
            ShipShapeAngle = 0.5f;
            ShipAngularDamping = 8;
            ShipHealth = 20000.0f;
            ShipThrust = 75.0f;
            ShipTorque = 120.0f;
            FuturePrediction = 0.0f;
        }

        public static Settings Load(string file)
        {
            var settings = new Settings();
            var type = settings.GetType();
            foreach (var line_ in File.ReadAllLines(file))
            {
                var line = line_.Trim();
                if (string.IsNullOrEmpty(line) || line[0] == '#')
                    continue;
                var equal = line.IndexOf('=');
                if (equal == -1)
                {
                    Console.WriteLine("Invalid config line:\n{0}\n - Ignoring", line);
                    continue;
                }
                var key = line.Substring(0, equal).Trim();
                var value = line.Substring(equal + 1).Trim();
                var fld = type.GetField(key);
                if (fld == null)
                {
                    Console.WriteLine("Unknown config line:\n{0}\n - Ignoring", line);
                    continue;
                }
                bool bvalue;
                int ivalue;
                float fvalue;
                double dvalue;
                SDL.SDL_Scancode svalue;
                if (fld.FieldType == typeof(bool) && bool.TryParse(value, out bvalue))
                {
                    fld.SetValue(settings, bvalue);
                }
                else if (fld.FieldType == typeof(int) && int.TryParse(value, out ivalue))
                {
                    fld.SetValue(settings, ivalue);
                }
                else if (fld.FieldType == typeof(float) && float.TryParse(value, out fvalue))
                {
                    fld.SetValue(settings, fvalue);
                }
                else if (fld.FieldType == typeof(double) && double.TryParse(value, out dvalue))
                {
                    fld.SetValue(settings, dvalue);
                }
                else if (fld.FieldType == typeof(SDL.SDL_Scancode) && (svalue = SDL.SDL_GetScancodeFromName(value)) != SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN)
                {
                    fld.SetValue(settings, svalue);
                }
                else
                {
                    Console.WriteLine("Bad value config line:\n{0}\n - Ignoring", line);
                }
            }
            return settings;
        }

        private string FieldToString(System.Reflection.FieldInfo f)
        {
            string value;
            if (f.FieldType == typeof(SDL.SDL_Scancode))
            {
                value = SDL.SDL_GetScancodeName((SDL.SDL_Scancode)f.GetValue(this));
            }
            else
            {
                value = f.GetValue(this).ToString();
            }
            return f.Name + " = " + value;
        }

        public void Save(string file)
        {
            var contents = this.GetType().GetFields().Where(f => f.Name != "Grab").Select(FieldToString);
            File.WriteAllLines(file, contents);
        }
    }
}
