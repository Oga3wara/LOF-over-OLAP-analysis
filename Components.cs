using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LOFoverOLAPanalysis
{
    public class C
    {
        public static double[][] testdata1 = new double[][]{
            new double[] {7, 13},
            new double[] {13, 15},
            new double[] {13, 9},
            new double[] {5, 7},
            new double[] {17, 17},
            new double[] {17, 13},
            new double[] {17, 9},
            new double[] {13, 5},
            new double[] {6, 3},
            new double[] {3, 3}
        };

        public static double[][] testdata2 = new double[][]{
            new double[] {7, 13},
            new double[] {13, 15},
            new double[] {13, 9},
            new double[] {5, 7},
            new double[] {14, 17},
            new double[] {14, 14},
            new double[] {15, 9},
            new double[] {13, 8},
            new double[] {7, 5},
            new double[] {1, 7}
        };

        public static List<double[]> getTestIntervals()
        {
            int numPoint = 10;
            int interval = 1;
            double[][] intervals = new double[numPoint][];

            for (int i = 0; i < numPoint; i++)
			{
			    intervals[i] = new double[G.NumDim];
                for (int j = 0; j < G.NumDim; j++)
			    {
			        intervals[i][j] = interval;
			    }
			}

            return intervals.ToList();
        }

        public static int[] partial_quicksort(double[] Array, int[] result, int left, int right, int k){
            if (left < right) {
                int i = left, j = right;

                /// Get Pivot
                int pidx = i + (j - i) / 2;
                int pidxtmp = i + (j - i) / 2;
                double pval = Array[pidx];

                /// Array[i..q-1] < Array[q] < Array[q+1..j]
                while (true)
                { /* a[] を pivot 以上と以下の集まりに分割する */
                    while (Array[i] < pval) i++; /* a[i] >= pivot となる位置を検索 */
                    while (pval < Array[j]) j--; /* a[j] <= pivot となる位置を検索 */
                    if (i >= j) break;

                    var tmp = Array[i]; Array[i] = Array[j]; Array[j] = tmp; /* a[i], a[j] を交換 */
                    var tmp2 = result[i]; result[i] = result[j]; result[j] = tmp2;
                    
                    /// Condition: pivot place is changed or not
                    if (pidx == i)
                    {
                        pidx = j;
                        i++;
                    }
                    else if (pidx == j)
                    {
                        pidx = i;
                        j--;
                    }
                    else
                    {
                        i++;
                        j--;
                    }

                    //Console.WriteLine("      {0} {1}", i, j);
                    //Console.WriteLine("pidx: {0} {1}", pidxtmp, pidx);
                    //i++; j--;
                }

                partial_quicksort(Array, result, left, pidx - 1, k);
                 if (pidx < k-1){
                     partial_quicksort(Array, result, pidx + 1, right, k);
                 }
            }
            return result;
        }

        public static void quicksort(double[] a, int left, int right) {
            if (left < right) {
                int i = left, j = right;
                double tmp, pivot = a[i + (j - i) / 2]; /* (i+j)/2 ではオーバーフローしてしまう */
                while (true) { /* a[] を pivot 以上と以下の集まりに分割する */
                    while (a[i] < pivot) i++; /* a[i] >= pivot となる位置を検索 */
                    while (pivot < a[j]) j--; /* a[j] <= pivot となる位置を検索 */
                    if (i >= j) break;
                    tmp = a[i]; a[i] = a[j]; a[j] = tmp; /* a[i], a[j] を交換 */
                    Console.WriteLine("{0} {1}", i, j);
                    i++; j--;
                }
                quicksort(a, left, i - 1);  /* 分割した左を再帰的にソート */
                quicksort(a, j + 1, right); /* 分割した右を再帰的にソート */
            }
        }

        public static void OutputRectangles(List<double[]> coordinateList)
        {
            // Rectangles を CSV 出力
            double[][] output = RectangularArrays.ReturnRectangularDoubleArray(G.NumPoint, 4);
            for (int i = 0; i < G.NumPoint; i++)
            {
                // 2次元のみ
                // x, y, width, height
                var x = coordinateList[i][0] - G.IntervalList[i][0];
                var y = coordinateList[i][1] - G.IntervalList[i][1];
                var width = G.IntervalList[i][0] * 2;
                var height = G.IntervalList[i][1] * 2;
                output[i][0] = x;
                output[i][1] = y;
                output[i][2] = width;
                output[i][3] = height;
            }
            C.OutputRectanglesAsCsv(G.file_rectangles, output);
        }

        public static void OutputLofBounds(double[] lof_maxTable, double[] lof_minTable, List<double[]> coordinateList)
        {
            double[][] output = RectangularArrays.ReturnRectangularDoubleArray(G.NumPoint, 4);
            double lofmax = lof_maxTable.Max();
            double xmax = 0;
            for (int i = 0; i < G.NumPoint; i++)
            {
                xmax = (xmax < coordinateList[i][0]) ? coordinateList[i][0] : xmax;
            }
            for (int i = 0; i < G.NumPoint; i++)
            {
                // 2次元のみ
                // x, y, width, height
                //var x = (CoordinateList[i][0] - G.IntervalList[i][0]) / xmax * lofmax;
                //var y = lof_minTable[i];
                //var width = 0.005;
                //var height = lof_maxTable[i];
                //output[i][0] = x;
                //output[i][1] = y;
                //output[i][2] = width;
                //output[i][3] = height;
                output[i][0] = coordinateList[i][0];
                output[i][1] = coordinateList[i][1];
                output[i][2] = lof_maxTable[i];
                output[i][3] = lof_minTable[i];
            }
            C.OutputRectanglesAsCsv(G.file_lofbounds, output);
        }

        public static double ComputeInterval(double Zp, double sampleCount, double Var)
        {
            double interval = -1;

            if (G.AggregateFunction == "AVG")
            {
                interval = Math.Pow(Zp, 2) * Var / sampleCount;
                interval = Math.Sqrt(interval);
            }
            if (G.AggregateFunction == "SUM")
            {
                interval = Math.Pow(Zp, 2) * Var * sampleCount;
                interval = Math.Sqrt(interval);
            }
            else
            {
                interval = double.NegativeInfinity;
            }

            return interval;
        }

        public static void OutputRectanglesAsCsv(string filePath, double[][] output)
        {
            /// csv に結果を出力
            using (var sw = new System.IO.StreamWriter(filePath, false, Encoding.UTF8)) /// true 上書き, false 新規作成
            {
                foreach (double[] data in output)
                {
                    string outputLine = "";
                    outputLine += data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3];
                    sw.WriteLine("{0}", outputLine);
                    //Console.WriteLine("{0} : {1}", outputKey, G.OutputDic[outputKey]);
                }

                //Console.WriteLine("FileName: {0}", G.FileName);
            }
        }

        public static void OutputPerformanceToCsv(string filePath , List<string> data)
        {
            /// csv に結果を出力
            using (var sw = new System.IO.StreamWriter(filePath, true)) /// true 上書き, false 新規作成
            {
                string outputLine = "";
                foreach (string value in data)
                {
                    outputLine += value + ", ";
                }
                sw.WriteLine("{0}", outputLine);
            }
        }

        public static int NewStopwatch()
        {
            int swIdx = G.StopwatchList.Count();
            Stopwatch sw = new Stopwatch();
            G.StopwatchList.Add(sw);
            G.StopwatchList[swIdx].Start();
            return swIdx;
        }

        public static double GetTime(int swIdx)
        {
            double time = 0;
            if (swIdx != -1)
            {
                time = G.StopwatchList[swIdx].ElapsedMilliseconds * 0.001;
                //time = G.StopwatchList[swIdx].ElapsedMilliseconds;
            }
            return time;
        }

        public static string GetDataSliceAtrs(List<List<string>> atrsList)
        {
            string dataSliceNames = "";

            foreach (List<string> atrs in atrsList)
            {
                dataSliceNames += "[";
                foreach (string atr in atrs)
                {
                    dataSliceNames += " '" + atr + "' ";
                }
                dataSliceNames += "]";
            }

            return dataSliceNames;
        }
    }

    public class Pair
    {
        public double Value = 0;
        public int Count = 0;

        public void Set(double value, int count)
        {
            Value = value;
            Count = count;
        }
    }

}
