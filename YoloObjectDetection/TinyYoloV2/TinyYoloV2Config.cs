namespace YoloObjectDetection.TinyYoloV2
{
   internal class TinyYoloV2Config
   {
      /// <summary>
      /// More Info: https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx
      /// </summary>
      public static readonly float[] ANCHORS = new float[]
      {
         1.08F, 1.19F, 3.42F, 4.41F, 6.63F, 11.38F, 9.42F, 5.11F, 16.62F, 10.52F
      };

      /// <summary>
      /// ROW_COUNT, COL_COUNT, BOXES_PER_CELL
      /// </summary>
      public static readonly int[] shapes = new int[] { 13, 13, 5 };

      public static readonly int ROW_COUNT = shapes[0];
      public static readonly int COL_COUNT = shapes[1];
      public static readonly int BOXES_PER_CELL = shapes[2];

      /// <summary>
      /// Height in pixes 
      /// </summary>
      public const int C_IMAGE_HEIGHT = 416;

      /// <summary>
      /// Width in pixes 
      /// </summary>
      public const int C_IMAGE_WIDTH = 416;

      // input tensor name
      public const string ModelInput = "image";

      // output tensor name
      public const string ModelOutput = "grid";

   }
}
