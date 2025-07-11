using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YoloObjectDetection;
using YoloObjectDetection.Interfaces;
using YoloObjectDetection.YoloV4;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Text.Json;

namespace YoloObjectDetectionApp
{
   public partial class MainWindow : System.Windows.Window, ICanvasHandler
   {
      private VideoCapture mVideoCapture;
      private string mUrl = @"rtsp://SomeUser:SomePassword@10.0.0.166:554/live/camera1/stream1";
      private CancellationTokenSource mCameraCaptureCancellationTokenSource;
      private readonly ManualResetEvent mManualAnalyzeResetEvent = new ManualResetEvent(true);
      private Task mConnectionTask;
      private WriteableBitmap _writeableBitmap;
      private int mWbPixelWidth = 0;
      private int mWbPixelHeight = 0;
      private int mVideoFrameCounter = 0;
      private int mAnalyticsFrameCounter = 0;
      private double mVideoFps;
      private double mAnalyticsFps = 0;
      private DateTime mLastVideoFpsUpdate = DateTime.Now;
      private DateTime mLastAnalyticsFpsUpdate = DateTime.Now;
      private double mLastVideoDecodeMs = 0;
      private double mLastInferenceMs = 0;
      private static readonly string SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YoloObjectDetectionApp", "usersettings.json");
      private UserSettings mUserSettings = new UserSettings();

      public MainWindow()
      {
         InitializeComponent();
         LoadSettings();
         if (ShowStatsCheckBox != null)
         {
            ShowStatsCheckBox.IsChecked = mUserSettings.ShowVideoStats;
            ShowStatsCheckBox.Checked += (s, e) => ToggleStatsOverlay(true);
            ShowStatsCheckBox.Unchecked += (s, e) => ToggleStatsOverlay(false);
         }
         if (EnableInferenceCheckBox != null)
         {
            EnableInferenceCheckBox.IsChecked = mUserSettings.EnableInference;
            EnableInferenceCheckBox.Checked += (s, e) => ToggleInference(true);
            EnableInferenceCheckBox.Unchecked += (s, e) => ToggleInference(false);
         }
         if (StatsOverlay != null)
            StatsOverlay.Visibility = mUserSettings.ShowVideoStats ? Visibility.Visible : Visibility.Collapsed;

         string[] args = Environment.GetCommandLineArgs();
         if (args.Length == 2)
         {
            mUrl = args[1];
         }
         else if (!string.IsNullOrWhiteSpace(mUserSettings.ConnectionUrl))
         {
            mUrl = mUserSettings.ConnectionUrl;
         }
         else
         {
            mUrl = new UserSettings().ConnectionUrl;
         }
         ConnectionUri.Text = mUrl;
      }

      private void ToggleStatsOverlay(bool show)
      {
         mUserSettings.ShowVideoStats = show;
         SaveSettings();
         StatsOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
      }

      private void ToggleInference(bool enable)
      {
         mUserSettings.EnableInference = enable;
         SaveSettings();
         if (!enable)
         {
            ClearCanvas();
            if (AnalyticsFpsText != null)
                AnalyticsFpsText.Text = "Analytics FPS: Disabled";
            if (InferenceTimeText != null)
                InferenceTimeText.Text = "Inference: Disabled";
         }
      }

      protected override void OnClosing(CancelEventArgs e)
      {
         StopCameraCapture();
         base.OnClosing(e);
      }

      private void LoadSettings()
      {
         try
         {
            if (File.Exists(SettingsFilePath))
            {
               var json = File.ReadAllText(SettingsFilePath);
               mUserSettings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Failed to load settings: {ex.Message}");
            mUserSettings = new UserSettings();
         }
      }

      private void SaveSettings()
      {
         try
         {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
            var json = JsonSerializer.Serialize(mUserSettings);
            File.WriteAllText(SettingsFilePath, json);
         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
         }
      }

      private void StartCameraCapture(string strUrl)
      {
         Debug.WriteLine($"StartCameraCapture called with URL: {strUrl}");
         if (mCameraCaptureCancellationTokenSource == null)
         {
            mCameraCaptureCancellationTokenSource = new CancellationTokenSource();
            mUrl = strUrl;
            mUserSettings.ConnectionUrl = strUrl;
            SaveSettings();
            mConnectionTask = Task.Run(() => CaptureCamera(mCameraCaptureCancellationTokenSource.Token));
         }
      }

      private async void StopCameraCapture()
      {
         if (mConnectionTask != null)
         {
            mCameraCaptureCancellationTokenSource?.Cancel();
            await mConnectionTask;
            mCameraCaptureCancellationTokenSource = null; // Ensure this is reset immediately
            // Do not set mUrl to null here
            ClearCanvas();
            DisplayImage.Source = null;
            _writeableBitmap = null; // Reset bitmap so it reinitializes on reconnect
            mWbPixelWidth = 0;
            mWbPixelHeight = 0;
         }
      }

      private async Task CaptureCamera(CancellationToken token)
      {
         if (mVideoCapture == null)
            mVideoCapture = new VideoCapture();

         mVideoCapture.Open(mUrl);
         mVideoCapture.Set(VideoCaptureProperties.BufferSize, 1); // Set buffer size to 1 for minimal latency
         Debug.WriteLine($"Camera FPS: {mVideoCapture.Get(VideoCaptureProperties.Fps)}");
         Debug.WriteLine($"Frame size: {mVideoCapture.FrameWidth}x{mVideoCapture.FrameHeight}");
         bool bIsOpen = mVideoCapture.IsOpened();
         if (bIsOpen)
         {
            IYoloDetector yoloV4Detector = new YoloV4Detector();
            int iFrameCount = int.MaxValue;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // --- Video decode timing ---
                    var swDecode = Stopwatch.StartNew();
                    bool grabbed = mVideoCapture.Grab();
                    if (!grabbed)
                    {
                        Debug.WriteLine("Grab failed, sleeping...");
                        Thread.Sleep(50);
                        continue;
                    }
                    Mat orgMatrix = new Mat();
                    bool retrieved = mVideoCapture.Retrieve(orgMatrix);
                    swDecode.Stop();
                    mLastVideoDecodeMs = swDecode.Elapsed.TotalMilliseconds;
                    if (!retrieved || orgMatrix.Empty())
                    {
                        Debug.WriteLine("No frame retrieved, sleeping...");
                        Thread.Sleep(50);
                        continue;
                    }

                    // --- Video FPS counter ---
                    mVideoFrameCounter++;
                    var now = DateTime.Now;
                    if ((now - mLastVideoFpsUpdate).TotalSeconds >= 1)
                    {
                        mVideoFps = mVideoFrameCounter / (now - mLastVideoFpsUpdate).TotalSeconds;
                        mVideoFrameCounter = 0;
                        mLastVideoFpsUpdate = now;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            VideoFpsText.Text = $"Video FPS: {mVideoFps:F1}";
                            VideoDecodeTimeText.Text = $"Video Decode: {mLastVideoDecodeMs:F1} ms";
                        });
                    }

                    // Update UI on every frame (no throttling)
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            // Initialize WriteableBitmap if needed
                            if (_writeableBitmap == null || mWbPixelWidth != orgMatrix.Width || mWbPixelHeight != orgMatrix.Height)
                            {
                                mWbPixelWidth = orgMatrix.Width;
                                mWbPixelHeight = orgMatrix.Height;
                                _writeableBitmap = new WriteableBitmap(
                                    orgMatrix.Width,
                                    orgMatrix.Height,
                                    96, 96, // DPI
                                    PixelFormats.Bgr24, // OpenCvSharp default
                                    null);
                                DisplayImage.Source = _writeableBitmap;
                            }
                            // Copy Mat data to WriteableBitmap
                            var rect = new Int32Rect(0, 0, orgMatrix.Width, orgMatrix.Height);
                            int stride = orgMatrix.Width * 3; // 3 bytes per pixel for Bgr24
                            _writeableBitmap.Lock();
                            _writeableBitmap.WritePixels(rect, orgMatrix.Data, stride * orgMatrix.Height, stride);
                            _writeableBitmap.AddDirtyRect(rect);
                            _writeableBitmap.Unlock();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Update image, catch exception triggered: {ex.Message}");
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);

                    if (iFrameCount > 2 && mManualAnalyzeResetEvent.WaitOne(0))
                    {
                        mManualAnalyzeResetEvent.Reset();
                        iFrameCount = 0;
                        if (mUserSettings.EnableInference)
                        {
                            Mat resizedMatrix = orgMatrix.Resize(new OpenCvSharp.Size(YoloV4Config.C_IMAGE_WIDTH, YoloV4Config.C_IMAGE_HEIGHT));
                            var size = new OpenCvSharp.Size(orgMatrix.Width, orgMatrix.Height);
                            _ = Task.Run(() => AnalyzeFrameAsync(yoloV4Detector, resizedMatrix, size));
                        }
                    }
                    iFrameCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in capture loop: {ex.Message}\n{ex.StackTrace}");
            }
            mVideoCapture.Release();
         }
         mVideoCapture.Dispose();
         mVideoCapture = null;
         mCameraCaptureCancellationTokenSource = null;
      }

      private async Task AnalyzeFrameAsync(IYoloDetector yoloDetector, Mat imageMatrix, OpenCvSharp.Size size)
      {
         try
         {
            var swInference = Stopwatch.StartNew();
            mAnalyticsFrameCounter++;
            var now = DateTime.Now;
            if ((now - mLastAnalyticsFpsUpdate).TotalSeconds >= 1)
            {
                mAnalyticsFps = mAnalyticsFrameCounter / (now - mLastAnalyticsFpsUpdate).TotalSeconds;
                mAnalyticsFrameCounter = 0;
                mLastAnalyticsFpsUpdate = now;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AnalyticsFpsText.Text = $"Analytics FPS: {mAnalyticsFps:F1}";
                    InferenceTimeText.Text = $"Inference: {mLastInferenceMs:F1} ms";
                });
            }

            List<BoundingBox> boundingBoxes = yoloDetector.DetectObjectsUsingModel(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(imageMatrix));
            swInference.Stop();
            mLastInferenceMs = swInference.Elapsed.TotalMilliseconds;
            imageMatrix.Dispose();

            if (boundingBoxes.Count > 0)
            {
               await Application.Current.Dispatcher.InvokeAsync(() =>
               {
                  OverlayHelper.DrawOverlays(this, boundingBoxes, DisplayImage.ActualHeight, DisplayImage.ActualWidth);
               });
            }
         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Analyze image, catch exception triggered: {ex.Message}");
         }
         finally
         {
            mManualAnalyzeResetEvent.Set();
         }

      }

      public void AddToCanvas(object control)
      {
         if (control is Rectangle rectangle)
            DisplayImageCanvas.Children.Add(rectangle);
         if (control is TextBlock textBlock)
            DisplayImageCanvas.Children.Add(textBlock);
         else
            Console.WriteLine("Object type not supported ");
      }

      public void ClearCanvas()
      {
         DisplayImageCanvas.Children.Clear();
      }

      private void ConnectButton_Click(object sender, RoutedEventArgs e)
      {
         StartCameraCapture(ConnectionUri.Text);
      }

      private void DisconnectButton_Click(object sender, RoutedEventArgs e)
      {
         StopCameraCapture();
      }
   }
}
