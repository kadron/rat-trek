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
using OfficeOpenXml;
using System.IO;
using System.Configuration;
using OfficeOpenXml;

namespace KinectWPFOpenCV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool rec = false;
        bool running = false;
        int c = 0;
        int c2 = 0;
        bool start = false;
        String recordLoc = "C:\\KinectVid\\test.avi";
        String excelLoc = "C:\\KinectVid\\test.xlsx";

        List<BackgroundWorker> workerList;
        ExcelPackage pck;
        ExcelWorksheet wsheet;



        KinectSensor sensor;

        WriteableBitmap depthBitmap;
        WriteableBitmap colorBitmap;
        DepthImagePixel[] depthPixels;
        VideoWriter vw;
        byte[] colorPixels;

        int blobCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            InitializeWorkers();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            this.MouseDown += MainWindow_MouseDown;

        }

        void InitializeWorkers(){
            workerList = new List<BackgroundWorker>();
            for(int i=0; i<100; ++i){
                BackgroundWorker x = new BackgroundWorker();
                x.DoWork += new DoWorkEventHandler(bg_DoWork);
                workerList.Add(x);
            }
        }

        BackgroundWorker getNext()
        {
            BackgroundWorker x = workerList.ElementAt(c%100);
            c++;
            return x;
        }
      
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
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
                //Depth Stream allows 320x240 resolution as well.
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);                
                this.colorImg.Source = this.colorBitmap;
                vw = new VideoWriter(recordLoc, 30, this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, true);
                pck = new ExcelPackage(new FileInfo(excelLoc));
                wsheet = pck.Workbook.Worksheets.Add("Mice Data");
                this.sensor.AllFramesReady += this.sensor_AllFramesReady;

                try
                {
                    this.sensor.Start();
                }
                    catch (IOException)
                {
                    this.sensor = null;
                }

                
            }
            if (null == this.sensor)
            {
                this.outputViewbox.Visibility = System.Windows.Visibility.Collapsed;
                this.txtError.Visibility = System.Windows.Visibility.Visible;
                this.txtInfo.Text = "No Kinect Found";
                
            }

        }

        private void bg_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (rec)
            {
                try {
                    vw.WriteFrame((Image<Bgr, Byte>)e.Argument);
                }
                catch (InvalidOperationException exp) {
                    //Do nothing
                    //This exception happens when bg workers try to save 
                    //the frame while the program is closed and video saved
                }
                
            }
           
            e.Result = 0;
        }

        /*
        private int saveImage(Image<Bgr, Byte> args, BackgroundWorker worker)
        {
            args.ToBitmap().Save("C:\\Users\\Burak\\Desktop\\KinectVid\\" + c2 + ".png");

            return 0;
        }
        */

        private void bg_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            //TODO Keep the previous frame image as well,
            //Compare both on a background process and save it to the worksheet
            //Convert x&y differences to millimeters according to depth data (distance)
            //and some trigonometry
            BitmapSource depthBmp = null;
            blobCount = 0;

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    if (depthFrame != null)
                    {

                        blobCount = 0;

                        depthBmp = depthFrame.SliceDepthImage((int)sliderMin.Value, (int)sliderMax.Value);
                        
                        Image<Bgr, Byte> openCVImg = new Image<Bgr, byte>(depthBmp.ToBitmap());
                        Image<Gray, byte> gray_image = openCVImg.Convert<Gray, byte>();

                        if (running)
                        {
                            using (MemStorage stor = new MemStorage())
                            {
                                //Find contours with no holes try CV_RETR_EXTERNAL to find holes
                                Contour<System.Drawing.Point> contours = gray_image.FindContours(
                                 Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                                 Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_EXTERNAL,
                                 stor);

                                for (int i = 0; contours != null; contours = contours.HNext)
                                {
                                    i++;

                                    if ((contours.Area > Math.Pow(sliderMinSize.Value, 2)) && (contours.Area < Math.Pow(sliderMaxSize.Value, 2)))
                                    {
                                        MCvBox2D box = contours.GetMinAreaRect();
                                        //DrQ RED BOX AROUND BLOB   
                                        openCVImg.Draw(box, new Bgr(System.Drawing.Color.Red), 2);
                                        blobCount++;
                                    }
                                }
                            } 
                        }

                        this.outImg.Source = ImageHelpers.ToBitmapSource(openCVImg);                        
                        txtBlobCount.Text = blobCount.ToString();

                        getNext().RunWorkerAsync(openCVImg);
                    }
                }


                if (colorFrame != null)
                {
                    
                      colorFrame.CopyPixelDataTo(this.colorPixels);
                      this.colorBitmap.WritePixels(
                          new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                          this.colorPixels,
                          this.colorBitmap.PixelWidth * sizeof(int),
                          0);
                    
                }
            }
        }


        #region Window Stuff
        void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }


        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e)
        {
            //TODO Do you want to exit prompt
            rec = false;
            /*
            if (pck != null)
            {
                pck.Save();
                //System.Diagnostics.Process.Start(excelLoc);
            }*/

            if (!this.vw.Equals(null))
            {
                this.vw.Dispose();    
            } 
            this.Close();
        }
        #endregion

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            CheckBox chk = (CheckBox)sender;

            if ((bool)chk.IsChecked)
                rec=true;
            else
                rec=false;
        }

        private void btn_startStop_Click(object sender, RoutedEventArgs e)
        {
            Button startStop = sender as Button;
            if ((String)startStop.Content == "Start")
            {
                this.btn_startStop.Content = "Stop";
                running = true;
            }
            else if ((String)startStop.Content == "Stop")
            {
                this.btn_startStop.Content = "Start";
                running = false;
            }
        }

        private void btn_New_Click(object sender, RoutedEventArgs e)
        {
            //Launch experiment setup initial page
            NewExp newExp = new NewExp();
            newExp.ShowDialog();
        }
    }
}
