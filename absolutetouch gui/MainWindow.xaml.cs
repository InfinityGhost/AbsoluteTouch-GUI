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
        }

        public void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Startup methods
            Defaults(); // load default settings
            DefaultSettingsCheck(); // check if default settings are available
            UpdateUseableOptions();

            // Canvas work
            CreateCanvasObjects();
        }

        #region Variables
        // Process
        public System.Diagnostics.Process absoluteTouchProcess;

        // Resolutions
        public double TouchpadWidth { get; set; }
        public double TouchpadHeight { get; set; }

        //// Synaptics API variables
        SYNCTRLLib.SynAPICtrl api;
        SYNCTRLLib.SynDeviceCtrl device;
        int DeviceHandle { get; set; }
        int xMin, xMax, yMin, yMax, xDPI, yDPI;

        // Canvas objects
        // ---------
        // Screen
        public Rectangle rectangleScreenMap;
        public Rectangle rectangleDesktop;

        // Touchpad
        public Rectangle rectangleTouchpad;
        public Rectangle rectangleTouchMap;

        // Setters / Getters
        // ---------
        // API checking
        private bool _APIAvailable = false;
        private bool APIAvailable
        {
            get
            {
                // Attempt to use Synaptics API
                if (_APIAvailable == false)
                {
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
                        Status(ex.ToString());
                        return false; // This usually shouldn't happen but its a precaution just in case.
                    }
                    _APIAvailable = true;
                    return true;
                }
                else
                {
                    return true;
                }
            }
        }

        // Executable path
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

        // Setting files
        public string SettingsLocation { get; set; }
        public double SettingsVersion => 1.1;

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
            catch (System.ComponentModel.Win32Exception ex)
            {
                ErrorPopup("Invalid program executable.", ex);
                return;
            }
            catch (System.NullReferenceException)
            {
                return;
            }
            catch (System.InvalidOperationException ex)
            {
                ErrorPopup("Cannot start process because an executable has not been provided.", ex);
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
                double screenX1Offset = double.Parse(screenX.Text);
                double screenY1Offset = double.Parse(screenY.Text);
                double screenX2Offset = double.Parse(screenWidth.Text) + double.Parse(screenX.Text);
                double screenY2Offset = double.Parse(screenHeight.Text) + double.Parse(screenY.Text);
                double weight = WeightSlider.Value;

                // get toggle arguments
                string otherArguments = String.Empty;
                if (EnableClick.IsChecked == true)
                {
                    otherArguments += "-c ";
                }
                if (DisableOnExit.IsChecked == true)
                {
                    otherArguments += "-m ";
                }
                // set up arguments
                programArguments = $"-s {screenX1Offset},{screenY1Offset},{screenX2Offset},{screenY2Offset} -t {touchpadX1Offset},{touchpadY1Offset},{touchpadX2Offset},{touchpadY2Offset} -w {weight} {otherArguments}";
                // set info
                absoluteTouchProcess = new System.Diagnostics.Process();
                absoluteTouchProcess.StartInfo.FileName = InstallLocation;
                absoluteTouchProcess.StartInfo.Arguments = programArguments;
            }
            catch (System.FormatException ex)
            {
                ErrorPopup("Values were not in the correct format", ex);
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
            screenHeight.Text = $"{Screen.PrimaryScreen.Bounds.Height}";

            touchpadX.Text = "0";
            touchpadY.Text = "0";
            touchpadWidth.Text = "6143";
            touchpadHeight.Text = "6143";

            WeightTextbox.Text = "0";

            GetTouchpadProperties();

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
                    Status("Ready.");
                }
                catch (System.NullReferenceException)
                {
                    Status("Error while finding synaptics touchpad properties.");
                    DefaultTouchpadValues(); // use default estimated values
                    return;
                }
            }
            else if (APIAvailable == false)
            {
                DefaultTouchpadValues();
                Status("Warning: Synaptics touchpad drivers are missing. Using default values.");
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
            if (debuggingCheckbox.IsChecked == true)
            {
                DebugTab.SetValue(VisibilityProperty, Visibility.Visible);
            }
            else if (debuggingCheckbox.IsChecked == false)
            {
                DebugTab.SetValue(VisibilityProperty, Visibility.Hidden);
            }
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
                AspectRatioCalc = Convert.ToInt32((double.Parse(screenHeight.Text) / double.Parse(screenWidth.Text)) * double.Parse(touchpadWidth.Text));
            }
            catch (Exception ex)
            {
                DebugUpdate(ex.ToString());
            }
            touchpadHeight.Text = $"{AspectRatioCalc}";
        }

        #endregion

        #region Canvas

        void CreateCanvasObjects()
        {
            //// Screen canvas
            canvasScreenMap.Children.Clear(); // Clear canvas
            // Screen map area
            rectangleScreenMap = new Rectangle
            {
                Stroke = Brushes.Transparent,
                Width = 5,
                Height = 5,
                StrokeThickness = 1.0,
                Fill = Brushes.SkyBlue
            };
            canvasScreenMap.Children.Add(rectangleScreenMap);

            // Desktop area
            rectangleDesktop = new Rectangle
            {
                Stroke = Brushes.Black,
                Width = 10,
                Height = 10,
                StrokeThickness = 2.0,
                Fill = Brushes.Transparent
            };
            canvasScreenMap.Children.Add(rectangleDesktop);

            //// Touchpad canvas
            canvasTouchpadArea.Children.Clear();
            // Touchpad map area 
            rectangleTouchMap = new Rectangle
            {
                Stroke = Brushes.Transparent,
                Width = 5,
                Height = 5,
                StrokeThickness = 1.0,
                Fill = Brushes.SkyBlue
            };
            canvasTouchpadArea.Children.Add(rectangleTouchMap);

            // Touchpad full area
            rectangleTouchpad = new Rectangle
            {
                Stroke = Brushes.Black,
                Width = 10,
                Height = 10,
                StrokeThickness = 2.0,
                Fill = Brushes.Transparent
            };
            canvasTouchpadArea.Children.Add(rectangleTouchpad);

            UpdateCanvasObjects();
        }

        void UpdateCanvasObjects()
        {
            UpdateScreenCanvas();
            UpdateTouchpadCanvas();
        }

        void UpdateScreenCanvas()
        {
            Rectangle desktopResolution = new Rectangle
            {
                Width = Screen.PrimaryScreen.Bounds.Width,
                Height = Screen.PrimaryScreen.Bounds.Height
            };
            double scaleX = canvasScreenMap.ActualWidth / desktopResolution.Width;
            double scaleY = canvasScreenMap.ActualHeight / desktopResolution.Height;
            double scale = scaleX;
            if (scaleX > scaleY)
            {
                scale = scaleY;
            }

            // Get textbox variables
            double width = Convert.ToDouble(screenWidth.Text);
            double height = Convert.ToDouble(screenHeight.Text);
            double xOffset = Convert.ToDouble(screenX.Text);
            double yOffset = Convert.ToDouble(screenY.Text);

            // Max & min values
            if (xOffset < 0)
            {
                xOffset = 0;
            }
            else if (xOffset + width > desktopResolution.Width)
            {
                xOffset = desktopResolution.Width - width;
                if (xOffset < 0)
                {
                    xOffset = 0;
                }
            }
            screenX.Text = xOffset.ToString();
            if (yOffset < 0)
            {
                yOffset = 0;
            }
            else if (yOffset + height > desktopResolution.Height)
            {
                yOffset = desktopResolution.Height - height;
                if (yOffset < 0)
                {
                    yOffset = 0;
                }
            }
            screenY.Text = yOffset.ToString();

            // Centered offset
            double offsetX = canvasScreenMap.ActualWidth / 2.0 - desktopResolution.Width * scale / 2.0;
            double offsetY = canvasScreenMap.ActualHeight / 2.0 - desktopResolution.Height * scale / 2.0;

            // Desktop area
            rectangleDesktop.Width = desktopResolution.Width * scale;
            rectangleDesktop.Height = desktopResolution.Height * scale;
            Canvas.SetLeft(rectangleDesktop, offsetX);
            Canvas.SetTop(rectangleDesktop, offsetY);

            // Screen map
            rectangleScreenMap.Width = width * scale;
            rectangleScreenMap.Height = height * scale;
            Canvas.SetLeft(rectangleScreenMap, offsetX + xOffset * scale);
            Canvas.SetTop(rectangleScreenMap, offsetY + yOffset * scale);

        }

        void UpdateTouchpadCanvas()
        {
            Rectangle touchpadResolution;
            if (_APIAvailable == true)
            {
                touchpadResolution = new Rectangle
                {
                    Width = xMax - xMin,
                    Height = yMax - yMin
                };
            }
            else
            {
                touchpadResolution = new Rectangle
                {
                    Width = 6143,
                    Height = 6143
                };
            }

            double scaleX = (canvasTouchpadArea.ActualWidth) / touchpadResolution.Width;
            double scaleY = (canvasTouchpadArea.ActualHeight) / touchpadResolution.Height;
            double scale = scaleX;
            if (scaleX > scaleY)
            {
                scale = scaleY;
            }

            // Get textbox variables
            double width = Convert.ToDouble(touchpadWidth.Text);
            double height = Convert.ToDouble(touchpadHeight.Text);
            double xOffset = Convert.ToDouble(touchpadX.Text);
            double yOffset = Convert.ToDouble(touchpadY.Text);

            // Max & min values
            if (xOffset < 0)
            {
                xOffset = 0;
            }
            else if (xOffset + width > touchpadResolution.Width)
            {
                xOffset = touchpadResolution.Width - width;
                if (xOffset < 0)
                {
                    xOffset = 0;
                }
            }
            touchpadX.Text = xOffset.ToString();
            if (yOffset < 0)
            {
                yOffset = 0;
            }
            else if (yOffset + height > touchpadResolution.Height)
            {
                yOffset = touchpadResolution.Height - height;
                if (yOffset < 0)
                {
                    yOffset = 0;
                }
            }
            touchpadY.Text = yOffset.ToString();

            // Centered offset
            double offsetX = canvasTouchpadArea.ActualWidth / 2.0 - touchpadResolution.Width * scale / 2.0;
            double offsetY = canvasTouchpadArea.ActualHeight / 2.0 - touchpadResolution.Height * scale / 2.0;

            // Touchpad area
            rectangleTouchpad.Width = touchpadResolution.Width * scale;
            rectangleTouchpad.Height = touchpadResolution.Height * scale;
            Canvas.SetLeft(rectangleTouchpad, offsetX);
            Canvas.SetTop(rectangleTouchpad, offsetY);

            // Touchpad map
            rectangleTouchMap.Width = width * scale;
            rectangleTouchMap.Height = height * scale;
            Canvas.SetLeft(rectangleTouchMap, offsetX + xOffset * scale);
            Canvas.SetTop(rectangleTouchMap, offsetY + yOffset * scale);
        }

        #endregion

        #region Settings files

        // Loading & Saving settings files

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
                    string localSettingsVersion = File.ReadLines(SettingsLocation).Take(1).First(); // get selected file version
                    if (localSettingsVersion == SettingsVersion.ToString())
                    {
                        //// Input settings tab
                        // Screen bounds
                        screenX.Text = File.ReadLines(SettingsLocation).Skip(1).Take(1).First();
                        screenY.Text = File.ReadLines(SettingsLocation).Skip(2).Take(1).First();
                        screenWidth.Text = File.ReadLines(SettingsLocation).Skip(3).Take(1).First();
                        screenHeight.Text = File.ReadLines(SettingsLocation).Skip(4).Take(1).First();
                        // Touchpad bounds
                        touchpadX.Text = File.ReadLines(SettingsLocation).Skip(5).Take(1).First();
                        touchpadY.Text = File.ReadLines(SettingsLocation).Skip(6).Take(1).First();
                        touchpadWidth.Text = File.ReadLines(SettingsLocation).Skip(7).Take(1).First();
                        touchpadHeight.Text = File.ReadLines(SettingsLocation).Skip(8).Take(1).First();
                        // Sliders
                        WeightSlider.Value = double.Parse(File.ReadLines(SettingsLocation).Skip(9).Take(1).First());
                        // Checkboxes
                        LockAspectRatio.IsChecked = bool.Parse(File.ReadLines(SettingsLocation).Skip(10).Take(1).First());
                        EnableClick.IsChecked = bool.Parse(File.ReadLines(SettingsLocation).Skip(11).Take(1).First());
                        DisableOnExit.IsChecked = bool.Parse(File.ReadLines(SettingsLocation).Skip(12).Take(1).First());
                        //// Setup tab
                        // Textboxes
                        InstallLocationTextbox.Text = File.ReadLines(SettingsLocation).Skip(13).Take(1).First();
                    }

                }
                catch (System.ArgumentException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    ErrorPopup("An error has occured while loading.", ex);
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
                    //// Input settings tab
                    saveSettings.WriteLine(SettingsVersion); // added to allow for new savable things to be added
                    // Screen bounds
                    saveSettings.WriteLine(screenX.Text);
                    saveSettings.WriteLine(screenY.Text);
                    saveSettings.WriteLine(screenWidth.Text);
                    saveSettings.WriteLine(screenHeight.Text);
                    // Touchpad bounds
                    saveSettings.WriteLine(touchpadX.Text);
                    saveSettings.WriteLine(touchpadY.Text);
                    saveSettings.WriteLine(touchpadWidth.Text);
                    saveSettings.WriteLine(touchpadHeight.Text);
                    // Sliders
                    saveSettings.WriteLine(WeightSlider.Value.ToString());
                    // Checkboxes
                    saveSettings.WriteLine(LockAspectRatio.IsChecked.ToString());
                    saveSettings.WriteLine(EnableClick.IsChecked.ToString());
                    saveSettings.WriteLine(DisableOnExit.IsChecked.ToString());
                    //// Setup tab
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
                    ErrorPopup("An error has occured while saving.", ex);
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
                    Status("Warning: Synaptics touchpad drivers are missing. Default settings loaded.");
                }
                else if (APIAvailable == true)
                {
                    StatusAdd("Default settings loaded.");
                }
                LoadSettings();
            }
        }

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

        #region Debugging, Error Handling

        public void DebugUpdate(string text)
        {
            if (Debug.Text == string.Empty)
            {
                Debug.Text += text;
            }
            else
            {
                Debug.Text += Environment.NewLine + text;
            }
        }

        void DebugCopy()
        {
            System.Windows.Forms.Clipboard.SetText(Debug.Text);
        }

        void DebugClear()
        {
            Debug.Text = string.Empty;
        }

        void ErrorPopup(string errorText, Exception ex)
        {
            System.Windows.Forms.MessageBox.Show("Error: " + errorText, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DebugUpdate(errorText);
            DebugUpdate(ex.ToString());
        }

        #endregion

        #region StatusBar Methods

        void Status(string text)
        {
            StatusBarText.Text = text;
            DebugUpdate("Status: " + text);
        }

        void StatusAdd(string text)
        {
            StatusBarText.Text += text;
            DebugUpdate("Status: " + StatusBarText.Text);
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
            catch (ArgumentNullException)
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
            catch (ArgumentException)
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

        private void SettingsTextChanged(object sender, TextChangedEventArgs e)
        {
            if (LockAspectRatio.IsChecked == true)
            {
                TouchpadAspectRatio();
            }
            try
            {
                UpdateCanvasObjects();
            }
            catch (Exception ex)
            {
                //DebugUpdate(ex.ToString());
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

        private void canvasScreenMap_MouseDown(object sender, MouseButtonEventArgs e) => UpdateCanvasObjects();

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutBox box = new AboutBox();
            box.ShowDialog();
        }

        // Debug

        private void DebugScreen_Focused(object sender, RoutedEventArgs e) { }

        private void UpdateDebugTab(object sender, RoutedEventArgs e) => UpdateUseableOptions();

        private void CopyDebug_Click(object sender, RoutedEventArgs e) => DebugCopy();  

        private void ClearDebug_Click(object sender, RoutedEventArgs e) => DebugClear();

        #endregion
    }
}
