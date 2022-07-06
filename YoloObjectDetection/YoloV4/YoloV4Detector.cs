using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using YoloObjectDetection.Defines;
using YoloObjectDetection.Interfaces;
using YoloObjectDetection.Utils;
using YoloObjectDetection.YoloV4;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;

namespace YoloObjectDetection
{
   /// <summary>
   /// Model available here:
   /// https://github.com/onnx/models/tree/master/vision/object_detection_segmentation/yolov4
   /// </summary>
   public class YoloV4Detector : BaseYoloDetector, IYoloDetector
   {
      #region Members

      private const string mModel = "Models\\yolov4.onnx";
      private readonly PredictionEngine<BitmapData, YoloV4Prediction> mPredictionEngine;

      #endregion

      #region Constructors 

      public YoloV4Detector()
      {
         if (mPredictionEngine == null)
         {
            mPredictionEngine = GetPredictionEngine();
         }
      }

      #endregion

      #region Private methods

      private PredictionEngine<BitmapData, YoloV4Prediction> GetPredictionEngine()
      {
         MLContext mlContext = new MLContext();

         // Init the class name array
         Model.ModuleType = ModelType.YoloV4;

         // Define scoring pipeline
         var pipeline = GetPipeline(mlContext);

         // Fit on empty list to obtain input data schema
         var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<BitmapData>()));

         // Create prediction engine
         var predictionEngine = mlContext.Model.CreatePredictionEngine<BitmapData, YoloV4Prediction>(model);

         // Return the prediction engine
         return predictionEngine;
      }

      private EstimatorChain<OnnxTransformer> GetPipeline(MLContext mlContext)
      {
         string mModelPath = Directory.GetCurrentDirectory() + "\\" + mModel;
         return mlContext.Transforms.ResizeImages(
            YoloV4Config.C_OUTPUT_COLUMN_NAME,
            YoloV4Config.C_IMAGE_WIDTH,
            YoloV4Config.C_IMAGE_HEIGHT,
            YoloV4Config.C_INPUT_COLUMN_NAME,
            ResizingKind.IsoPad)
         .Append(mlContext.Transforms.ExtractPixels(
            YoloV4Config.C_OUTPUT_COLUMN_NAME,
            scaleImage: 1f / 255f,
            interleavePixelColors: true))
         .Append(mlContext.Transforms.ApplyOnnxModel(
            YoloV4Config.OUTPUT_COL_NAMES,
            YoloV4Config.INPUT_COL_NAMES,
            mModelPath,
            YoloV4Config.SHAPE_DICTIONARY,
            recursionLimit: 100));
      }

      private IReadOnlyList<PredictionResult> GetPredictionResults(PredictionEngine<BitmapData, YoloV4Prediction> predictionEngine, Bitmap bitmap)
      {
         YoloV4Prediction prediction = predictionEngine.Predict(new BitmapData() { Image = bitmap });
         IReadOnlyList<PredictionResult> results = prediction.GetResults(0.05f, 0.1f);
         return results;
      }

      #endregion

      public List<BoundingBox> DetectObjectsUsingModel(Bitmap bitmap)
      {
         IReadOnlyList<PredictionResult> results = GetPredictionResults(mPredictionEngine, bitmap);

         List<BoundingBox> returnResults = new List<BoundingBox>();
         foreach (PredictionResult result in results)
         {
            BoundingBox box = new BoundingBox()
            {
               Dimensions = GetDimensions(result),
               Label = result.ClassName,
               Confidence = result.Confidence,
               BoxColor = ColorArray.GetColor(result.ClassNameIndex)
            };
            returnResults.Add(box);
         }
         return returnResults;
      }
   }
}
