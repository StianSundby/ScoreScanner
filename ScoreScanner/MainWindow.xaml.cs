using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using static ScoreScanner.Services.FileProcessor;
using static ScoreScanner.Services.ImageProcessor;
using System.Drawing;
using System.Windows.Input;

namespace ScoreScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Init();
            AppConsole.Text += "Loading...\n";
        }

        // 2305    442   220    240
        //C:\Users\Stian\.runelite\screenshots\Baktus Bror\Boss Kills

        //TODO: Hvis den ikke finner size så er det en solo
        //TODO: Hør med christian ang. filnavn
        //TODO: Async ChangeCellValue?


        private void Init()
        {
            AppConsole.Text += "Initializing Score Scanner...\nReading settings...\n";
            var settings = Properties.Settings.Default;
            ImageFolderInput.Text = settings.ImageFolderLocation;
            ExcelSheetInput.Text = settings.ExcelSheetLocation;
            RectXInput.Text = settings.RectX == 0 ? null : settings.RectX.ToString();
            RectYInput.Text = settings.RectY == 0 ? null : settings.RectY.ToString();
            RectHeightInput.Text = settings.RectHeight == 0 ? null : settings.RectHeight.ToString();
            RectWidthInput.Text = settings.RectWidth == 0 ? null : settings.RectWidth.ToString();
            AppConsole.Text += "Finished reading settings.\n";


        }

        private void Start(object sender, RoutedEventArgs e)
        {
            if (!CheckInputFields()) return;
            CheckAppSettings();

            var lastKcExcelRow = FindNextEmptyCell(ExcelSheetInput.Text, 1);
            var lastListedKcRowNum = int.Parse(lastKcExcelRow[(lastKcExcelRow.IndexOf("B") + 1)..]) - 1;

            var lastKc = FindLastCoXKill(ImageFolderInput.Text);
            var lastKcNum = int.Parse(lastKc.Substring(lastKc.IndexOf("(") + 1, lastKc.IndexOf(")") - lastKc.IndexOf("(") - 1));

            if (lastListedKcRowNum - lastKcNum != 0)
            {
                for (int i = lastListedKcRowNum; i < lastKcNum; i++)
                {
                    var imgToScan = FindNextCoXKill(ImageFolderInput.Text, i);
                    var scanResult = ScanImage(imgToScan, int.Parse(RectXInput.Text), int.Parse(RectYInput.Text),
                        int.Parse(RectHeightInput.Text), int.Parse(RectWidthInput.Text));
                    var values = ShrinkScanResult(scanResult);

                    ChangeCellValue(ExcelSheetInput.Text, new string("B" + (i + 1)), values[0]);
                    ChangeCellValue(ExcelSheetInput.Text, new string("E" + (i + 1)), values[1]);
                }
            }
        }


        private bool CheckInputFields()
        {
            AppConsole.Text += "Checking input fields...\n";
            var digits = new Regex("^[0-9]*$");
            if (string.IsNullOrEmpty(ImageFolderInput.Text))
            {
                MessageBox.Show("Image folder path cannot be empty.");
                AppConsole.Text += "ImageFolderInput error...\n";
                return false;
            }
            if (string.IsNullOrEmpty(ExcelSheetInput.Text))
            {
                MessageBox.Show("Excel sheet path cannot be empty.");
                AppConsole.Text += "ExcelSheetInput error...\n";
                return false;
            }
            if (string.IsNullOrEmpty(RectXInput.Text) || !digits.IsMatch(RectXInput.Text))
            {
                MessageBox.Show("Rectangle X coordinate cannot be empty");
                AppConsole.Text += "RectXInput error...\n";
                return false;
            }
            if (string.IsNullOrEmpty(RectYInput.Text) || !digits.IsMatch(RectYInput.Text))
            {
                MessageBox.Show("Rectangle Y coordinate cannot be empty");
                AppConsole.Text += "RectYInput error...\n";
                return false;
            }
            if (string.IsNullOrEmpty(RectWidthInput.Text) || !digits.IsMatch(RectWidthInput.Text))
            {
                MessageBox.Show("Rectangle width coordinate cannot be empty");
                AppConsole.Text += "RectWidthInput error...\n";
                return false;
            }
            if (string.IsNullOrEmpty(RectHeightInput.Text) || !digits.IsMatch(RectHeightInput.Text))
            {
                AppConsole.Text += "RectHeightInput error...\n";
                MessageBox.Show("Rectangle height coordinate cannot be empty");
                return false;
            }
            AppConsole.Text += "Finished checking input fields. OK!\n";
            return true;
        }

        private void CheckAppSettings()
        {
            if (Properties.Settings.Default.FirstTimeSetup) return;

            AppConsole.Text += "Reading stored variables...\n";
            var settings = Properties.Settings.Default;

            if (string.IsNullOrEmpty(settings.ImageFolderLocation))
                settings.ImageFolderLocation = ImageFolderInput.Text;

            if (string.IsNullOrEmpty(settings.ExcelSheetLocation))
                settings.ExcelSheetLocation = ExcelSheetInput.Text;

            if (string.IsNullOrEmpty(settings.RectX.ToString()) 
                || settings.RectX == 0)
                    settings.RectX = int.Parse(RectXInput.Text);

            if (string.IsNullOrEmpty(settings.RectY.ToString()) 
                || settings.RectY == 0)
                    settings.RectY = int.Parse(RectYInput.Text);

            if (string.IsNullOrEmpty(settings.RectWidth.ToString()) 
                || settings.RectHeight == 0)
                    settings.RectWidth = int.Parse(RectWidthInput.Text);

            if (string.IsNullOrEmpty(settings.RectHeight.ToString()) 
                || settings.RectWidth == 0)
                    settings.RectHeight = int.Parse(RectHeightInput.Text);

            Properties.Settings.Default.FirstTimeSetup = true;
            AppConsole.Text += "Finished reading stored variables.\n";
        }

        public void ImageFolderBrowser(object sender, RoutedEventArgs e)
        {
            AppConsole.Text += "Waiting for user input...\n";
            string selectedFolder = string.Empty;
            using var fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result.ToString().Equals("OK") && 
                !string.IsNullOrWhiteSpace(fbd.SelectedPath)) 
                    selectedFolder = fbd.SelectedPath;
            ImageFolderInput.Text = selectedFolder;
            AppConsole.Text += "ImageFolder set.\n";
        }

        private void ExcelSheetFolderBrowser(object sender, RoutedEventArgs e)
        {
            AppConsole.Text += "Waiting for user input...\n";
            string selectedFile;
            OpenFileDialog fd = new()
            {
                Filter = "All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = true
            };

            if (fd.ShowDialog() == true) selectedFile = fd.FileName;
            else selectedFile = string.Empty;
            ExcelSheetInput.Text = selectedFile;
            AppConsole.Text += "Excel Sheet set.\n";
        }

        private void GridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            AppConsole.Text += "Exiting application with code 0...\n";
            Environment.Exit(0);
        }

        private void ImageCropChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(RectXInput.Text) &&
                !string.IsNullOrEmpty(RectYInput.Text) &&
                !string.IsNullOrEmpty(RectHeightInput.Text) &&
                !string.IsNullOrEmpty(RectWidthInput.Text))
            {
                AppConsole.Text += "Refreshing crop preview...\n";
                var imgPath = FindLastCoXKill(ImageFolderInput.Text);

                var imgPreview = Crop(
                    new Bitmap(imgPath),
                    new Rectangle(int.Parse(RectXInput.Text),
                                  int.Parse(RectYInput.Text),
                                  int.Parse(RectWidthInput.Text),
                                  int.Parse(RectHeightInput.Text)));
                var previewBmp = new Bitmap(imgPreview);
                CropPreview.Source = BitmapToBitmapSource(previewBmp);
            }
            else return;
        }
    }
}
