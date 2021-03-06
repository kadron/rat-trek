﻿using System;
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
        int frameCount = 1;
        DepthImageFormat depthFormat = DepthImageFormat.Resolution640x480Fps30;
        ColorImageFormat colorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        List<BackgroundWorker> workerList;
        ExcelPackage pck;
        ExcelWorksheet wsheet;

        KinectSensor sensor;
        CoordinateMapper mapper;
        WriteableBitmap depthBitmap;
        WriteableBitmap colorBitmap;
        DepthImagePixel[] depthPixels;
        SkeletonPoint[] skeletonPoints;
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

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C mkdir C:\\KinectVid";
            process.StartInfo = startInfo;
            process.Start();

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
                this.sensor.DepthStream.Enable(depthFormat);
                this.sensor.ColorStream.Enable(colorFormat);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);                
                this.colorImg.Source = this.colorBitmap;
                this.mapper = new CoordinateMapper(sensor);
                this.skeletonPoints = new SkeletonPoint[307200];

                vw = new VideoWriter(recordLoc, 30, this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, true);
                FileInfo newFile = new FileInfo(excelLoc);

                if (newFile.Exists)
                {
                    newFile.Delete();
                    newFile = new FileInfo(excelLoc);
                }

                pck = new ExcelPackage(newFile);
                wsheet = pck.Workbook.Worksheets.Add("Rat Data");

                wsheet.Cells[1, 1].Value = "Coord (m)";
                for (int i = 1; i <= 4; i++)
                {
                    wsheet.Cells[3 * i - 1, 1].Value = "Rat " + i + " X";
                    wsheet.Cells[3 * i, 1].Value = "Rat " + i + " Y";
                    wsheet.Cells[3 * i + 1, 1].Value = "Rat " + i + " Z";
                }

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
                            wsheet.Cells[1, frameCount + 1].Value = "Frame " + frameCount;
                            frameCount++;
                            using (MemStorage stor = new MemStorage())
                            {
                                //Find contours with no holes try CV_RETR_EXTERNAL to find holes
                                Contour<System.Drawing.Point> contours = gray_image.FindContours(
                                 Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                                 Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_EXTERNAL,
                                 stor);
                                
                                //Conversion of depthPixels to skeletonPoints which contain all three dimensions in meters.
                                //The conversion and copying is assumed to be costly but there are no single pixel to single point conversion I could find.
                                depthFrame.CopyDepthImagePixelDataTo(depthPixels);
                                //mapper.MapDepthFrameToSkeletonFrame(depthFormat, depthPixels, skeletonPoints);

                                for (int i = 0; contours != null; contours = contours.HNext)
                                {
                                    i++;

                                    if ((contours.Area > Math.Pow(sliderMinSize.Value, 2)) && (contours.Area < Math.Pow(sliderMaxSize.Value, 2)))
                                    {
                                        MCvBox2D box = contours.GetMinAreaRect();
                                        //DrQ RED BOX AROUND BLOB   
                                        openCVImg.Draw(box, new Bgr(System.Drawing.Color.Red), 2);
                                        blobCount++;
                                        int x = (int) box.center.X;
                                        int y = (int) box.center.Y;
                                        DepthImagePoint p = new DepthImagePoint();
                                        p.X = x;
                                        p.Y = y;
                                        p.Depth = depthPixels[x + 640 * y].Depth;
                                        SkeletonPoint s = mapper.MapDepthPointToSkeletonPoint(depthFormat, p);

                                        //TODO Conversion from absolute coordinates to relative coordinates
                                        
                                        addCoordData(3 * blobCount - 1, frameCount, s.X, s.Y, s.Z);
                                        /*if (KinectSensor.IsKnownPoint(s))
                                        {
                                            addCoordData(3 * blobCount - 1, frameCount, s.X, s.Y, s.Z);
                                        }*/
                                        
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

        void addCoordData(int r, int c, float x, float y, float z)
        {
            wsheet.Cells[r, c].Value = x;
            wsheet.Cells[r + 1, c].Value = y;
            wsheet.Cells[r + 2, c].Value = z;
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
            
            if (pck != null)
            {
                pck.Save();
                //System.Diagnostics.Process.Start(excelLoc);
            }

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
