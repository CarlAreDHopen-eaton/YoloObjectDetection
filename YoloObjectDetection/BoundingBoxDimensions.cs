namespace YoloObjectDetection
{
   public class BoundingBoxDimensions
   {
      public BoundingBoxDimensions()
      {
      }

      public BoundingBoxDimensions(float x, float y, float height, float width)
      {
         X = x;
         Y = y;
         Height = height;
         Width = width;
      }

      /// <summary>
      /// The X position
      /// </summary>
      public float X { get; set; }
      /// <summary>
      /// The Y position
      /// </summary>
      public float Y { get; set; }
      /// <summary>
      /// The height
      /// </summary>
      public float Height { get; set; }
      /// <summary>
      /// The width
      /// </summary>
      public float Width { get; set; }
   }
}