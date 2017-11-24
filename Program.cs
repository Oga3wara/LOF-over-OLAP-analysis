using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Diagnostics;

namespace LOFoverOLAPanalysis
{
    public class G
    {
        /// Parameters
        public static int LOF_K = 20;
        public static int TOP_N = 1;
        public static int GroupValueThreshold = 1;
        public static int GroupValueNum = 1;
        public static int SampleNumber = 1;
        public static double Zp = 1.960;               // confidence interval p = 0.95  →  (p+1)/2 = 0.975 quantile in normal cumulative distribution function  →  Zp = 1.960
        public static bool enableMinLrd = true;       // enable or not setting a MIN of lrd_upper when lrd_upper is infinity
        public static bool enableNormalizing = false;  // enable or not doing normalization
        public static bool enablePruning = true;
        public static bool experimentFlug = false;      // whether or not that this program is setted for experiment's environment
        public static bool rough_knn_candidates = false; // whether or not that take rough knn candidates
        public static int isTestData = 0;
        public static int prunningThresholdFlug = 0;   // 0: Top N 番目の要素, 1: 指定したLOFの閾値
        public static double prunningThresholdLOF = 3.0; // ↑のLOFの閾値
        public static double min_distance = 0.00001; //Math.Pow(10, -5);
        public static string StatisticsThreshold;
        //public static double min_distance = 1;
        public static string file_rectangles = @"C:\\Users\\ogasawara\\Documents\\Visual Studio 2013\\Experiments\\output.csv";
        public static string file_lofbounds = @"C:\\Users\\ogasawara\\Documents\\Visual Studio 2013\\Experiments\\output2.csv";
        public static string file_performance = @"C:\\Users\\ogasawara\\Documents\\Visual Studio 2013\\Experiments\\LOFoverOLAPanalysis_performance.txt";
        public static string file_loflrd_bounds_statistics = @"C:\\Users\\ogasawara\\Documents\\Visual Studio 2013\\Experiments\\loflrd_bounds_statistics.txt";

        /// Components
        public static int NumDim;                            // a number of dimension of data point
        public static int NumPoint;                          // a number of data slices
        public static int NumSamplePoint;                    // a number of data slices in sampling
        public static int NumPrunedPoint = -1;               // a number of pruned points
        public static int NumNeededkNNPoint = -1;            // a number of needed points in correctly computing LOFs
        public static int NumNeededTotalPoint = -1;            // a number of needed points in correctly computing LOFs
        public static bool haveGroupByValueList = false;     // a flug whether I could create Group-by Value List from query result
        public static string[] GroupByValueList;             // a list of Group-by Value List from query result
        public static string[] GroupByValueListForced;       // a list of Group-by Value List from user input
        public static Dictionary<string, int> DataSliceDic;  // a dictionary of Data Slices  {data slice name: data slice ID}
        public static Dictionary<int, string> DataSliceIdx;  // a index of Data Slices  {data slice ID: data slice name}
        public static List<double[]> CoordinateList;         // a coordinate list of Data Slices
        public static List<double[]> CoordinateListSampling; // a coordinate list of Data Slices on sampled query result
        public static List<double[]> IntervalList;           // a confident interval list per data slice, group-by value
        public static List<Stopwatch> StopwatchList = new List<Stopwatch>();

        /// Resource
        public static string ServerName = string.Empty;
        public static string DataBaseName = string.Empty;
        public static string TableName = string.Empty;
        public static string TableName1 = string.Empty;
        public static string TableName2 = string.Empty;
        public static string GroupbyAtr = string.Empty;
        public static string AggregateAtr = string.Empty;
        public static string AggregateFunction = string.Empty;
        public static List<List<string>> DataSliceAtrs;                  // a list of data slice attributes (enable to be with AND condition)
        public static List<string> GroupbyAtrVals = new List<string>();  // a list of group-by attributes
        public static string strSqlConnection = string.Empty;
        public static string GroupAtrForGraph = string.Empty;
        public static string AggregateAtrForGraph = string.Empty;
        public static string WhereCondition = string.Empty;              // where clause
        public static string Join = string.Empty;                        // join clause
        public static string Having = string.Empty;                      // having clause
        public static string Var = string.Empty;
    }

    static class Program
    {
        static void Main()
        {

            /// データセット設定
            int dataset = 2; /// データセット切替
            switch (dataset)
            {
                /// dataset 1
                case 1:
                    /// サーバー/データベース
                    G.ServerName = "GANESHA";    /// サーバー名
                    G.DataBaseName = "FashionEC";  /// データベース名  

                    /// サーバーへの認証方式： SQL server 認証の場合
                    string UserID = "";   /// データベースログイン用 ユーザID
                    string Password = ""; /// データベースログイン用 パスワード
                    G.strSqlConnection += "Data Source = " + G.ServerName + ";" + "Initial Catalog =" + G.DataBaseName + ";" + "User ID =" + UserID + ";" + "Password =" + Password + ";";
                    /// サーバーへの認証方式： Windows 認証の場合
                    G.strSqlConnection += "Data Source = " + G.ServerName + ";" + "Initial Catalog =" + G.DataBaseName + ";" + "Integrated Security = true;";

                    /// SQLクエリ関連
                    G.SampleNumber = 1800000;
                    G.TableName = "dbo.[All]";         /// 6,175,693 records
                    G.TableName = "dbo.[AllwithStatistics]"; 
                    //G.TableName1 = "( select * from dbo.[All] where ID <=" + G.SampleNumber + ") as tmp";
                    //G.TableName2 = "( select * from dbo.[All] where ID >" + G.SampleNumber + ") as tmp";                    //G.GroupbyAtr = "性別";       G.GroupValueThreshold = 2;  G.GroupValueNum = 2    ;
                    //G.GroupbyAtr = "年代";       G.GroupValueThreshold = 6;
                    G.GroupbyAtr = "[8地方区分]";      G.GroupValueThreshold = 8;  G.GroupValueNum = 8;
                    G.GroupbyAtr = "年代2";      G.GroupValueThreshold = 3;  G.GroupValueNum = 3;
                    G.GroupbyAtr = "性別";      G.GroupValueThreshold = 2;  G.GroupValueNum = 2;
                    //G.GroupbyAtr = "年代3";      G.GroupValueThreshold = 3;
                    G.AggregateFunction = "SUM"; /// 集約関数  例）SUM
                    G.AggregateAtr = "注文金額";      /// 集約属性  例）注文金額
                    G.DataSliceAtrs = new List<List<string>>();                        /// 部分データ属性格納リスト
                    //G.DataSliceAtrs.Add(new List<string> { "[All].商品番号" });         /// 部分データ属性を設定
                    G.DataSliceAtrs.Add(new List<string> { "商品番号" });         /// 部分データ属性を設定
                    //G.DataSliceAtrs.Add(new List<string> { "商品カテゴリ小" }); 
                    //G.DataSliceAtrs.Add(new List<string> { "ブランド番号" }); 
                    //G.DataSliceAtrs.Add(new List<string> { "商品カテゴリ小", "カラーカテゴリ" });
                    //G.DataSliceAtrs.Add(new List<string> { "商品カテゴリ大", "性別" }); /// 部分データ属性を設定 （2つ以上指定した場合 AND になる）．この例では 商品カテゴリ大=○○　かつ 性別=×× の部分データが選択される
                    //G.StatisticsThreshold = "5000";
                    G.StatisticsThreshold = "900";
                    string query_dataslices = G.DataSliceAtrs[0][0];
                    query_dataslices += (G.DataSliceAtrs[0].Count() > 1) ? ", [All]."+G.DataSliceAtrs[0][1] : "";
                    G.TableName1 = "( select [All]." + query_dataslices + ", " + G.GroupbyAtr + ", " + G.AggregateAtr +
                                   //" from [All] inner join Statistics_Brand on [All].ブランド番号 = Statistics_Brand.ブランド番号  where Statistics_Brand.Order数" +
                                   " from [All] inner join Statistics_Item on [All].商品番号 = Statistics_Item.商品番号  where Statistics_Item.Order数" +
                                   " > " + G.StatisticsThreshold +" and ID <=" + G.SampleNumber + ") as tmp";
                    G.TableName2 = "( select [All]." + query_dataslices + ", " + G.GroupbyAtr + ", " + G.AggregateAtr +
                                   //" from [All] inner join Statistics_Brand on [All].ブランド番号 = Statistics_Brand.ブランド番号  where Statistics_Brand.Order数" +
                                   " from [All] inner join Statistics_Item on [All].商品番号 = Statistics_Item.商品番号  where Statistics_Item.Order数" +
                                   " > " + G.StatisticsThreshold + " and ID >" + G.SampleNumber + ") as tmp";
                    G.TableName1 = "SampleAll";
                    G.TableName2 = "PrunedAll";
                    //G.Join = " inner join Statistics_Item on [All].商品番号 = Statistics_Item.商品番号 ";
                    //G.WhereCondition = "Statistics_Item.Order数 > " + G.StatisticsThreshold + " ";  /// Where条件　
                    G.Having = "";          /// Having条件　
                    G.Var = "(CASE WHEN var("+ G.AggregateAtr + ") IS NULL THEN 0 ELSE var(" + G.AggregateAtr + ") END)";
                    
                    break;

                /// dataset 2
                case 2:
                    //G.ServerName = "DESKTOP-9IPGUJJ\\MSSQLSERVER2";
                    G.ServerName = "GANESHA";
                    G.DataBaseName = "IDS";
                    G.strSqlConnection += "Data Source = " + G.ServerName + ";" + "Initial Catalog =" + G.DataBaseName + ";" + "Integrated Security = true;";
                    G.TableName = "dbo.[EntireTable2]";  // 103,382,016 records
                    G.Join = "";
                    G.DataSliceAtrs = new List<List<string>>();  

                    //G.TableName1 = "dbo.[EntireTable_1_2]";
                    //G.TableName2 = "dbo.[EntireTable_2_2]";
                    //G.TableName1 = "( select * from dbo.[EntireTable2] where ID <=" + G.SampleNumber + ") as tmp";
                    //G.TableName2 = "( select * from dbo.[EntireTable2] where ID >" + G.SampleNumber + ") as tmp";
                    //G.GroupbyAtr = "会員区分";        G.GroupValueThreshold = 2;
                    //G.GroupbyAtr = "大ブロックCD";    G.GroupValueThreshold = 2;G.GroupValueNum = 2;
                    //G.GroupbyAtr = "性別区分";        G.GroupValueThreshold = 4; G.GroupValueNum = 4;
                    //G.GroupbyAtr = "店舗名_仮";       G.GroupValueThreshold = 9;
                    //G.GroupbyAtr = "時";       G.GroupValueThreshold = 17; G.GroupValueNum = 17;
                    //G.GroupbyAtr = "SUBSTRING(CONVERT(VARCHAR, 日付情報, 111), 1, 7)"; G.GroupValueThreshold = 24; G.GroupValueNum = 24; G.GroupByValueListForced = new string[24]{"2013/07","2013/08","2013/09","2013/10","2013/11","2013/12","2014/01","2014/02","2014/03","2014/04","2014/05","2014/06","2014/07","2014/08","2014/09","2014/10","2014/11","2014/12","2015/01","2015/02","2015/03","2015/04","2015/05","2015/06"};
                    //G.DataSliceAtrs.Add(new List<string> { "分類３名称" }); 
                    //G.DataSliceAtrs.Add(new List<string> { "メーカー・ブランド名" }); 
                    //G.DataSliceAtrs.Add(new List<string> { "分類４名称" });  
                    //G.DataSliceAtrs.Add(new List<string> { "分類４名称", "店舗名_仮" });  
                    //G.DataSliceAtrs.Add(new List<string> { "分類３名称", "店舗名_仮" });  
                    //G.DataSliceAtrs.Add(new List<string> { "分類３名称", "時" });  
                    //G.WhereCondition = "ID <= " + G.SampleNumber.ToString() + " ";
                    //G.WhereCondition = "時 != '0' ";
                    //G.WhereCondition = "メーカー・ブランド名 != 'NULL' and (性別区分='1' or 性別区分='2') ";
                    //G.WhereCondition = "メーカー・ブランド名 != 'NULL' ";


                    int query = 2; /// クエリ切替
                    switch (query)
                    {
                        case 1:  // 理想環境
                            G.SampleNumber = 10000000;
                            G.GroupbyAtr = "性別区分";        G.GroupValueThreshold = 2; G.GroupValueNum = 2;
                            G.WhereCondition = "(性別区分='1' or 性別区分='2') ";
                            G.AggregateFunction = "SUM";
                            G.AggregateAtr = "税込金額";
                            G.DataSliceAtrs.Add(new List<string> { "分類３コード" }); 
                            G.StatisticsThreshold = "30000";
                            G.TableName1 = "dbo.[SampleAll]";
                            G.TableName2 = "dbo.[PrunedAll]";
                            break;
                        case 2:  // 通常環境
                            G.SampleNumber = 20000000;
                            //G.GroupbyAtr = "性別区分";        G.GroupValueThreshold = 4; G.GroupValueNum = 4;
                            G.GroupbyAtr = "性別区分"; G.GroupValueThreshold = 2; G.GroupValueNum = 2;
                            G.WhereCondition = "(性別区分='1' or 性別区分='2') ";
                            G.AggregateFunction = "SUM";
                            G.AggregateAtr = "税込金額";
                            //G.DataSliceAtrs.Add(new List<string> { "分類３名称" });
                            G.DataSliceAtrs.Add(new List<string> { "分類３名称", "店舗名_仮" });  
                            G.StatisticsThreshold = "500";
                            query_dataslices = G.DataSliceAtrs[0][0];
                            query_dataslices += (G.DataSliceAtrs[0].Count() > 1) ? ", [EntireTable2]." + G.DataSliceAtrs[0][1] : "";
                            G.TableName1 = "( select [EntireTable2]." + query_dataslices + ", " + G.GroupbyAtr + ", " + G.AggregateAtr +
                                           " from [EntireTable2] inner join Statistics_Category3 on [EntireTable2].[分類３コード] = Statistics_Category3.[分類３コード]  where Statistics_Category3.Order数" +
                                           " > " + G.StatisticsThreshold +" and ID <=" + G.SampleNumber + ") as tmp";
                            G.TableName2 = "( select [EntireTable2]." + query_dataslices + ", " + G.GroupbyAtr + ", " + G.AggregateAtr +
                                           " from [EntireTable2] inner join Statistics_Category3 on [EntireTable2].[分類３コード] = Statistics_Category3.[分類３コード]  where Statistics_Category3.Order数" +
                                           " > " + G.StatisticsThreshold + " and ID >" + G.SampleNumber + ") as tmp";
                            break;
                    }
                    G.Having = "";        
                    G.Var = "(CASE WHEN var("+ G.AggregateAtr + ") IS NULL THEN 0 ELSE var(" + G.AggregateAtr + ") END) ";

                    break;

                /// dataset 3
                case 3:
                    G.ServerName = "GANESHA";    /// サーバー名  例）DESKTOP-9IPGUJJ\\MSSQLSERVER2
                    G.DataBaseName = "Experiment";
                    G.strSqlConnection += "Data Source = " + G.ServerName + ";" + "Initial Catalog =" + G.DataBaseName + ";" + "Integrated Security = true;";
                    G.SampleNumber = 1000000;
                    G.TableName = "( select * from dbo.[sd1000-ds1000-gb2-rcd100000000] ) as tmp";
                    G.TableName1 = "( select * from dbo.[sd1000-ds1000-gb2-rcd100000000] where id <=" + G.SampleNumber + ") as tmp";
                    G.TableName2 = "( select * from dbo.[sd1000-ds1000-gb2-rcd100000000] where id >" + G.SampleNumber + ") as tmp";
                    G.Join = "";              /// Join句  例）left outer join OrderDetail on [Order].注文番号 = OrderDetail.注文番号 left outer join Member on [Order].顧客番号 = Member.顧客番号
                    G.GroupbyAtr = "category"; G.GroupValueThreshold = 2; G.GroupValueNum = 2;
                    G.AggregateFunction = "SUM"; /// 集約関数  例）SUM
                    G.AggregateAtr = "price";      /// 集約属性  例）注文金額
                    G.DataSliceAtrs = new List<List<string>>();                        /// 部分データ属性格納リスト
                    G.DataSliceAtrs.Add(new List<string> { "item" });         /// 部分データ属性を設定
                    G.WhereCondition = "";  /// Where条件　例）商品カテゴリ小 = 'ネックレス'
                    G.Having = "";          /// Having条件　例）COUNT(*) > 10
                    G.Var = "(CASE WHEN var("+ G.AggregateAtr + ") IS NULL THEN 0 ELSE var(" + G.AggregateAtr + ") END)";
                    break;

                /// dataset 4
                case 4:
                    G.ServerName = "GANESHA";
                    G.DataBaseName = "HairSalon"; 
                    G.strSqlConnection += "Data Source = " + G.ServerName + ";" + "Initial Catalog =" + G.DataBaseName + ";" + "Integrated Security = true;";
                    G.SampleNumber = 40000;
                    G.TableName = "( select * from dbo.[All] ) as tmp";  /// 408,868 records
                    G.TableName1 = "( select * from dbo.[All] where ID <=" + G.SampleNumber + ") as tmp";
                    G.TableName2 = "( select * from dbo.[All] where ID >" + G.SampleNumber + ") as tmp";
                    G.Join = ""; 
                    G.GroupbyAtr = "性別"; G.GroupValueThreshold = 2; G.GroupValueNum = 2;
                    G.AggregateFunction = "SUM"; 
                    G.AggregateAtr = "会計税込売上";  
                    G.DataSliceAtrs = new List<List<string>>();       
                    G.DataSliceAtrs.Add(new List<string> { "商品名" }); 
                    G.WhereCondition = "性別 = '男性' or 性別 = '女性'";
                    G.Having = "count(*) > 5";   
                    G.Var = "(CASE WHEN var(" + G.AggregateAtr + ") IS NULL THEN 0 ELSE var(" + G.AggregateAtr + ") END)";
                    break;
            }

            /// Stop watch
            int sw_sql_1_time = -1;
            int sw_sql_2_time = -1;
            int sw_normalize_1_time = -1;
            int sw_normalize_2_time = -1;
            int sw_all_lof_time = -1;
            int sw_lof_bound_time = -1;
            int sw_lof_prune_time = -1;
            int sw_remain_lof_time = -1;
            int sw_total_time = C.NewStopwatch();

            if (G.enablePruning)
            {
                /// クエリ結果取得・信頼区間の計算
                G.TableName = G.TableName1;
                sw_sql_1_time = C.NewStopwatch(); // sw
                MakeResultDSs();
                G.StopwatchList[sw_sql_1_time].Stop(); // sw

                /* start test */
                if (G.isTestData == 1)
                {
                    G.CoordinateList = C.testdata1.ToList();
                }
                if (G.isTestData == 2)
                {
                    G.CoordinateList = C.testdata2.ToList();
                }
                /* end test */

                LOF lof = new LOF();
                G.NumPoint = G.CoordinateList.Count();
                G.NumDim = (G.CoordinateList[0].Count() > G.GroupValueNum) ? G.CoordinateList[0].Count() : G.GroupValueNum;
                G.NumSamplePoint = G.NumPoint;

                /* start test */
                if (G.isTestData != 0)
                {
                    G.IntervalList = C.getTestIntervals();
                }
                /* end test */

                /// クエリ結果・信頼区間の正規化
                if (G.enableNormalizing)
                {
                    /// 正規化
                    sw_normalize_1_time = C.NewStopwatch(); // sw
                    List<double[]> normalizedCoordinateList = MakeNormalizeResultDSs();
                    G.StopwatchList[sw_normalize_1_time].Stop(); // sw

                    /// LOF 上限下限の計算
                    sw_lof_bound_time = C.NewStopwatch(); // sw
                    lof.ComputeLOFBounds(normalizedCoordinateList);
                    G.StopwatchList[sw_lof_bound_time].Stop(); // sw

                    // 分布・LOF bounds出力
                    C.OutputRectangles(normalizedCoordinateList);
                    C.OutputLofBounds(lof.lof_maxTable, lof.lof_minTable, normalizedCoordinateList);

                    /// 足切り
                    sw_lof_prune_time = C.NewStopwatch(); // sw
                    lof.Prune();
                    G.StopwatchList[sw_lof_prune_time].Stop(); // sw

                    /// 残りのクエリ結果取得
                    //G.WhereCondition = "ID > " + G.SampleNumber.ToString() + " ";
                    G.TableName = G.TableName2;

                    sw_sql_2_time = C.NewStopwatch(); // sw
                    ReMakeResultDSs();
                    G.StopwatchList[sw_sql_2_time].Stop(); // sw
                    G.NumPoint = G.CoordinateList.Count();

                    /// 正規化
                    sw_normalize_2_time = C.NewStopwatch(); // sw
                    normalizedCoordinateList = ReMakeNormalizeResultDSs();
                    G.StopwatchList[sw_normalize_2_time].Stop(); // sw

                    /// LOF を正確に計算
                    sw_remain_lof_time = C.NewStopwatch(); // sw
                    lof.ComputeRemainingLOF(normalizedCoordinateList);
                    G.StopwatchList[sw_remain_lof_time].Stop(); // sw
                }
                else
                {
                    /// LOF 上限下限の計算
                    sw_lof_bound_time = C.NewStopwatch(); // sw
                    lof.ComputeLOFBounds(G.CoordinateList);
                    //lof.ComputeLOFBoundsLoose(G.CoordinateList);
                    G.StopwatchList[sw_lof_bound_time].Stop(); // sw

                    // 分布・LOF bounds出力
                    C.OutputRectangles(G.CoordinateList);
                    C.OutputLofBounds(lof.lof_maxTable, lof.lof_minTable, G.CoordinateList);

                    /// 足切り
                    sw_lof_prune_time = C.NewStopwatch(); // sw
                    lof.Prune();
                    G.StopwatchList[sw_lof_prune_time].Stop(); // sw

                    /// 残りのクエリ結果取得
                    //G.WhereCondition = "ID > " + G.SampleNumber.ToString() + " ";
                    G.TableName = G.TableName2;

                    sw_sql_2_time = C.NewStopwatch(); // sw
                    ReMakeResultDSs();
                    G.StopwatchList[sw_sql_2_time].Stop(); // sw
                    G.NumPoint = G.CoordinateList.Count();

                    /// LOF を正確に計算
                    sw_remain_lof_time = C.NewStopwatch();  // sw
                    lof.ComputeRemainingLOF(G.CoordinateList);
                    G.StopwatchList[sw_remain_lof_time].Stop(); // sw
                }

            }
            else
            {
                /// クエリ結果取得
                //G.WhereCondition = "";
                sw_sql_1_time = C.NewStopwatch(); // sw
                MakeResultDSs();
                G.StopwatchList[sw_sql_1_time].Stop(); // sw

                LOF lof = new LOF();
                G.NumPoint = G.CoordinateList.Count();
                G.NumDim = (G.CoordinateList[0].Count() > G.GroupValueNum) ? G.CoordinateList[0].Count() : G.GroupValueNum;

                if (G.enableNormalizing)
                {
                    /// クエリ結果正規化
                    sw_normalize_1_time = C.NewStopwatch(); // sw
                    List<double[]> normalizedCoordinateList = MakeNormalizeResultDSs();
                    G.StopwatchList[sw_normalize_1_time].Stop(); // sw

                    /// LOF 計算
                    sw_all_lof_time = C.NewStopwatch(); // sw
                    lof.ComputeAllLOF(normalizedCoordinateList);
                    G.StopwatchList[sw_all_lof_time].Stop(); // sw
                }
                else
                {
                    /// LOF 計算
                    sw_all_lof_time = C.NewStopwatch(); // sw
                    lof.ComputeAllLOF(G.CoordinateList);
                    G.StopwatchList[sw_all_lof_time].Stop(); // sw
                }
            }
            G.StopwatchList[sw_total_time].Stop();  /// 実行時間計測 Stop

            /// debug
            /*for (int i = 0; i < G.CoordinateList.Count(); i++)
            {
                string output = i.ToString() + " )  ";
                for (int j = 0; j < G.GroupValueNum; j++)
                {
                    output += G.CoordinateList[i][j] + "  ";
                }
                Console.WriteLine(output);
            }*/
            List<string> output_data = new List<string>();
            Console.WriteLine("== Time ==============================");
            Console.WriteLine("sw_sql_1_time:       {0} s", C.GetTime(sw_sql_1_time)); output_data.Add(C.GetTime(sw_sql_1_time).ToString());
            Console.WriteLine("sw_normalize_1_time: {0} s", C.GetTime(sw_normalize_1_time)); output_data.Add(C.GetTime(sw_normalize_1_time).ToString());
            Console.WriteLine("sw_all_lof_time:     {0} s", C.GetTime(sw_all_lof_time)); output_data.Add(C.GetTime(sw_all_lof_time).ToString());
            Console.WriteLine("sw_lof_bound_time:   {0} s", C.GetTime(sw_lof_bound_time)); output_data.Add(C.GetTime(sw_lof_bound_time).ToString());
            Console.WriteLine("sw_lof_prune_time:   {0} s", C.GetTime(sw_lof_prune_time)); output_data.Add(C.GetTime(sw_lof_prune_time).ToString());
            Console.WriteLine("sw_sql_2_time:       {0} s", C.GetTime(sw_sql_2_time)); output_data.Add(C.GetTime(sw_sql_2_time).ToString());
            Console.WriteLine("sw_normalize_2_time: {0} s", C.GetTime(sw_normalize_2_time)); output_data.Add(C.GetTime(sw_normalize_2_time).ToString());
            Console.WriteLine("sw_remain_lof_time:  {0} s", C.GetTime(sw_remain_lof_time)); output_data.Add(C.GetTime(sw_remain_lof_time).ToString());
            Console.WriteLine("sw_total_time:       {0} s", C.GetTime(sw_total_time)); output_data.Add(C.GetTime(sw_total_time).ToString());
            Console.WriteLine("== Setting 1 =========================");
            Console.WriteLine("Normalize:           {0}", G.enableNormalizing); output_data.Add(G.enableNormalizing.ToString());
            Console.WriteLine("Top_N:               {0}", G.TOP_N); output_data.Add(G.TOP_N.ToString());
            Console.WriteLine("DataSliceAtrs:       {0}", C.GetDataSliceAtrs(G.DataSliceAtrs)); output_data.Add(C.GetDataSliceAtrs(G.DataSliceAtrs).ToString());
            Console.WriteLine("== Prune =============================");
            Console.WriteLine("NumPoint:            {0}", G.NumSamplePoint); output_data.Add(G.NumSamplePoint.ToString());
            Console.WriteLine("NumPrunedPoint:      {0}", G.NumPrunedPoint); output_data.Add(G.NumPrunedPoint.ToString());
            Console.WriteLine("PrunedRate:          {0}", Math.Round((double)G.NumPrunedPoint / (double)G.NumSamplePoint, 3)); output_data.Add(Math.Round((double)G.NumPrunedPoint / (double)G.NumSamplePoint, 3).ToString());
            Console.WriteLine("NumNeededkNNPoint:   {0}", G.NumNeededkNNPoint); output_data.Add(G.NumNeededkNNPoint.ToString());
            Console.WriteLine("PrunedRate2:         {0}", Math.Round(1 - (double)G.NumNeededkNNPoint / (double)G.NumSamplePoint, 3)); output_data.Add(Math.Round(1 - (double)G.NumNeededkNNPoint / (double)G.NumSamplePoint, 3).ToString());
            Console.WriteLine("NumNeededTotalPoint: {0}", G.NumNeededTotalPoint); output_data.Add(G.NumNeededTotalPoint.ToString());
            Console.WriteLine("PrunedRate3:         {0}", Math.Round(1 - (double)G.NumNeededTotalPoint / (double)G.NumSamplePoint, 3)); output_data.Add(Math.Round(1 - (double)G.NumNeededTotalPoint / (double)G.NumSamplePoint, 3).ToString());
            Console.WriteLine("== Setting 2 =========================");
            Console.WriteLine("NumDim:              {0}", G.NumDim); output_data.Add(G.NumDim.ToString());
            Console.WriteLine("LOF_K:               {0}", G.LOF_K); output_data.Add(G.LOF_K.ToString());
            Console.WriteLine("NumSampling:         {0}", G.SampleNumber); output_data.Add(G.SampleNumber.ToString());
            Console.WriteLine("min_distance:        {0}", G.min_distance); output_data.Add(G.min_distance.ToString());
            Console.WriteLine("Zp:                  {0}", G.Zp); output_data.Add(G.Zp.ToString());
            Console.WriteLine("G.StatisticsThreshold: {0}", G.StatisticsThreshold); output_data.Add(G.StatisticsThreshold.ToString());
            C.OutputPerformanceToCsv(G.file_performance, output_data);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
        }

        /// <summary>
        /// 全部分データのクエリ結果・信頼区間を計算する
        /// </summary>
        private static void MakeResultDSs()
        {
            using (SqlConnection sn = new SqlConnection(G.strSqlConnection))
            {
                sn.Open();

                System.Globalization.CompareInfo ci = System.Globalization.CultureInfo.CurrentCulture.CompareInfo;

                /// SqlCommand インスタンス
                SqlCommand com = sn.CreateCommand();
                com.CommandTimeout = 0;

                Dictionary<int, List<string>> incompleteDic = new Dictionary<int, List<string>>();

                for (int i = 0; i < G.DataSliceAtrs.Count; i++)  /// 部分データ属性毎にSQlクエリを実行する
                {
                    int ANDatrs = G.DataSliceAtrs[i].Count();  /// AND 属性の数
                    int idxGroupbyVal = ANDatrs;               /// クエリ結果 reader[] において Group-by 値 を指す index
                    int idxAggreVal = ANDatrs + 1;             /// クエリ結果 reader[] において Aggregation 値 を指す index
                    int idxCount = ANDatrs + 2;                /// クエリ結果 reader[] において Count 値 を指す index
                    int idxVar = ANDatrs + 3;                  /// クエリ結果 reader[] において Var 値 を指す index

                    /// SQLクエリ実行
                    com.CommandText = getSqlQuery(G.DataSliceAtrs[i]);
                    if (G.experimentFlug == false) Console.WriteLine(com.CommandText);
                    string DataSliceName = string.Empty;

                    G.DataSliceDic = new Dictionary<string, int>();
                    if (G.experimentFlug == false) G.DataSliceIdx = new Dictionary<int, string>();

                    G.CoordinateList = new List<double[]>(); /// coordinate list
                    List<double> tmpCoordinate = new List<double>();
                    List<string> tmpGroupNameList = new List<string>();

                    G.IntervalList = new List<double[]>();   /// interval list
                    List<double> tmpInterval = new List<double>();

                    int pID = 0;  /// Data Slice ID

                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        reader.Read();  /// do while 風

                        DataSliceName = getDataSliceName(ANDatrs, reader);
                        tmpCoordinate.Add(double.Parse(reader[idxAggreVal].ToString()));
                        tmpGroupNameList.Add(reader[idxGroupbyVal].ToString());
                        tmpInterval.Add(C.ComputeInterval(G.Zp, double.Parse(reader[idxCount].ToString()), double.Parse(reader[idxVar].ToString())));

                        while (reader.Read())
                        {
                            /// 部分データ名で クエリ結果の各行 を識別
                            string prevName = DataSliceName;
                            string currentName = getDataSliceName(ANDatrs, reader);

                            /// 部分データ名が 一致する場合）
                            /// その部分データのクエリ結果は不完全であるため，一時的にキャッシュ
                            if (String.Compare(prevName, currentName, true) == 0)  /// false: 大文字小文字の区別あり
                            {
                                tmpCoordinate.Add(double.Parse(reader[idxAggreVal].ToString())); /// tmp coordinate
                                tmpGroupNameList.Add(reader[idxGroupbyVal].ToString());
                                tmpInterval.Add(C.ComputeInterval(G.Zp, double.Parse(reader[idxCount].ToString()), double.Parse(reader[idxVar].ToString()))); /// tmp interval
                            }
                            else if (ci.Compare(DataSliceName, currentName, System.Globalization.CompareOptions.IgnoreKanaType) == 0)
                            {
                                tmpCoordinate.Add(double.Parse(reader[idxAggreVal].ToString())); /// tmp coordinate
                                tmpGroupNameList.Add(reader[idxGroupbyVal].ToString());
                                tmpInterval.Add(C.ComputeInterval(G.Zp, double.Parse(reader[idxCount].ToString()), double.Parse(reader[idxVar].ToString()))); /// tmp interval
                            }
                            /// 部分データ名が 不一致の場合）
                            /// その部分データのクエリ結果は完全であるため，保存
                            else
                            {
                                /// Create group-by value list
                                if (G.haveGroupByValueList == false)
                                {
                                    if (tmpGroupNameList.Count() == G.GroupValueNum)
                                    {
                                        G.GroupByValueList = new string[G.GroupValueNum];
                                        for (int j = 0; j < G.GroupValueNum; j++)
                                        {
                                            G.GroupByValueList[j] = tmpGroupNameList[j];
                                        }
                                        G.haveGroupByValueList = true;
                                    }
                                }

                                if (G.haveGroupByValueList == true)
                                {
                                    /// Save
                                    if (tmpGroupNameList.Count() == G.GroupValueNum)  /// if it is a data slice that All of Group-by values are retrieved (had)
                                    {
                                        G.CoordinateList.Add(tmpCoordinate.ToArray()); /// tmp coordinate
                                        G.IntervalList.Add(tmpInterval.ToArray()); /// tmp interval
                                    }
                                    else  /// num of Group-by values are within G.GroupValueNum
                                    {
                                        double[] coordinate = new double[G.GroupValueNum];  /// 0 で初期化 : 値がないものは 0
                                        double[] interval = new double[G.GroupValueNum];    /// 0 で初期化 : 値がないものは 0
                                        for (int j = 0; j < tmpGroupNameList.Count(); j++)
                                        {
                                            var correctDimIdx = Array.IndexOf(G.GroupByValueList, tmpGroupNameList[j]);
                                            coordinate[correctDimIdx] = tmpCoordinate[j];
                                            interval[correctDimIdx] = tmpInterval[j];
                                        }
                                        G.CoordinateList.Add(coordinate);
                                        G.IntervalList.Add(interval);
                                    }
                                    
                                }
                                else
                                {
                                    /// Save
                                    if (tmpGroupNameList.Count() != G.GroupValueNum)
                                    {
                                        /// keep ID of the data slice retrieved while GroupByValueList is not created
                                        incompleteDic.Add(pID, tmpGroupNameList);
                                    }
                                    G.CoordinateList.Add(tmpCoordinate.ToArray()); /// tmp coordinate
                                    G.IntervalList.Add(tmpInterval.ToArray()); /// tmp interval
                                }
                                G.DataSliceDic.Add(DataSliceName, pID);
                                if (G.experimentFlug == false) G.DataSliceIdx.Add(pID, DataSliceName);
                                pID++;

                                /// グループ化属性値数が許容数以上の場合のみ確保
                                /*if (tmpCoordinate.Count() >= G.GroupValueThreshold)
                                {
                                    G.CoordinateList.Add(tmpCoordinate.ToArray()); /// tmp coordinate
                                    G.IntervalList.Add(tmpInterval.ToArray()); /// tmp interval
                                    G.DataSliceDic.Add(DataSliceName, pID);
                                    pID++;
                                }*/
                                
                                /// 次の部分データへ
                                DataSliceName = getDataSliceName(ANDatrs, reader);

                                tmpCoordinate = new List<double>(); /// tmp coordinate
                                tmpCoordinate.Add(double.Parse(reader[idxAggreVal].ToString())); /// tmp coordinate
                                tmpGroupNameList = new List<string>();
                                tmpGroupNameList.Add(reader[idxGroupbyVal].ToString());

                                tmpInterval = new List<double>(); /// tmp interval
                                tmpInterval.Add(C.ComputeInterval(G.Zp, double.Parse(reader[idxCount].ToString()), double.Parse(reader[idxVar].ToString()))); /// tmp interval
                            }
                        }

                        reader.Close();
                    }

                    if (G.GroupByValueList == null)
                    {
                        G.GroupByValueList = new string[G.GroupValueNum];
                        for (int j = 0; j < G.GroupValueNum; j++)
                        {
                            G.GroupByValueList[j] = G.GroupByValueListForced[j];
                        }
                    }

                    /// 部分データの最後の１つを保存
                    /// Save
                    if (tmpGroupNameList.Count() == G.GroupValueNum)  /// if it is a data slice that All of Group-by values are retrieved (had)
                    {
                        G.CoordinateList.Add(tmpCoordinate.ToArray()); /// tmp coordinate
                        G.IntervalList.Add(tmpInterval.ToArray()); /// tmp interval
                    }
                    else  /// num of Group-by values are within G.GroupValueNum
                    {
                        double[] coordinate = new double[G.GroupValueNum];
                        double[] interval = new double[G.GroupValueNum];
                        for (int j = 0; j < tmpGroupNameList.Count(); j++)
                        {
                            var correctDimIdx = Array.IndexOf(G.GroupByValueList, tmpGroupNameList[j]);
                            coordinate[correctDimIdx] = tmpCoordinate[j];
                            interval[correctDimIdx] = tmpInterval[j];
                        }
                        G.CoordinateList.Add(coordinate);
                        G.IntervalList.Add(interval);
                    }
                    G.DataSliceDic.Add(DataSliceName, pID);
                    if (G.experimentFlug == false) G.DataSliceIdx.Add(pID, DataSliceName);
                    pID++;
                }

                /// 不完全な部分データのクエリ結果を修正
                foreach (KeyValuePair<int, List<string>> ds in incompleteDic)
                {
                    List<string> tmpGroupNameList = ds.Value;
                    double[] tmpCoordinate = G.CoordinateList[ds.Key];
                    double[] tmpInterval = G.IntervalList[ds.Key];
                    double[] coordinate = new double[G.GroupValueNum];
                    double[] interval = new double[G.GroupValueNum];
                    for (int i = 0; i < tmpGroupNameList.Count(); i++)
                    {
                        var correctDimIdx = Array.IndexOf(G.GroupByValueList, tmpGroupNameList[i]);
                        coordinate[correctDimIdx] = tmpCoordinate[i];
                        interval[correctDimIdx] = tmpInterval[i];
                    }
                    G.CoordinateList[ds.Key] = coordinate;
                    G.IntervalList[ds.Key] = interval;
                }

                sn.Close();
            }
        }

        /// <summary>
        /// クエリ結果・信頼区間を正規化する
        /// </summary>
        private static List<double[]> MakeNormalizeResultDSs()
        {
            List<double[]> coordinateList = G.CoordinateList;

            for (int i = 0; i < G.DataSliceAtrs.Count; i++)
            {
                for (int j = 0; j < coordinateList.Count; j++)
                {
                    double sum = 0;

                    for (int k = 0; k < coordinateList[j].Length; k++) { sum += coordinateList[j][k]; }
                    for (int k = 0; k < coordinateList[j].Length; k++)
                    {
                        coordinateList[j][k] = coordinateList[j][k] / sum;
                        G.IntervalList[j][k] = G.IntervalList[j][k] / sum;
                    }
                }
            }

            return coordinateList;
        }

        /// <summary>
        /// クエリ結果を正規化する
        /// </summary>
        private static List<double[]> ReMakeNormalizeResultDSs()
        {
            List<double[]> coordinateList = G.CoordinateList;

            for (int i = 0; i < G.DataSliceAtrs.Count; i++)
            {
                for (int j = 0; j < coordinateList.Count; j++)
                {
                    double sum = 0;

                    for (int k = 0; k < coordinateList[j].Length; k++) { sum += coordinateList[j][k]; }
                    for (int k = 0; k < coordinateList[j].Length; k++)
                    {
                        coordinateList[j][k] = coordinateList[j][k] / sum;
                    }
                }
            }

            return coordinateList;
        }

        /// <summary>
        /// 全部分データのクエリ結果を再計算する
        /// </summary>
        private static void ReMakeResultDSs()
        {
            using (SqlConnection sn = new SqlConnection(G.strSqlConnection))
            {
                sn.Open();

                System.Globalization.CompareInfo ci = System.Globalization.CultureInfo.CurrentCulture.CompareInfo;

                /// SqlCommand インスタンス
                SqlCommand com = sn.CreateCommand();
                com.CommandTimeout = 0;

                int finalID = G.DataSliceDic.Count();  // 末尾のID

                for (int i = 0; i < G.DataSliceAtrs.Count; i++)  /// 部分データ属性毎にSQlクエリを実行する
                {
                    int ANDatrs = G.DataSliceAtrs[i].Count();  /// AND 属性の数
                    int idxGroupbyVal = ANDatrs;               /// クエリ結果 reader[] において Group-by 値 を指す index
                    int idxAggreVal = ANDatrs + 1;             /// クエリ結果 reader[] において Aggregation 値 を指す index
                    int idxCount = ANDatrs + 2;                /// クエリ結果 reader[] において Count 値 を指す index
                    int idxVar = ANDatrs + 3;                  /// クエリ結果 reader[] において Var 値 を指す index

                    /// SQLクエリ実行
                    com.CommandText = getSqlQuery(G.DataSliceAtrs[i]);
                    string DataSliceName = string.Empty;

                    List<double> tmpCoordinate = new List<double>(); /// tmp coordinate
                    List<string> tmpGroupNameList = new List<string>();

                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        reader.Read();

                        DataSliceName = getDataSliceName(ANDatrs, reader);
                        tmpCoordinate.Add(double.Parse(reader[idxAggreVal].ToString())); /// tmp coordinate
                        tmpGroupNameList.Add(reader[idxGroupbyVal].ToString());

                        while (reader.Read())
                        {
                            /// 部分データ名で クエリ結果の各行 を識別
                            string prevName = DataSliceName;
                            string currentName = getDataSliceName(ANDatrs, reader);

                            /// 部分データ名が 一致する場合）
                            /// その部分データのクエリ結果は不完全であるため，一時的にキャッシュ
                            if (String.Compare(prevName, currentName, true) == 0)  /// false: 大文字小文字の区別あり
                            {
                                tmpCoordinate.Add(double.Parse(reader[idxAggreVal].ToString())); /// tmp coordinate
                                tmpGroupNameList.Add(reader[idxGroupbyVal].ToString());
                            }
                            else if (ci.Compare(DataSliceName, currentName, System.Globalization.CompareOptions.IgnoreKanaType) == 0)
                            {
                                tmpCoordinate.Add(double.Parse(reader[idxAggreVal].ToString())); /// tmp coordinate
                                tmpGroupNameList.Add(reader[idxGroupbyVal].ToString());
                            }
                            /// 部分データ名が 不一致の場合）
                            /// その部分データのクエリ結果は完全であるため，保存
                            else
                            {
                                if (G.DataSliceDic.ContainsKey(DataSliceName))
                                {
                                    var pID = G.DataSliceDic[DataSliceName];

                                    /// Save
                                    if (tmpGroupNameList.Count() == G.GroupValueNum)  /// if it is a data slice that All of Group-by values are retrieved (had)
                                    {
                                        int dim_ID = 0;
                                        foreach (double value in tmpCoordinate)
                                        {
                                            G.CoordinateList[pID][dim_ID] += value;
                                            dim_ID++;
                                        }
                                    }
                                    else  /// num of Group-by values are within G.GroupValueNum
                                    {
                                        double[] coordinate = new double[G.GroupValueNum];
                                        for (int j = 0; j < tmpGroupNameList.Count(); j++)
                                        {
                                            var correctDimIdx = Array.IndexOf(G.GroupByValueList, tmpGroupNameList[j]);
                                            coordinate[correctDimIdx] = tmpCoordinate[j];
                                        }

                                        int dim_ID = 0;
                                        foreach (double value in coordinate)
                                        {
                                            G.CoordinateList[pID][dim_ID] += value;
                                            dim_ID++;
                                        }
                                    }
                                }
                                else
                                {
                                    /// Save
                                    if (tmpGroupNameList.Count() == G.GroupValueNum)  /// if it is a data slice that All of Group-by values are retrieved (had)
                                    {
                                        G.CoordinateList.Add(tmpCoordinate.ToArray()); /// tmp coordinate
                                    }
                                    else  /// num of Group-by values are within G.GroupValueNum
                                    {
                                        double[] coordinate = new double[G.GroupValueNum];
                                        for (int j = 0; j < tmpGroupNameList.Count(); j++)
                                        {
                                            var correctDimIdx = Array.IndexOf(G.GroupByValueList, tmpGroupNameList[j]);
                                            coordinate[correctDimIdx] = tmpCoordinate[j];
                                        }
                                        G.CoordinateList.Add(coordinate);
                                    }
                                    G.DataSliceDic.Add(DataSliceName, finalID);
                                    finalID++;
                                }
                                    
                                /// グループ化属性値数が許容数以上の場合のみ確保
                                /*if (tmpCoordinate.Count() >= G.GroupValueThreshold)
                                {
                                    if (G.DataSliceDic.ContainsKey(DataSliceName))
                                    {
                                        var pID = G.DataSliceDic[DataSliceName];
                                        int dim_ID = 0;
                                        foreach (double value in tmpCoordinate)
                                        {
                                            G.CoordinateList[pID][dim_ID] += value;
                                            dim_ID++;
                                        }
                                    }
                                    else
                                    {
                                        G.CoordinateList.Add(tmpCoordinate.ToArray());
                                        G.DataSliceDic.Add(DataSliceName, finalID);
                                        finalID++;
                                    }
                                }*/

                                /// 次の部分データへ
                                DataSliceName = getDataSliceName(ANDatrs, reader);
                                tmpCoordinate = new List<double>(); /// tmp coordinate
                                tmpCoordinate.Add(double.Parse(reader[idxAggreVal].ToString())); /// tmp coordinate
                                tmpGroupNameList = new List<string>();
                                tmpGroupNameList.Add(reader[idxGroupbyVal].ToString());
                            }
                        }

                        reader.Close();
                    }
                    /// 部分データの最後の１つを保存
                    if (G.DataSliceDic.ContainsKey(DataSliceName))
                    {
                        var pID = G.DataSliceDic[DataSliceName];

                        if (tmpGroupNameList.Count() == G.GroupValueNum)
                        {
                            int dim_ID = 0;
                            foreach (double value in tmpCoordinate)
                            {
                                G.CoordinateList[pID][dim_ID] += value;
                                dim_ID++;
                            }
                        }
                        else  /// num of Group-by values are within G.GroupValueNum
                        {
                            double[] coordinate = new double[G.GroupValueNum];
                            for (int j = 0; j < tmpGroupNameList.Count(); j++)
                            {
                                var correctDimIdx = Array.IndexOf(G.GroupByValueList, tmpGroupNameList[j]);
                                coordinate[correctDimIdx] = tmpCoordinate[j];
                            }

                            int dim_ID = 0;
                            foreach (double value in coordinate)
                            {
                                G.CoordinateList[pID][dim_ID] += value;
                                dim_ID++;
                            }
                        }
                    }
                    else
                    {
                        if (tmpGroupNameList.Count() == G.GroupValueNum)
                        {
                            G.CoordinateList.Add(tmpCoordinate.ToArray()); /// tmp coordinate
                        }
                        else  /// num of Group-by values are within G.GroupValueNum
                        {
                            double[] coordinate = new double[G.GroupValueNum];
                            for (int j = 0; j < tmpGroupNameList.Count(); j++)
                            {
                                var correctDimIdx = Array.IndexOf(G.GroupByValueList, tmpGroupNameList[j]);
                                coordinate[correctDimIdx] = tmpCoordinate[j];
                            }
                            G.CoordinateList.Add(coordinate);
                        }
                        G.DataSliceDic.Add(DataSliceName, finalID);
                        finalID++;
                    }
                }

                sn.Close();
            }
        }

        private static string getSqlQuery(List<string> dataSliceAtr)
        {
            /// SQLクエリ
            ///   SELECT 部分データ1, [部分データ2, ...,] グループ化属性, 集約関数(集約属性) FROM テーブル
            ///   [JOIN ～]
            ///   [WHERE 条件]
            ///   GROUP BY 部分データ1, [部分データ2, ...,] グループ化属性
            ///   ORDER BY 部分データ1, [部分データ2, ...,]

            int ANDatrs = dataSliceAtr.Count();
            string dataSliceAtrsQuery = string.Empty;  /// SQLクエリの中の　部分データ1, [部分データ2, ...,]　の部分
            for (int j = 0; j < ANDatrs; j++) dataSliceAtrsQuery += dataSliceAtr[j] + ", ";

            string sqlQuery = string.Empty;
            sqlQuery = "SELECT " + dataSliceAtrsQuery + G.GroupbyAtr + ", " + G.AggregateFunction + "(" + G.AggregateAtr + ") " + ", count(*), " + G.Var + "FROM " + G.TableName + " ";
            if (G.Join != string.Empty)
            {
                sqlQuery += G.Join + " ";  /// join句追加
            }
            if (G.WhereCondition != string.Empty)
            {
                sqlQuery += "WHERE " + G.WhereCondition + " ";  /// where条件追加
            }
            sqlQuery += "GROUP BY " + dataSliceAtrsQuery + G.GroupbyAtr + " ";
            if (G.Having != string.Empty)
            {
                sqlQuery += "HAVING " + G.Having + " ";  /// Having条件追加
            }
            sqlQuery += "ORDER BY " + dataSliceAtrsQuery + G.GroupbyAtr + "";

            return sqlQuery;
        }

        private static string getDataSliceName(int ANDatrs, SqlDataReader reader)
        {
            string dataSliceName = string.Empty;

            for (int j = 0; j < ANDatrs; j++)
            {
                if (j >= 1) dataSliceName += ", ";     ///2つ目以降は"AND"の文字列を付与
                dataSliceName += reader[j].ToString();  /// 部分データの属性追加
            }

            return dataSliceName;
        }


    }
}
