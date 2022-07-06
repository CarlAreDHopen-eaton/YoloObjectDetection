namespace YoloObjectDetection
{
   public interface ICanvasHandler
   {
      /// <summary>
      /// Add a control to the canvas.
      /// </summary>
      /// <param name="control"></param>
      public void AddToCanvas(object control);
      /// <summary>
      /// Clear the canvas.
      /// </summary>
      public void Clear();
   }
}
