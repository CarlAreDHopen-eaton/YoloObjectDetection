using System;
using System.Collections.Generic;
using System.Linq;
using YoloObjectDetection.Utils;

/// <summary>
/// Adapted from https://github.com/NhatTanVu/Object-Detection-ML.NET.
/// </summary>
namespace YoloObjectDetection.TinyYoloV2
{
   internal class TinyYoloV2Prediction : IPrediction
   {
      public const int CHANNEL_COUNT = 125;
      public const int BOX_INFO_FEATURE_COUNT = 5;
      public const int CLASS_COUNT = 20;
      public const float CELL_WIDTH = 32;
      public const float CELL_HEIGHT = 32;

      private readonly int channelStride = TinyYoloV2Config.ROW_COUNT * TinyYoloV2Config.COL_COUNT;

      //private float[] anchors = new float[]
      //{
      //   1.08F, 1.19F, 3.42F, 4.41F, 6.63F, 11.38F, 9.42F, 5.11F, 16.62F, 10.52F
      //};

      private int GetOffset(int x, int y, int channel)
      {
         // YOLO outputs a tensor that has a shape of 125x13x13, which 
         // WinML flattens into a 1D array.  To access a specific channel 
         // for a given (x,y) cell position, we need to calculate an offset
         // into the array
         return (channel * this.channelStride) + (y * TinyYoloV2Config.COL_COUNT) + x;
      }

      private BoundingBoxDimensions ExtractBoundingBoxDimensions(float[] modelOutput, int x, int y, int channel)
      {
         return new BoundingBoxDimensions
         {
            X = modelOutput[GetOffset(x, y, channel)],
            Y = modelOutput[GetOffset(x, y, channel + 1)],
            Width = modelOutput[GetOffset(x, y, channel + 2)],
            Height = modelOutput[GetOffset(x, y, channel + 3)]
         };
      }

      private float GetConfidence(float[] modelOutput, int x, int y, int channel)
      {
         return MathUtils.Sigmoid2(modelOutput[GetOffset(x, y, channel + 4)]);
      }

      private BoundingBoxDimensions MapBoundingBoxToCell(int x, int y, int box, BoundingBoxDimensions boxDimensions)
      {
         return new BoundingBoxDimensions
         {
            X = ((float)x + MathUtils.Sigmoid2(boxDimensions.X)) * CELL_WIDTH,
            Y = ((float)y + MathUtils.Sigmoid2(boxDimensions.Y)) * CELL_HEIGHT,
            Width = (float)Math.Exp(boxDimensions.Width) * CELL_WIDTH * TinyYoloV2Config.ANCHORS[box * 2],
            Height = (float)Math.Exp(boxDimensions.Height) * CELL_HEIGHT * TinyYoloV2Config.ANCHORS[box * 2 + 1],
         };
      }

      public float[] ExtractClasses(float[] modelOutput, int x, int y, int channel)
      {
         float[] predictedClasses = new float[CLASS_COUNT];
         int predictedClassOffset = channel + BOX_INFO_FEATURE_COUNT;
         for (int predictedClass = 0; predictedClass < CLASS_COUNT; predictedClass++)
         {
            predictedClasses[predictedClass] = modelOutput[GetOffset(x, y, predictedClass + predictedClassOffset)];
         }
         return MathUtils.Softmax(predictedClasses);
      }

      private ValueTuple<int, float> GetTopResult(float[] predictedClasses)
      {
         return predictedClasses
               .Select((predictedClass, index) => (Index: index, Value: predictedClass))
               .OrderByDescending(result => result.Value)
               .First();
      }

      public IList<BoundingBox> ParseOutputs(float[] yoloModelOutputs, float threshold = .3F)
      {
         var boxes = new List<BoundingBox>();

         for (int row = 0; row < TinyYoloV2Config.ROW_COUNT; row++)
         {
            for (int column = 0; column < TinyYoloV2Config.COL_COUNT; column++)
            {
               for (int box = 0; box < TinyYoloV2Config.BOXES_PER_CELL; box++)
               {
                  var channel = (box * (CLASS_COUNT + BOX_INFO_FEATURE_COUNT));
                  BoundingBoxDimensions boundingBoxDimensions = ExtractBoundingBoxDimensions(yoloModelOutputs, row, column, channel);
                  float confidence = GetConfidence(yoloModelOutputs, row, column, channel);
                  if (confidence < threshold)
                     continue;
                  float[] predictedClasses = ExtractClasses(yoloModelOutputs, row, column, channel);
                  var (topResultIndex, topResultScore) = GetTopResult(predictedClasses);
                  var topScore = topResultScore * confidence;
                  if (topScore < threshold)
                     continue;

                  BoundingBoxDimensions mappedBoundingBox = MapBoundingBoxToCell(row, column, box, boundingBoxDimensions);
                  boxes.Add(new BoundingBox()
                  {
                     Dimensions = new BoundingBoxDimensions
                     {
                        X = (mappedBoundingBox.X - mappedBoundingBox.Width / 2),
                        Y = (mappedBoundingBox.Y - mappedBoundingBox.Height / 2),
                        Width = mappedBoundingBox.Width,
                        Height = mappedBoundingBox.Height,
                     },
                     Confidence = topScore,
                     Label = TinyYoloV2ClassNames.ClassNames[topResultIndex],
                     BoxColor = ColorArray.GetColor(topResultIndex)
                  });
               }
            }
         }

         return boxes;
      }

      public IList<BoundingBox> FilterBoundingBoxes(IList<BoundingBox> boxes, int limit, float threshold)
      {
         var activeCount = boxes.Count;
         var isActiveBoxes = new bool[boxes.Count];

         for (int i = 0; i < isActiveBoxes.Length; i++)
            isActiveBoxes[i] = true;

         var sortedBoxes = boxes.Select((b, i) => new { Box = b, Index = i })
                  .OrderByDescending(b => b.Box.Confidence)
                  .ToList();
         var results = new List<BoundingBox>();
         for (int i = 0; i < boxes.Count; i++)
         {
            if (isActiveBoxes[i])
            {
               var boxA = sortedBoxes[i].Box;
               results.Add(boxA);

               if (results.Count >= limit)
                  break;
               for (var j = i + 1; j < boxes.Count; j++)
               {
                  if (isActiveBoxes[j])
                  {
                     var boxB = sortedBoxes[j].Box;

                     if (MathUtils.IntersectionOverUnion(boxA.Rect, boxB.Rect) > threshold)
                     {
                        isActiveBoxes[j] = false;
                        activeCount--;

                        if (activeCount <= 0)
                           break;
                     }
                  }

                  if (activeCount <= 0)
                     break;
               }
            }
         }

         return results;
      }

      public IReadOnlyList<PredictionResult> GetResults(float scoreThreshold = 0.5f, float iouThres = 0.5f)
      {
         //IDataView scoredData = model.Transform(testData);
         //IEnumerable<float[]> probabilities = scoredData.GetColumn<float[]>(TinyYoloV2Config.ModelOutput);


         //List<float[]> postProcesssedResults = ParseOutputs(probabilities, scoreThreshold);

         //// Non-maximum Suppression
         //List<float[]> orderedResults = postProcesssedResults.OrderByDescending(x => x[(int)ResultArrayDefinition.Confidence]).ToList(); // sort by confidence

         //List<TinyYoloV2PredictionResult> resultsNms = FilterResults(iouThres, orderedResults);

         //return resultsNms;
         return null;
      }
   }
}

