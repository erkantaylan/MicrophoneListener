using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
        private float peak;

        private double percentagePeak;
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
            GetDefaultRecordingFormat(SelectedDevice);
            Record();
        }

        public ObservableCollection<MMDevice> Devices { get; }

        public MMDevice SelectedDevice
        {
            get => selectedDevice;
            set => SetProperty(ref selectedDevice, value);
        }

        public float Peak
        {
            get => peak;
            private set
            {
                if (SetProperty(ref peak, value * 2))
                {
                    PercentagePeak = peak;
                }
            }
        }

        public double PercentagePeak
        {
            get => percentagePeak;
            set => SetProperty(ref percentagePeak, value);
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

        public int ShareModeIndex
        {
            get => shareModeIndex;
            set => SetProperty(ref shareModeIndex, value);
        }

        public void Dispose()
        {
            Stop();
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
            capture.Dispose();
            capture = null;
        }

        private void Record()
        {
            capture = new WasapiCapture(SelectedDevice);
            capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
            if (SampleTypeIndex == 0)
            {
                capture.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
            }
            else
            {
                capture.WaveFormat = new WaveFormat(sampleRate, bitDepth, channelCount);
            }

            capture.StartRecording();
            capture.RecordingStopped += OnRecordingStopped;
            capture.DataAvailable += CaptureOnDataAvailable;
        }

        private void Stop()
        {
            capture?.StopRecording();
        }

        private void UpdatePeakMeter()
        {
            synchronizationContext.Post(_ => Peak = SelectedDevice.AudioMeterInformation.MasterPeakValue, null);
        }

        private void GetDefaultRecordingFormat(MMDevice value)
        {
            using var c = new WasapiCapture(value);
            SampleTypeIndex = c.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat ? 0 : 1;
            sampleRate = c.WaveFormat.SampleRate;
            BitDepth = c.WaveFormat.BitsPerSample;
            channelCount = c.WaveFormat.Channels;
        }
    }
}