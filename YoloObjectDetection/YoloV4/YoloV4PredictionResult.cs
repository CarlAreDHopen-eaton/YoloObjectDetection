using System.Linq;
using YoloObjectDetection.Defines;

namespace YoloObjectDetection.YoloV4
{
   public class YoloV4PredictionResult : PredictionResult
   {
      public YoloV4PredictionResult(float[] boundingBox, string className, float confidence, int classNameIndex)
         : base(boundingBox, className, confidence, classNameIndex)
      {
      }

      /// <summary>
      /// Gets a new result object.
      /// </summary>
      /// <param name="result"></param>
      /// <returns></returns>
      public static YoloV4PredictionResult GetYoloResult(float[] result)
      {
         float[] boundingBox = result.Take(4).ToArray();
         float confidence = result[(int)ResultArrayDefinition.Confidence];
         int classNameIndex = (int)result[(int)ResultArrayDefinition.ClassNameIndex];
         string className = Model.ClassNames[classNameIndex];
         return new YoloV4PredictionResult(boundingBox, className, confidence, classNameIndex);
      }
   }


}
