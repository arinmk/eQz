using System;
using NAudio.Dsp;
using System.Linq;
using NAudio.Wave;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraCharts;
using System.Linq.Expressions;
using DevExpress.Utils.Extensions;


namespace eQz
{
    public partial class eQz : DevExpress.XtraEditors.XtraForm
    {
        private double bandaV = 0.00001d;
        private double cuurValue = 1.9d;
        private int fftLength = 1024; // Make sure this is a power of 2
        private Thread myThread;
        private string outputFilePath = "eQzGarbage.wav"; // Consider making this configurable

        private bool up;
        private IWaveIn waveIn;
        private WaveFileWriter writer;
        public bool Default = false;
        public string Device;

        public float[] frequencyBands;
        public double gaba = 10;

        public eQz()
        {
            InitializeComponent();
            AudioCapture();
        }

        private void AudioCapture()
        {
            try
            {
                waveIn = new WasapiLoopbackCapture();

                waveIn.DataAvailable += OnDataAvailable;
                waveIn.RecordingStopped += OnRecordingStopped;
                waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error initializing audio capture: {ex.Message}");
                Dispose(true);
            }
        }
        private void defaults()
        {

            if (Default)
            {
                simpleButton1.ForeColor = Color.Red;
                if (myThread == null || !myThread.IsAlive)
                {
                    try
                    {
                        myThread = new Thread(() =>
                        {

                            lock (this)
                            {
                                while (Default)
                                {
                                    Thread.Sleep(10);
                                    cuurValue = 2.1;
                                    bandaV = 0.00001d;
                                    gaba = 70;
                                }
                            }

                        })
                        { IsBackground = false };
                        myThread.Start();
                    }
                    catch { Dispose(true); }
                }
            }
            else
            {
                simpleButton1.ForeColor = Color.White;

                if (myThread != null && myThread.IsAlive)
                {
                    myThread.Yield();
                }
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = " eQz [Displaying Algorithmic Shift]";
            var sview = (SideBySideBarSeriesView)chartControl1.Series.First().SeriesView;
            chartControl1.Series.Clear();
            for (int i = 0; i < 100; i++)
            {
                Series series = new Series("", ViewType.Bar);
                sview.BarWidth = 0.8D;
                series.View = sview;
                series.Points.Add(new SeriesPoint(i, 0));
                chartControl1.Series.Add(series);
                sview.EqualBarWidth = false;

            }
        }
        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                if (writer == null)
                {
                    writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat);
                }

                writer.Write(e.Buffer, 0, e.BytesRecorded);

                frequencyBands = PerformFFT(e.Buffer, e.BytesRecorded);

            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error processing audio data: {ex.Message}");
                Dispose(true);
            }
        }


        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }

            if (waveIn != null)
            {
                waveIn.Dispose();
            }
        }
        private float[] PerformFFT(byte[] buffer, int bytesRecorded)
        {

            var waveBuffer = new WaveBuffer(buffer);
            var complexBuffer = new NAudio.Dsp.Complex[fftLength];
            for (int i = 0; i < fftLength; i++)
            {
                if (i < bytesRecorded / 4)
                {
                    complexBuffer[i] = new NAudio.Dsp.Complex();

                    complexBuffer[i].X = waveBuffer.FloatBuffer[i];
                }
                else
                    complexBuffer[i] = new NAudio.Dsp.Complex();
            }

            if (cuurValue >= 3d)
            {
                up = false;
            }
            else if (cuurValue <= 1.9d)
            {
                up = true;
            }


            if (up)
            {
                cuurValue += 0.0005d;
                bandaV += 0.000001d;
            }
            else
            {
                cuurValue -= 0.0005d;
                bandaV -= 0.000001d;
            }


            if (cuurValue < 2)
            {
                if (up == true)
                {
                    gaba += 0.4;
                }
                else
                {
                    gaba -= 0.1;
                }
            }
            else if (cuurValue >= 2 && cuurValue <= 2.1)
            {
                gaba = 90;


            }
            else if (cuurValue >= 2.15 && cuurValue <= 2.25)
            {
                gaba = 10;

            }
            else if (cuurValue >= 2.3)
            {
                gaba = 5;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(fftLength, cuurValue), complexBuffer);


            float[] magnitude = new float[fftLength / 2];
            for (int i = 0; i < magnitude.Length; i++)
            {
                magnitude[i] = (float)Math.Sqrt((complexBuffer[i].X * complexBuffer[i].X) +
                                                (complexBuffer[i].Y * complexBuffer[i].Y));
            }

            int numBands = 100;
            var bandWidth = ((float)fftLength / 2) / numBands;
            float[] bandAverages = new float[numBands];

            for (int band = 0; band < numBands; band++)
            {
                float sum = 0;
                var startFreq = band * bandWidth;
                var endFreq = (band + 1) * bandWidth;

                for (var i = startFreq; i < endFreq; i++)
                {
                    sum += magnitude[(int)i];
                }

                bandAverages[band] = (float)Math.Max((sum / bandWidth) * gaba, bandaV);
            }

            return bandAverages;
        }
        private void Simplewarn()
        { 
        
        
        }
        private void simpleButton1_Click(object sender, EventArgs e)
        {
            Default = !Default;
            defaults();
            if (Default == true)
            {
                this.Text = "eQz [Displaying Locked Default]";
            }
            else if (Default == false)
            {
                this.Text = "eQz [Displaying Shifter Pattern]";
            }

        }
        private void textEdit1_EditValueChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var DIx = Math.Round(cuurValue, 3);

            var DIx2 = Math.Round(bandaV, 5);
            UpdateChart();
            textEdit1.Text = $"Master Visualiser | Buffer Shift Value: {DIx:n3} | Band GB Value: {gaba:n1} | Band AV Shift Value: {DIx2:n5}";
        }

        private void UpdateChart()
        {
            if (frequencyBands != null)
            {
                for (int i = 0; i < frequencyBands.Length; i++)
                {
                    chartControl1.Series[i].Points.BeginUpdate();
                    chartControl1.Series[i].Points[0].Values.SetValue(frequencyBands[i], 0);
                    chartControl1.Series[i].Points.EndUpdate();
                  
                }
            }
        }
        private void updateGauges()
        {
            textEdit2.Visible = false;
            textEdit3.Visible = false;
            if (frequencyBands != null)
            {
                for (int i = 0; i < frequencyBands.Length; i++)
                {
                    progressBarControl1.EditValue = (frequencyBands[i] * 20);
                    progressBarControl3.EditValue = (frequencyBands[i] * (cuurValue * 30));
                    if (frequencyBands[i] > 1)
                    { 
                    textEdit2.Visible = true;
                    }
                    if (frequencyBands[i] > 2)
                    { 
                    textEdit3.Visible = true;
                    }
                }
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            updateGauges();
        }
    }
}