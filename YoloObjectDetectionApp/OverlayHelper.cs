using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using YoloObjectDetection;
using YoloObjectDetection.YoloV4;

namespace YoloObjectDetectionApp
{
   internal class OverlayHelper
   {
      public static void DrawOverlays(ICanvasHandler canvasHandler, List<BoundingBox> boundingBoxes, double originalHeight, double originalWidth)
      {
         canvasHandler.Clear();

         foreach (BoundingBox box in boundingBoxes)
         {
            // process output boxes
            double x = Math.Max(box.Dimensions.X, 0);
            double y = Math.Max(box.Dimensions.Y, 0);
            double width = Math.Min(originalWidth - x, box.Dimensions.Width);
            double height = Math.Min(originalHeight - y, box.Dimensions.Height);

            // fit to current image size
            x = originalWidth * x / YoloV4Config.C_IMAGE_WIDTH;
            y = originalHeight * y / YoloV4Config.C_IMAGE_HEIGHT;
            width = originalWidth * width / YoloV4Config.C_IMAGE_WIDTH;
            height = originalHeight * height / YoloV4Config.C_IMAGE_HEIGHT;

            var boxColor = box.BoxColor.ToMediaColor();

            var objBox = GetObjBox(x, y, width, height, boxColor);

            var objDescription = new TextBlock
            {
               Margin = new Thickness(x + 4, y + 4, 0, 0),
               Text = box.Description,
               FontWeight = FontWeights.Bold,
               Width = 126,
               Height = 21,
               TextAlignment = TextAlignment.Center
            };

            var objDescriptionBackground = new Rectangle
            {
               Width = 134,
               Height = 29,
               Fill = new SolidColorBrush(boxColor),
               Margin = new Thickness(x, y, 0, 0)
            };

            canvasHandler.AddToCanvas(objDescriptionBackground);
            canvasHandler.AddToCanvas(objDescription);
            canvasHandler.AddToCanvas(objBox);
         }
      }

      private static Rectangle GetObjBox(double x, double y, double width, double height, System.Windows.Media.Color boxColor)
      {
         return new Rectangle
         {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush(Colors.Transparent),
            Stroke = new SolidColorBrush(boxColor),
            StrokeThickness = 2.0,
            Margin = new Thickness(x, y, 0, 0)
         };
      }
   }
}
