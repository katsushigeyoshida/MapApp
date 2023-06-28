using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Wpf3DLib;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// _3DMapView.xaml の相互作用ロジック
    /// </summary>
    public partial class Map3DView : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private System.Windows.WindowState mWindowState = System.Windows.WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        public MapData mMapData;

        private List<Vector3[,]> mPositionList;     //  座標データリスト
        private int mXDivideCount = 40;             //  X方向の分割数
        private int mYDivideCount = 40;             //  Y方向の分割数
        private double mXStart = -100;              //  X方向の開始位置
        private double mXEnd = 100;                 //  X方向の終了位置
        private double mYStart = -100;              //  Y方向の開始位置
        private double mYEnd = 100;                 //  Y方向の終了位置
        private Vector3 mMin;                       //  表示領域の最小値
        private Vector3 mMax;                       //  表示領域の最大値
        private Vector3 mManMin;
        private Vector3 mManMax;
        private double mMinZ = 0;
        private double mMaxZ = 2000;
        private Vector3 mInitRotate = new Vector3(-80f, 0f, 0f);     //  初期回転角
        private float mInitZoom = 1.8f;             //  初期倍率

        private string[] mElevatorRatioTitle = {    //  標高倍率タイトル
            "1.0", "1.5", "2.0", "3.0", "4.0", "5.0", "10.0", "20.0", "30.0"
        };
        private string[] mResolutionTitle = {       //  解像度(分割数)タイトル
            "50", "100", "200", "300", "400", "500", "600", "800", "1000"
        };
        private double mElevatorRatio = 1.0;        //  標高倍率
        private int mResolution = 200;              //  3D表示の解像度
        private bool mFrameDisp = false;            //  枠表示の有無
        private bool mSerface = true;               //  サーフェイスモデル/ワイヤモデルの切替
        private Color4 mBackColor = Color4.Aqua;    //  背景色(初期値)
        private double mScaleUnit = 1000.0;         //  スケールの大きさ(m)

        //  標高の色配分　(Kashimil3Dのデータを参照)
        private Dictionary<string,　List<int[]>> mColorPallet = new Dictionary<string,　List<int[]>> {
            {   // 地図帳配色(StandardPal)
                "地図帳配色",
                //            標高(m),  R,  G,    B
                new List<int[]> {
                    new int[] {    0,  20, 150,  20 },
                    new int[] { 1000, 200, 200,   0 },
                    new int[] { 2000, 135,  71,   0 },
                    new int[] { 3000,  83,  47,   6 },
                    new int[] { 4000, 103,  94,   0 },
                }
            },
            {   // 5mメッシュ微地形強調(5mBitikei.pal)
                "微地形強調",
                new List<int[]> {
                    new int[] {   -5,  37,  64,  90},
                    new int[] {   -4, 115, 164, 200},
                    new int[] {   -3,  73,  45, 102},
                    new int[] {   -2, 190, 179, 204},
                    new int[] {   -1,  49, 102, 134},
                    new int[] {    0, 205, 220, 217},
                    new int[] {    1,  40,  98,  84},
                    new int[] {    2, 158, 207, 192},
                    new int[] {    3,  40,  98,  44},
                    new int[] {    4, 149, 221, 140},
                    new int[] {    5,  75, 100,  26},
                    new int[] {    6, 188, 207, 139},
                    new int[] {    7, 108, 114,  37},
                    new int[] {    8, 221, 216, 153},
                    new int[] {   10, 138, 117,  45},
                    new int[] {   20, 222, 206, 152},
                    new int[] {   30, 148,  88,  35},
                    new int[] {   40, 218, 154,  97},
                    new int[] {   50,  73,  61,  86},
                    new int[] {   60, 190, 179, 204},
                    new int[] {   70,  49, 102, 134},
                    new int[] {   80, 205, 220, 217},
                    new int[] {   90,  40,  98,  84},
                    new int[] {  100, 158, 207, 192},
                    new int[] {  200,  40,  98,  44},
                    new int[] {  300, 149, 221, 140},
                    new int[] {  400,  75, 100,  26},
                    new int[] {  500, 188, 207, 139},
                    new int[] {  600, 108, 114,  37},
                    new int[] {  700, 221, 216, 153},
                    new int[] {  800, 138, 117,  45},
                    new int[] {  900, 222, 206, 152},
                    new int[] { 1000, 148,  88,  35},
                    new int[] { 2000, 218, 154,  97},
                    new int[] { 2100, 128, 202, 123},
                    new int[] { 2200, 218, 221, 111},
                    new int[] { 2300, 218, 154,  97},
                    new int[] { 2500, 255, 255, 255},
                    new int[] { 8000, 255, 255, 255},
                }
            },
        };
        private string mColorPalleteTitle = "地図帳配色";    //  標高色配分タイトル
        private string mColorPalletFile = "Map3DColorPallet.csv";

        private GLControl glControl;                //  OpenTK.GLcontrol
        private GL3DLib m3Dlib;                     //  三次元表示ライブラリ
        private YLib mYlib = new YLib();            //  単なるライブラリ


        public Map3DView()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();       //  Windowの位置とサイズを復元

            //  カラーパレットの読込
            LoadColorPallet(mColorPalletFile);

            //  OpenGLの設定
            glControl = new GLControl();
            m3Dlib = new GL3DLib(glControl);
            m3Dlib.initPosition(mInitZoom, mInitRotate.X, mInitRotate.Y, mInitRotate.Z); // 表示位置関係を初期化(拡大率と回転方向)
            //m3Dlib.initMove(0.0f, 0.0f, 0.0f);
            m3Dlib.setBackColor(mBackColor);            //  背景色
            m3Dlib.mAxisDisp = false;

            glControl.Load += glControl_Load;
            glControl.Paint += glControl_Paint;
            glControl.Resize += glControl_Resize;
            glControl.MouseDown += glControl_MouseDown;
            glControl.MouseUp += glControl_MouseUp;
            glControl.MouseMove += glControl_MouseMove;
            glControl.MouseWheel += glControl_MouseWheel;

            glMapView.Child = glControl;                //  OpenGLをWindowsに接続

            //  コントロールの設定
            CbAspect.ItemsSource = mElevatorRatioTitle;
            CbAspect.SelectedIndex = 0;
            CbResolution.ItemsSource = mResolutionTitle;
            CbResolution.SelectedIndex = Array.IndexOf(mResolutionTitle, mResolution.ToString());
            CbColorPallete.ItemsSource = mColorPallet.Keys.ToList();
            CbColorPallete.SelectedIndex = 0;
            CbBackColor.ItemsSource = GL3DLib.mColor4Title;
            CbBackColor.SelectedIndex = Array.IndexOf(GL3DLib.mColor4, mBackColor);
            CbFrame.IsChecked = mFrameDisp;
        }

        /// <summary>
        /// OpenGLの初期設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Load(object sender, EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);

            //GL.Enable(EnableCap.Lighting);    //  光源の使用
            //float[] position = new float[] { 1.0f, 2.0f, 3.0f, 0.0f };
            //GL.Light(LightName.Light0, LightParameter.Position, position);
            //GL.Enable(EnableCap.Light0);

            GL.PointSize(3.0f);                 //  点の大きさ
            GL.LineWidth(1.5f);                 //  線の太さ
            m3Dlib.mZoomMax = 5.0f;             //  最大拡大率

            setParameter();
            makePlotData();                     //  座標データの作成
            setHeightParameter(false);          //  表示領域の再設定(高さ方向)
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 再表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            renderFrame();

            //throw new NotImplementedException();
        }

        /// <summary>
        /// ビューサイズの変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Resize(object sender, EventArgs e)
        {
            GL.Viewport(glControl.ClientRectangle);

            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスホイールの変更で拡大縮小
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseWheel(object sender, MouseEventArgs e)
        {
            float delta = (float)e.Delta / 1000f;// - wheelPrevious;
            m3Dlib.setZoom(delta);

            renderFrame();

            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスの移動でオブジェクトの回転
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (m3Dlib.moveObject(e.X, e.Y))
                renderFrame();

            //throw new NotImplementedException();
        }

        /// <summary>
        /// 移動解除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseUp(object sender, MouseEventArgs e)
        {
            m3Dlib.setMoveEnd();

            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスボタンを押したままでオブジェクトの移動
        /// 左ボタンで回転、右ボタンで上下左右へ移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                m3Dlib.setMoveStart(true, e.X, e.Y);
            } else if (e.Button == MouseButtons.Right) {
                m3Dlib.setMoveStart(false, e.X, e.Y);
            }

            //throw new NotImplementedException();
        }

        /// <summary>
        /// 三次元データ表示
        /// </summary>
        private void renderFrame()
        {
            m3Dlib.renderFrameStart();
            foreach (Vector3[,] position in mPositionList) {
                //  3Dグラフの表示
                if (mSerface) {
                    //m3Dlib.drawSurfaceShape(position);
                    m3Dlib.drawSurfaceShape(elevator2Color, position);
                } else {
                    m3Dlib.drawWireShape(position);
                }
            }
            m3Dlib.setAreaFrameDisp(mFrameDisp);   //  領域軸表示設定
            //m3Dlib.drawAxis();
            if (mFrameDisp)
                drawFrame();
            drawScale();
            m3Dlib.rendeFrameEnd();
        }

        /// <summary>
        /// 表示パラメータの設定
        /// </summary>
        private void setParameter()
        {
            mXStart = mMapData.mStart.X;                    //  X開始値
            mXEnd = mXStart + mMapData.mColCount;           //  X終了値
            mXDivideCount = mResolution;                    //  X分割数
            mYStart = mMapData.mStart.Y;                    //  Y開始値
            mYEnd = mYStart + mMapData.getRowCountF();      //  Y終了値
            mYDivideCount = mResolution;                    //  Y分割数
            //  サンプルデータ設定
            //mXStart = -5;
            //mXEnd = 5;
            //mYStart = -5;
            //mYEnd = 5;
        }

        //  三軸スケール用
        private double mXStepSize = 0;   //  1stepの距離(m)
        private double mYStepSize = 0;   //  1stepの距離(m)

        /// <summary>
        /// 表示データの作成と領域の取得
        /// </summary>
        private void makePlotData()
        {
            //  プロットのステップ幅をもとめる
            double dx = (mXEnd - mXStart) / mXDivideCount;
            double dy = (mYEnd - mYStart) / mYDivideCount;
            //  1stepの距離(m)
            mXStepSize = mMapData.map2Distance(new Point(mXStart, mYStart), new Point(mXEnd, mYStart)) / (mXEnd - mXStart) * 1000.0;
            mYStepSize = mMapData.map2Distance(new Point(mXStart, mYStart), new Point(mXStart, mYEnd)) / (mYEnd - mYStart) * 1000.0;


            //  プロットデータの配列領域の取得
            mPositionList = new List<Vector3[,]>();
            bool arearInit = true;
            //  座標設定と表示領域を求める
            Vector3[,] position = new Vector3[mYDivideCount + 1, mXDivideCount + 1];
            for (int i = 0; i <= mYDivideCount; i++) {
                for (int j = 0; j <= mXDivideCount; j++) {
                    //  分割位置からMap座標と標高を求める
                    position[i, j] = map2Position(i, j, dx, dy);
                    //  表示領域の取得
                    if (arearInit) {
                        m3Dlib.setArea(new Vector3(position[i, j]), new Vector3(position[i, j]));
                        arearInit = false;
                    } else {
                        m3Dlib.extendArea(position[i, j]);
                    }
                }
            }
            mPositionList.Add(position);

            //  表示領域をチェックする
            m3Dlib.areaCheck();
            //  表示領域を取得
            mManMax = mMax = m3Dlib.getAreaMax();
            mManMin = mMin = m3Dlib.getAreaMin();
            //  カラーレベルの設定
            //m3Dlib.setColorLevel(mMin.Z, mMax.Z);
        }

        /// <summary>
        /// 三軸スケール表示
        /// </summary>
        private void drawScale()
        {
            double dx = mScaleUnit / mXStepSize;
            double dy = mScaleUnit / mYStepSize;
            Vector3 org = new Vector3((float)mXStart, (float)mYStart, 0f);
            Vector3 orgX = new Vector3((float)(mXStart + dx), (float)mYStart, 0f);
            Vector3 orgY = new Vector3((float)mXStart, (float)(mYStart + dy), 0f);
            Vector3 orgZ = new Vector3((float)mXStart, (float)mYStart, (float)mScaleUnit);
            GL.Color3(System.Drawing.Color.Black);
            m3Dlib.drawLine(org, orgX);
            m3Dlib.drawLine(org, orgY);
            m3Dlib.drawLine(org, orgZ);
        }

        /// <summary>
        /// 枠線をつける
        /// 高さは1000m単位、XYはタイルサイズ
        /// </summary>
        private void drawFrame()
        {
            GL.Color3(System.Drawing.Color.LightGreen);
            for (int z = 0; z < 6; z++) {
                Vector3 xsys = new Vector3((float)mXStart, (float)mYStart, (float)mScaleUnit * z);
                Vector3 xeys = new Vector3((float)mXEnd, (float)mYStart, (float)mScaleUnit * z);
                Vector3 xsye = new Vector3((float)mXStart, (float)mYEnd, (float)mScaleUnit * z);
                Vector3 xeye = new Vector3((float)mXEnd, (float)mYEnd, (float)mScaleUnit * z);
                m3Dlib.drawLine(xsys, xeys);
                m3Dlib.drawLine(xsye, xeye);
                m3Dlib.drawLine(xsys, xsye);
                m3Dlib.drawLine(xeys, xeye);
                for (int x = (int)mXStart; x < (int)mXEnd; x++) {
                    Vector3 ys = new Vector3((float)x + 1, (float)mYStart, (float)mScaleUnit * z);
                    Vector3 ye = new Vector3((float)x + 1, (float)mYEnd, (float)mScaleUnit * z);
                    m3Dlib.drawLine(ys, ye);
                }
                for (int y = (int)mYStart; y < (int)mYEnd; y++) {
                    float yf = (float)(mYEnd - ((double)(y + 1) - mYStart));
                    Vector3 xs = new Vector3((float)mXStart, yf, (float)mScaleUnit * z);
                    Vector3 xe = new Vector3((float)mXEnd, yf, (float)mScaleUnit * z);
                    m3Dlib.drawLine(xs, xe);
                }
            }

        }

        /// <summary>
        /// 分割位置からMap座標と標高に変換する
        /// </summary>
        /// <param name="i">X方向分割位置</param>
        /// <param name="j">Y方向分割位置</param>
        /// <param name="dx">1stepのX幅(Map座標)</param>
        /// <param name="dy">1stepのY幅(Map座標)</param>
        /// <returns>三次元座標</returns>
        private Vector3 map2Position(int i, int j, double dx, double dy)
        {
            Vector3 pos = new Vector3();
            pos.X = (float)(mXStart + dx * j);      //  X座標(Map座標)
            pos.Y = (float)(mYStart + dy * i);      //  Y座標(Map座標)
            pos.Z = (float)mMapData.getMapElavtor(new Point(pos.X, pos.Y), null);  //  標高データ(m)
            pos.Y = (float)(mYEnd - dy * i);        //  Y方向の向きをかえる
            return pos;
        }

        /// <summary>
        /// 標高倍率の設定(アスペクトの調整)
        /// </summary>
        /// <param name="autoHeight">自動設定</param>
        private void setHeightParameter(bool autoHeight = false)
        {
            //  東西方向の長さ
            Point sp = mMapData.map2Coordinates(mMapData.mStart);
            Point ep = mMapData.map2Coordinates(new Point(mMapData.mStart.X + mMapData.mColCount, mMapData.mStart.Y));
            double dis = mYlib.coordinateDistance(sp, ep) * 1000.0;     //  (m)

            mManMin = mMin;
            mManMax = mMax;
            if (autoHeight) {
                mManMin.Z = mMin.Z;
                mManMax.Z = mMax.Z;
            } else {
                mManMin.Z = (float)(mMinZ + mMax.Z / 2.0 - dis / 2.0 / mElevatorRatio);
                mManMax.Z = (float)(dis / 2.0 / mElevatorRatio);
            }
            m3Dlib.setArea(mManMin, mManMax);
        }

        /// <summary>
        /// 標高に対する色の設定(GL3DLibのdrawSurfaceShapeの色設定に使用)
        /// 標高によるグラデーションを作成
        /// </summary>
        /// <param name="ele">標高(m)</param>
        /// <returns>カラー(Color4)</returns>
        public Color4 elevator2Color(double ele)
        {
            Color4 col = new Color4();
            col.R = mColorPallet[mColorPalleteTitle][mColorPallet[mColorPalleteTitle].Count - 1][1];
            col.G = mColorPallet[mColorPalleteTitle][mColorPallet[mColorPalleteTitle].Count - 1][2];
            col.B = mColorPallet[mColorPalleteTitle][mColorPallet[mColorPalleteTitle].Count - 1][3];
            for (int i = 0; i < mColorPallet[mColorPalleteTitle].Count - 1; i++) {
                if (mColorPallet[mColorPalleteTitle][i][0] < ele && ele < mColorPallet[mColorPalleteTitle][i + 1][0]) {
                    col.R = (float)((mColorPallet[mColorPalleteTitle][i + 1][1] - mColorPallet[mColorPalleteTitle][i][1]) *
                        (ele - mColorPallet[mColorPalleteTitle][i][0]) / (mColorPallet[mColorPalleteTitle][i + 1][0] - mColorPallet[mColorPalleteTitle][i][0]) +
                        mColorPallet[mColorPalleteTitle][i][1]);
                    col.G = (float)((mColorPallet[mColorPalleteTitle][i + 1][2] - mColorPallet[mColorPalleteTitle][i][2]) *
                        (ele - mColorPallet[mColorPalleteTitle][i][0]) / (mColorPallet[mColorPalleteTitle][i + 1][0] - mColorPallet[mColorPalleteTitle][i][0]) +
                        mColorPallet[mColorPalleteTitle][i][2]);
                    col.B = (float)((mColorPallet[mColorPalleteTitle][i + 1][3] - mColorPallet[mColorPalleteTitle][i][3]) *
                        (ele - mColorPallet[mColorPalleteTitle][i][0]) / (mColorPallet[mColorPalleteTitle][i + 1][0] - mColorPallet[mColorPalleteTitle][i][0]) +
                        mColorPallet[mColorPalleteTitle][i][3]);
                    break;
                }
            }
            col.R /= 256f;
            col.G /= 256f;
            col.B /= 256f;
            return col;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveColorPallet(mColorPalletFile);  //  カラーパレットの保存
            WindowFormSave();       //  ウィンドの位置と大きさを保存
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.Map3DViewWidth < 100 ||
                Properties.Settings.Default.Map3DViewHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.Map3DViewHeight) {
                Properties.Settings.Default.Map3DViewWidth = mWindowWidth;
                Properties.Settings.Default.Map3DViewHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.Map3DViewTop;
                this.Left = Properties.Settings.Default.Map3DViewLeft;
                this.Width = Properties.Settings.Default.Map3DViewWidth;
                this.Height = Properties.Settings.Default.Map3DViewHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Map3DViewTop = this.Top;
            Properties.Settings.Default.Map3DViewLeft = this.Left;
            Properties.Settings.Default.Map3DViewWidth = this.Width;
            Properties.Settings.Default.Map3DViewHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (this.WindowState != mWindowState &&
                this.WindowState == System.Windows.WindowState.Maximized) {
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
            //sampleGraphInit();
            //drawSampleGraph(mStartPosition, mEndPosition);

        }

        /// <summary>
        /// キー入力処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.C && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control) {
                //  Ctrl + C で GL画面をキャプチャーしてクリップボードに入れる
                YDrawingShapes ydraw = new YDrawingShapes();
                Point sp = glMapView.PointToScreen(new Point(0, 0));
                ydraw.screenCapture((int)(sp.X), (int)(sp.Y), (int)glMapView.ActualWidth, (int)glMapView.ActualHeight);
            }
        }

        /// <summary>
        /// 標高倍率の選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbAspect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= CbAspect.SelectedIndex) {
                mElevatorRatio = mYlib.string2double(CbAspect.Items[CbAspect.SelectedIndex].ToString());
            }
            if (mMapData != null) {
                setParameter();
                makePlotData();              //  座標データの作成
                setHeightParameter(false);   //  表示領域の再設定(高さ方向)
                renderFrame();
            }
        }

        /// <summary>
        /// 解像度の選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbResolution_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= CbResolution.SelectedIndex) {
                mResolution = mYlib.intParse(CbResolution.Items[CbResolution.SelectedIndex].ToString());
            }
            if (mMapData != null) {
                setParameter();
                makePlotData();              //  座標データの作成
                setHeightParameter(false);   //  表示領域の再設定(高さ方向)
                renderFrame();
            }
        }

        /// <summary>
        /// 標高に対する配色パターンの設定変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbColorPallete_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= CbColorPallete.SelectedIndex) {
                mColorPalleteTitle = CbColorPallete.Items[CbColorPallete.SelectedIndex].ToString();
            }
            if (mMapData != null) {
                //setParameter();
                //makePlotData();              //  座標データの作成
                //setHeightParameter(false);   //  表示領域の再設定(高さ方向)
                renderFrame();
            }
        }

        /// <summary>
        /// 背景色の変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbBackColor_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= CbBackColor.SelectedIndex) {
                mBackColor = GL3DLib.mColor4[CbBackColor.SelectedIndex];
            }
            if (mMapData != null) {
                m3Dlib.setBackColor(mBackColor);
                renderFrame();
            }
        }

        /// <summary>
        /// オブジェクトの向きなどを初期化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtReset_Click(object sender, RoutedEventArgs e)
        {
            m3Dlib.initPosition(mInitZoom, mInitRotate.X, mInitRotate.Y, mInitRotate.Z);   // 表示位置関係を初期化(拡大率と回転方向)
            renderFrame();
        }

        /// <summary>
        /// 枠線の表示/非表示の切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbFrame_Checked(object sender, RoutedEventArgs e)
        {
            mFrameDisp = true;
            renderFrame();
        }

        /// <summary>
        /// 枠線の表示/非表示の切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbFrame_Unchecked(object sender, RoutedEventArgs e)
        {
            mFrameDisp = false;
            renderFrame();
        }

        /// <summary>
        /// ファイルからカラーパレットデータの読込
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void LoadColorPallet(string path)
        {
            if (!File.Exists(path))
                return;
            List<string> palletList = mYlib.loadListData(path);
            if (0 < palletList.Count) {
                mColorPallet = list2DicList(palletList);
            }
        }

        /// <summary>
        /// カラーパレットをファイルに保存
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void SaveColorPallet(string path)
        {
            if (File.Exists(path))
                return;
            List<string> palletList = dicList2List(mColorPallet);
            palletList.Insert(0, "タイトル/標高(m),R,G,B");
            mYlib.saveListData(path, palletList);
        }


        /// <summary>
        /// カラーパレットのバイナリデータをテキストデータに変換
        /// </summary>
        /// <param name="pallet">カラーパレットデータ</param>
        /// <returns>テキストリスト</returns>
        private List<string> dicList2List(Dictionary<string, List<int[]>> pallet)
        {
            List<string> palletList = new List<string>();
            foreach (string key in pallet.Keys) {
                palletList.Add(key);
                foreach (int[] data in pallet[key]) {
                    palletList.Add(string.Join(",", data));
                }
            }
            return palletList;
        }

        /// <summary>
        /// カラーパレットのテキストデータをバイナリデータに変換
        /// </summary>
        /// <param name="palletList">テキストデータ</param>
        /// <returns>バイナリデータ</returns>
        private Dictionary<string, List<int[]>> list2DicList(List<string> palletList)
        {
            Dictionary<string, List<int[]>> pallet = new Dictionary<string, List<int[]>>();
            char[] sep = { ',' };
            string key = "";
            List<int[]> buf = new List<int[]>();
            foreach (string data in palletList) {
                if (0 == data.Length || data[0] == '#' || 0 <= data.IndexOf("タイトル"))
                    continue;
                string[] datas = data.Split(sep);
                if (datas.Length == 1) {
                    if (0 < buf.Count && 0 < key.Length)
                        pallet.Add(key, buf);
                    key = datas[0];
                    buf = new List<int[]>();
                } else if (buf != null && 3 < datas.Length) {
                    int[] intBuf = new int[datas.Length];
                    for (int i = 0; i < datas.Length; i++)
                        intBuf[i] = mYlib.intParse(datas[i]);
                    buf.Add(intBuf);
                }
            }
            if (0 < buf.Count && 0 < key.Length)
                pallet.Add(key, buf);
            return pallet;
        }
    }
}
