using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using YoloObjectDetection.Defines;
using YoloObjectDetection.Interfaces;
using YoloObjectDetection.Utils;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;

namespace YoloObjectDetection.TinyYoloV2
{
   public class TinyYoloV2Detector : BaseYoloDetector, IYoloDetector
   {
      #region Members

      private const string mModel = "Models\\tinyYoloV2.onnx";
      private readonly PredictionEngine<BitmapData, TinyYoloV2Prediction> mPredictionEngine;

      #endregion

      #region Constructors 

      public TinyYoloV2Detector()
      {
         if (mPredictionEngine == null)
         {
            mPredictionEngine = GetPredictionEngine();
         }
      }

      #endregion

      #region Private methods

      private PredictionEngine<BitmapData, TinyYoloV2Prediction> GetPredictionEngine()
      {
         MLContext mlContext = new MLContext();

         // Init the class name array
         Model.ModuleType = ModelType.TinyYoloV2;

         // Define scoring pipeline
         var pipeline = GetPipeline(mlContext);

         var data = mlContext.Data.LoadFromEnumerable(new List<BitmapData>());

         var model = pipeline.Fit(data);

         // Create prediction engine
         var predictionEngine = mlContext.Model.CreatePredictionEngine<BitmapData, TinyYoloV2Prediction>(model);

         // Return the prediction engine
         return predictionEngine;

      }

      private EstimatorChain<TransformerChain<OnnxTransformer>> GetPipeline(MLContext mlContext)
      {
         string mModelPath = Directory.GetCurrentDirectory() + "\\" + mModel;
         EstimatorChain<TransformerChain<OnnxTransformer>> pipline = mlContext.Transforms.ResizeImages(
            TinyYoloV2Config.ModelOutput,
            TinyYoloV2Config.C_IMAGE_WIDTH,
            TinyYoloV2Config.C_IMAGE_HEIGHT,
            TinyYoloV2Config.ModelInput,
            ResizingKind.IsoPad)
         .Append(mlContext.Transforms.ExtractPixels(
            TinyYoloV2Config.ModelInput)
         .Append(mlContext.Transforms.ApplyOnnxModel(
            new[] { TinyYoloV2Config.ModelOutput },
            new[] { TinyYoloV2Config.ModelInput },
            mModelPath)));

         return pipline;
      }

      private IReadOnlyList<PredictionResult> GetPredictionResults(PredictionEngine<BitmapData, TinyYoloV2Prediction> predictionEngine, Bitmap bitmap)
      {
         TinyYoloV2Prediction prediction = predictionEngine.Predict(new BitmapData() { Image = bitmap });
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

      //private IEnumerable<float[]> PredictDataUsingModel(IDataView testData, ITransformer model)
      //{
      //   IDataView scoredData = model.Transform(testData);
      //   IEnumerable<float[]> probabilities = scoredData.GetColumn<float[]>(TinyYoloV2Config.ModelOutput);
      //   return probabilities;
      //}

   }
}
