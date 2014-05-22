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
using System.Windows.Forms;
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
    /// Interaction logic for wTime.xaml
    /// </summary>
    public partial class wTime : Window
    {
        String folderPath = "";
        int h=0;
        int m=0;
        int s=0;
        bool h_isNumeric = false;
        bool m_isNumeric = false;
        bool s_isNumeric = false;
        public wTime()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                folderPath = fbd.SelectedPath.Replace(@"\", @"\\");
                this.fPath.Text = folderPath;
            }
        }

        private void btn_Done_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void tHour_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.tHour.Text !=null)
            {
                h_isNumeric = int.TryParse(this.tHour.Text, out h);
                enableDone(); 
            }
        }

        private void tMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.tHour.Text != null)
            {
                m_isNumeric = int.TryParse(this.tMin.Text, out m);
                enableDone();
            }
        }

        private void tSec_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.tHour.Text != null)
            {
                s_isNumeric = int.TryParse(this.tSec.Text, out s);
                enableDone();
            }
        }

        private void enableDone()
        {
            if (h_isNumeric && m_isNumeric && s_isNumeric)
            {
                //this.btn_Done.IsEnabled = true;
            }
            else
            {
                //this.btn_Done.IsEnabled = false;
            }
        }
    }
}
