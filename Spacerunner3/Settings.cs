using System;
using System.IO;
using System.Xml.Serialization;

namespace Spacerunner3
{
    [Serializable]
    public class Settings
    {
        private static XmlSerializer serializer = new XmlSerializer(typeof(Settings));
        public static Settings Grab;

        public System.Windows.Forms.Keys KeyThrust;
        public System.Windows.Forms.Keys KeyTurnLeft;
        public System.Windows.Forms.Keys KeyTurnRight;
        public System.Windows.Forms.Keys KeyPause;
        public System.Windows.Forms.Keys KeyReset;
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
            KeyThrust = System.Windows.Forms.Keys.W;
            KeyTurnLeft = System.Windows.Forms.Keys.A;
            KeyTurnRight = System.Windows.Forms.Keys.D;
            KeyPause = System.Windows.Forms.Keys.Escape;
            KeyReset = System.Windows.Forms.Keys.Space;
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
            using (var stream = File.OpenRead(file))
                return (Settings)serializer.Deserialize(stream);
        }

        public void Save(string file)
        {
            using (var stream = File.OpenWrite(file))
                serializer.Serialize(stream, this);
        }
    }
}
