using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;

namespace spywin
{
    class Photo
    {
        static FilterInfoCollection WebcamColl;
        static VideoCaptureDevice Device;
        static string fileName = "Image.jpg";
        static public String fullFileName = @"C:\Users\NataliaW\Pictures\" + fileName;

        public static void capture()
        {
            WebcamColl = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            Device = new VideoCaptureDevice(WebcamColl[0].MonikerString);
            Device.Start();
            Device.NewFrame += new NewFrameEventHandler(save);
        }

        static void save(object sender, NewFrameEventArgs eventArgs)
        {
            Image img = (Bitmap)eventArgs.Frame.Clone();

            img.Save(fullFileName);
            Device.SignalToStop();
        }
    }
}
