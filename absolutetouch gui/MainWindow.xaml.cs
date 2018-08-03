using System;
using System.IO;
using System.Collections.Generic;
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
using System.Windows.Forms;
using System.Threading;

namespace absolutetouch_gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Defaults();
            DefaultSettingsCheck();
            UpdateUseableOptions();
        }

        #region Public Variables

        public string installLocation;
        public string touchpadRegion;
        public string screenRegion;
        public bool touchpadDisable;
        public decimal weight;
        public bool touchDebug;
        public string programArguments;
        public System.Diagnostics.Process absoluteTouchProcess;
        public string settingsLocation;
        // resolutions
        public double screenWidth = Screen.PrimaryScreen.Bounds.Width;
        public double screenHeight = Screen.PrimaryScreen.Bounds.Height;
        public double touchpadWidth = 4684;
        public double touchpadHeight = 3840;

        #endregion

        #region Keyboard Shortcuts

        public void RunShortcut(Object sender, ExecutedRoutedEventArgs e) => RunAbsoluteTouch();

        #endregion

        #region Main Methods

        public void RunAbsoluteTouch()
        {
            //AbsoluteTouch.exe [arguments]
            //-t x1,y1,x2,y2 | Sets the mapped touchpad region
            //-s x1,y1,x2,y2 | Sets the mapped screen region
            //-m             | Enables the touchpad on start, disables it on exit
            //-w weight      | weight Sets the touch smoothing weight factor(0 to 1, default 0)
            //-d             | Enables debug mode(may reduce performance)
            //-c             | Enables touchscreen-like clicking
            
            CollectInformation();
            try
            {
                absoluteTouchProcess.Start();
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Windows.Forms.MessageBox.Show("Error: invalid program executable.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch(System.NullReferenceException)
            {
                return;
            }
            catch(System.InvalidOperationException)
            {
                System.Windows.Forms.MessageBox.Show("Error: Cannot start process because an executable has not been provided.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetupTab.IsSelected = true;
                FindInstallLocation();
            }
        }

        private void CollectInformation()
        {
            try
            {
                // get offsets and put into arguments
                double touchpadX1Offset = double.Parse(touchpadX1.Text) + TouchpadXOffset.Value;
                double touchpadY1Offset = double.Parse(touchpadY1.Text) + TouchpadYOffset.Value;
                double touchpadX2Offset = double.Parse(touchpadX2.Text) + TouchpadXOffset.Value;
                double touchpadY2Offset = double.Parse(touchpadY2.Text) + TouchpadYOffset.Value;
                double screenX1Offset = double.Parse(screenX1.Text) + ScreenXOffset.Value;
                double screenY1Offset = double.Parse(screenY1.Text) + ScreenYOffset.Value;
                double screenX2Offset = double.Parse(screenX2.Text) + ScreenXOffset.Value;
                double screenY2Offset = double.Parse(screenY2.Text) + ScreenYOffset.Value;
                // get toggle arguments
                string otherArguments = String.Empty;
                if (EnableClick.IsChecked == true)
                {
                    otherArguments = "-c";
                }
                if (DisableOnExit.IsChecked == true)
                {
                    otherArguments = $"{otherArguments} -m";
                }
                // set up arguments
                programArguments = $"-s {screenX1Offset},{screenY1Offset},{screenX2Offset},{screenY2Offset} -t {touchpadX1Offset},{touchpadY1Offset},{touchpadX2Offset},{touchpadY2Offset} -w {weight} {otherArguments}";
                // set info
                absoluteTouchProcess = new System.Diagnostics.Process();
                absoluteTouchProcess.StartInfo.FileName = installLocation;
                absoluteTouchProcess.StartInfo.Arguments = programArguments;
            }
            catch (System.FormatException)
            {
                System.Windows.Forms.MessageBox.Show("Error: Values were not in the correct format", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Defaults();
            }
            return;
        }

        private void Defaults()
        {
            // Update Textboxes
            screenX1.Text = "0";
            screenY1.Text = "0";
            screenX2.Text = $"{screenWidth}";
            screenY2.Text = $"{screenHeight}";

            touchpadX1.Text = "0";
            touchpadY1.Text = "0";
            touchpadX2.Text = "6143";
            touchpadY2.Text = "6143";

            WeightTextbox.Text = "0";

            ScreenXOffsetTextbox.Text = "0";
            ScreenYOffsetTextbox.Text = "0";
            TouchpadXOffsetTextbox.Text = "0";
            TouchpadYOffsetTextbox.Text = "0";

            // Set max offsets
            ScreenXOffset.Maximum = screenWidth;
            ScreenYOffset.Maximum = screenHeight;
            TouchpadXOffset.Maximum = touchpadWidth;
            TouchpadYOffset.Maximum = touchpadHeight;

            // Set large change maximums
            ScreenXOffset.LargeChange = screenWidth / 10;
            ScreenYOffset.LargeChange = screenHeight / 10;
            TouchpadXOffset.LargeChange = touchpadWidth / 10;
            TouchpadYOffset.LargeChange = touchpadHeight / 10;

            // Automatically adjust to Aspect Ratio
            if (LockAspectRatio.IsChecked == true)
            {
                TouchpadAspectRatio();
            }

            // Reset checkboxes
            UseOffset.IsChecked = false;
            LockAspectRatio.IsChecked = false;
            EnableClick.IsChecked = false;
            DisableOnExit.IsChecked = false;

            UpdateUseableOptions();
            return;
        }

        private void UpdateUseableOptions()
        {
            if (UseOffset.IsChecked == true)
            {
                screenX1.IsEnabled = false;
                screenY1.IsEnabled = false;
                touchpadX1.IsEnabled = false;
                touchpadY1.IsEnabled = false;
                ScreenXOffset.IsEnabled = true;
                ScreenYOffset.IsEnabled = true;
                TouchpadXOffset.IsEnabled = true;
                TouchpadYOffset.IsEnabled = true;
                ScreenXOffsetTextbox.IsEnabled = true;
                ScreenYOffsetTextbox.IsEnabled = true;
                TouchpadXOffsetTextbox.IsEnabled = true;
                TouchpadYOffsetTextbox.IsEnabled = true;
            }
            else if (UseOffset.IsChecked == false)
            {
                screenX1.IsEnabled = true;
                screenY1.IsEnabled = true;
                touchpadX1.IsEnabled = true;
                touchpadY1.IsEnabled = true;
                ScreenXOffset.IsEnabled = false;
                ScreenYOffset.IsEnabled = false;
                TouchpadXOffset.IsEnabled = false;
                TouchpadYOffset.IsEnabled = false;
                ScreenXOffsetTextbox.IsEnabled = false;
                ScreenYOffsetTextbox.IsEnabled = false;
                TouchpadXOffsetTextbox.IsEnabled = false;
                TouchpadYOffsetTextbox.IsEnabled = false;
            }
            if (LockAspectRatio.IsChecked == true)
            {
                touchpadY2.IsEnabled = false;
            }
            else if (LockAspectRatio.IsChecked == false)
            {
                touchpadY2.IsEnabled = true;
            }
            return;
        }

        private void FindInstallLocation()
        {
            OpenFileDialog FInstallLocation = new OpenFileDialog
            {
                InitialDirectory = "C:\\",
                Filter = "Executible Files (*.exe)|*.exe|All files (*.*)|*.*",
                RestoreDirectory = true
            };
            FInstallLocation.ShowDialog();

            try
            {
                InstallLocationTextbox.Text = FInstallLocation.FileName;
            }
            catch(ArgumentNullException)
            {
                InstallLocationTextbox.Text = String.Empty;
                FindInstallLocation();
            }
            return;
        }

        private void TouchpadAspectRatio()
        {
            int AspectRatioCalc = 0;
            try
            {
                AspectRatioCalc = Convert.ToInt32((double.Parse(screenY2.Text) / double.Parse(screenX2.Text)) * double.Parse(touchpadX2.Text));
            }
            catch
            {
                return;
            }
            touchpadY2.Text = $"{AspectRatioCalc}";
        }

        public void LoadSettings()
        {
            // Begin loading settings from text file

            try
            {
                //  -- Note --
                //  This code could probably be cleaned up / improved, possibly StreamReader?
                
                // Input settings tab
                // Screen bounds
                screenX1.Text = File.ReadLines(settingsLocation).Take(1).First();
                screenY1.Text = File.ReadLines(settingsLocation).Skip(1).Take(1).First();
                screenX2.Text = File.ReadLines(settingsLocation).Skip(2).Take(1).First();
                screenY2.Text = File.ReadLines(settingsLocation).Skip(3).Take(1).First();
                // Touchpad bounds
                touchpadX1.Text = File.ReadLines(settingsLocation).Skip(4).Take(1).First();
                touchpadY1.Text = File.ReadLines(settingsLocation).Skip(5).Take(1).First();
                touchpadX2.Text = File.ReadLines(settingsLocation).Skip(6).Take(1).First();
                touchpadY2.Text = File.ReadLines(settingsLocation).Skip(7).Take(1).First();
                // Sliders
                WeightSlider.Value = double.Parse(File.ReadLines(settingsLocation).Skip(8).Take(1).First());
                // Checkboxes
                UseOffset.IsChecked = bool.Parse(File.ReadLines(settingsLocation).Skip(9).Take(1).First());
                LockAspectRatio.IsChecked = bool.Parse(File.ReadLines(settingsLocation).Skip(10).Take(1).First());
                EnableClick.IsChecked = bool.Parse(File.ReadLines(settingsLocation).Skip(11).Take(1).First());
                DisableOnExit.IsChecked = bool.Parse(File.ReadLines(settingsLocation).Skip(12).Take(1).First());
                // Offset tab
                // Sliders
                ScreenXOffset.Value = double.Parse(File.ReadLines(settingsLocation).Skip(13).Take(1).First());
                ScreenYOffset.Value = double.Parse(File.ReadLines(settingsLocation).Skip(14).Take(1).First());
                TouchpadXOffset.Value = double.Parse(File.ReadLines(settingsLocation).Skip(15).Take(1).First());
                TouchpadYOffset.Value = double.Parse(File.ReadLines(settingsLocation).Skip(16).Take(1).First());
                // Setup tab
                // Textboxes
                InstallLocationTextbox.Text = File.ReadLines(settingsLocation).Skip(17).Take(1).First();
            }
            catch (System.ArgumentException)
            {
                return;
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("An error has occured while loading.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UpdateUseableOptions();
            return;
        }

        private void LoadSettingsDialog()
        {
            OpenFileDialog openFile = new OpenFileDialog
            {
                InitialDirectory = "\\",
                Filter = "AbsoluteTouch GUI setup (*.setup)|*.setup|All files (*.*)|*.*",
                RestoreDirectory = true,
            };
            openFile.ShowDialog();

            settingsLocation = openFile.FileName;
            return;
        }

        public void SaveSettings()
        {
            try
            {
                File.WriteAllText(settingsLocation, String.Empty);
                StreamWriter saveSettings = File.AppendText(settingsLocation);
                // Input settings tab
                // Screen bounds
                saveSettings.WriteLine(screenX1.Text);
                saveSettings.WriteLine(screenY1.Text);
                saveSettings.WriteLine(screenX2.Text);
                saveSettings.WriteLine(screenY2.Text);
                // Touchpad bounds
                saveSettings.WriteLine(touchpadX1.Text);
                saveSettings.WriteLine(touchpadY1.Text);
                saveSettings.WriteLine(touchpadX2.Text);
                saveSettings.WriteLine(touchpadY2.Text);
                // Sliders
                saveSettings.WriteLine(WeightSlider.Value.ToString());
                // Checkboxes
                saveSettings.WriteLine(UseOffset.IsChecked.ToString());
                saveSettings.WriteLine(LockAspectRatio.IsChecked.ToString());
                saveSettings.WriteLine(EnableClick.IsChecked.ToString());
                saveSettings.WriteLine(DisableOnExit.IsChecked.ToString());
                // Offset tab
                // Sliders
                saveSettings.WriteLine(ScreenXOffset.Value.ToString());
                saveSettings.WriteLine(ScreenYOffset.Value.ToString());
                saveSettings.WriteLine(TouchpadXOffset.Value.ToString());
                saveSettings.WriteLine(TouchpadYOffset.Value.ToString());
                // Setup tab
                // Textboxes
                saveSettings.WriteLine(InstallLocationTextbox.Text);
                saveSettings.Close();
            }
            catch (System.ArgumentException)
            {
                return;
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("An error has occured while saving.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }
        
        private void SaveSettingsDialog()
        {
            SaveFileDialog openFile = new SaveFileDialog
            {
                InitialDirectory = "\\",
                Filter = "AbsoluteTouch GUI setup (*.setup)|*.setup|All files (*.*)|*.*",
                RestoreDirectory = true,
                DefaultExt = "setup",
            };
            openFile.ShowDialog();

            settingsLocation = openFile.FileName;
            return;
        }

        public void DefaultSettingsCheck()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + @"\AbsoluteTouchDefault.setup") == true)
            {
                settingsLocation = Directory.GetCurrentDirectory() + @"\AbsoluteTouchDefault.setup";
                LoadSettings();
            }
        }

        #endregion

        #region  Button Methods

        private void RunButton_Click(object sender, RoutedEventArgs e) => RunAbsoluteTouch();

        private void InstallLocationTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update install location variable
            try
            {
                installLocation = InstallLocationTextbox.Text;
            }
            catch
            {
                installLocation = "C:\\";
                return;
            }
            return;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();

        private void FindInstallLocationButton_Click(object sender, RoutedEventArgs e) => FindInstallLocation();

        private void UpdateArgumentsButton_Click(object sender, RoutedEventArgs e) => CollectInformation();

        private void WeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                WeightTextbox.Text = $"{WeightSlider.Value}";
            }
            catch(ArgumentNullException)
            {
                return;
            }
            return;
        }

        private void WeightTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                WeightSlider.Value = double.Parse(WeightTextbox.Text);
            }
            catch(ArgumentException)
            {
                return;
            }
            return;
        }

        private void GetResolution_Click(object sender, RoutedEventArgs e) => Defaults();

        private void TouchpadAspectRatioUpdate_Click(object sender, RoutedEventArgs e) => TouchpadAspectRatio();

        private void UseOffset_Clicked(object sender, RoutedEventArgs e)
        {
            if (UseOffset.IsChecked == true)
            {
                //OffsetTab.IsSelected = true;
                //OffsetTab.IsEnabled = true;
            }
            else if (UseOffset.IsChecked == false)
            {
                //OffsetTab.IsEnabled = false;
            }
            UpdateUseableOptions();
        }

        private void LockAspectRatio_Click(object sender, RoutedEventArgs e)
        {
            if (LockAspectRatio.IsChecked == true)
            {
                touchpadY2.IsEnabled = false;
            }
            else if (LockAspectRatio.IsChecked == false)
            {
                touchpadY2.IsEnabled = true;
            }
            TouchpadAspectRatio();
        }

        private void TouchpadX2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LockAspectRatio.IsChecked == true)
            {
                TouchpadAspectRatio();
            }
        }

        private void EnableClick_Clicked(object sender, RoutedEventArgs e)
        {

        }

        private void DisableOnExit_Click(object sender, RoutedEventArgs e)
        {

        }

        // Offset Values

        private void ScreenXOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScreenXOffsetTextbox.Text = ScreenXOffset.Value.ToString();
        }

        private void ScreenXOffsetTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ScreenXOffset.Value = double.Parse(ScreenXOffsetTextbox.Text);
            }
            catch
            {
                ScreenXOffsetTextbox.SelectedText = "0";
            }
        }

        private void ScreenYOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScreenYOffsetTextbox.Text = ScreenYOffset.Value.ToString();
        }

        private void ScreenYOffsetTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ScreenYOffset.Value = double.Parse(ScreenYOffsetTextbox.Text);
            }
            catch
            {
                ScreenYOffsetTextbox.SelectedText = "0";
            }
        }

        private void TouchpadXOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TouchpadXOffsetTextbox.Text = TouchpadXOffset.Value.ToString();
        }

        private void TouchpadXOffsetTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                TouchpadXOffset.Value = double.Parse(TouchpadXOffsetTextbox.Text);
            }
            catch
            {
                TouchpadXOffsetTextbox.SelectedText = "0";
            }
        }

        private void TouchpadYOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TouchpadYOffsetTextbox.Text = TouchpadYOffset.Value.ToString();
        }

        private void TouchpadYOffsetTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                TouchpadYOffset.Value = double.Parse(TouchpadYOffsetTextbox.Text);
            }
            catch
            {
                TouchpadYOffsetTextbox.SelectedText = "0";
            }
        }

        // Load / Save buttons

        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSettingsDialog();
            LoadSettings();
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettingsDialog();
            SaveSettings();
        }

        private void SaveDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            settingsLocation = Directory.GetCurrentDirectory() + @"\AbsoluteTouchDefault.setup";
            SaveSettings();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutBox box = new AboutBox();  
            box.ShowDialog();
        }
        #endregion
    }
}
