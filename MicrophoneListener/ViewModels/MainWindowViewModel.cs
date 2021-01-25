using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Annotations;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Prism.Mvvm;

namespace MicrophoneListener.ViewModels
{
    [UsedImplicitly]
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private readonly SynchronizationContext synchronizationContext;
        private int bitDepth;
        private WasapiCapture capture;

        private int channelCount;

        private string message;
        private float peak;

        private int sampleRate;

        private int sampleTypeIndex;
        private MMDevice selectedDevice;

        private int shareModeIndex;
        private WaveFileWriter writer;

        public MainWindowViewModel()
        {
            synchronizationContext = SynchronizationContext.Current;
            Devices = new ObservableCollection<MMDevice>();
            var enumerator = new MMDeviceEnumerator();
            Devices.AddRange(enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active));
            MMDevice defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            SelectedDevice = Devices.FirstOrDefault(c => c.ID == defaultDevice.ID);
            Record();
        }

        public ObservableCollection<MMDevice> Devices { get; }

        public MMDevice SelectedDevice
        {
            get => selectedDevice;
            set
            {
                if (SetProperty(ref selectedDevice, value))
                {
                    GetDefaultRecordingFormat(value);
                }
            }
        }

        public float Peak
        {
            get => peak;
            set => SetProperty(ref peak, value);
        }

        public int SampleTypeIndex
        {
            get => sampleTypeIndex;
            set
            {
                if (SetProperty(ref sampleTypeIndex, value))
                {
                    BitDepth = sampleTypeIndex == 1 ? 16 : 32;
                    RaisePropertyChanged(nameof(IsBitDepthConfigurable));
                }
            }
        }

        public bool IsBitDepthConfigurable => SampleTypeIndex == 1;

        public int BitDepth
        {
            get => bitDepth;
            set => SetProperty(ref bitDepth, value);
        }

        public int SampleRate
        {
            get => sampleRate;
            set => SetProperty(ref sampleRate, value);
        }

        public int ChannelCount
        {
            get => channelCount;
            set => SetProperty(ref channelCount, value);
        }

        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }

        public int ShareModeIndex
        {
            get => shareModeIndex;
            set => SetProperty(ref shareModeIndex, value);
        }

        public void Dispose()
        {
            Stop();
        }
        
        private void GetDefaultRecordingFormat(MMDevice device)
        {
            using (var c = new WasapiCapture(device))
            {
                SampleTypeIndex = c.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat ? 0 : 1;
                SampleRate = c.WaveFormat.SampleRate;
                BitDepth = c.WaveFormat.BitsPerSample;
                ChannelCount = c.WaveFormat.Channels;
                Message = "";
            }
        }

        private void CaptureOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            // if (writer == null)
            // {
            //     writer = new WaveFileWriter("output.mp3", capture.WaveFormat);
            // }
            //
            // writer.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);

            UpdatePeakMeter();
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            writer.Dispose();
            writer = null;
            if (e.Exception == null)
            {
                Message = "Recording Stopped";
            }
            else
            {
                Message = "Recording Error: " + e.Exception.Message;
            }

            capture.Dispose();
            capture = null;
        }

        private void Record()
        {
            try
            {
                capture = new WasapiCapture(SelectedDevice);
                capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
                capture.WaveFormat =
                    SampleTypeIndex == 0
                        ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount)
                        : new WaveFormat(sampleRate, bitDepth, channelCount);
                //RecordLevel = SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                capture.StartRecording();
                capture.RecordingStopped += OnRecordingStopped;
                capture.DataAvailable += CaptureOnDataAvailable;
                Message = "Recording...";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void Stop()
        {
            capture?.StopRecording();
        }

        private void UpdatePeakMeter()
        {
            // can't access this on a different thread from the one it was created on, so get back to GUI thread
            synchronizationContext.Post(s => Peak = SelectedDevice.AudioMeterInformation.MasterPeakValue, null);
        }
    }
}