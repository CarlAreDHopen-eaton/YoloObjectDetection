using YoloObjectDetection.Defines;
using YoloObjectDetection.TinyYoloV2;
using YoloObjectDetection.YoloV4;

namespace YoloObjectDetection
{
   internal class Model
   {
      public static ModelType ModuleType;

      public static string[] ClassNames
      {
         get
         {
            if (ModuleType == ModelType.TinyYoloV2)
               return TinyYoloV2ClassNames.ClassNames;
            if (ModuleType == ModelType.YoloV4)
               return YoloV4ClassNames.ClassNames;
            return null;
         }
      }
   }
}