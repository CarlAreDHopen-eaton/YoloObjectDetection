using System.Collections.Generic;
using System.Drawing;

namespace YoloObjectDetection.Interfaces
{
   public interface IYoloDetector
   {
      public List<BoundingBox> DetectObjectsUsingModel(Bitmap bitmap);

   }

   public class BaseYoloDetector
   {
      public static BoundingBoxDimensions GetDimensions(PredictionResult result)
      {
         var x1 = result.BoundingBox[0];
         var y1 = result.BoundingBox[1];
         var x2 = result.BoundingBox[2];
         var y2 = result.BoundingBox[3];
         return new BoundingBoxDimensions()
         {
            X = x1,
            Y = y1,
            Width = x2 - x1,
            Height = y2 - y1
         };
      }

   }
}
