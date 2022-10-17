using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
         if (mCameraCaptureCancellationTokenSource == null)
         {
            mCameraCaptureCancellationTokenSource = new CancellationTokenSource();
            mUrl = strUrl;
            mConnectionTask = Task.Run(() => CaptureCamera(mCameraCaptureCancellationTokenSource.Token));
            //Task.Run(() => CaptureCamera(mCameraCaptureCancellationTokenSource.Token), mCameraCaptureCancellationTokenSource.Token);
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
            while (!token.IsCancellationRequested)
            {
               //Console.WriteLine("mVideoCapture.FrameCount:" + mVideoCapture.FrameCount);

               Mat orgMatrix = new Mat();
               if (!mVideoCapture.Read(orgMatrix))
               {
                  Thread.Sleep(50);
                  continue;
               }
               // TODO Figure out a better way to keep up with the stream.
               Mat temp = new Mat();
               if (mVideoCapture.Read(temp))
                  temp.Dispose();
               temp = new Mat();
               if (mVideoCapture.Read(temp))
                  temp.Dispose();

               await Application.Current.Dispatcher.InvokeAsync(() =>
               {
                  try
                  {
                     BitmapImage displayImageSource = new BitmapImage();
                     displayImageSource.BeginInit();
                     displayImageSource.CacheOption = BitmapCacheOption.OnLoad;
                     displayImageSource.StreamSource = orgMatrix.ToMemoryStream();
                     displayImageSource.EndInit();
                     DisplayImage.Source = displayImageSource;
                  }
                  catch (Exception ex)
                  {
                     Console.WriteLine($"Update image, catch exception triggered: {ex.Message}");
                  }
               });

               if (iFrameCount > 25 && mManualAnalyzeResetEvent.WaitOne(0))
               {
                  mManualAnalyzeResetEvent.Reset();
                  iFrameCount = 0;
                  Mat resizedMatrix = orgMatrix.Resize(new OpenCvSharp.Size(YoloV4Config.C_IMAGE_WIDTH, YoloV4Config.C_IMAGE_HEIGHT));
                  var size = new OpenCvSharp.Size(orgMatrix.Width, orgMatrix.Height);
                  _ = Task.Run(() => AnalyzeFrameAsync(yoloV4Detector, resizedMatrix, size));
               }
               iFrameCount++;
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
            Console.WriteLine($"Analyze image, catch exception triggered: {ex.Message}");
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
