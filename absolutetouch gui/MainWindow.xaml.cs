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

            // Debugging
            GetComboBoxItems();
        }

        #region Variables
        // Process
        public System.Diagnostics.Process absoluteTouchProcess;

        // TESTING***
        public Settings settings = new Settings();

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

        // Setting files
        public string SettingsLocation { get; set; }

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
            try
            {
                UpdateSettings();
                absoluteTouchProcess = new System.Diagnostics.Process();
                absoluteTouchProcess.StartInfo.FileName = settings.InstallLocation;
                absoluteTouchProcess.StartInfo.Arguments = settings.ProgramArguments;
                if(DebugTab.IsVisible == true)
                {
                    DebugUpdate(absoluteTouchProcess.StartInfo.ToString());
                }
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

        public void LoadSettings(string location)
        {
            // Begin loading settings from text file
            if (location == null)
            {
                return;
            }
            else
            {
                try
                {
                    string[] newSettings = File.ReadAllLines(location);
                    if (newSettings[0] == settings.Version.ToString())
                    {
                        string[] touchpad = newSettings[1].Split(',');
                        string[] screen = newSettings[2].Split(',');

                        screenWidth.Text = screen[0];
                        screenHeight.Text = screen[1];
                        screenX.Text = screen[2];
                        screenY.Text = screen[3];

                        touchpadWidth.Text = touchpad[0];
                        touchpadHeight.Text = touchpad[1];
                        touchpadX.Text = touchpad[2];
                        touchpadY.Text = touchpad[3];

                        WeightSlider.Value = Convert.ToDouble(newSettings[3]);
                        InstallLocationTextbox.Text = newSettings[4];
                        LockAspectRatio.IsChecked = Convert.ToBoolean(newSettings[5]);
                        EnableClick.IsChecked = Convert.ToBoolean(newSettings[6]);
                        DisableOnExit.IsChecked = Convert.ToBoolean(newSettings[7]);
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

        private string LoadSettingsDialog()
        {
            OpenFileDialog openFile = new OpenFileDialog
            {
                InitialDirectory = "\\",
                Filter = "AbsoluteTouch GUI setup (*.setup)|*.setup|All files (*.*)|*.*",
                RestoreDirectory = true,
            };
            if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return openFile.FileName;
            }
            else
            {
                return null;
            }
        }

        public void SaveSettings(string location)
        {
            // Begin saving settings to text file
            if (location == null)
            {
                return;
            }
            else
            {
                try
                {
                    File.WriteAllText(location, String.Empty);
                    StreamWriter save = File.AppendText(SettingsLocation);
                    Array.ForEach(settings.Dump(), setting => save.WriteLine(setting));
                    save.Close();
                }
                catch (System.ArgumentException ex)
                {
                    DebugUpdate(ex.ToString());
                    return;
                }
                catch (Exception ex)
                {
                    ErrorPopup("An error has occured while saving.", ex);
                }
                return;
            }
        }

        private string SaveSettingsDialog()
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
                return openFile.FileName;
            }
            else
            {
                return null;
            }
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
                LoadSettings(SettingsLocation);
            }
        }

        private void UpdateSettings()
        {
            // Install information
            settings.InstallLocation = InstallLocationTextbox.Text;
            // Touchpad settings
            settings.TouchpadArea.X = int.Parse(touchpadX.Text);
            settings.TouchpadArea.Y = int.Parse(touchpadY.Text);
            settings.TouchpadArea.Width = int.Parse(touchpadWidth.Text);
            settings.TouchpadArea.Height = int.Parse(touchpadHeight.Text);
            settings.Weight = WeightSlider.Value;
            // Screen settings
            settings.ScreenArea.X = int.Parse(screenX.Text);
            settings.ScreenArea.Y = int.Parse(screenY.Text);
            settings.ScreenArea.Width = int.Parse(screenWidth.Text);
            settings.ScreenArea.Height = int.Parse(screenHeight.Text);
            // Others
            settings.EnableClick = EnableClick.IsChecked.Value;
            settings.DisableOnExit = DisableOnExit.IsChecked.Value;
            settings.LockAspectRatio = LockAspectRatio.IsChecked.Value;
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

        public void DebugWrite(string[] text)
        {
            Array.ForEach(text, e => DebugUpdate(e));
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
            if(ex is null == false)
            {
                DebugUpdate(ex.ToString());
            }
        }

        void RunDebugCommand(object sender, RoutedEventArgs e)
        {
            var item = ButtonOptions.SelectedItem as ComboBoxItem;
            string activeCommand = item.Name;
            switch (activeCommand)
            {
                case "SettingsDump":
                    {
                        DumpSettings(settings);
                        return;
                    }
                case "CollectInformation":
                    {
                        CollectInformation();
                        return;
                    }
                case null:
                    {
                        ErrorPopup("Selected item is null.", null);
                        return;
                    }
            }
        }

        void AddDebugCommands(string[] Commands)
        {
            Array.ForEach(Commands, item => ButtonOptions.Items.Add(new ComboBoxItem
            {
                Content = item,
                Name = item
            })
            );
        }

        void GetComboBoxItems()
        {
            var DebugItems = new string[]
            {
                "SettingsDump",
                "CollectInformation",
            };
            AddDebugCommands(DebugItems);
        }

        #endregion

        #region Debug Methods

        void DumpSettings(Settings s)
        {
            DebugWrite(settings.Dump());
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

        private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();

        private void FindInstallLocationButton(object sender, RoutedEventArgs e) => settings.InstallLocation = FindInstallLocation();

        private void ResetToDefaults(object sender, RoutedEventArgs e) => Defaults();

        private void WeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                WeightTextbox.Text = $"{WeightSlider.Value}";
            }
            catch (ArgumentException) { }
            SettingsTextChanged(sender, null);
        }

        private void WeightTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                WeightSlider.Value = double.Parse(WeightTextbox.Text);
            }
            catch (ArgumentException) { }
            SettingsTextChanged(sender, null);
        }

        private void Checkbox_Click(object sender, RoutedEventArgs e) => SettingsTextChanged(sender, null);

        private void SettingsTextChanged(object sender, TextChangedEventArgs e)
        {
            if (LockAspectRatio.IsChecked == true) TouchpadAspectRatio();
            try
            {
                UpdateCanvasObjects();
            }
            catch { }
            try
            {
                UpdateSettings();
            }
            catch { }
        }

        // Load / Save buttons

        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e) => LoadSettings(LoadSettingsDialog());

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(SaveSettingsDialog());
        }

        private void SaveDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsLocation = Directory.GetCurrentDirectory() + @"\AbsoluteTouchDefault.setup";
            SaveSettings(SettingsLocation);
        }

        private void canvasScreenMap_MouseDown(object sender, MouseButtonEventArgs e) => UpdateCanvasObjects();

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutBox box = new AboutBox();
            box.ShowDialog();
        }

        // Debug

        private void UpdateDebugTab(object sender, RoutedEventArgs e) => UpdateUseableOptions();

        private void CopyDebug_Click(object sender, RoutedEventArgs e) => DebugCopy();  

        private void ClearDebug_Click(object sender, RoutedEventArgs e) => DebugClear();

        #endregion
    }

    public class Settings
    {
        public double Version = 1.2;
        public Area TouchpadArea = new Area();
        public Area ScreenArea = new Area();
        public double Weight;
        public string InstallLocation;
        public bool LockAspectRatio;
        public bool EnableClick;
        public bool DisableOnExit;

        public string ProgramArguments
        {
            get
            {
                string args = string.Empty;
                // screen area
                args += $"-s {ScreenArea.X},{ScreenArea.Y},{ScreenArea.Width + ScreenArea.X},{ScreenArea.Height + ScreenArea.Y} ";
                // touchpad area
                args += $"-t {TouchpadArea.X},{TouchpadArea.Y},{TouchpadArea.Width + TouchpadArea.X},{TouchpadArea.Height + TouchpadArea.Y} ";
                // weight
                args += $"-w {Weight} ";
                // other arguments
                if (EnableClick == true)
                {
                    args += "-c ";
                }
                if (DisableOnExit == true)
                {
                    args += "-m ";
                }
                return args;
            }
        }

        public string[] Dump()
        {
            string[] dump = new string[]
            {
                Version.ToString(),
                TouchpadArea.ToString(),
                ScreenArea.ToString(),
                Weight.ToString(),
                InstallLocation,
                LockAspectRatio.ToString(),
                EnableClick.ToString(),
                DisableOnExit.ToString(),
                ProgramArguments,
            };
            return dump;
        }
    }

    public class Area
    {
        public Area()
        {
            Width = 0;
            Height = 0;
            X = 0;
            Y = 0;
        }

        public int Width;
        public int Height;
        public int X;
        public int Y;

        public override string ToString()
        {
            return $"{Width},{Height},{X},{Y}";
        }
    }
}
