using System.Drawing;

namespace YoloObjectDetection
{
   public class BoundingBox
   {
      public BoundingBox()
      {
      }

      public BoundingBox(BoundingBoxDimensions dimensions, string label, float confidence, Color boxColor)
      {
         Dimensions = dimensions;
         Label = label;
         Confidence = confidence;
         BoxColor = boxColor;
      }

      /// <summary>
      /// The dimensions of the bounding box.
      /// </summary>
      public BoundingBoxDimensions Dimensions { get; set; }

      /// <summary>
      /// The label/category of the contents of bounding box.
      /// </summary>
      public string Label { get; set; }

      /// <summary>
      /// The confidence of the content classification.
      /// </summary>
      public float Confidence { get; set; }

      /// <summary>
      /// The color used when drawing the bounding box,
      /// </summary>
      public Color BoxColor { get; set; }

      /// <summary>
      /// The descrition including the label and confidence.
      /// </summary>
      public string Description
      {
         get
         {
            return $"{Label} ({Confidence * 100:0}%)";
         }
      }

      public RectangleF Rect
      {
         get { return new RectangleF(Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height); }
      }
   }
}