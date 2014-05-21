using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using Microsoft.Kinect;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.Configuration;

namespace KinectWPFOpenCV
{
    /// <summary>
    /// Interaction logic for Cage1.xaml
    /// </summary>
    public partial class Cage4 : Window
    {
        KinectSensor sensor;
        WriteableBitmap colorBitmap;
        DepthImagePixel[] depthPixels;
        byte[] colorPixels;
        public Cage4()
        {
            InitializeComponent();
            this.Loaded += Cage4_Loaded;
            //this.cimg_cage1.Source = this.colorBitmap;
        }
        void Cage4_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }
            if (null != this.sensor)
            {

                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.cimg_cage4.Source = this.colorBitmap;
                this.sensor.AllFramesReady += this.sensor_AllFramesReady;
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    //this.sensor = null;
                }


            }
        }
        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyPixelDataTo(this.colorPixels);
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                    int sx = (int)this.sld_c1_sX.Value;
                    int sy = (int)this.sld_c1_sY.Value;
                    int dx = (int)this.sld_c1_dX.Value;
                    int dy = (int)this.sld_c1_dY.Value;
                    int w = 0;
                    int h = 0;
                    if (dx >= sx)
                        w = (dx - sx);
                    if (dy >= sy)
                        h = (dy - sy);
                    float cx = (float)sx + ((float)w) / 2;
                    float cy = (float)sy + ((float)h) / 2;
                    Image<Bgr, Byte> openCVImg = new Image<Bgr, byte>(colorBitmap.ToBitmap());
                    MCvBox2D box = new MCvBox2D(new PointF(cx, cy), new SizeF(new PointF((float)w, (float)h)), 0);
                    openCVImg.Draw(box, new Bgr(System.Drawing.Color.Green), 4);
                    this.cimg_cage4.Source = ImageHelpers.ToBitmapSource(openCVImg);
                    //Dimensions of the cage known at this point 
                    //TODO write to config file
                }
            }
        }
    }
}
