using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LOFoverOLAPanalysis
{
    public class LOF
    {
        public static double[][] d_Table;
        public static Dictionary<int, List<int>> knn_Table;
        //public static int[][] d_Sorted;

        public static double[][] d_maxTable;
        public static double[][] d_minTable;
        public static int[][] d_maxSorted;  // value: index
        public static int[][] d_minSorted;  // value: index
        public static double[] lrd_maxTable;
        public static double[] lrd_minTable;
        public double[] lof_maxTable;
        public double[] lof_minTable;
        public static double[] lof_Table;
        public static List<int> remaining_points;

        public void ComputeLOFBounds(List<double[]> CoordinateList)
        {
            var sw_1 = C.NewStopwatch(); // sw
            d_maxTable = RectangularArrays.ReturnRectangularDoubleArray(G.NumPoint, G.NumPoint);
            d_minTable = RectangularArrays.ReturnRectangularDoubleArray(G.NumPoint, G.NumPoint);
            d_maxSorted = RectangularArrays.ReturnRectangularIntArray(G.NumPoint, G.NumPoint);
            d_minSorted = RectangularArrays.ReturnRectangularIntArray(G.NumPoint, G.NumPoint);
            lrd_maxTable = new double[G.NumPoint];
            lrd_minTable = new double[G.NumPoint];
            lof_maxTable = new double[G.NumPoint];
            lof_minTable = new double[G.NumPoint];
            G.StopwatchList[sw_1].Stop(); // sw

            /// Compute Distance Bounds
            var sw_2 = C.NewStopwatch(); // sw
            ComputeDistanceBounds(CoordinateList);
            G.StopwatchList[sw_2].Stop(); // sw

            /// Make arrays sorted by distances for making each point's k nearest neighbor 
            var sw_3 = C.NewStopwatch(); // sw
            MakeNNsArray();
            G.StopwatchList[sw_3].Stop(); // sw

            /// Compute Lrd Bounds
            var sw_4 = C.NewStopwatch();  // sw
            ComputeLrdBounds();
            G.StopwatchList[sw_4].Stop(); // sw

            /// Compute LOF Bounds
            var sw_5 = C.NewStopwatch(); // sw
            ComputeLOFBounds();
            G.StopwatchList[sw_5].Stop(); // sw

            /// Output
            //if (G.experimentFlug == false) Console.WriteLine("{0} {1} {2} {3} {4}", C.GetTime(sw_1), C.GetTime(sw_2), C.GetTime(sw_3), C.GetTime(sw_4), C.GetTime(sw_5));

            // LOF 上限下限 を出力
            if (G.experimentFlug == false)
            {
                var lofmax = lof_maxTable.OrderBy(x => x);
                var lofmin = lof_minTable.OrderBy(x => x);
                var lrdmax = lrd_maxTable.OrderBy(x => x);
                var lrdmin = lrd_minTable.OrderBy(x => x);

                Console.WriteLine("------------: Max, Avg, Median, Min");
                Console.WriteLine("lof_maxTable: {0}, {1}, {2}, {3}", lof_maxTable.Max(), lof_maxTable.Average(), lofmax.ToArray()[(G.NumPoint/2)], lof_maxTable.Min());
                Console.WriteLine("lof_minTable: {0}, {1}, {2}, {3}", lof_minTable.Max(), lof_minTable.Average(), lofmin.ToArray()[(G.NumPoint / 2)], lof_minTable.Min());
                Console.WriteLine("lrd_maxTable: {0}, {1}, {2}, {3}", lrd_maxTable.Max(), lrd_maxTable.Average(), lrdmax.ToArray()[(G.NumPoint / 2)], lrd_maxTable.Min());
                Console.WriteLine("lrd_minTable: {0}, {1}, {2}, {3}", lrd_minTable.Max(), lrd_minTable.Average(), lrdmin.ToArray()[(G.NumPoint / 2)], lrd_minTable.Min());

                using (var sw = new System.IO.StreamWriter(G.file_loflrd_bounds_statistics, true)) /// true 上書き, false 新規作成
                {
                    string outputLine = "";
                    outputLine = "LOF_upper, " + lof_maxTable.Max() + ", " + lof_maxTable.Average() + ", " + lofmax.ToArray()[(G.NumPoint / 2)] + ", " + lof_maxTable.Min();
                    sw.WriteLine("{0}", outputLine);
                    outputLine = "LOF_lower, " + lof_minTable.Max() + ", " + lof_minTable.Average() + ", " + lofmin.ToArray()[(G.NumPoint / 2)] + ", " + lof_minTable.Min();
                    sw.WriteLine("{0}", outputLine);
                    outputLine = "lrd_upper, " + lrd_maxTable.Max() + ", " + lrd_maxTable.Average() + ", " + lrdmax.ToArray()[(G.NumPoint / 2)] + ", " + lrd_maxTable.Min();
                    sw.WriteLine("{0}", outputLine);
                    outputLine = "lrd_lower, " + lrd_minTable.Max() + ", " + lrd_minTable.Average() + ", " + lrdmin.ToArray()[(G.NumPoint / 2)] + ", " + lrd_minTable.Min();
                    sw.WriteLine("{0}", outputLine);
                }
       	    } 
	    }

        public void ComputeDistanceBounds(List<double[]> CoordinateList)
        {
            int Aid = 0;
            int Bid = 0;
            foreach (double[] pointA in CoordinateList)
            {
                Bid = 0;
                foreach (double[] pointB in CoordinateList)
                {
                    if (Aid != Bid)
                    {
                        double[] pointA2 = new double[G.NumDim];
                        double[] pointB2 = new double[G.NumDim];
                        double d_max = 0;
                        double d_min = 0;
                        for (int i = 0; i < G.NumDim; i++)
                        {
                            double intervalA = G.IntervalList[Aid][i];
                            double intervalB = G.IntervalList[Bid][i];
                            if (pointA[i] >= pointB[i])
                            {
                                if (pointA[i] - intervalA >= pointB[i] + intervalB)  // : d_min on the axis ≠ 0
                                {
                                    d_min += Math.Pow(pointA[i] - pointB[i] - intervalA - intervalB, 2);
                                }
                                else  // (pointA[i] - intervalA < pointB[i] + intervalB)  : d_min on the axis = 0
                                {
                                    d_min += 0;
                                }
                                d_max += Math.Pow(pointA[i] - pointB[i] + intervalA + intervalB, 2);  /// (pointA[i] + intervalA) - (pointB[i] - intervalB)
                            }
                            else  // (pointA[i] < pointB[i])
                            {
                                if (pointA[i] + intervalA >= pointB[i] - intervalB)  // : d_min on the axis = 0
                                {
                                    d_min += 0;
                                }
                                else  // (pointA[i] + intervalA < pointB[i] - intervalB)  : d_min on the axis ≠ 0
                                {
                                    d_min += Math.Pow(pointB[i] - pointA[i] - intervalB - intervalA, 2);
                                }
                                d_max += Math.Pow(pointB[i] - pointA[i] + intervalB + intervalA, 2);  /// (pointB[i] + intervalB) - (pointA[i] - intervalA)
                            }
                        }
                        d_maxTable[Aid][Bid] = Math.Sqrt(d_max);
                        d_minTable[Aid][Bid] = Math.Sqrt(d_min);
                    }
                    else
                    {
                        d_maxTable[Aid][Bid] = double.PositiveInfinity;
                        d_minTable[Aid][Bid] = double.PositiveInfinity;
                    }

                    Bid++;
                }
                Aid++;
            }
        }
        public void MakeNNsArray()
        {
            for (int j = 0; j < G.NumPoint; j++)
            {
                int[] result_max = new int[G.NumPoint];
                int[] result_min;
                for (int l = 0; l < result_max.Length; l++) { result_max[l] = l; }
                result_min = (int[])result_max.Clone();
                double[] tmp_d_maxTable = (double[])d_maxTable[j].Clone(); /// Deplicate Table
                double[] tmp_d_minTable = (double[])d_minTable[j].Clone(); /// Deplicate Table
                C.partial_quicksort(tmp_d_maxTable, result_max, 0, G.NumPoint - 1, G.LOF_K);
                C.partial_quicksort(tmp_d_minTable, result_min, 0, G.NumPoint - 1, G.LOF_K);

                d_maxSorted[j] = result_max;
                d_minSorted[j] = result_min;
            }
        }
        public int[] GetKnnCandidates(int idA)
        {
            List<int> candidates = new List<int>();
            double knnThreshold = d_maxTable[idA][d_maxSorted[idA][G.LOF_K - 1]];  // k近傍候補特定のための閾値
            for (int idB = 0; idB < G.NumPoint; idB++)
            {
                if (d_minTable[idA][idB] <= knnThreshold) // k近傍の候補か判定
                {
                    candidates.Add(idB);
                }
            }
            return candidates.ToArray();
        }
        public void ComputeLrdBounds()
        {
            /*
             * lrd_upper, lrd_lower
             */
            for (int idA = 0; idA < G.NumPoint; idA++)
            {
                double lrd_upper = 0;
                double lrd_lower = 0;

                /*  mean, knn: eigther d_max or d_min */
                if (G.rough_knn_candidates) {
                    for (int j = 0; j < G.LOF_K; j++)
                    {
                        int idB = d_minSorted[idA][j];
                        double d = d_minTable[idA][idB];
                        //double k_d = d_minTable[idB][d_minSorted[idB][j]];
                        double k_d = d_minTable[idB][d_minSorted[idB][G.LOF_K - 1]];
                        lrd_upper += (d >= k_d) ? d : k_d;

                        // d と k_d が両方 0 の場合の対処
                        ///if (d == 0 && k_d == 0){ lrd_upper += G.min_distance; }

                        //idB = d_maxSorted[idA][j];
                        d = d_maxTable[idA][idB];
                        //k_d = d_maxTable[idB][d_maxSorted[idB][j]];
                        k_d = d_maxTable[idB][d_maxSorted[idB][G.LOF_K -1 ]];

                        lrd_lower += (d >= k_d) ? d : k_d;
                    }
                    lrd_upper = G.LOF_K / lrd_upper;
                    lrd_lower = G.LOF_K / lrd_lower;

                } else {
                
                    int[] candidates = GetKnnCandidates(idA);
                    double reach_dist_upper = 0;
                    double reach_dist_lower = 0;
                    foreach (int idB in candidates)
                    {
                        double d = d_minTable[idA][idB];
                        double k_d = d_minTable[idB][d_minSorted[idB][G.LOF_K - 1]];
                        reach_dist_lower += (d >= k_d) ? d : k_d;

                        d = d_maxTable[idA][idB];
                        k_d = d_maxTable[idB][d_maxSorted[idB][G.LOF_K - 1]];
                        reach_dist_upper += (d >= k_d) ? d : k_d;
                    }
                    lrd_upper = candidates.Count() / reach_dist_lower;
                    lrd_lower = candidates.Count() / reach_dist_upper;
                }
                /// lrd が無限の場合の対処
                if (G.enableMinLrd)
                {
                    if (double.IsPositiveInfinity(lrd_upper))
                    {
                        lrd_upper = 1 / G.min_distance;
                    }
                }

                /// lrd_upper
                /*int idB    = d_minSorted[idA][G.LOF_K - 1];
                double d   = d_minTable[idA][idB];
                double k_d = d_minTable[idB][d_minSorted[idB][G.LOF_K - 1]];
                lrd_upper  = 1 / ((d >= k_d) ? d : k_d);

                /// lrd_lower
                idB = d_maxSorted[idA][G.LOF_K - 1];
                d   = d_maxTable[idA][idB];
                k_d = d_maxTable[idB][d_maxSorted[idB][G.LOF_K - 1]];
                lrd_lower = 1 / ((d >= k_d) ? d : k_d);*/

                lrd_maxTable[idA] = lrd_upper;
                lrd_minTable[idA] = lrd_lower;
            }
        }
        public void ComputeLOFBounds()
        {
            /*
             * LOF_upper, LOF_lower
             */
            double sum_lrdb_upper = 0;
            double sum_lrdb_lower = 0;
            double sum_lof_upper = 0;
            double sum_lof_lower = 0;

            for (int idA = 0; idA < G.NumPoint; idA++)
            {
                double lrdA_upper = lrd_maxTable[idA];
                double lrdA_lower = lrd_minTable[idA];
                double lrdB_upper = 0;
                double lrdB_lower = 0;

                /// knn candidates search
                if (G.rough_knn_candidates)
                {
                    List<double> lrd_upper_list = new List<double>();
                    List<double> lrd_lower_list = new List<double>();
                    int numkNN = G.LOF_K;
                    double knnThreshold = d_maxTable[idA][d_maxSorted[idA][numkNN - 1]];  // k近傍候補特定のための閾値
                    for (int idB = 0; idB < G.NumPoint; idB++)
                    {
                        if (d_minTable[idA][idB] <= knnThreshold) // k近傍の候補か判定
                        {
                            lrd_upper_list.Add(lrd_maxTable[idB]);
                            lrd_lower_list.Add(lrd_minTable[idB]);
                        }
                    }
                    lrd_upper_list.Sort();
                    lrd_lower_list.Sort();
                    for (int j = 0; j < G.LOF_K; j++)
                    {
                        lrdB_upper += lrd_upper_list[G.LOF_K - 1 - j];
                        lrdB_lower += lrd_lower_list[j];
                    }
                    lrdB_upper = lrdB_upper / G.LOF_K;
                    lrdB_lower = lrdB_lower / G.LOF_K;
                }
                else
                {
                    int[] candidates = GetKnnCandidates(idA);
                    foreach (int idB in candidates)
                    {
                        lrdB_upper += lrd_maxTable[idB];
                        lrdB_lower += lrd_minTable[idB];
                    }
                    lrdB_upper = lrdB_upper / candidates.Count();
                    lrdB_lower = lrdB_lower / candidates.Count();

                }

                lof_maxTable[idA] = lrdB_upper / lrdA_lower;
                lof_minTable[idA] = lrdB_lower / lrdA_upper;

                sum_lrdb_upper += lrdB_upper;
                //sum_lrdb_lower += lrdB_lower;
                //sum_lof_upper += lof_maxTable[idA];
                //sum_lof_lower += lof_minTable[idA];
            }
        }

        public void Prune()
        {
            remaining_points = new List<int>();
            var sorted_lof_lowers = lof_minTable.OrderByDescending(x => x);
            double topNth_lof_lower = sorted_lof_lowers.Skip(G.TOP_N - 1).First();
            if (G.prunningThresholdFlug == 1)
            {
                topNth_lof_lower = G.prunningThresholdLOF;
            }

            if (G.experimentFlug == false)
            {
                Console.WriteLine("prunning threshold: {0}", topNth_lof_lower);
            }

            G.NumPrunedPoint = 0;
            int id = 0;
            foreach (double lof_upper in lof_maxTable)
            {
                if (topNth_lof_lower > lof_upper)
                {
                    // prune
                    G.NumPrunedPoint++;
                }
                else
                {
                    // keep remained point
                    remaining_points.Add(id);   
                }
                id++;
            }

            /// Identify NumNeededPoint
            List<int> neededkNNPoints = new List<int>();    // k近傍までの候補
            List<int> neededkNNkNNPoints = new List<int>(); // k近傍のk近傍までの候補
            foreach (int ID in remaining_points)
            {
                /// for the point
                if (neededkNNPoints.Contains(ID) == false) neededkNNPoints.Add(ID);
                if (neededkNNkNNPoints.Contains(ID) == false) neededkNNkNNPoints.Add(ID);

                /// for the kNN of the point
                int numkNN = G.LOF_K;
                double knnThreshold = d_maxTable[ID][d_maxSorted[ID][numkNN - 1]];  // k近傍候補特定のための閾値
                for (int knnID = 0; knnID < G.NumPoint; knnID++)
                {
                    if (d_minTable[ID][knnID] <= knnThreshold) // k近傍の候補か判定
                    {
                        if (neededkNNPoints.Contains(knnID) == false) neededkNNPoints.Add(knnID);
                        if (neededkNNkNNPoints.Contains(knnID) == false) neededkNNkNNPoints.Add(knnID);

                        /// for the kNN of the kNN of the point
                        double knnknnThreshold = d_maxTable[knnID][d_maxSorted[knnID][numkNN - 1]];  // k近傍のk近傍候補特定のための閾値
                        for (int knnknnID = 0; knnknnID < G.NumPoint; knnknnID++)
                        {
                            if (d_minTable[knnID][knnknnID] <= knnknnThreshold) // k近傍のk近傍の候補か判定
                            {
                                if (neededkNNkNNPoints.Contains(knnknnID) == false) neededkNNkNNPoints.Add(knnknnID);
                            }
                        }

                    }
                }

            }
            G.NumNeededkNNPoint = neededkNNPoints.Count();
            G.NumNeededTotalPoint = neededkNNkNNPoints.Count();

            /// Identify NumNeededPoint
            if (G.experimentFlug == false)
            {
                

                /// output not needed ID list query
                List<int> unnecessaryIDs = new List<int>();
                for (int i = 0; i < G.NumPoint; i++)
			    {
                    if (neededkNNkNNPoints.Contains(i) == false) unnecessaryIDs.Add(i);
			    }
                string outputkNNkNN = "";
                int numNeedIDs = unnecessaryIDs.Count();
                int j = 1;
                foreach (int unnecessaryID in unnecessaryIDs)
                {
                    //outputkNNkNN += "[All]." +G.DataSliceAtrs[0][0] + " != " + G.DataSliceIdx[unnecessaryID];
                    outputkNNkNN += "[EntireTable2]." + G.DataSliceAtrs[0][0] + " != " + G.DataSliceIdx[unnecessaryID];
                    if (numNeedIDs != j) outputkNNkNN += " and ";
                    j++;
                }
                Console.WriteLine(outputkNNkNN);
            }
            if (G.experimentFlug == false)
            {
                Console.WriteLine("== Prune =============================");
                Console.WriteLine("NumPoint:            {0}", G.NumPoint);
                Console.WriteLine("NumPrunedPoint:      {0}", G.NumPrunedPoint);
                Console.WriteLine("PrunedRate:          {0}", Math.Round((double)G.NumPrunedPoint / (double)G.NumPoint, 3));
                Console.WriteLine("NumNeededkNNPoint:   {0}", G.NumNeededkNNPoint);
                Console.WriteLine("PrunedRate2:         {0}", Math.Round(1 - (double)G.NumNeededkNNPoint / (double)G.NumPoint, 3));
                Console.WriteLine("NumNeededTotalPoint: {0}", G.NumNeededTotalPoint);
                Console.WriteLine("PrunedRate3:         {0}", Math.Round(1 - (double)G.NumNeededTotalPoint / (double)G.NumPoint, 3));
                Console.WriteLine("======================================");

            }
            
            //Console.WriteLine("pruned: {0}", G.NumPrunedPoint);
        }

        public void ComputeRemainingLOF(List<double[]> CoordinateList)
        {
            lof_Table = new double[G.NumPoint];

            d_Table = RectangularArrays.ReturnRectangularDoubleArray(G.NumPoint, G.NumPoint);
            for (int i = 0; i < G.NumPoint; i++) { for (int j = 0; j < G.NumPoint; j++) { d_Table[i][j] = -1; } }
            knn_Table = new Dictionary<int, List<int>>();
            for (int i = 0; i < G.NumPoint; i++) { knn_Table.Add(i, new List<int>()); }

            foreach (int id in remaining_points)
            {
                lof_Table[id] = GetLOF(id, CoordinateList);
            }

            /// Identify NumNeededPoint
            /*if (G.experimentFlug == false)
            {
                List<int> neededkNNPoints = new List<int>();
                List<int> neededTotalPoints = new List<int>();
                foreach (int id in remaining_points)
                {
                    /// for the point
                    if (neededkNNPoints.Contains(id) == false) neededkNNPoints.Add(id); //Console.WriteLine("needed kNN:    {0}", id);
                    if (neededTotalPoints.Contains(id) == false) neededTotalPoints.Add(id); 

                    /// for the kNN of the point
                    int numkNN = GetNNCount(id, CoordinateList);
                    for (int i = 0; i < numkNN; i++)
                    {
                        var nn = knn_Table[id][i];
                        if (neededkNNPoints.Contains(nn) == false) neededkNNPoints.Add(nn); //Console.WriteLine("needed kNN:    {0}", nn);
                        if (neededTotalPoints.Contains(nn) == false) neededTotalPoints.Add(nn); 

                        int numkNNkNN = GetNNCount(nn, CoordinateList);
                        for (int j = 0; j < numkNNkNN; j++)
                        {
                            var nnnn = knn_Table[nn][j];
                            if (neededTotalPoints.Contains(nnnn) == false) neededTotalPoints.Add(nnnn); //Console.WriteLine("needed kNNkNN: {0}", nnnn);
                        }
                    }
                }
                G.NumNeededkNNPoint = neededkNNPoints.Count();
                G.NumNeededTotalPoint = neededTotalPoints.Count();

                //neededkNNPoints.Sort();
                //neededTotalPoints.Sort();
            }*/

            /// Output
            var top_n_outliers = lof_Table.OrderByDescending(x => x).Take(G.TOP_N);
            foreach (var outlier in top_n_outliers)
            {
                Console.WriteLine(outlier);
            }
        }

        public void ComputeAllLOF(List<double[]> CoordinateList)
        {
            lof_Table = new double[G.NumPoint];

            d_Table = RectangularArrays.ReturnRectangularDoubleArray(G.NumPoint, G.NumPoint);
            for (int i = 0; i < G.NumPoint; i++) { for (int j = 0; j < G.NumPoint; j++) { d_Table[i][j] = -1; } }
            knn_Table = new Dictionary<int, List<int>>();
            for (int i = 0; i < G.NumPoint; i++) { knn_Table.Add(i, new List<int>()); }

            for (int id = 0; id < G.NumPoint; id++)
            {
                lof_Table[id] = GetLOF(id, CoordinateList);
            }

            /// Output
            var top_n_outliers = lof_Table.OrderByDescending(x => x).Take(G.TOP_N);
            foreach (var outlier in top_n_outliers)
            {
                Console.WriteLine(outlier);
            }
        }

        // LOF
        private static double GetLOF(int Aid, List<double[]> CoordinateList)
        {
            double lof = 0;

            int numNN = GetNNCount(Aid, CoordinateList);

            double lrd = GetLocalReachDensity(Aid, CoordinateList);
            for (int i = 0; i < numNN; i++)
            {
                lof += (lrd == 0) ? 0 : GetLocalReachDensity(knn_Table[Aid][i], CoordinateList) / lrd;
            }
            lof /= numNN;

            return lof;
        }
        private static double GetLocalReachDensity(int Aid, List<double[]> CoordinateList)
        {
            double lrd = 0;

            int numNN = GetNNCount(Aid, CoordinateList);

            for (int i = 0; i < numNN; i++)
            {
                lrd += GetReachDistance(Aid, knn_Table[Aid][i], CoordinateList);
            }
            lrd = (lrd == 0) ? 0 : numNN / lrd;

            return lrd;
        }
        private static double GetReachDistance(int Aid, int Bid, List<double[]> CoordinateList)
        {
            double reachDist;
            double k_distance;

            if (d_Table[Aid][Bid] == -1)
            {
                CalculateDistanceArray(Aid, CoordinateList);
            }
            if (knn_Table[Bid].Count() == 0)
            {
                CalculateDistanceArray(Bid, CoordinateList);
            }

            reachDist = d_Table[Aid][Bid];
            k_distance = d_Table[Bid][knn_Table[Bid][G.LOF_K - 1]];

            if (reachDist < k_distance) reachDist = k_distance;

            return reachDist;
        }
        private static double GetDistance(double[] first, double[] second)
        {
            double distance = 0;

            for (int i = 0; i < G.NumDim; i++)
            {
                distance += Math.Pow(first[i] - second[i], 2);
            }
            distance = Math.Sqrt(distance);

            return distance;
        }
        private static int GetNNCount(int Aid, List<double[]> CoordinateList)
        {
            int numNN;

            /// Check Cache
            if (knn_Table[Aid].Count() == 0)
            {
                CalculateDistanceArray(Aid, CoordinateList);
            }
            numNN = knn_Table[Aid].Count();

            return numNN;
        }
        private static void CalculateDistanceArray(int Aid, List<double[]> CoordinateList)
        {
            CalcuDistTable(Aid, CoordinateList);
            CalcuDistSortedIdx(Aid);
        }
        private static void CalcuDistTable(int Aid, List<double[]> CoordinateList)
        {
            for (int Bid = 0; Bid < G.NumPoint; Bid++)
            {
                double distance = GetDistance(CoordinateList[Aid], CoordinateList[Bid]);

                d_Table[Aid][Bid] = distance;
                d_Table[Bid][Aid] = distance;
            }
        }
        private static void CalcuDistSortedIdx(int Aid)
        {
            /// kNN Search
            //int[] Aids = Enumerable.Range(0, d_Table[Aid].Length).OrderBy(x => d_Table[Aid][x]).ToArray();  /// 非破壊的昇順 ソート
            int[] Aids = new int[G.NumPoint];
            for (int l = 0; l < Aids.Length; l++) { Aids[l] = l; }
            double[] tmp_d_Table = (double[])d_Table[Aid].Clone(); /// Deplicate Table
            C.partial_quicksort(tmp_d_Table, Aids, 0, G.NumPoint - 1, G.LOF_K);

            List<int> knnList = new List<int>();
            int i = 0;
            foreach (int Bid in Aids)
            {
                if (Bid != Aid)  /// 自身の点でない場合に kNN リストに格納
                {
                    if (G.LOF_K <= i)  /// k 個以上の kNN がある場合
                    {
                        if (d_Table[Aid][knnList[i - 1]] != d_Table[Aid][Bid]) break;  /// 1 つ前の Iteretion 時の distance と 現在の distance が 異なる場合: kNN の格納終了
                    }
                    knnList.Add(Bid);  /// kNN の Point の ID をテーブルに格納
                    i++;
                }
            }
            /// kNN の Point の ID をテーブルに格納
            knn_Table[Aid] = knnList;
        }
    }

    internal static class RectangularArrays
    {
        internal static double[][] ReturnRectangularDoubleArray(int size1, int size2)
        {
            double[][] newArray = new double[size1][];
            for (int array1 = 0; array1 < size1; array1++)
            {
                newArray[array1] = new double[size2];
            }

            return newArray;
        }

        internal static int[][] ReturnRectangularIntArray(int size1, int size2)
        {
            int[][] newArray = new int[size1][];
            for (int array1 = 0; array1 < size1; array1++)
            {
                newArray[array1] = new int[size2];
            }

            return newArray;
        }
    }
}
