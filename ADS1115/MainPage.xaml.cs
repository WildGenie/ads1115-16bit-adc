using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ADC.Devices.I2c.ADS1115;
using Microsoft.IoT.Lightning.Providers;
using Windows.Devices;

namespace ADC
{
    using System.Runtime.InteropServices;
    using Windows.Devices.Enumeration;
    using Windows.UI.Core;

    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        #region Fields
        private DispatcherTimer timer;
        private ADS1115Sensor adc;
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        public bool Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            // if unchanged return false
            if (Equals(storage, value))
                return false;
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            // if PropertyChanged not null call the Invoke method
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region FieldsBindToComboBox
        public IEnumerable<AdcDataRate> DataRates => Enum.GetValues(typeof(AdcDataRate)).Cast<AdcDataRate>();
        public IEnumerable<AdcInput> Inputs => Enum.GetValues(typeof(AdcInput)).Cast<AdcInput>();
        public IEnumerable<AdcPga> Pgas => Enum.GetValues(typeof(AdcPga)).Cast<AdcPga>();
        public IEnumerable<AdcMode> Modes => Enum.GetValues(typeof(AdcMode)).Cast<AdcMode>();
        public IEnumerable<AdcComparatorMode> ComparatorModes => Enum.GetValues(typeof(AdcComparatorMode)).Cast<AdcComparatorMode>();
        public IEnumerable<AdcComparatorPolarity> ComparatorPolarities => Enum.GetValues(typeof(AdcComparatorPolarity)).Cast<AdcComparatorPolarity>();
        public IEnumerable<AdcComparatorLatching> ComparatorLatchings => Enum.GetValues(typeof(AdcComparatorLatching)).Cast<AdcComparatorLatching>();
        public IEnumerable<AdcComparatorQueue> ComparatorQueue => Enum.GetValues(typeof(AdcComparatorQueue)).Cast<AdcComparatorQueue>();
        #endregion

        #region Properties
        public double ConvertedValue
        {
            get { return _convertedValue; }
            set { Set(ref _convertedValue, value); }
        }
        private double _convertedValue = 0;

        public double ConvertedVoltage
        {
            get { return _convertedVoltage; }
            set { Set(ref _convertedVoltage, value); }
        }
        private double _convertedVoltage = 0;

        public ADS1115SensorSetting Setting
        {
            get { return _setting; }
            set { Set(ref _setting, value); }
        }
        private ADS1115SensorSetting _setting = new ADS1115SensorSetting();

        private string _logs;

        public string Logs {
            get { return _logs; }
            set { Set(ref _logs, value); }
        }

        Windows.UI.Core.CoreDispatcher dispatcher;
        public static DeviceWatcher watcher = null;

        #endregion

        public MainPage()
        {
            this.InitializeComponent();

            // Setting the DataContext
            this.DataContext = this;

            // Register for the unloaded event so we can clean up upon exit
            Unloaded += MainPage_Unloaded;

            // Set Lightning as the default provider
            if (LightningProvider.IsLightningEnabled)
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

            // Initialize the DispatcherTimer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += timer_tick;


            //string aqs = "System.Devices.InterfaceClassGuid:=\"{36FC9E60-C465-11CF-8056-444553540000}\" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

            //DeviceWatcher DW = DeviceInformation.CreateWatcher();  //Advance querry string
            //DW.Added += DeviceAddedEvent;
            //DW.Removed += DeviceRemovedEvent;
            //DW.Updated += DeviceUpdatedEvent;


            dispatcher = Window.Current.CoreWindow.Dispatcher;
            watcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
            // Add event handlers
            watcher.Added += DeviceAddedEvent;
            watcher.Removed += DeviceRemovedEvent;
            watcher.Updated += DeviceUpdatedEvent;
            watcher.EnumerationCompleted += DeviceEnumerationCompletedEvent;
            watcher.Stopped += DeviceStoppedEvent;
            watcher.Start();

            // Initialize the sensors
            InitializeSensors();
        }

        private void DeviceStoppedEvent(DeviceWatcher sender, object args)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    WriteLog("DeviceStoppedEvent");
                });
        }

        private void DeviceEnumerationCompletedEvent(DeviceWatcher sender, object args)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    WriteLog("DeviceEnumerationCompletedEvent");
                });
        }


        private void DeviceAddedEvent(DeviceWatcher sender, DeviceInformation deviceInterface)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    WriteLog("DeviceAddedEvent");
                });
        }

        private void DeviceRemovedEvent(DeviceWatcher sender, DeviceInformationUpdate devUpdate)
        {

            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    WriteLog("DeviceRemovedEvent");
                });
        }

        private void DeviceUpdatedEvent(DeviceWatcher sender, DeviceInformationUpdate devUpdate)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    
                    WriteLog("DeviceUpdatedEvent" + devUpdate.Id);
                });
        }

        private void WriteLog(string messsage)
        {
            Logs = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + ";" + messsage + "\r\n" + Logs;

        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if(adc != null)
            {
                adc.Dispose();
                adc = null;
            };

            timer.Stop();
            timer = null;
        }

        private void timer_tick(object sender, object e)
        {
            if (adc != null && adc.IsInitialized)
            {
                try
                {
                    var temp = adc.readContinuous();
                    ConvertedVoltage = 0;
                    ConvertedValue = temp;

                    if (temp > 10000)
                    {
                        //Logs = string.Format("\n{0};{1}",DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),temp) + Logs;
                        WriteLog(temp.ToString());
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Continuous read has failed" + ex);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemTime
        {
            [MarshalAs(UnmanagedType.U2)]
            public short Year;
            [MarshalAs(UnmanagedType.U2)]
            public short Month;
            [MarshalAs(UnmanagedType.U2)]
            public short DayOfWeek;
            [MarshalAs(UnmanagedType.U2)]
            public short Day;
            [MarshalAs(UnmanagedType.U2)]
            public short Hour;
            [MarshalAs(UnmanagedType.U2)]
            public short Minute;
            [MarshalAs(UnmanagedType.U2)]
            public short Second;
            [MarshalAs(UnmanagedType.U2)]
            public short Milliseconds;

            public SystemTime(DateTime dt)
            {
                Year = (short)dt.Year;
                Month = (short)dt.Month;
                DayOfWeek = (short)dt.DayOfWeek;
                Day = (short)dt.Day;
                Hour = (short)dt.Hour;
                Minute = (short)dt.Minute;
                Second = (short)dt.Second;
                Milliseconds = (short)dt.Millisecond;
            }
        }

        [DllImport("kernelbase.dll", SetLastError = true)]
        static extern bool SetSystemTime(ref SystemTime time);

        private async void InitializeSensors()
        {
            try
            {

                //WriteLog($"RTC Saatı: {dt.ToString("yyyy/MM/dd HH:mm:ss")}");
                //WriteLog($"Sistem Saatı: {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}");
                //WriteLog($"RTC Sıcaklık: {rtcTemp} ℃");

                //if (dt < new DateTime(2019, 02, 16) || dt < DateTime.Now)
                //{
                //    this.rtc.DateTime = DateTime.Now;
                //    WriteLog("RTC'nin saati ayarlandı.");
                //    dt = rtc.DateTime;
                //    WriteLog($"RTC Saatı: {dt.ToString("yyyy/MM/dd HH:mm:ss")}");

                //}
                //else
                //{
                //    var sysTime = new SystemTime(dt);
                //    SetSystemTime(ref sysTime);
                //    WriteLog("RTC'den sistem saati ayarlandı.");
                //}

                adc = new ADS1115Sensor(AdcAddress.GND);
                await adc.InitializeAsync();
                if (Setting.Mode == AdcMode.CONTINOUS_CONVERSION)
                {
                    if (adc != null && adc.IsInitialized)
                    {
                        try
                        {
                            await adc.readContinuousInit(Setting);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Initialization of continuous read has failed" + ex);
                        }

                        timer.Start();
                    }
                }
                else
                {
                    timer.Stop();

                    if (adc != null && adc.IsInitialized)
                    {
                        try
                        {
                            var temp = await adc.readSingleShot(Setting);
                            ConvertedValue = temp.DecimalValue;
                            ConvertedVoltage = temp.VoltageValue;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Read from ADS1115 has failed: " + ex);
                        }
                    }
                }
                await adc.writeTreshold(ushort.Parse(tb_tresh_a.Text), ushort.Parse(tb_tresh_b.Text));

            }
            catch (Exception ex)
            {
                WriteLog("Initialization has failed: " + ex);

                throw new Exception("Initialization has failed: " + ex);
            }
        }

        private async void bt_convert_Click(object sender, RoutedEventArgs e)
        {
            if (Setting.Mode == AdcMode.CONTINOUS_CONVERSION)
            {
                if (adc != null && adc.IsInitialized)
                {
                    try
                    {
                        await adc.readContinuousInit(Setting);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Initialization of continuous read has failed" + ex);
                    }

                    timer.Start();
                }      
            }
            else
            {
                timer.Stop();

                if (adc != null && adc.IsInitialized)
                {
                    try
                    {
                        var temp = await adc.readSingleShot(Setting);
                        ConvertedValue = temp.DecimalValue;
                        ConvertedVoltage = temp.VoltageValue;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Read from ADS1115 has failed: " + ex);
                    }
                }
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            await adc.writeTreshold(ushort.Parse(tb_tresh_a.Text), ushort.Parse(tb_tresh_b.Text));
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            adc.TurnAlertIntoConversionReady();
        }
    }
}

