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
    /// Interaction logic for NewExp.xaml
    /// </summary>
    public partial class NewExp : Window
    {
        String expName = "";
        String folderPath = "";
        public NewExp()
        {
            InitializeComponent();
            this.btn_toNew2.IsEnabled = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.btn_toNew2.IsEnabled = true;
                folderPath = fbd.SelectedPath.Replace(@"\", @"\\");
                this.fPath.Text = folderPath;
            }
        }

        private void eName_TextChanged(object sender, TextChangedEventArgs e)
        {
            expName = eName.Text;
        }

        private void btn_toNew2_Click(object sender, RoutedEventArgs e)
        {
            //Folder path and experiment name configs gathered at this point
            //TODO Write to config file
            folderPath += @"\\" + expName + @"\\";
            NewExp2 newExp2 = new NewExp2();
            this.Close();
            newExp2.ShowDialog();
        }
    }
}
