using System.Drawing;

namespace YoloObjectDetection.Utils
{
   /// <summary>
   /// Class used to color code the bounding boxes. 
   /// </summary>
   public static class ColorArray
   {
      /// <summary>
      /// Predefined array of colors.
      /// </summary>
      private static readonly Color[] classColors = new Color[]
      {
         Color.Khaki,
         Color.Fuchsia,
         Color.Silver,
         Color.RoyalBlue,
         Color.Green,
         Color.DarkOrange,
         Color.Purple,
         Color.Gold,
         Color.Red,
         Color.Aquamarine,
         Color.Lime,
         Color.AliceBlue,
         Color.Sienna,
         Color.Orchid,
         Color.Tan,
         Color.LightPink,
         Color.Yellow,
         Color.HotPink,
         Color.OliveDrab,
         Color.SandyBrown,
         Color.DarkTurquoise
      };

      /// <summary>
      /// Gets a color for the given label index.
      /// </summary>
      /// <param name="index">Index of the label</param>
      /// <returns>A color</returns>
      public static Color GetColor(int index)
      {
         return index < classColors.Length ? classColors[index] : classColors[index % classColors.Length];
      }
   }
}
