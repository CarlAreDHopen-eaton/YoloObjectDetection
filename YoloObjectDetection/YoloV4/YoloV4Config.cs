using System.Collections.Generic;

namespace YoloObjectDetection.YoloV4
{
   public static class YoloV4Config
   {

      public const int C_ANCORS_COUNT = 3;
      public const string C_INPUT_COLUMN_NAME = "bitmap";
      public const string C_OUTPUT_COLUMN_NAME = "input_1:0";

      public static readonly int[] shapes = new int[] { 52, 26, 13 };

      /// <summary>
      /// More info: https://github.com/hunglc007/tensorflow-yolov4-tflite/blob/master/data/anchors/yolov4_anchors.txt
      /// </summary>
      public static readonly float[][][] ANCHORS = new float[][][]
      {
            new float[][]
            {
               new float[] { 12, 16 },
               new float[] { 19, 36 },
               new float[] { 40, 28 }
            },
            new float[][]
            {
               new float[] { 36, 75 },
               new float[] { 76, 55 },
               new float[] { 72, 146 }
            },
            new float[][]
            {
               new float[] { 142, 110 },
               new float[] { 192, 243 },
               new float[] { 459, 401 }
            }
      };

      /// <summary>
      /// More info:
      /// https://github.com/hunglc007/tensorflow-yolov4-tflite/blob/9f16748aa3f45ff240608da4bd9b1216a29127f5/core/config.py#L18
      /// </summary>
      public static readonly float[] STRIDES = new float[] { 8, 16, 32 };

      /// <summary>
      /// More info
      /// https://github.com/hunglc007/tensorflow-yolov4-tflite/blob/9f16748aa3f45ff240608da4bd9b1216a29127f5/core/config.py#L20
      /// </summary>
      public static readonly float[] XYSCALE = new float[] { 1.2f, 1.1f, 1.05f };

      /// <summary>
      /// Height in pixes 
      /// </summary>
      public const int C_IMAGE_HEIGHT = 416;

      /// <summary>
      /// Width in pixes 
      /// </summary>
      public const int C_IMAGE_WIDTH = 416;

      public static readonly string[] INPUT_COL_NAMES = new[] { "input_1:0" };

      public static readonly string[] OUTPUT_COL_NAMES = new[] { "Identity:0", "Identity_1:0", "Identity_2:0" };

      public static Dictionary<string, int[]> SHAPE_DICTIONARY = new Dictionary<string, int[]>()
         {
            { "input_1:0", new[] { 1, 416, 416, 3 } },
            { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
            { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
            { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
         };
   }
}
