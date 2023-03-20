using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// グラフ描画用データ
    /// GPSリストに登録されたGPX,FITファイルデータのグラフ表示
    /// 縦軸: 標高、標高差、速度
    /// 横軸: 距離、経過時間、時刻
    /// 移動平均の散布目数の設定
    /// </summary>
    class GpsGraphData
    {
        public DateTime mDateTime;      //  測定時間
        public double mLap;             //  経過時間(s)
        public Point mCoordinate;       //  座標(経度Longitude(deg),緯度Latitude(deg))
        public double mElevator;        //  高度(m)
        public double mDistance;        //  累積距離(km)
        public double mSpeed;           //  速度(km/s)

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="gpsData">GPSデータ</param>
        public GpsGraphData(GpsData gpsData) {
            mDateTime = gpsData.mDateTime;
            mLap = 0;
            mCoordinate =new Point(gpsData.mLongitude, gpsData.mLatitude);
            mElevator = gpsData.mElevator;
            mDistance = 0;
            mSpeed = 0;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="gpsData">GPSデータ</param>
        /// <param name="preData">前回値</param>
        public GpsGraphData(GpsData gpsData, GpsGraphData preData)
        {
            mDateTime = gpsData.mDateTime;
            double subTime = mDateTime.Subtract(preData.mDateTime).TotalSeconds;
            mLap = preData.mLap + subTime;
            mCoordinate = new Point(gpsData.mLongitude, gpsData.mLatitude);
            mElevator = gpsData.mElevator;
            double distance = YLib.CoordinateDistance(mCoordinate, preData.mCoordinate);
            mDistance = preData.mDistance + distance;
            mSpeed = distance / subTime * 3600;
        }
    }

    /// <summary>
    /// GpsGraph.xaml の相互作用ロジック
    /// </summary>
    public partial class GpsGraph : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private double mTextSize = 15;                          //  文字の大きさ
        private Rect mArea;                                     //  グラフの表示領域
        private double mStepYsize;                              //  グラフの補助線の間隔
        private double mStepXsize;                              //  グラフの補助線の間隔
        private int mBackColor = 138;                           //  WhiteSmoke 141色の138番目
        private string[] mGraphYType = new string[] {
            "標高", "標高差", "速度", 
        };
        private string[] mGraphXType = new string[] {
            "距離", "経過時間", "時刻",
        };
        private string[] mXTitle = new string[] {
            "距離(km)", "経過時間", "時刻",
        };
        private string[] mYTitle = new string[] {
            "標高(m)", "標高(m)", "速度(km/h)",
        };
        private string[] mMoveAverageNo = new string[] {
            "なし", "2", "3", "4", "5", "7", "10", "15", "20", "25", "30", "40",
            "50", "60", "80", "100", "200", "300", "500", "1000", "2000", "3000"
        };
        private List<GpsGraphData> mGraphData;
        enum GRAPHDATATYPE { DateTime, Lap, Coordinate, Elevator, Distance, Speed };

        public string mGraphFilePath;
        private YWorldShapes ydraw;
        private YLib ylib = new YLib();

        public GpsGraph()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();       //  Windowの位置とサイズを復元

            ydraw = new YWorldShapes(canvas);
            CbGrphYType.ItemsSource = mGraphYType;
            CbGrphYType.SelectedIndex = 0;
            CbGrphXType.ItemsSource = mGraphXType;
            CbGrphXType.SelectedIndex = 0;
            CbMoveAverage.ItemsSource = mMoveAverageNo;
            CbMoveAverage.SelectedIndex = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = Path.GetFileNameWithoutExtension(mGraphFilePath);
            loadData(mGraphFilePath);
            drawGraph();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();       //  ウィンドの位置と大きさを保存
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                //  ウィンドウの最大化時
                mWindowWidth = System.Windows.SystemParameters.WorkArea.Width;
                mWindowHeight = System.Windows.SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != this.Width ||
                mWindowHeight != this.Height) {
                //  ウィンドウサイズが変わった時
                mWindowWidth = this.Width;
                mWindowHeight = this.Height;
            } else {
                //  ウィンドウサイズが変わらない時は何もしない
                mWindowState = this.WindowState;
                return;
            }
            mWindowState = this.WindowState;
            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            double dx = mWindowWidth - mPrevWindowWidth;
            mPrevWindowWidth = mWindowWidth;
            //  表示の更新
            drawGraph();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.GpsGraphWidth < 100 ||
                Properties.Settings.Default.GpsGraphHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.GpsGraphHeight) {
                Properties.Settings.Default.GpsGraphWidth = mWindowWidth;
                Properties.Settings.Default.GpsGraphHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.GpsGraphTop;
                this.Left = Properties.Settings.Default.GpsGraphLeft;
                this.Width = Properties.Settings.Default.GpsGraphWidth;
                this.Height = Properties.Settings.Default.GpsGraphHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.GpsGraphTop = this.Top;
            Properties.Settings.Default.GpsGraphLeft = this.Left;
            Properties.Settings.Default.GpsGraphWidth = this.Width;
            Properties.Settings.Default.GpsGraphHeight = this.Height;
            Properties.Settings.Default.Save();
        }


        private void CbGrphYType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            drawGraph();
        }

        private void CbGrphXType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            drawGraph();
        }

        private void CbMoveAverage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            drawGraph();
        }

        /// <summary>
        /// グラフの表示
        /// </summary>
        private void drawGraph()
        {
            if (mGraphData != null) {
                initGraphArea();
                setAxis();
                drawData();
            }
        }

        /// <summary>
        /// 表示エリアの初期設定
        /// </summary>
        private void initGraphArea()
        {
            //  データエリアを求める
            setGraphArea();

            //  補助線の間隔を設定
            setStepSize();
            mArea.Height = ylib.graphHeightSize(mArea.Height, mStepYsize);  //  グラフ高さ

            //  アスペクト比無効
            ydraw.setAspectFix(false);

            //  Window領域の設定
            ydraw.setWindowSize(canvas.ActualWidth, canvas.ActualHeight);
            ydraw.setViewArea(0, 0, canvas.ActualWidth, canvas.ActualHeight);
            //  グラフエリアの仮設定
            ydraw.setWorldWindow(mArea.X - mArea.Width * 0.05, mArea.Y + mArea.Height * (1 + 0.05),
                mArea.X + mArea.Width * (1 + 0.05), mArea.Y - mArea.Height * 0.1);

            //  グラフエリアのマージンを求める
            ydraw.setScreenTextSize(mTextSize);
            double leftMargine = 0;
            double bottomMargine = 0;
            double righMargine = Math.Abs(30 / ydraw.world2screenXlength(1));
            double topMargine = Math.Abs(30 / ydraw.world2screenYlength(1));


            //  縦軸の目盛り文字列の最大幅から左側マージンを求める
            for (double y = mArea.Y; y <= mArea.Y + mArea.Height; y += mStepYsize) {
                Size size = ydraw.measureWText(getYScaleValue(y));
                leftMargine = Math.Max(leftMargine, size.Width);
            }
            //  横軸の目盛り文字列の最大幅から下側マージンを求める
            for (double x = mArea.X; x <= mArea.X + mArea.Width; x += mStepXsize) {
                Size size = ydraw.measureWText(getXScaleValue(x));
                bottomMargine = Math.Max(bottomMargine, size.Width);
            }
            leftMargine += righMargine;
            bottomMargine = Math.Abs(ydraw.screen2worldYlength(ydraw.world2screenXlength(bottomMargine)));
            bottomMargine += ydraw.getTextSize();
            bottomMargine += topMargine;

            //  グラフエリアの設定
            ydraw.setWorldWindow(mArea.X - leftMargine, mArea.Y + mArea.Height + topMargine,
                mArea.X + mArea.Width + righMargine, mArea.Y - bottomMargine);
        }

        /// <summary>
        /// 縦軸と横軸の目盛のステップサイズを設定
        /// </summary>
        private void setStepSize()
        {
            //  縦軸目盛り線の間隔
            mStepYsize = ylib.graphStepSize(mArea.Height, 5);
            //  横軸軸目盛り線の間隔
            if (CbGrphXType.SelectedIndex == 0) {
                //  横軸目盛り距離
                mStepXsize = ylib.graphStepSize(mArea.Width, 10);
            } else if (CbGrphXType.SelectedIndex == 1) {
                //  横軸目盛り経過時間(s)
                double minit = mArea.Width / 60.0;  //  分に換算
                if (60.0 * 24.0 * 10.0 < minit) {   //  10日以上は日単位で計算
                    mStepXsize = ylib.graphStepSize(minit / 60.0 / 24.0, 10, 24) * 24.0 * 60.0;
                } else {
                    mStepXsize = ylib.graphStepSize(minit, 10, 60);
                }
                mStepXsize *= 60.0;
            } else if (CbGrphXType.SelectedIndex == 2) {
                //  横軸目盛り経過時間(s)
                double minit = mArea.Width / 60.0 / 10000000.0;  //  分に換算
                if (60.0 * 24.0 * 10.0 < minit) {               //  10日以上は日単位で計算
                    mStepXsize = ylib.graphStepSize(minit / 60.0 / 24.0, 10, 24) * 24.0 * 60.0;
                } else {
                    mStepXsize = ylib.graphStepSize(minit, 10, 60);
                }
                mStepXsize *= 60.0 * 10000000.0;
            }
        }

        /// <summary>
        /// 補助線の描画
        /// </summary>
        private void setAxis()
        {
            ydraw.backColor(ydraw.getColor(mBackColor));
            ydraw.setScreenTextSize(mTextSize);
            ydraw.setThickness(1);

            ydraw.clear();
            //  縦軸の目盛りと補助線の表示
            ydraw.setColor(Brushes.Gray);
            for (double y = mArea.Top; y <= mArea.Bottom; y += mStepYsize) {
                //  補助線
                ydraw.drawWLine(new Point(mArea.Left, y), new Point(mArea.Right, y));
                //  目盛
                ydraw.drawWText(getYScaleValue(y), new Point(mArea.Left, y), 0,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
                if (y == mArea.Top && y % mStepXsize != 0) {
                    y -= y % mStepYsize;
                }
            }
            //  縦軸ののタイトル
            ydraw.drawWText(mYTitle[CbGrphYType.SelectedIndex],
                new Point(ydraw.mWorld.Left, mArea.Y + mArea.Height * 0.5),
                -Math.PI / 2, HorizontalAlignment.Center, VerticalAlignment.Bottom);

            //  横軸軸の目盛りと補助線の表示
            ydraw.setColor(Brushes.Aqua);
            double textMargine = Math.Abs(3 / ydraw.world2screenYlength(1));
            for (double x = mArea.Left; x < mArea.Right; x += mStepXsize) {
                //  指定ステップで補助線表示
                ydraw.drawWLine(new Point(x, mArea.Y), new Point(x, mArea.Y + mArea.Height));
                //  目盛表示
                if (x < mArea.Right - mArea.Width * 0.05)
                    ydraw.drawWText(getXScaleValue(x), new Point(x, mArea.Y - textMargine),
                        -Math.PI / 2, HorizontalAlignment.Left, VerticalAlignment.Center);
                if (x == mArea.Left && x % mStepXsize != 0)
                    x -= x % mStepXsize;
            }
            ydraw.drawWText(getXScaleValue(mArea.Right),
                new Point(mArea.Right, mArea.Y - textMargine),
                -Math.PI / 2, HorizontalAlignment.Left, VerticalAlignment.Center);

            //  横軸タイトル
            ydraw.drawWText(mXTitle[CbGrphXType.SelectedIndex],
                new Point(mArea.X + mArea.Width / 2.0, ydraw.mWorld.Bottom), 0,
                HorizontalAlignment.Center, VerticalAlignment.Bottom);

            //  グラフ枠の表示
            ydraw.setColor(Brushes.Black);
            ydraw.setFillColor(null);
            ydraw.drawWRectangle(new Point(mArea.Left, mArea.Top), new Point(mArea.Right, mArea.Bottom), 0);
        }

        /// <summary>
        /// グラフデータの表示
        /// </summary>
        private void drawData()
        {
            ydraw.setColor(Brushes.Black);
            ydraw.setThickness(1);
            Point sp = new Point(getXvalue(0), getYvalue(0));
            for (int i = 1; i < mGraphData.Count; i++) {
                Point ep = new Point(getXvalue(i), getYvalue(i));
                ydraw.drawWLine(sp, ep);
                sp = ep;
            }
        }

        /// <summary>
        /// グラフエリアの設定
        /// </summary>
        private void setGraphArea()
        {
            (double ymin, double ymax) = getMinMaxYvalue();
            //  縦範囲
            if (CbGrphYType.SelectedIndex == 0) {
                //  標高
                mArea.Y = 0;
                mArea.Height = ymax;
            } else if (CbGrphYType.SelectedIndex == 1) {
                //  標高差
                mArea.Y = ymin;
                mArea.Height = ymax - ymin; 
            } else if (CbGrphYType.SelectedIndex == 2) {
                //  速度
                mArea.Y = 0;
                mArea.Height = ymax;
            }
            //  横範囲
            if (CbGrphXType.SelectedIndex == 0) {
                //  距離
                mArea.X = mGraphData[0].mDistance;
                mArea.Width = mGraphData[mGraphData.Count - 1].mDistance - mArea.X;
            } else if (CbGrphXType.SelectedIndex == 1) {
                //  経過時間(sec)
                mArea.X = mGraphData[0].mLap;
                mArea.Width = mGraphData[mGraphData.Count - 1].mLap - mArea.X;
            } else if (CbGrphXType.SelectedIndex == 2) {
                // 時刻(Ticks = 1/10,000,000second)
                mArea.X = mGraphData[0].mDateTime.Ticks;
                mArea.Width = mGraphData[mGraphData.Count - 1].mDateTime.Ticks - mArea.X;
            }
        }

        /// <summary>
        /// 縦軸目盛
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        private string getYScaleValue(double y)
        {
            if (CbGrphYType.SelectedIndex == 0) {           //  標高
                return y.ToString("#,##0");
            } else if (CbGrphYType.SelectedIndex == 1) {    //  標高
                return y.ToString("#,##0");
            } else if (CbGrphYType.SelectedIndex == 2) {    //  速度
                return y.ToString("#,##0.0");
            } else {
                return y.ToString();
            }
        }

        /// <summary>
        /// 横軸目盛
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private string getXScaleValue(double x)
        {
            if (CbGrphXType.SelectedIndex == 0) {           //  距離
                return x.ToString("#,##0.0");
            } else if (CbGrphXType.SelectedIndex == 1) {    //  経過時間
                return ylib.second2String(x, true);
            } else if (CbGrphXType.SelectedIndex == 2) {    //  時刻
                DateTime dt = new DateTime((Int64)x);
                return dt.ToString("yyyy/MM/dd\n HH:mm:ss");
            } else {
                return x.ToString();
            }
        }

        /// <summary>
        /// 表示グラフの種類に縦軸データのmin,maxを求める
        /// </summary>
        /// <returns>(min.max)</returns>
        private (double, double) getMinMaxYvalue()
        {
            double max = double.MinValue;
            double min = double.MaxValue;
            for (int i = 1; i < mGraphData.Count; i++) {
                double v = getYvalue(i);
                max = Math.Max(max, v);
                min = Math.Min(min, v);
            }
            return (min, max);
        }

        /// <summary>
        /// 縦軸のデータ値
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private double getYvalue(int i)
        {
            int moveAveNo = 1;
            if (0 <= CbMoveAverage.SelectedIndex)
                moveAveNo = ylib.intParse(CbMoveAverage.Items[CbMoveAverage.SelectedIndex].ToString());
            if (CbGrphYType.SelectedIndex == 0) {           //  標高(m)
                if (CbMoveAverage.SelectedIndex == 0) {
                    return mGraphData[i].mElevator;
                } else {
                    return movingAverage(mGraphData, GRAPHDATATYPE.Elevator, i, moveAveNo, true);
                }
            } else if (CbGrphYType.SelectedIndex == 1) {    //  標高差(m)
                if (CbMoveAverage.SelectedIndex == 0) {
                    return mGraphData[i].mElevator;
                } else {
                    return movingAverage(mGraphData, GRAPHDATATYPE.Elevator, i, moveAveNo, true);
                }
            } else if (CbGrphYType.SelectedIndex == 2) {    //  速度(km/h)
                if (CbMoveAverage.SelectedIndex == 0) {
                    return mGraphData[i].mSpeed;
                } else {
                    return movingAverage(mGraphData, GRAPHDATATYPE.Speed, i, moveAveNo, true);
                }
            } else {
                return 0;
            }
        }

        /// <summary>
        /// 移動平均を求める
        /// </summary>
        /// <param name="data">データリスト</param>
        /// <param name="pos">データ位置</param>
        /// <param name="nearCount">平均値のデータ数</param>
        /// <param name="center">移動平均の中心合わせ</param>
        /// <returns>指定値の平均値</returns>
        private double movingAverage(List<GpsGraphData> data, GRAPHDATATYPE type, int pos, int nearCount, bool center)
        {
            double sum = 0;
            int count = 0;
            int startCount = center ? nearCount / 2 : nearCount;
            for (int i = Math.Max(0, pos - startCount); i < Math.Min(data.Count, pos + startCount); i++) {
                if (type == GRAPHDATATYPE.Elevator) {
                    sum += data[i].mElevator;
                } else if (type == GRAPHDATATYPE.Speed) {
                    sum += data[i].mSpeed;
                }
                count++;
            }
            return sum / count;
        }

        /// <summary>
        /// 横軸のデータ値
        /// </summary>
        /// <param name="i"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private double getXvalue(int i)
        {
            if (CbGrphXType.SelectedIndex == 0) {           //  距離
                return mGraphData[i].mDistance;
            } else if (CbGrphXType.SelectedIndex == 1) {    //  経過時間(s)
                return mGraphData[i].mLap;
            } else if (CbGrphXType.SelectedIndex == 2) {    //  時刻(Ticks)
                return mGraphData[i].mDateTime.Ticks;       //  1000万分の1秒単位
            } else {
                return 0;
            }
        }

        /// <summary>
        /// グラフ用のgpxデータの読込
        /// </summary>
        /// <param name="graphFilePath">GPXファイルパス</param>
        private void loadData(string graphFilePath)
        {
            //  ファイルデータの読込
            string ext = Path.GetExtension(graphFilePath);
            List<GpsData> gpsListData;
            if (ext.ToLower().CompareTo(".gpx") == 0) {
                GpxReader gpxReader = new GpxReader(graphFilePath, GpxReader.DATATYPE.gpxData);
                gpxReader.dataChk();
                gpsListData = gpxReader.mListGpsData;
            } else if (ext.ToLower().CompareTo(".fit") == 0) {
                FitReader fitReader = new FitReader(graphFilePath);
                fitReader.getDataRecordAll(FitReader.DATATYPE.gpxData);
                fitReader.dataChk();
                gpsListData = fitReader.mListGpsData;
            } else {
                return;
            }
            if (mGraphData == null)
                mGraphData = new List<GpsGraphData> ();

            if (0 <  gpsListData.Count) {
                mGraphData.Add(new GpsGraphData(gpsListData[0]));
                for (int i = 1; i < gpsListData.Count; i++) {
                    GpsGraphData gpsGraphData = new GpsGraphData(gpsListData[i], mGraphData[i-1]);
                    mGraphData.Add(gpsGraphData);
                }
            }
        }
    }
}
