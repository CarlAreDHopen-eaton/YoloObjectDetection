using System;
using System.Drawing;
using System.Linq;

namespace YoloObjectDetection.Utils
{
   internal static class MathUtils
   {
      /// <summary>
      /// expit = https://docs.scipy.org/doc/scipy/reference/generated/scipy.special.expit.html
      /// </summary>
      public static float Sigmoid(float x)
      {
         return 1f / (1f + (float)Math.Exp(-x));
      }

      public static float Sigmoid2(float value)
      {
         var k = (float)Math.Exp(value);
         return k / (1.0f + k);
      }

      public static float[] Softmax(float[] values)
      {
         var maxVal = values.Max();
         var exp = values.Select(v => Math.Exp(v - maxVal));
         var sumExp = exp.Sum();

         return exp.Select(v => (float)(v / sumExp)).ToArray();
      }

      public static float IntersectionOverUnion(RectangleF boundingBoxA, RectangleF boundingBoxB)
      {
         var areaA = boundingBoxA.Width * boundingBoxA.Height;

         if (areaA <= 0)
            return 0;

         var areaB = boundingBoxB.Width * boundingBoxB.Height;

         if (areaB <= 0)
            return 0;

         var minX = Math.Max(boundingBoxA.Left, boundingBoxB.Left);
         var minY = Math.Max(boundingBoxA.Top, boundingBoxB.Top);
         var maxX = Math.Min(boundingBoxA.Right, boundingBoxB.Right);
         var maxY = Math.Min(boundingBoxA.Bottom, boundingBoxB.Bottom);

         var intersectionArea = Math.Max(maxY - minY, 0) * Math.Max(maxX - minX, 0);

         return intersectionArea / (areaA + areaB - intersectionArea);
      }
   }
}
