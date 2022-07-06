using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using YoloObjectDetection.Defines;
using YoloObjectDetection.Utils;
using YoloObjectDetection.YoloV4;

namespace YoloObjectDetection
{
   public class YoloV4Prediction : IPrediction
   {
      #region Model Result Data

      /// <summary>
      /// Identity
      /// </summary>
      [VectorType(1, 52, 52, 3, 85)]
      [ColumnName("Identity:0")]
      public float[] Identity { get; set; }

      /// <summary>
      /// Identity1
      /// </summary>
      [VectorType(1, 26, 26, 3, 85)]
      [ColumnName("Identity_1:0")]
      public float[] Identity1 { get; set; }

      /// <summary>
      /// Identity2
      /// </summary>
      [VectorType(1, 13, 13, 3, 85)]
      [ColumnName("Identity_2:0")]
      public float[] Identity2 { get; set; }

      [ColumnName("width")]
      public float ImageWidth { get; set; }

      [ColumnName("height")]
      public float ImageHeight { get; set; }

      #endregion

      #region Public Methods

      /// <summary>
      /// 
      /// </summary>
      /// <param name="scoreThreshold">The threshold for the confidence</param>
      /// <param name="iouThres">Intersection over Union threshold</param>
      /// <returns>The results</returns>
      public IReadOnlyList<PredictionResult> GetResults(float scoreThreshold = 0.5f, float iouThres = 0.5f)
      {
         List<float[]> postProcesssedResults = GetResults(scoreThreshold);

         // Non-maximum Suppression
         List<float[]> orderedResults = postProcesssedResults.OrderByDescending(x => x[(int)ResultArrayDefinition.Confidence]).ToList(); // sort by confidence

         List<YoloV4PredictionResult> resultsNms = FilterResults(iouThres, orderedResults);

         return resultsNms;
      }

      #endregion

      #region Private Methods

      private List<float[]> GetResults(float scoreThres)
      {
         List<float[]> postProcesssedResults = new List<float[]>();

         int classesCount = YoloV4ClassNames.ClassNames.Length;
         var results = new[] { Identity, Identity1, Identity2 };

         for (int i = 0; i < results.Length; i++)
         {
            var pred = results[i];
            var outputSize = YoloV4Config.shapes[i];

            for (int row = 0; row < outputSize; row++)
            {
               for (int column = 0; column < outputSize; column++)
               {
                  for (int box = 0; box < YoloV4Config.C_ANCORS_COUNT; box++)
                  {
                     var offset = (row * outputSize * (classesCount + 5) * YoloV4Config.C_ANCORS_COUNT) + (column * (classesCount + 5) * YoloV4Config.C_ANCORS_COUNT) + box * (classesCount + 5);
                     var predBbox = pred.Skip(offset).Take(classesCount + 5).ToArray();

                     // ported from https://github.com/onnx/models/tree/master/vision/object_detection_segmentation/yolov4#postprocessing-steps

                     // postprocess_bbbox()
                     var predXywh = predBbox.Take(4).ToArray();
                     var predConf = predBbox[4];
                     var predProb = predBbox.Skip(5).ToArray();

                     var rawDx = predXywh[0];
                     var rawDy = predXywh[1];
                     var rawDw = predXywh[2];
                     var rawDh = predXywh[3];

                     float predX = ((MathUtils.Sigmoid(rawDx) * YoloV4Config.XYSCALE[i]) - 0.5f * (YoloV4Config.XYSCALE[i] - 1) + column) * YoloV4Config.STRIDES[i];
                     float predY = ((MathUtils.Sigmoid(rawDy) * YoloV4Config.XYSCALE[i]) - 0.5f * (YoloV4Config.XYSCALE[i] - 1) + row) * YoloV4Config.STRIDES[i];
                     float predW = (float)Math.Exp(rawDw) * YoloV4Config.ANCHORS[i][box][0];
                     float predH = (float)Math.Exp(rawDh) * YoloV4Config.ANCHORS[i][box][1];

                     // postprocess_boxes
                     // (1) (x, y, w, h) --> (xmin, ymin, xmax, ymax)
                     float predX1 = predX - predW * 0.5f;
                     float predY1 = predY - predH * 0.5f;
                     float predX2 = predX + predW * 0.5f;
                     float predY2 = predY + predH * 0.5f;

                     // (2) (xmin, ymin, xmax, ymax) -> (xmin_org, ymin_org, xmax_org, ymax_org)
                     float org_h = ImageHeight;
                     float org_w = ImageWidth;

                     float inputSize = 416f;
                     float resizeRatio = Math.Min(inputSize / org_w, inputSize / org_h);
                     float dw = (inputSize - resizeRatio * org_w) / 2f;
                     float dh = (inputSize - resizeRatio * org_h) / 2f;

                     var orgX1 = 1f * (predX1 - dw) / resizeRatio; // left
                     var orgX2 = 1f * (predX2 - dw) / resizeRatio; // right
                     var orgY1 = 1f * (predY1 - dh) / resizeRatio; // top
                     var orgY2 = 1f * (predY2 - dh) / resizeRatio; // bottom

                     // (3) clip some boxes that are out of range
                     orgX1 = Math.Max(orgX1, 0);
                     orgY1 = Math.Max(orgY1, 0);
                     orgX2 = Math.Min(orgX2, org_w - 1);
                     orgY2 = Math.Min(orgY2, org_h - 1);

                     if (orgX1 > orgX2 || orgY1 > orgY2) continue; // invalid_mask

                     // (4) discard some invalid boxes
                     // TODO

                     // (5) discard some boxes with low scores
                     var scores = predProb.Select(p => p * predConf).ToList();

                     float topScore = scores.Max();
                     if (topScore > scoreThres)
                     {
                        int index = scores.IndexOf(topScore);
                        postProcesssedResults.Add(new float[] { orgX1, orgY1, orgX2, orgY2, topScore, index });
                     }
                  }
               }
            }
         }

         return postProcesssedResults;
      }

      private static List<YoloV4PredictionResult> FilterResults(float iouThres, List<float[]> unfilteredResults)
      {
         List<YoloV4PredictionResult> filteredResults = new List<YoloV4PredictionResult>();

         int index = 0;
         while (index < unfilteredResults.Count)
         {
            float[] result = unfilteredResults[index];

            // No result found at index.
            if (result == null)
            {
               index++;
               continue;
            }

            filteredResults.Add(YoloV4PredictionResult.GetYoloResult(result));
            unfilteredResults[index] = null;

            var iou = unfilteredResults.Select(boundingBox => boundingBox == null ? float.NaN : BoxIoU(result, boundingBox)).ToList();

            for (int i = 0; i < iou.Count; i++)
            {
               if (float.IsNaN(iou[i]))
                  continue;

               if (iou[i] > iouThres)
               {
                  unfilteredResults[i] = null;
               }
            }
            index++;
         }

         return filteredResults;
      }

      /// <summary>
      /// Return intersection-over-union (Jaccard index) of boxes.
      /// <para>Both sets of boxes are expected to be in (x1, y1, x2, y2) format.</para>
      /// </summary>
      private static float BoxIoU(float[] boxes1, float[] boxes2)
      {
         static float box_area(float[] box)
         {
            return (box[2] - box[0]) * (box[3] - box[1]);
         }

         var area1 = box_area(boxes1);
         var area2 = box_area(boxes2);

         Debug.Assert(area1 >= 0);
         Debug.Assert(area2 >= 0);

         var dx = Math.Max(0, Math.Min(boxes1[2], boxes2[2]) - Math.Max(boxes1[0], boxes2[0]));
         var dy = Math.Max(0, Math.Min(boxes1[3], boxes2[3]) - Math.Max(boxes1[1], boxes2[1]));
         var inter = dx * dy;

         return inter / (area1 + area2 - inter);
      }

      #endregion
   }
}
