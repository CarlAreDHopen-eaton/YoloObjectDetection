using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;
using System.Drawing;
using YoloObjectDetection.YoloV4;

namespace YoloObjectDetection
{
   public class BitmapData
   {
      [ColumnName("bitmap")]
      [ImageType(YoloV4Config.C_IMAGE_HEIGHT, YoloV4Config.C_IMAGE_WIDTH)]
      public Bitmap Image { get; set; }

      [ColumnName("width")]
      public float ImageWidth => Image.Width;

      [ColumnName("height")]
      public float ImageHeight => Image.Height;
   }
}
