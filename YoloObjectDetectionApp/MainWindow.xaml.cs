using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
      private int _wbPixelWidth = 0;
      private int _wbPixelHeight = 0;

      public MainWindow()
      {
         InitializeComponent();

         string[] args = Environment.GetCommandLineArgs();
         if (args.Length == 2)
         {
            mUrl = args[1];
         }
         ConnectionUri.Text = mUrl;
      }

      protected override void OnClosing(CancelEventArgs e)
      {
         StopCameraCapture();
         base.OnClosing(e);
      }

      private void StartCameraCapture(string strUrl)
      {
         Debug.WriteLine($"StartCameraCapture called with URL: {strUrl}");
         if (mCameraCaptureCancellationTokenSource == null)
         {
            mCameraCaptureCancellationTokenSource = new CancellationTokenSource();
            mUrl = strUrl;
            mConnectionTask = Task.Run(() => CaptureCamera(mCameraCaptureCancellationTokenSource.Token));
         }
      }

      private async void StopCameraCapture()
      {
         if (mConnectionTask != null)
         {
            mCameraCaptureCancellationTokenSource?.Cancel();
            await mConnectionTask;
            //mCameraCaptureCancellationTokenSource = null;
            mUrl = null;
            ClearCanvas();
            DisplayImage.Source = null;
         }
      }

      private async Task CaptureCamera(CancellationToken token)
      {
         if (mVideoCapture == null)
            mVideoCapture = new VideoCapture();

         mVideoCapture.Open(mUrl);
         mVideoCapture.Set(VideoCaptureProperties.BufferSize, 3);
         bool bIsOpen = mVideoCapture.IsOpened();
         if (bIsOpen)
         {
            IYoloDetector yoloV4Detector = new YoloV4Detector();
            int iFrameCount = int.MaxValue;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    bool grabbed = mVideoCapture.Grab();
                    if (!grabbed)
                    {
                        Debug.WriteLine("Grab failed, sleeping...");
                        Thread.Sleep(50);
                        continue;
                    }
                    Mat orgMatrix = new Mat();
                    bool retrieved = mVideoCapture.Retrieve(orgMatrix);
                    if (!retrieved || orgMatrix.Empty())
                    {
                        Debug.WriteLine("No frame retrieved, sleeping...");
                        Thread.Sleep(50);
                        continue;
                    }

                    // Update UI on every frame (no throttling)
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            // Initialize WriteableBitmap if needed
                            if (_writeableBitmap == null || _wbPixelWidth != orgMatrix.Width || _wbPixelHeight != orgMatrix.Height)
                            {
                                _wbPixelWidth = orgMatrix.Width;
                                _wbPixelHeight = orgMatrix.Height;
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
                        Mat resizedMatrix = orgMatrix.Resize(new OpenCvSharp.Size(YoloV4Config.C_IMAGE_WIDTH, YoloV4Config.C_IMAGE_HEIGHT));
                        var size = new OpenCvSharp.Size(orgMatrix.Width, orgMatrix.Height);
                        _ = Task.Run(() => AnalyzeFrameAsync(yoloV4Detector, resizedMatrix, size));
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
            List<BoundingBox> boundingBoxes = yoloDetector.DetectObjectsUsingModel(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(imageMatrix));
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
