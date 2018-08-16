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
            // Startup methods

            InitializeComponent();

            Defaults();

            DefaultSettingsCheck();

            UpdateUseableOptions();
        }

        #region Variables

        public System.Diagnostics.Process absoluteTouchProcess;
        
        // resolutions
        public double TouchpadWidth { get; set; }
        public double TouchpadHeight { get; set; }

        // Synaptics API variables
        SYNCTRLLib.SynAPICtrl api;
        SYNCTRLLib.SynDeviceCtrl device;
        int DeviceHandle { get; set; }
        int xMin, xMax, yMin, yMax, xDPI, yDPI;

        // setters / getters

        public bool APIAvailable
        {
            get
            {
                // Attempt to use Synaptics API
                try
                {
                    api = new SYNCTRLLib.SynAPICtrl();
                    device = new SYNCTRLLib.SynDeviceCtrl();
                    SYNCTRLLib.SynPacketCtrl packet = new SYNCTRLLib.SynPacketCtrl();
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    return false; // Ignores error and opens GUI anyway
                }
                catch (Exception ex)
                {
                    StatusbarText.Text = $"{ex}";
                    return false; // This usually shouldn't happen but its a precaution just in case.
                }
                return true;
            }
            set
            {
                // There should be no way to set this variable, only get.
            }
        }

        private string _InstallLocation;
        public string InstallLocation
        {
            get
            {
                if (_InstallLocation == null)
                {
                    _InstallLocation = FindInstallLocation();
                    return _InstallLocation;
                }
                else
                {
                    return _InstallLocation;
                }
            }
            set
            {
                _InstallLocation = value;
            }
        }

        public string SettingsLocation { get; set; }

        #endregion

        #region Keyboard Shortcuts

        public void RunShortcut(Object sender, ExecutedRoutedEventArgs e) => RunAbsoluteTouch();

        public void SaveShortcut(Object sender, ExecutedRoutedEventArgs e)
        {
            SaveSettingsDialog();
            SaveSettings();
            return;
        }

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
            string programArguments;
            try
            {
                // Get offsets and put into arguments
                double touchpadX1Offset = double.Parse(touchpadX.Text);
                double touchpadY1Offset = double.Parse(touchpadY.Text);
                double touchpadX2Offset = double.Parse(touchpadWidth.Text) + double.Parse(touchpadX.Text);
                double touchpadY2Offset = double.Parse(touchpadHeight.Text) + double.Parse(touchpadY.Text);
                double screenX1Offset = double.Parse(touchpadX.Text);
                double screenY1Offset = double.Parse(touchpadY.Text);
                double screenX2Offset = double.Parse(screenWidth.Text) + double.Parse(touchpadX.Text);
                double screenY2Offset = double.Parse(screenY2.Text) + double.Parse(touchpadY.Text);
                double weight = WeightSlider.Value;

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
                absoluteTouchProcess.StartInfo.FileName = InstallLocation;
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
            // Reset checkboxes
            LockAspectRatio.IsChecked = false;
            EnableClick.IsChecked = false;
            DisableOnExit.IsChecked = false;

            // Update Textboxes
            screenX.Text = "0";
            screenY.Text = "0";
            screenWidth.Text = $"{Screen.PrimaryScreen.Bounds.Width}";
            screenY2.Text = $"{Screen.PrimaryScreen.Bounds.Height}";

            touchpadX.Text = "0";
            touchpadY.Text = "0";
            touchpadWidth.Text = "6143";
            touchpadHeight.Text = "6143";

            WeightTextbox.Text = "0";

            ScreenXOffsetTextbox.Text = "0";
            ScreenYOffsetTextbox.Text = "0";
            TouchpadXOffsetTextbox.Text = "0";
            TouchpadYOffsetTextbox.Text = "0";

            GetTouchpadProperties();

            // Set max offsets
            ScreenXOffset.Maximum = Screen.PrimaryScreen.Bounds.Width;
            ScreenYOffset.Maximum = Screen.PrimaryScreen.Bounds.Height;
            TouchpadXOffset.Maximum = Convert.ToDouble(xMax);
            TouchpadYOffset.Maximum = Convert.ToDouble(yMax);

            // Set large change maximums
            ScreenXOffset.LargeChange = Screen.PrimaryScreen.Bounds.Width / 10;
            ScreenYOffset.LargeChange = Screen.PrimaryScreen.Bounds.Height / 10;
            TouchpadXOffset.LargeChange = TouchpadWidth / 10;
            TouchpadYOffset.LargeChange = TouchpadHeight / 10;

            UpdateUseableOptions();
            return;
        }

        private void GetTouchpadProperties()
        {
            if (APIAvailable == true)
            {
                try
                {
                    api.Initialize();
                    api.Activate();
                    // Select first device found
                    DeviceHandle = api.FindDevice(SYNCTRLLib.SynConnectionType.SE_ConnectionAny, SYNCTRLLib.SynDeviceType.SE_DeviceTouchPad, -1);
                    device.Select(DeviceHandle);
                    device.Activate();

                    xMin = int.Parse((device.GetLongProperty(SYNCTRLLib.SynDeviceProperty.SP_XLoSensor).ToString()));
                    xMax = int.Parse((device.GetLongProperty(SYNCTRLLib.SynDeviceProperty.SP_XHiSensor).ToString()));
                    yMin = int.Parse((device.GetLongProperty(SYNCTRLLib.SynDeviceProperty.SP_YLoSensor).ToString()));
                    yMax = int.Parse((device.GetLongProperty(SYNCTRLLib.SynDeviceProperty.SP_YHiSensor).ToString()));
                    xDPI = ((device.GetLongProperty(SYNCTRLLib.SynDeviceProperty.SP_XDPI)));
                    yDPI = ((device.GetLongProperty(SYNCTRLLib.SynDeviceProperty.SP_YDPI)));

                    api.Deactivate();
                    StatusbarText.Text = "Ready.";
                }
                catch (System.NullReferenceException)
                {
                    StatusbarText.Text = "Error while finding synaptics touchpad properties.";
                    DefaultTouchpadValues(); // use default estimated values
                    return;
                }
            }
            else if (APIAvailable == false) 
            {
                DefaultTouchpadValues();
                StatusbarText.Text = "Warning: Synaptics touchpad drivers are missing. Using default values.";
            }
            return;
        }

        private void DefaultTouchpadValues()
        {
            xMin = 0;
            xMax = 6143;
            yMin = 0;
            yMax = 6143;
            xDPI = 0;
            yDPI = 0;
            return;
        }

        private void UpdateUseableOptions()
        {
            if (LockAspectRatio.IsChecked == true)
            {
                touchpadHeight.IsEnabled = false;
            }
            else if (LockAspectRatio.IsChecked == false)
            {
                touchpadHeight.IsEnabled = true;
            }
            DebugUpdate();
            return;
        }

        private string FindInstallLocation()
        {
            OpenFileDialog location = new OpenFileDialog
            {
                InitialDirectory = "C:\\",
                Filter = "Executible Files (*.exe)|*.exe|All files (*.*)|*.*",
                RestoreDirectory = true
            };
            
            if (location.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    InstallLocationTextbox.Text = location.FileName;
                }
                catch (ArgumentNullException)
                {
                    InstallLocationTextbox.Text = String.Empty;
                    FindInstallLocation();
                }
                return location.FileName;
            }
            return InstallLocationTextbox.Text; 
        }

        private void TouchpadAspectRatio()
        {
            int AspectRatioCalc = 0;
            try
            {
                AspectRatioCalc = Convert.ToInt32((double.Parse(screenY2.Text) / double.Parse(screenWidth.Text)) * double.Parse(touchpadWidth.Text));
            }
            catch
            {
                return;
            }
            touchpadHeight.Text = $"{AspectRatioCalc}";
        }

        // Loading & Saving setup files

        public void LoadSettings()
        {
            // Begin loading settings from text file
            if (SettingsLocation == null)
            {
                return;
            }
            else
            {
                try
                {
                    //  -- Note --
                    //  This code could probably be cleaned up / improved, possibly StreamReader?

                    // Input settings tab
                    // Screen bounds
                    screenX.Text = File.ReadLines(SettingsLocation).Take(1).First();
                    screenY.Text = File.ReadLines(SettingsLocation).Skip(1).Take(1).First();
                    screenWidth.Text = File.ReadLines(SettingsLocation).Skip(2).Take(1).First();
                    screenY2.Text = File.ReadLines(SettingsLocation).Skip(3).Take(1).First();
                    // Touchpad bounds
                    touchpadX.Text = File.ReadLines(SettingsLocation).Skip(4).Take(1).First();
                    touchpadY.Text = File.ReadLines(SettingsLocation).Skip(5).Take(1).First();
                    touchpadWidth.Text = File.ReadLines(SettingsLocation).Skip(6).Take(1).First();
                    touchpadHeight.Text = File.ReadLines(SettingsLocation).Skip(7).Take(1).First();
                    // Sliders
                    WeightSlider.Value = double.Parse(File.ReadLines(SettingsLocation).Skip(8).Take(1).First());
                    // Checkboxes
                    // old UseOffset line
                    LockAspectRatio.IsChecked = bool.Parse(File.ReadLines(SettingsLocation).Skip(10).Take(1).First());
                    EnableClick.IsChecked = bool.Parse(File.ReadLines(SettingsLocation).Skip(11).Take(1).First());
                    DisableOnExit.IsChecked = bool.Parse(File.ReadLines(SettingsLocation).Skip(12).Take(1).First());
                    // Offset tab
                    // Sliders
                    ScreenXOffset.Value = double.Parse(File.ReadLines(SettingsLocation).Skip(13).Take(1).First());
                    ScreenYOffset.Value = double.Parse(File.ReadLines(SettingsLocation).Skip(14).Take(1).First());
                    TouchpadXOffset.Value = double.Parse(File.ReadLines(SettingsLocation).Skip(15).Take(1).First());
                    TouchpadYOffset.Value = double.Parse(File.ReadLines(SettingsLocation).Skip(16).Take(1).First());
                    // Setup tab
                    // Textboxes
                    InstallLocationTextbox.Text = File.ReadLines(SettingsLocation).Skip(17).Take(1).First();
                }
                catch (System.ArgumentException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show($"An error has occured while loading. {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
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
            if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SettingsLocation = openFile.FileName;
            }
            else
            {
                SettingsLocation = null;
            }
        }

        public void SaveSettings()
        {
            // Begin saving settings to text file
            if (SettingsLocation == null)
            {
                return;
            }
            else
            {
                try
                {
                    File.WriteAllText(SettingsLocation, String.Empty);
                    StreamWriter saveSettings = File.AppendText(SettingsLocation);
                    // Input settings tab
                    // Screen bounds
                    saveSettings.WriteLine(screenX.Text);
                    saveSettings.WriteLine(screenY.Text);
                    saveSettings.WriteLine(screenWidth.Text);
                    saveSettings.WriteLine(screenY2.Text);
                    // Touchpad bounds
                    saveSettings.WriteLine(touchpadX.Text);
                    saveSettings.WriteLine(touchpadY.Text);
                    saveSettings.WriteLine(touchpadWidth.Text);
                    saveSettings.WriteLine(touchpadHeight.Text);
                    // Sliders
                    saveSettings.WriteLine(WeightSlider.Value.ToString());
                    // Checkboxes
                    saveSettings.WriteLine(String.Empty); // old UseOffset line
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
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show($"An error has occured while saving. {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
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
            if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SettingsLocation = openFile.FileName;
            }
            else
            {
                SettingsLocation = null;
            }
            return;
        }

        public void DefaultSettingsCheck()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + @"\AbsoluteTouchDefault.setup") == true)
            {
                SettingsLocation = Directory.GetCurrentDirectory() + @"\AbsoluteTouchDefault.setup";
                if (APIAvailable == false)
                {
                    StatusbarText.Text = "Warning: Synaptics touchpad drivers are missing. Default settings loaded.";
                }
                else if (APIAvailable == true)
                {
                    StatusbarText.Text = StatusbarText.Text + " Default settings loaded.";
                }
                LoadSettings();
            }
        }

        // Debug group methods
        
        public void DebugUpdate()
        {
            if (debuggingCheckbox.IsChecked == true)
            {
                DebugTab.Visibility = Visibility.Visible;

                DebugTextBlock.Text = null; // clear textblock
                var debugtext = new StringBuilder()
                    .AppendLine($"Version: {System.Windows.Forms.Application.ProductVersion}")
                    .AppendLine($"InstallLocation: {InstallLocation}")
                    .AppendLine($"screenX1: {screenX.Text}")
                    .AppendLine($"screenY1: {screenY.Text}")
                    .AppendLine($"screenX2: {screenWidth.Text}")
                    .AppendLine($"screenY2: {screenY2.Text}")
                    .AppendLine($"touchpadX1: {touchpadX.Text}")
                    .AppendLine($"touchpadY1: {touchpadY.Text}")
                    .AppendLine($"touchpadX2: {touchpadWidth.Text}")
                    .AppendLine($"touchpadY2: {touchpadHeight.Text}")
                    .AppendLine($"weight: {WeightSlider.Value}");
                DebugTextBlock.Text = debugtext.ToString();
            }
            else if (debuggingCheckbox.IsChecked == false)
            {
                DebugTab.Visibility = Visibility.Hidden;
                DebugTextBlock.Text = null;
            }
        }

        void CopyDebugInfo()
        {
            System.Windows.Forms.Clipboard.SetText(DebugTextBlock.Text);
        }
        
        #endregion

        #region  Button Methods

        private void InstallLocationTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update install location variable
            try
            {
                InstallLocation = InstallLocationTextbox.Text;
            }
            catch
            {
                InstallLocation = "C:\\";
                return;
            }
            return;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();

        private void FindInstallLocationButton_Click(object sender, RoutedEventArgs e) => InstallLocation = FindInstallLocation();

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
        
        private void LockAspectRatio_Click(object sender, RoutedEventArgs e)
        {
            if (LockAspectRatio.IsChecked == true)
            {
                touchpadHeight.IsEnabled = false;
            }
            else if (LockAspectRatio.IsChecked == false)
            {
                touchpadHeight.IsEnabled = true;
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
            SettingsLocation = Directory.GetCurrentDirectory() + @"\AbsoluteTouchDefault.setup";
            SaveSettings();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutBox box = new AboutBox();  
            box.ShowDialog();
        }

        // Debug

        private void DebugScreen_Focused(object sender, RoutedEventArgs e) => DebugUpdate();

        private void UpdateDebugTab(object sender, RoutedEventArgs e) => DebugUpdate();

        private void DebugTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => CopyDebugInfo();

        #endregion
    }
}
