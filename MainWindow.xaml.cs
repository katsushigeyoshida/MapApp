﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfLib;

namespace MapApp
{

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {


        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private double mTextSize = 0;                           //  文字の大きさ
        private double mWidth = 1000;                           //  Viewの論理サイズ
        private double mHeight = 1000;                          //  Viewの論理サイズ
        private int mDataImageSize = 256;                       //  画像データのサイズ256x256pixcel

        private bool mCombboxEnable = true;                     //  地図、ズームレベルや列数切替時の抑制フラグ
        private double mCenterCrossSize = 100.0;                //  地図の中心クロスの大きさ
        private bool? mOnLine = null;                           //  地図データのダウンロードモード

        private string[] mZoomName = {                          //  ズームレベル選択表示用
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
            "11", "12", "13", "14", "15", "16", "17", "18"
        };
        private string[] mColCountName = {                       //  列数選択表示用
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
            "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "25", "30"
        };

        public MapData mMapData = new MapData();                //  地図データクラス
        private AreaDataList mAreaDataList = new AreaDataList();//  地図画面保存リスト
        private MarkList mMapMarkList = new MarkList();         //  地図マークリストクラス
        private Measure mMeasure = new Measure();               //  測定クラス
        private static GpsDataList mGpsDataList = new GpsDataList();    //  GPSデータ表示クラス(スレッド処理のためstaticとなる)

        private string mBaseFolder = "Map";                     //  保存先フォルダ
        private string mImageFileSetPath = "ImageFileSet.csv";  //  地図イメージデータ取得リスト保存ファイル名
        private string mMapDataListPath = "MapDataList.csv";    //  地図データリスト保存ファイル名
        private string mPostionListFIle = "MapAreaList.csv";    //  地図画面保存リスト保存ファイル名
        private string mMarkListFile = "MarkList.csv";          //  マークリスト保存ファイル名
        private static string mGpsListFile = "GpsDataList.csv"; //  GPSトレースデータリストファイル名(スレッド処理のためstaticとなる)
        private string mHelpFile = "MapAppManual.pdf";          //  PDFのヘルプファイル

        private MarkListDialg mMarkListDialog;                  //  位置情報登録ダイヤログ
        private GpsListDialog mGpsListDialog;                   //  GPSファイルの管理ダイヤログ
        private WikiList mWikiListDialog;                       //  Wikipedia検索ダイヤログ
        private YamaRecoList mYamaRecoList;                     //  YamaRecoの検索ダイヤログ
        private Map3DView mMap3DView;                           //  地図の3次元表示
        private PhotoList mPhotoList;
        private Task mTaskGpsLoad;                              //  GPSデータリスト読込タスク
        private ImageView mImageView;                           //  写真データ表示ダイヤログ

        //  マウスの位置
        private Point mLastMovePoint = new Point(0, 0);
        private Point mLeftPressPoint = new Point(0, 0);
        private Point mRightPressPoint = new Point(0, 0);
        private bool mMapMoveMode = false;
        public bool mPhotoLoacMode = false;
        public string mSetPhotLocFile = "";

        private YGButton ydraw;
        private YLib ylib;


        public MainWindow()
        {
            InitializeComponent();


            mWindowWidth = Width;
            mWindowHeight = Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();

            ydraw = new YGButton(CvMapData);
            ydraw.mClipping = true;
            ylib = new YLib();

            //  地図データの読み込み
            MapInfoData.loadMapData(mMapDataListPath);

            //  前回値取得
            mMapData.setMapInfoData(Properties.Settings.Default.MapAppDataNum);
            mMapData.mZoom = Properties.Settings.Default.MapAppZoom;
            mMapData.mStart.X = Properties.Settings.Default.MapAppX;
            mMapData.mStart.Y = Properties.Settings.Default.MapAppY;
            mMapData.mColCount = Properties.Settings.Default.MapAppSize;
            mMapData.normarized();
            var markSort = Properties.Settings.Default.MarkListSort;
            mMapMarkList.mSizeRate = Properties.Settings.Default.MarkSizeRate;
            mMapMarkList.mListSort = (MarkList.SORTTYPE)Enum.Parse(typeof(MarkList.SORTTYPE), markSort.Length == 0 ? "Non": markSort);

            //  コントロールに値の設定
            setMapData();
            CbZoom.ItemsSource = mZoomName;
            CbSize.ItemsSource = mColCountName;
            ChkAutoOnLine.IsChecked = null;
            BtMapsGSI.Content = MapInfoData.mMapData[mMapData.mDataId][9].Length == 0 ? "国土地理院" : MapInfoData.mMapData[mMapData.mDataId][0];

            setParametor();

            //  位置画面データの読み込みと設定
            mAreaDataList.setSvaePath(mPostionListFIle);
            mAreaDataList.loadData();
            setMapList();
            //  地図データの表示
            mImageFileSetPath = mBaseFolder + "\\" + mImageFileSetPath; //  地図データ保存パス
            mMapData.loadImageFileSet(mImageFileSetPath);
            //  マークデータ読込
            mMapMarkList.setSaveFilePath(mMarkListFile);
            mMapMarkList.loadFile();
            //  GPSトレースデータを読み込む
            mGpsDataList.loadGpsFile(mGpsListFile);
            //  非同期でGPSトレースデータを読み込む
            //mTaskGpsLoad = Task.Run(async () => {
            //    await gpsDataLoad();
            //});
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (getParametor()) {
                setParametor();
                mMapData.setDateTime(true);         //  地図取得時間
                if (mMapData.isDateTimeData()) {
                    setAddTimeSelectData();
                    CbAddTime.SelectedIndex = 0;
                }
                mapDisp(true);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //  地図情報の保存
            MapInfoData.saveMapData(mMapDataListPath);
            mMapData.saveImageFileSet(mImageFileSetPath);
            mAreaDataList.saveData();
            mMapMarkList.saveFile();
            mGpsDataList.saveGpsFile(mGpsListFile);
            mMapMarkList.savePathData();

            //  地図データ設定値の保存
            Properties.Settings.Default.MapAppDataNum = mMapData.mDataId;
            Properties.Settings.Default.MapAppZoom = mMapData.mZoom;
            Properties.Settings.Default.MapAppX = mMapData.mStart.X;
            Properties.Settings.Default.MapAppY = mMapData.mStart.Y;
            Properties.Settings.Default.MapAppSize = mMapData.mColCount;

            Properties.Settings.Default.MarkSizeRate = mMapMarkList.mSizeRate;
            Properties.Settings.Default.MarkListSort = mMapMarkList.mListSort.ToString();

            if (mMarkListDialog != null)
                mMarkListDialog.Close();
            if (mGpsListDialog != null)
                mGpsListDialog.Close();
            if (mWikiListDialog != null)
                mWikiListDialog.Close();
            if (mMap3DView != null)
                mMap3DView.Close();
            if (mYamaRecoList != null)
                mYamaRecoList.Close();
            if (mPhotoList != null)
                mPhotoList.Close();

            WindowFormSave();
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            //  最大化時の処理
            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                mWindowWidth = SystemParameters.WorkArea.Width;
                mWindowHeight = SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != Width ||
                mWindowHeight != Height) {
                mWindowWidth = Width;
                mWindowHeight = Height;
            } else {
                return;
            }
            //  地図の再表示
            refresh(false);

            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            //double dx = mWindowWidth - mPrevWindowWidth;
            //コントロール.Width += dx;

            mWindowState = this.WindowState;
            mPrevWindowWidth = mWindowWidth;
        }


        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MapAppWidth < 100 || Properties.Settings.Default.MapAppHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.MapAppHeight) {
                Properties.Settings.Default.MapAppWidth = mWindowWidth;
                Properties.Settings.Default.MapAppHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MapAppTop;
                Left = Properties.Settings.Default.MapAppLeft;
                Width = Properties.Settings.Default.MapAppWidth;
                Height = Properties.Settings.Default.MapAppHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MapAppTop = Top;
            Properties.Settings.Default.MapAppLeft = Left;
            Properties.Settings.Default.MapAppWidth = Width;
            Properties.Settings.Default.MapAppHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// キー入力処理
        /// 画面上にButtonやComboBoxを追加すると矢印キーやタブキーがkeyDownでは
        /// 取得できなくなるのでPreviewKeyDownで取得する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            double d = 1.0;
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                d = 0.5;
            if (e.Key == Key.Left) {                //  左に移動
                setMove(-d, 0);
            } else if (e.Key == Key.Right) {        //  右に移動
                setMove(d, 0);
            } else if (e.Key == Key.Up) {           //  上に移動
                setMove(0, -d);
            } else if (e.Key == Key.Down) {         //  下に移動
                setMove(0, d);
            } else if (e.Key == Key.PageUp) {       //  拡大
                setZoom(mMapData.mZoom, mMapData.mZoom + 1);
            } else if (e.Key == Key.PageDown) {     //  縮小
                setZoom(mMapData.mZoom, mMapData.mZoom - 1);
            } else if (e.Key == Key.F5) {           //  再表示
                refresh(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            }
        }

        /// <summary>
        /// [データ名(地図名)]変更コンボボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbDataID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mCombboxEnable && getParametor()) {
                mMapData.setDateTime();             //  地図取得時間
                if (mMapData.isDateTimeData()) {
                    setAddTimeSelectData();
                    CbAddTime.SelectedIndex = 0;
                }
                //  再表示
                mapDisp(true);
            }
        }

        /// <summary>
        /// [位置情報リスト]変更コンボボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbPositionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mCombboxEnable && 0 <= CbPositionList.SelectedIndex) {
                MapData mapData = mAreaDataList.getData(CbPositionList.Items[CbPositionList.SelectedIndex].ToString()).Copy();

                //  地図情報の設定
                setDispParametor(mapData.mDataId, mapData.mZoom, mapData.mColCount);
                //  座標情報の設定
                mMapData.mStart = mapData.mStart;

                mMapData.setDateTime();                 //  地図取得時間
                if (mMapData.isDateTimeData()) {
                    setAddTimeSelectData();
                }
                //  再表示
                mapDisp(true);
                setParametor();
            }
        }

        /// <summary>
        /// [ズーム]変更コンボボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbZoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mCombboxEnable) {
                int nextZoom = int.Parse(mZoomName[CbZoom.SelectedIndex]);
                setZoom(mMapData.mZoom, nextZoom);
            }
        }

        /// <summary>
        /// [タイル列]変更コンボボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mCombboxEnable && getParametor())
                mapDisp(true);
        }

        /// <summary>
        /// [地図ロード]チェックボックス 地図のロード状態を表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkAutoOnLine_Click(object sender, RoutedEventArgs e)
        {
            //  チェックボックスは3ステート設定
            if (ChkAutoOnLine.IsChecked == true) {
                ChkAutoOnLine.Content = "オンライン";
                mOnLine = true;
            } else if (ChkAutoOnLine.IsChecked == false) {
                ChkAutoOnLine.Content = "オフライン";
                mOnLine = false;
            } else {
                ChkAutoOnLine.Content = "自動オンライン";
                mOnLine = null;
            }
        }

        /// <summary>
        /// [+]ボタン　(ズームアップ)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtZoomUp_Click(object sender, RoutedEventArgs e)
        {
            setZoom(mMapData.mZoom, mMapData.mZoom + 1);
        }

        /// <summary>
        /// [-]ボタン　(ズームダウン)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtZoomDown_Click(object sender, RoutedEventArgs e)
        {
            setZoom(mMapData.mZoom, mMapData.mZoom - 1);
        }

        /// <summary>
        /// [↑]ボタン 上に移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtUpMove_Click(object sender, RoutedEventArgs e)
        {
            setMove(0, -0.5);
        }

        /// <summary>
        /// [←]ボタン 左に移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtLeftMove_Click(object sender, RoutedEventArgs e)
        {
            setMove(-0.5, 0);
        }

        /// <summary>
        /// [→]ボタン 右に移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtRightMove_Click(object sender, RoutedEventArgs e)
        {
            setMove(0.5, 0);
        }

        /// <summary>
        /// [↓]ボタン 下に移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtDownMove_Click(object sender, RoutedEventArgs e)
        {
            setMove(0, 0.5);
        }

        /// <summary>
        /// [登録]ボタン(画面登録) 現在の表示を名前を付けて登録する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtDataRegist_Click(object sender, RoutedEventArgs e)
        {
            //  登録タイトル入力
            InputBox dialog = new InputBox();
            dialog.mMainWindow = this;
            dialog.Title = "地図データの登録";
            dialog.mEditText = CbPositionList.Text;
            var result = dialog.ShowDialog();
            if (result == true) {
                string key = dialog.mEditText;
                //  現在の画面の情報を登録
                mAreaDataList.add(key, mMapData.Copy());
                if (!CbPositionList.Items.Contains(key)) {
                    CbPositionList.Items.Add(key);
                }
            }
        }

        /// <summary>
        /// [削除]ボタン(画面登録) 　登録データの削除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtDataDelete_Click(object sender, RoutedEventArgs e)
        {
            if (0 <= CbPositionList.SelectedIndex) {
                string key = CbPositionList.Items[CbPositionList.SelectedIndex].ToString();
                if (MessageBox.Show("[" + key + "] を削除します", "確認", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                    if (mAreaDataList.remove(key)) {
                        CbPositionList.Items.Remove(key);
                    }
                }
            }
        }

        /// <summary>
        /// [マーク表示]チェック マーク表示切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkMarkDisp_Click(object sender, RoutedEventArgs e)
        {
            mapDisp(false);
        }

        /// <summary>
        /// [GPS軌跡]チェック GPSトレース表示切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkGpsDisp_Click(object sender, RoutedEventArgs e)
        {
            mapDisp(false);
        }

        /// <summary>
        /// [マークリスト] ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtMarkEdit_Click(object sender, RoutedEventArgs e)
        {
            mMarkListDialog = new MarkListDialg();
            mMarkListDialog.Topmost = true;
            mMarkListDialog.mMarkList = mMapMarkList;
            mMarkListDialog.mMainWindow = this;
            mMarkListDialog.Show();
        }

        /// <summary>
        /// [GPSリスト]ボタン GPSデータリストダイヤログ表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtGpsList_Click(object sender, RoutedEventArgs e)
        {
            if (mTaskGpsLoad != null) {
                //  非同期処理をしている場合
                while (!mTaskGpsLoad.IsCompleted) {
                    Thread.Sleep(100);
                }
            }
            mGpsListDialog = new GpsListDialog();
            mGpsListDialog.mGpsDataList = mGpsDataList;
            mGpsListDialog.Topmost = true;
            mGpsListDialog.mMainWindow = this;
            mGpsListDialog.Show();
        }

        /// <summary>
        /// [Wikiリスト]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtWikiList_Click(object sender, RoutedEventArgs e)
        {
            if (mWikiListDialog == null || !mWikiListDialog.IsVisible) {
                mWikiListDialog = new WikiList();
                //mWikiListDialog.Topmost = true;
                mWikiListDialog.mMarkList = mMapMarkList;
                mWikiListDialog.mMainWindow = this;
                mWikiListDialog.Show();
            }
        }

        /// <summary>
        /// [ヤマレコリスト]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtYamaRecoList_Click(object sender, RoutedEventArgs e)
        {
            if (mYamaRecoList == null || !mYamaRecoList.IsVisible) {
                mYamaRecoList = new YamaRecoList();
                mYamaRecoList.mMarkList = mMapMarkList;
                mYamaRecoList.mMainWindow = this;
                mYamaRecoList.Show();
            }
        }

        /// <summary>
        /// [3D表示]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtMap3DView_Click(object sender, RoutedEventArgs e)
        {
            mMap3DView = new Map3DView();
            mMap3DView.mMapData = mMapData;
            mMap3DView.Show();
        }

        /// <summary>
        /// [写真]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtPhotoList_Click(object sender, RoutedEventArgs e)
        {
            if (mPhotoList == null || !mPhotoList.IsVisible) {
                mPhotoList = new PhotoList();
                mPhotoList.mMarkList = mMapMarkList;
                mPhotoList.mMainWindow = this;
                mPhotoList.mDoubleClikDefualt = PhotoList.DOUBLECLICK.coonrdinate;
                mPhotoList.Show();
            }
        }

        /// <summary>
        /// [国土地理院]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtMapsGSI_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (MapInfoData.mMapData[mMapData.mDataId][9].Length == 0) {
                System.Diagnostics.Process p =
                        System.Diagnostics.Process.Start(MapInfoData.mHelpUrl);
            } else {
                System.Diagnostics.Process p =
                        System.Diagnostics.Process.Start(MapInfoData.mMapData[mMapData.mDataId][9]);
            }
        }

        /// <summary>
        /// [凡例]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtMapLegend_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (0 < MapInfoData.mMapData[mMapData.mDataId][14].Length) {
                if (File.Exists(mMapData.getMapLegenFIleAddress())) {
                    ylib.openUrl(mMapData.getMapLegenFIleAddress());
                } else {
                    System.Diagnostics.Process p =
                        System.Diagnostics.Process.Start(mMapData.mMapLegend);
                }
            }
        }

        /// <summary>
        /// [■]ボタン 気象庁の天気図予報を現在時間設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtNowTime_Click(object sender, RoutedEventArgs e)
        {
            mMapData.mDateTimeInc = 0;
            CbAddTime.SelectedIndex = mMapData.mDateTimeInc;
        }

        /// <summary>
        /// [◀]ボタン 気象庁の天気図予報を一つ戻す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtPrevTime_Click(object sender, RoutedEventArgs e)
        {
            mMapData.mDateTimeInc--;
            if (0 <= mMapData.mDateTimeInc && mMapData.mDateTimeInc < CbAddTime.Items.Count) {
                CbAddTime.SelectedIndex = mMapData.mDateTimeInc;
            } else {
                CbAddTime.SelectedIndex = 0;
                refresh(true);
            }

        }

        /// <summary>
        /// [▶]ボタン 気象庁の天気図予報を一つ進める
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtNextTime_Click(object sender, RoutedEventArgs e)
        {
            mMapData.mDateTimeInc++;
            if (0 <= mMapData.mDateTimeInc && mMapData.mDateTimeInc < CbAddTime.Items.Count) {
                CbAddTime.SelectedIndex = mMapData.mDateTimeInc;
            } else {
                refresh(true);
            }
        }

        /// <summary>
        /// [時間]選択 天気予報図などのの日時データを含む地図の予報時間の選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbAddTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbAddTime.SelectedIndex) {
                mMapData.mDateTimeInc = CbAddTime.SelectedIndex;
                refresh(true);
            }
        }

        /// <summary>
        /// [(更新)]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtRefresh_Click(object sender, RoutedEventArgs e)
        {
            refresh(true);
        }

        /// <summary>
        /// [?]ボタン ヘルプ表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtHelp_Click(object sender, RoutedEventArgs e)
        {
            //HelpView help = new HelpView();
            //help.mHelpText = HelpText.mMapAppHelp + HelpText.mMarkListHelp + HelpText.mGpsTraceListHelp + HelpText.mWikiListHelp;
            //help.mPdfFile = new string[] { "mHelpFile" };
            //help.Show();
            ylib.fileExecute(mHelpFile);
        }

        /// <summary>
        /// [MAPデータ追加/編集/削除]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataIDMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            int dataIdNo = CbDataID.SelectedIndex;
            if (menuItem.Name.CompareTo("DataIDAddMenu") == 0) {
                //  データの追加
                MapDataSet dlg = new MapDataSet();
                var result = dlg.ShowDialog();
                if (result == true) {
                    MapInfoData.mMapData.Add(dlg.mDatas);
                    setMapData();
                    CbDataID.SelectedIndex = 0;
                }
            } else if (menuItem.Name.CompareTo("DataIDEditMenu") == 0) {
                //  データの編集
                if (0 <= dataIdNo) {
                    MapDataSet dlg = new MapDataSet();
                    for (int i = 0; i < dlg.mDatas.Length && i < MapInfoData.mMapData[CbDataID.SelectedIndex].Length; i++)
                        dlg.mDatas[i] = MapInfoData.mMapData[CbDataID.SelectedIndex][i];
                    var result = dlg.ShowDialog();
                    if (result == true) {
                        for (int i = 0; i < dlg.mDatas.Length && i < MapInfoData.mMapData[CbDataID.SelectedIndex].Length; i++)
                            MapInfoData.mMapData[CbDataID.SelectedIndex][i] = dlg.mDatas[i];
                        setMapData();
                        mMapData.mDataId = -1;              //  データを更新させるため仮設定
                        CbDataID.SelectedIndex = dataIdNo;  //  再表示させる
                    }
                }
            } else if (menuItem.Name.CompareTo("DataIDRemoveMenu") == 0) {
                //  データの削除
                if (0 <= dataIdNo) {
                    MessageBoxResult result = MessageBox.Show(MapInfoData.mMapData[CbDataID.SelectedIndex][0] + "削除します", "確認", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK) {
                        MapInfoData.mMapData.RemoveAt(CbDataID.SelectedIndex);
                        setMapData();
                    }
                }
            } else if (menuItem.Name.CompareTo("DataIDMapInitMenu") == 0) {
                //  地図データの削除
                if (0 <= dataIdNo) {
                    mMapData.removeMapDataTask(true);
                }
            } else if (menuItem.Name.CompareTo("DataIDMapAllInitMenu") == 0) {
                //  全地図データの削除
                mMapData.removeAllMapDataTask();
            }
        }

        /// <summary>
        /// [地図画像コピー]コンテキストメニュー Canvasのイメージをクリップボードにコピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CvImageCopyMenu_Click(object sender, RoutedEventArgs e)
        {
            mapImageCopy(mMapData);
        }

        /// <summary>
        /// [座標のコピー]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CordinateCopyMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("CordinateCopyMenu") == 0) {
                ////  緯度経度のコピー
                //Point cp = mMapData.screen2TempCoordinates(mLastMovePoint);
                //string coordinate = cp.Y + ", " + cp.X;
                //Clipboard.SetText(coordinate);
            } else if (menuItem.Name.CompareTo("TextCopyMenu") == 0) {
                //  座標のテキストコピー
                Point cp = mMapData.screen2Coordinates(mRightPressPoint);
                string coordinate = cp.Y + ", " + cp.X;
                Clipboard.SetText(coordinate);
                //  座標と標高をダイヤログ表示
                coordinate = "座標(" + cp.Y.ToString("#.######") + ", " + cp.X.ToString("#.######") + ")";
                coordinate += mMapData.getMapElavtor(mMapData.screen2Map(mRightPressPoint), ChkAutoOnLine.IsChecked).ToString(" 標高 #,### m");
                MessageBox.Show(coordinate);
            }
        }

        /// <summary>
        /// [マーク追加/編集/削除] コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarkMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("AddMarkMenu") == 0) {
                //  マーク追加
                MapMark mapMark = new MapMark("", mMapData.screen2BaseMap(mRightPressPoint), 0);
                MarkInput markInput = new MarkInput();
                markInput.mMapMark = mapMark;
                markInput.mMarkList = mMapMarkList;
                var result = markInput.ShowDialog();
                if (result == true) {
                    mMapMarkList.add(mapMark);
                    mapDisp(false);
                }
            } else if (menuItem.Name.CompareTo("EditMarkMenu") == 0) {
                //  マーク編集
                MapMark mapMark = mMapMarkList.getMark(mRightPressPoint, mMapData);
                if (mapMark != null) {
                    MarkInput markInput = new MarkInput();
                    markInput.mMapMark = mapMark;
                    markInput.mMarkList = mMapMarkList;
                    var result = markInput.ShowDialog();
                    if (result == true) {
                        mapDisp(false);
                    }
                }
            } else if (menuItem.Name.CompareTo("ReferenceMarkMenu") == 0) {
                //  マーク参照
                MapMark mapMark = mMapMarkList.getMark(mRightPressPoint, mMapData);
                if (mapMark != null) {
                    if (0 < mapMark.mLink.Length) {
                        if (Path.GetExtension(mapMark.mLink).ToLower().CompareTo(".jpg") == 0)
                            dispPhotoData(mapMark.mLink);
                        else
                            System.Diagnostics.Process.Start(mapMark.mLink);
                    }
                }
            } else if (menuItem.Name.CompareTo("DeleteMarkMenu") == 0) {
                //  マーク削除
                MapMark mapMark = mMapMarkList.getMark(mRightPressPoint, mMapData);
                MessageBoxResult result = MessageBox.Show(mapMark.mTitle + " 削除します", "確認", MessageBoxButton.OKCancel);
                if (mapMark != null && result == MessageBoxResult.OK) {
                    mMapMarkList.remove(mapMark);
                    mapDisp(false);
                }
            }
        }

        /// <summary>
        /// [Wikiリスト検索] 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WikiMenu_Click(object sender, RoutedEventArgs e)
        {
            Point cp = mMapData.screen2Coordinates(mRightPressPoint);
            string coordinate = "北緯" + cp.Y + "度東経" + cp.X + "度";

            mWikiListDialog = new WikiList();
            mWikiListDialog.Topmost = true;
            mWikiListDialog.mMainWindow = this;
            mWikiListDialog.mCoordinate = coordinate + " 10km以内";
            mWikiListDialog.Show();
        }

        /// <summary>
        /// [ヤマレコリスト]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void YamaRecoMenu_Click(object sender, RoutedEventArgs e)
        {
            Point cp = mMapData.screen2Coordinates(mRightPressPoint);
            string coordinate = "北緯" + cp.Y + "度東経" + cp.X + "度";

            mYamaRecoList = new YamaRecoList();
            mYamaRecoList.mMainWindow = this;
            mYamaRecoList.mCoordinate = coordinate + " 10km以内";
            mYamaRecoList.Show();
        }

        /// <summary>
        /// [測定] コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeasureMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("MeasureMenu") == 0) {
                if (menuItem.Header.ToString().CompareTo("距離測定開始") == 0) {
                    mMeasure.mMeasureMode = true;
                    menuItem.Header = "距離測定終了";
                    UndoMeasureMenu.Visibility = Visibility.Visible;
                } else if (menuItem.Header.ToString().CompareTo("距離測定終了") == 0) {
                    MessageBox.Show(mMeasure.measure(mMapData).ToString("#0.### km"), "測定距離");
                    mMeasure.clear();
                    mMeasure.mMeasureMode = false;
                    menuItem.Header = "距離測定開始";
                    UndoMeasureMenu.Visibility = Visibility.Hidden;
                }
            } else if (menuItem.Name.CompareTo("UndoMeasureMenu") == 0) {
                //  距離測定点を一つ戻す
                if (0 < mMeasure.getCount()) {
                    mMeasure.decriment();
                    mapDisp(false);
                }
            }
        }

        /// <summary>
        /// [マウスの移動]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CvMapData_MouseMove(object sender, MouseEventArgs e)
        {
            //  スクリーン座標をCanvasの座標に
            Point pos = screen2Canvas(e.GetPosition(this));
            //System.Diagnostics.Debug.WriteLine($"MouseMove {pos.X} {pos.Y} {mMapMoveMode}");
            if (!mMapMoveMode && e.LeftButton == MouseButtonState.Pressed)
                mMapMoveMode = true;
            mLastMovePoint = ydraw.cnvScreen2World(pos);
            //  緯度経度表示
            Point cp = mMapData.screen2Coordinates(mLastMovePoint);
            //  Map座標(Tile No)
            Point mp = mMapData.screen2Map(mLastMovePoint);
            //  標高取得
            double ele = mMapData.getMapElavtor(mMapData.screen2Map(mLastMovePoint), ChkAutoOnLine.IsChecked);
            //  色の凡例表示
            System.Drawing.Color color = getPointColor(mMapData.screen2Map(mLastMovePoint));
            string colorLegendTitle = "色 [" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2") + "]";
            if (mMapData.mColorLegend != null) {
                colorLegendTitle += " " + mMapData.getColorLegend(getPointColor(mMapData.screen2Map(mLastMovePoint)));
            }
            //  ステータスバーに表示
            TbCordinate.Text = "(" + cp.Y.ToString("#0.######") + "," + cp.X.ToString("#0.######") + ") 標高 "
                + ele.ToString("#,###") + " m TileNo[" + (int)mp.X + "," + (int)mp.Y + "] " + colorLegendTitle;
        }

        /// <summary>
        /// [マウス左ボタンダウン]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CvMapData_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = screen2Canvas(e.GetPosition(this));
            //System.Diagnostics.Debug.WriteLine($"LeftButtonDown {pos.X} {pos.Y} {mMapMoveMode}");
            if (mPhotoLoacMode) {
                setPhotoLocation(mMapData.screen2BaseMap(ydraw.cnvScreen2World(pos)), mSetPhotLocFile);
                mPhotoLoacMode = false;
                mSetPhotLocFile = "";
                return;
            }
            if (!mMapMoveMode) {
                //System.Diagnostics.Debug.WriteLine($"MoveStart {pos.X} {pos.Y}");
                //  画面移動
                mLeftPressPoint = ydraw.cnvScreen2World(pos);
            }
            if (mMeasure.mMeasureMode && !mMapMoveMode) {
                //System.Diagnostics.Debug.WriteLine($"Mesure {pos.X} {pos.Y}");
                //  測定モード
                mMeasure.add(mMapData.screen2BaseMap(ydraw.cnvScreen2World(pos)));
                if (1 < mMeasure.getCount())
                    mapDisp(false);
            }
        }

        /// <summary>
        /// [マウス左ボタンアップ]地図の移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CvMapData_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = screen2Canvas(e.GetPosition(this));
            //System.Diagnostics.Debug.WriteLine($"LeftButtonUp {pos.X} {pos.Y} {mMapMoveMode}");
            if (mMapMoveMode) {
                //  画面移動
                pos = ydraw.cnvScreen2World(pos);
                double dx = mLeftPressPoint.X - pos.X;
                double dy = mLeftPressPoint.Y - pos.Y;
                if (dx < mWidth && dy < mHeight)
                    setMove(dx / mMapData.mCellSize, dy / mMapData.mCellSize);
                mMapMoveMode = false;
            }
        }

        /// <summary>
        /// [マウス右ボタンダウン]　右ボタンメニューの座標取得
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CvMapData_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(this);
            if (e.RightButton == MouseButtonState.Pressed) {
                mRightPressPoint = ydraw.cnvScreen2World(screen2Canvas(pos));
            }
        }

        /// <summary>
        /// [マウスホイール] 地図の拡大縮小(ズームレベルの変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CvMapData_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"MOuseWheel: {e.Delta}");
            if (0 != e.Delta) {
                setZoom(mMapData.mZoom, mMapData.mZoom + (e.Delta / 120), mMapData.screen2Map(mLastMovePoint));
            }
        }

        /// <summary>
        /// スクリーン座標からCanvasの座標に変換
        /// 左と上のコントロール分オフセットする
        /// </summary>
        /// <param name="sp">スクリーン座標</param>
        /// <returns>Cnavas座標</returns>
        private Point screen2Canvas(Point sp)
        {
            Point offset = new Point(SpLeftPanel.ActualWidth, SbTopStatusBar.ActualHeight);
            sp.Offset(-offset.X, -offset.Y);
            return sp;
        }

        /// <summary>
        /// 指定値を中心になるように画面を移動する
        /// </summary>
        /// <param name="ctr">緯度経度座標</param>
        public void setMoveCtrCoordinate(Point ctr)
        {
            if (ctr.X != 0.0 && ctr.Y != 0.0)
                setMoveCtr(MapData.coordinates2BaseMap(ctr));
        }

        /// <summary>
        /// 指定値を中心になるように画面を移動する
        /// </summary>
        /// <param name="ctr">BaseMap座標</param>
        public void setMoveCtr(Point ctr)
        {
            if (ctr.X == 0.0 || ctr.Y == 0.0)
                return;

            mMapData.setMoveCenter(ctr);

            setParametor();
            mapDisp(true);
        }

        /// <summary>
        /// 画像の移動による再表示
        /// </summary>
        /// <param name="dx">X方向の移動量</param>
        /// <param name="dy">Y方向の移動量</param>
        private void setMove(double dx, double dy)
        {
            mMapData.setMove(dx, dy);

            setParametor();
            mapDisp(true);
        }

        /// <summary>
        /// ズーム値の変更して再表示
        /// </summary>
        /// <param name="curZoom">現在のズーム値</param>
        /// <param name="nextZoom">変更後のズーム値</param>
        private void setZoom(int curZoom, int nextZoom)
        {
            mMapData.setZoom(nextZoom);
            setParametor();
            mapDisp(true);
        }

        /// <summary>
        /// 指定値を中心にしてズーム値の変更
        /// </summary>
        /// <param name="curZoom">現在のズーム値</param>
        /// <param name="nextZoom">変更後のズーム値</param>
        /// <param name="ctr">中心座標(MAP座標)</param>
        private void setZoom(int curZoom, int nextZoom, Point ctr)
        {
            mMapData.setZoom(nextZoom, ctr);
            setParametor();
            mapDisp(true);
        }

        /// <summary>
        /// 地図画面の表示更新
        /// </summary>
        /// <param name="onLine">OnLineでデータも更新する</param>
        private void refresh(bool onLine)
        {
            if (getParametor()) {
                setParametor();
                bool? tmpOnLine = mOnLine;
                if (onLine) {
                    //  データ更新と地図取得時間 (雨雲レーダーに合わせ5分おきに設定)
                    mOnLine = true;
                    mMapData.setDateTime();
                }
                mapDisp(onLine);
                mOnLine = tmpOnLine;
            }
        }

        /// <summary>
        /// 画像データのパラメータをコントロールから取得
        /// </summary>
        /// <returns></returns>
        private bool getParametor()
        {
            if (CbDataID.SelectedIndex < 0 || CbZoom.SelectedIndex < 0 ||
                CbSize.SelectedIndex < 0)
                return false;
            return setDispParametor(CbDataID.SelectedIndex, CbZoom.SelectedIndex, ylib.intParse(CbSize.Items[CbSize.SelectedIndex].ToString()));
        }

        /// <summary>
        /// 画像データのパラメータの設定
        /// </summary>
        /// <param name="mapIndex">MAP No</param>
        /// <param name="zoomIndex">Zoom Level</param>
        /// <param name="colCount">列数</param>
        /// <returns></returns>
        private bool setDispParametor(int mapIndex, int zoomIndex, int colCount)
        {
            //  地図情報の設定
            if (mapIndex != mMapData.mDataId) {
                mMapData.setMapInfoData(mapIndex);
                if (MapInfoData.mMapData[mMapData.mDataId][9].Length == 0) {
                    BtMapsGSI.Content = "国土地理院";
                } else {
                    BtMapsGSI.Content = MapInfoData.mMapData[mMapData.mDataId][8];
                }
            }
            //  座標情報の設定
            mMapData.mZoom = int.Parse(mZoomName[zoomIndex]);
            mMapData.mColCount = colCount;
            mMapData.normarized();

            return true;
        }

        /// <summary>
        /// 画像データのパラメータをコントロールに設定
        /// </summary>
        private void setParametor()
        {
            mCombboxEnable = false;
            mMapData.normarized();
            CbDataID.SelectedIndex = mMapData.mDataId;
            CbZoom.SelectedIndex = mMapData.mZoom;
            int colIndex = mColCountName.FindIndex<string>(mMapData.mColCount.ToString());
            if (0 <= colIndex)
                CbSize.SelectedIndex = colIndex;
            TbScale.Text = "1 / " + MapInfoData.mZoomScale[mMapData.mZoom].ToString("#,##0");
            mCombboxEnable = true;
        }

        /// <summary>
        /// 画像データの表示
        /// </summary>
        /// <param name="doEvent">コントロールの更新</param>
        public void mapDisp(bool doEvent)
        {
            initBoard();
            if (getMapFile(mMapData, doEvent))
                drawMap(mMapData);
            mapDiscription();
            timeButtonSet();
            mapLegendButtonSet();
        }

        /// <summary>
        /// 天気図などの時間設定ボタンの表示/非表示
        /// </summary>
        private void timeButtonSet()
        {
            if (mMapData.isDateTimeData()) {
                BtNowTime.Visibility = Visibility.Visible;
                BtPrevTime.Visibility = Visibility.Visible;
                BtNextTime.Visibility = Visibility.Visible;
                CbAddTime.Visibility = Visibility.Visible;
            } else {
                BtNowTime.Visibility = Visibility.Hidden;
                BtPrevTime.Visibility = Visibility.Hidden;
                BtNextTime.Visibility = Visibility.Hidden;
                CbAddTime.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 地図の凡例ボタンの表示/非表示設定
        /// </summary>
        private void mapLegendButtonSet()
        {
            if (0 < mMapData.mMapLegend.Length)
                BtMapLegend.Visibility = Visibility.Visible;
            else
                BtMapLegend.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 予報追加時間の設定
        /// </summary>
        private void setAddTimeSelectData()
        {
            CbAddTime.Items.Clear(); 
            CbAddTime.Items.Add("予想時間");
            for (int i = 1; i < 16; i++) {
                int addtime = i * mMapData.mDateTimeInterval;
                string timeString = "";
                if (30 <= mMapData.mDateTimeInterval)
                    timeString = (addtime / 60).ToString() + "時間後";
                else
                    timeString = addtime.ToString() + "分後";
                CbAddTime.Items.Add(timeString);
            }
            //CbAddTime.SelectedIndex = 0;
        }


        /// <summary>
        /// 地図の説明表示
        /// </summary>
        private void mapDiscription()
        {
            TbDataId.Text = MapInfoData.mMapData[mMapData.mDataId][3];
            TbZoomLevel.Text = "ズームレベル: " + MapInfoData.mMapData[mMapData.mDataId][4];
            TbArea.Text = MapInfoData.mMapData[mMapData.mDataId][5];
            TbDiscription.Text = MapInfoData.mMapData[mMapData.mDataId][6];
        }

        /// <summary>
        /// 盤の作成
        /// </summary>
        private void initBoard()
        {
            if (!windowSet())
                return;

            mMapData.mScreen = new Size(CvMapData.ActualWidth, CvMapData.ActualHeight);
            mMapData.setViewSize(mWidth, mHeight);

            //  盤をグラフィックボタンで作成
            ydraw.GButtonClear();

            //  盤の完成状態を作成
            double dy = 0;
            double offsetX = mMapData.getOffset().X * mMapData.mCellSize;
            double offsetY = mMapData.getOffset().Y * mMapData.mCellSize;
            Point org = new Point(0, 0);                                    //  画像の表示位置
            Size size = new Size(mMapData.mCellSize, mMapData.mCellSize);   //  画像データの仮想サイズ
            for (int y = 0; y < mMapData.mRowCount + 1; y++) {
                org.X = 0;
                int colCount = 0;
                for (int x = 0; x < mMapData.mColCount + 1; x++) {
                    Rect trimRect = new Rect(new Point(0, 0), size);    //  トリミング領域
                    Rect dispRect = new Rect(org, size);                //  表示領域
                    if (x == 0) {
                        dispRect.Width -= offsetX;
                        trimRect.X = offsetX;
                        trimRect.Width -= offsetX;
                    }
                    if (y == 0) {
                        dispRect.Height -= offsetY;
                        trimRect.Y = offsetY;
                        trimRect.Height -= offsetY;
                    }
                    if (mWidth < org.X) {
                        continue;
                    } else if (mWidth <= (org.X + size.Width)) {
                        dispRect.Width = mWidth - org.X;
                        trimRect.Width = dispRect.Width;
                    }
                    if (mHeight < org.Y) {
                        continue;
                    } else if (mHeight <= (org.Y + size.Height)) {
                        dispRect.Height = mHeight - org.Y;
                        trimRect.Height = dispRect.Height;
                    }
                    if (0 < trimRect.Width && 0 < trimRect.Height) {
                        Rect rect = new Rect(org, size);
                        ydraw.GButtonAdd(getId(x, y), BUTTONTYPE.RECT, dispRect);
                        ydraw.GButtonTrimmingSize(getId(x, y), trimRect, size);
                        ydraw.GButtonBorderThickness(getId(x, y), 0.8f);
                        colCount++;
                    }
                    org.X += dispRect.Width;
                    dy = dispRect.Height;
                }
                org.Y += dy;
            }
            //  盤の表示
            ydraw.GButtonDraws();
        }

        /// <summary>
        /// データのダウンロードと表示
        /// 画面サイズの変更の時にコントロールの更新を行うと無限ループに入るため
        /// doEventをfalseにする
        /// </summary>
        /// <param name="mapData">データの種類</param>
        /// <param name="doEvent">コントロールの更新</param>
        /// <returns></returns>
        private bool getMapFile(MapData mapData, bool doEvent)
        {
            PbDownLoadCount.Minimum = 0;
            PbDownLoadCount.Maximum = mapData.mColCount * mapData.mRowCount;
            //  プログレスバーを表示するためにDoEventでコントロールを更新する
            PbDownLoadCount.Value = 0;
            if (doEvent)
                ylib.DoEvents();

            mMapData.mUseCol = 0;
            int loadCount = 0;
            for (int i = (int)mapData.mStart.X; i < mapData.mStart.X + mapData.mColCount; i++) {
                mMapData.mUseRow = 0;
                for (int j = (int)mapData.mStart.Y; j < mapData.mStart.Y + mapData.mRowCount; j++) {
                    if (i <= (int)Math.Pow(2, mapData.mZoom) && j <= (int)Math.Pow(2, mapData.mZoom)) {
                        //  標高データの取得
                        mapData.getElevatorDataFile(i, j, null);
                        //  地図データの取得
                        string downloadPath = mapData.getMapData(i, j, mOnLine);
                        ydraw.GButtonImageFile(getId(i - (int)mapData.mStart.X, j - (int)mapData.mStart.Y), downloadPath);
                        if (mapData.getMapDataResult() == true) {
                            //  プログレスバーを表示するためにDoEventでコントロールを更新する
                            PbDownLoadCount.Value = loadCount;
                            if (doEvent)
                                ylib.DoEvents();
                        }
                    } else {
                        ydraw.GButtonImageFile(getId(i - (int)mapData.mStart.X, j - (int)mapData.mStart.Y), null);
                    }
                    mMapData.mUseRow++;
                    loadCount++;
                }
                mMapData.mUseCol++;
            }
            //  プログレスバーを表示するためにDoEventでコントロールを更新する
            PbDownLoadCount.Value = loadCount;
            //if (doEvent)
            //    ylib.DoEvents();
            return true;
        }

        /// <summary>
        /// データの表示
        /// </summary>
        /// <param name="mapData"></param>
        private void drawMap(MapData mapData)
        {
            //  盤の表示
            ydraw.GButtonDraws();

            //  枠線
            ydraw.setThickness(2);
            ydraw.setFillColor(null);
            ydraw.drawWRectangle(new Rect(new Point(2,1), new Size(mWidth - 4, mHeight - 2)), 0);

            //  中心線
            drawCenterCross();
            drawScaler();

            //  マーク表示
            if (ChkMarkDisp.IsChecked == true) {
                mMapMarkList.draw(ydraw, mapData);
            }
            //  距離測定中の線分表示
            if (1 < mMeasure.getCount()) {
                mMeasure.draw(ydraw, mapData);
            }
            //  GPSトレースの表示
            if (ChkGpsDisp.IsChecked == true) {
                mGpsDataList.draw(ydraw, mapData);
            }
            //  日時指定付きURLのデータ取得日時の表示
            if (mMapData.isDateTimeData()) {
                drawDateTime();
            }
        }

        /// <summary>
        /// 地図上に中心のクロスを表示
        /// </summary>
        private void drawCenterCross()
        {
            ydraw.setThickness(1);
            ydraw.setColor("Green");
            ydraw.setFillColor(null);
            Point ctr = mMapData.baseMap2Screen(mMapData.getCenter());
            ydraw.drawWLine(new Point(ctr.X + mCenterCrossSize / 2.0, ctr.Y), new Point(ctr.X - mCenterCrossSize / 2.0, ctr.Y));
            ydraw.drawWLine(new Point(ctr.X, ctr.Y + mCenterCrossSize / 2.0), new Point(ctr.X, ctr.Y - mCenterCrossSize / 2.0));
        }

        /// <summary>
        /// 地図上に基準長さのスケール線分を表示
        /// </summary>
        private void drawScaler()
        {
            ydraw.setThickness(5);
            ydraw.setColor("Green");
            ydraw.setFillColor(System.Windows.Media.Brushes.Green);
            double l = 0.2;
            double epx = 0.95;
            double epy = 0.96;
            Point sp = new Point(mMapData.mView.Width * epx, mMapData.mView.Height * epy);
            Point ep = new Point(mMapData.mView.Width * (epx - l), mMapData.mView.Height * epy);
            Point scp = mMapData.screen2Coordinates(sp);
            Point ecp = mMapData.screen2Coordinates(ep);
            double ls = ylib.coordinateDistance(scp, ecp);
            double mls = ylib.floorStepSize(ls);
            ep.X = mMapData.mView.Width * (epx - l * mls / ls);
            ydraw.drawWLine(sp, ep);
            ydraw.drawWText(string.Format("{0:F2} km", mls), 
                new Point(mMapData.mView.Width * (epx - l * mls / ls / 2.0), mMapData.mView.Height * epy), 0.0);
        }

        /// <summary>
        /// 日時指定付きURLのデータ取得日時の表示
        /// </summary>
        private void drawDateTime()
        {
            ydraw.setTextColor(System.Windows.Media.Brushes.Black);
            string[] msg = { "取得時", "予想" };
            double size = ydraw.getTextSize();
            for (int i = 0; i < mMapData.mDispMapDateTime.Count(); i++) {
                string dateTimeText = mMapData.mDispMapDateTime[i].ToString("yyyy年MM月dd日HH時mm分ss秒");
                dateTimeText += " " + msg[i %  2];
                ydraw.drawWText(dateTimeText, new Point(10, 5 + size * i), 0.0);
            }
        }

        /// <summary>
        /// 地図画像をクリップボードにコピーする
        /// </summary>
        private void mapImageCopy(MapData mapData)
        {
            System.Drawing.Bitmap[] hBitmap = new System.Drawing.Bitmap[mapData.mUseCol];
            for (int i = 0; i < mapData.mUseCol; i++) {
                System.Drawing.Bitmap[] vBitmap = new System.Drawing.Bitmap[mapData.mUseRow];
                for (int j = 0; j < mapData.mUseRow; j++) {
                    if ((i + (int)mapData.mStart.X) <= (int)Math.Pow(2, mapData.mZoom) &&
                        (j + (int)mapData.mStart.Y) <= (int)Math.Pow(2, mapData.mZoom)) {
                        //  画像データの結合
                        vBitmap[j] = ydraw.GButtonBitmapGet(getId(i, j));
                        if (vBitmap[j] == null) {
                            //  データがない場合空白データを設定
                            vBitmap[j] = new System.Drawing.Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        }
                    }
                }
                //  画像データの結合
                hBitmap[i] = ydraw.verticalCombineImage(vBitmap);
            }
            System.Drawing.Bitmap bitmap = ydraw.horizontalCombineImage(hBitmap);
            Clipboard.SetImage(ydraw.bitmap2BitmapSource(bitmap));
        }

        /// <summary>
        /// 論理座標の設定と画面クリア
        /// </summary>
        private bool windowSet()
        {
            if (CvMapData.ActualWidth <= 0 || CvMapData.ActualHeight <= 0)
                return false;
            ydraw.setWindowSize(CvMapData.ActualWidth, CvMapData.ActualHeight);
            ydraw.setViewArea(0, 0, CvMapData.ActualWidth, CvMapData.ActualHeight);
            //  アスペクト固定でWindowサイズを設定
            ydraw.setWorldWindow(0, 0, mWidth, mHeight);
            //  Windowのサイズを領域に合わせて再取得
            mWidth = ydraw.mWorld.Width;
            mHeight = ydraw.mWorld.Height;
            if (mTextSize == 0)
                mTextSize = ydraw.getTextSize();
            ydraw.clear();
            return true;
        }


        /// <summary>
        /// 行(y)列(x)からボタンのIDを求める
        /// </summary>
        /// <param name="x">列番豪</param>
        /// <param name="y">行番号</param>
        /// <returns>ID</returns>
        private int getId(int x, int y)
        {
            return x + y * (mMapData.mColCount + 2);
        }

        /// <summary>
        /// IDから列数を取得
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>列番号</returns>
        private int getXId(int id)
        {
            return id % (mMapData.mColCount + 2);
        }

        /// <summary>
        /// IDから行数を取得
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>行数</returns>
        private int getYId(int id)
        {
            return id / (mMapData.mColCount + 2);
        }

        /// <summary>
        /// 地図データのタイトルをコンボボックスに登録
        /// </summary>
        private void setMapData()
        {
            CbDataID.Items.Clear();
            foreach (string[] data in MapInfoData.mMapData)
                CbDataID.Items.Add(data[0]);
        }

        /// <summary>
        /// 位置情報をコンボボックスに設定
        /// </summary>
        private void setMapList()
        {
            //  ItemsResourceを使うとコントロールで追加ができない
            CbPositionList.Items.Clear();
            foreach (string title in mAreaDataList.getTitleList())
                CbPositionList.Items.Add(title);
        }

        /// <summary>
        /// 非同期でGPSトレースのリストファイルとGPSデータを読み込む
        /// </summary>
        /// <returns></returns>
        private async static Task gpsDataLoad()
        {
            //  GPSトレースファイルリストの読み込み
            mGpsDataList.loadGpsFile(mGpsListFile);
        }

        /// <summary>
        /// 指定点のカラーを取得する
        /// </summary>
        /// <param name="mp">Map座標</param>
        /// <returns>色</returns>
        private System.Drawing.Color getPointColor(Point mp)
        {
            PointI imp = new PointI(mp);
            imp.Subtruct(new PointI(mMapData.mStart));
            int dx = (int)(mDataImageSize * (mp.X % 1.0));
            int dy = (int)(mDataImageSize * (mp.Y % 1.0));
            if (imp.X == 0 || imp.Y == 0) {
                Rect rect = ydraw.GButtonSize(getId(imp.X, imp.Y));
                if (rect.IsEmpty) {
                    dx = 0;
                    dy = 0;
                } else {
                    Size size = ydraw.GButtonImageSize(getId(imp.X, imp.Y));
                    dx -= (int)(mDataImageSize * (size.Width - rect.Width) / size.Width);
                    dy -= (int)(mDataImageSize * (size.Height - rect.Height) / size.Height);
                    dx = dx < 0 ? 0 : dx;
                    dy = dy < 0 ? 0 : dy;
                }
            }
            return ydraw.GButtonGetImagePixcel(getId(imp.X, imp.Y), dx, dy);
        }

        /// <summary>
        /// イメージファイルをダイヤログ表示
        /// </summary>
        /// <param name="path"></param>
        private void dispPhotoData(string path)
        {
            if (mImageView != null) {
                mImageView.Close();
            }
            mImageView = new ImageView();
            //mImageView.mImageList = Photos.ConvertAll(p => p.path);
            mImageView.mImagePath = path;
            mImageView.Show();
        }

        /// <summary>
        /// 写真ファイルに座標を設定する
        /// </summary>
        /// <param name="bp">BaseMap座標</param>
        /// <param name="path">写真ファイルパス</param>
        private void setPhotoLocation(Point bp, string path)
        {
            Point cp = MapData.baseMap2Coordinates(bp);
            if (File.Exists(path)) {
                ExifInfo exifInfo = new ExifInfo(path);
                if (exifInfo.setExifGpsCoordinate(cp)) {
                    exifInfo.save();
                    MessageBox.Show("座標を設定しました");
                    return;
                }
            }
            MessageBox.Show("座標を設定できませんでした");
        }
    }
}
