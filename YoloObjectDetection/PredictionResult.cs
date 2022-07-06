using YoloObjectDetection.YoloV4;

namespace YoloObjectDetection
{
   public class PredictionResult
   {
      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="boundingBox"></param>
      /// <param name="className"></param>
      /// <param name="confidence"></param>
      public PredictionResult(float[] boundingBox, string className, float confidence, int classNameIndex)
      {
         BoundingBox = boundingBox;
         ClassName = className;
         Confidence = confidence;
         ClassNameIndex = classNameIndex;
      }

      /// <summary>
      /// x1, y1, x2, y2 in page coordinates.
      /// <para>left, top, right, bottom.</para>
      /// </summary>
      public float[] BoundingBox { get; }

      /// <summary>
      /// The type of object in the bounding box.
      /// </summary>
      public string ClassName { get; }

      /// <summary>
      /// Index that indicates the index of the class name <seealso cref="YoloV4ClassNames.ClassNames"/>.
      /// </summary>
      public int ClassNameIndex { get; }

      /// <summary>
      /// Confidence level.
      /// </summary>
      public float Confidence { get; }

   }


}
