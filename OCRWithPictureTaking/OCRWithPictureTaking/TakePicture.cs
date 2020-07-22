using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace OCRWithPictureTaking
{
    class TakePicture
    {
        public TakePicture()
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap((int)Math.Round(0.28125*bounds.Width), (int)Math.Round(0.037037037*bounds.Height)))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(new Point((int)Math.Round(0.359375*bounds.Width), (int)Math.Round(0.0648148*bounds.Height)), Point.Empty, bounds.Size);
                }
                bitmap.Save("depth.png", ImageFormat.Png);
            }
        }
    }
}
