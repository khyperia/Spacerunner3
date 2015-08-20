using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Spacerunner3
{
    public class DisplayWindow
    {
        private readonly Form form;
        private readonly Scene scene;
        private int thing;
        private Stopwatch fps;
        private double fpsCounter;
        private bool paused;

        private class MyForm : Form
        {
            public MyForm()
            {
                DoubleBuffered = true;
            }
        }

        public DisplayWindow(Scene scene)
        {
            this.scene = scene;
            form = new MyForm();
            form.Text = "Spacerunner 3";
            form.ClientSize = new Size(1000, 800);
            form.Paint += FormPaint;
            form.KeyDown += (o, e) =>
            {
                if (e.KeyCode == Settings.Grab.KeyReset)
                    Program.Reset(scene);
                if (e.KeyCode == Settings.Grab.KeyPause)
                    paused = !paused;
                scene.PressedKeys.Add(e.KeyCode);
            };
            form.KeyUp += (o, e) => scene.PressedKeys.Remove(e.KeyCode);
        }

        public void Run()
        {
            Application.Run(form);
        }

        private void FormPaint(object sender, PaintEventArgs e)
        {
            scene.Camera.ScreenScale = form.ClientSize;
            if (fps == null)
                fps = Stopwatch.StartNew();
            else
            {
                var elapsed = fps.Elapsed.TotalSeconds;
                var timeToWait = 0.01 - elapsed;
                if (timeToWait > 0)
                {
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(timeToWait));
                    elapsed = fps.Elapsed.TotalSeconds;
                }
                fps.Restart();
                fpsCounter = (fpsCounter * 20 + 1 / elapsed) / 21;
                if (!paused)
                    scene.Update(elapsed);
            }

            var graphics = e.Graphics;
            graphics.Clear(Color.DarkSlateGray);

            var camera = scene.Camera;
            foreach (var drawable in scene.Drawables)
            {
                drawable.Draw(graphics, camera);
            }

            graphics.DrawArc(Pens.DarkOrange, form.ClientSize.Width - 98, 2, 96, 96, 0, thing = (thing + 1) % 360);
            var str = (int)fpsCounter + "fps";
            var measure = graphics.MeasureString(str, Util.font);
            graphics.DrawString(str, Util.font, Brushes.DarkOrange, new PointF(form.ClientSize.Width - 50 - measure.Width / 2, 50 - measure.Height / 2));

            if (paused)
            {
                str = "PAUSED";
                measure = graphics.MeasureString(str, Util.font);
                graphics.DrawString(str, Util.font, Brushes.Red, new PointF(form.ClientSize.Width / 2 - measure.Width / 2, form.ClientSize.Height / 2 - measure.Height / 2));
            }

            form.Invalidate();
        }
    }
}
