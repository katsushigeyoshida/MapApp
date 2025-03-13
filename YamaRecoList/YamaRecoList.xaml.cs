using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// YamaRecoList.xaml の相互作用ロジック
    /// </summary>
    public partial class YamaRecoList : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        public MainWindow mMainWindow;
        public MarkList mMarkList;
        public string mCoordinate = "";
        private string mHelpFile = "MapAppManual.pdf";              //  PDFのヘルプファイル

        private string[] mGetDataButtonLabel = { "データ取得", "中断" };
        private bool mGetInfoDataAbort = true;                      //  データ取得中断フラグ
        private bool mGenreChangeEnabled = true;                    //  山の分類コンボボックス有効フラグ
        private enum DOWNLOADMODE { normal, select };               //  通常,詳細(周辺情報)
        private DOWNLOADMODE mDownloadMode = DOWNLOADMODE.normal;   //  ダウンロード方法
        private enum GENREMODE { yamadata, route, guide, yamalist };//  データの種別(山データ/登山ルート/おすすめルート)
        private GENREMODE mGenreMode = GENREMODE.yamadata;          //  データの種別
        private string mSplitWord = " : ";                          //  分類データの分轄ワード
        private string[] mGenreTitle = {                            //  データ種別のタイトル
            "山データ", "登山ルート", "おすすめルート", "山リスト"
        };
        private string[] mGenreUrlWord = {                          //  ジャンルに対応するURL_ID
            "ptinfo.php?ptid=", "rtinfo.php?rtid=",
            "guide_detail.php?route_id=", "ptlist.php?groupid=",
        };
        private string mYamaBaseUrl = "https://www.yamareco.com/modules/yamainfo/";

        private int mCellDispSize = 80;                             //  セルの表示文字数
        private string[] mDataTitle;                                //  データタイトル
        private bool[] mDispCol;                                    //  表示カラムフラグ
        private bool[] mNumVal;                                     //  数値データ判定
        private bool[] mDetailCol;                                  //  詳細データ簡略表示
        private int[] mColWidth;                                    //  カラムの幅
        private List<string[]> mDataList;                           //  山/ルートデータ
        private List<string[]> mDetailUrlList;                      //   詳細データの(URL,項目)リスト
        private List<string[]> mSelectListData = new List<string[]>();   //  周辺データのURLリスト

        private YamaRecoData mYamaData = new YamaRecoData();
        private YamaRouteData mRouteData = new YamaRouteData();
        private GuideRouteData mGuideRouteData = new GuideRouteData();
        private YamaListData mYamaListData = new YamaListData();

        private YLib ylib = new YLib();

        public YamaRecoList()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            mPrevWindowWidth = mWindowWidth;
            WindowFormLoad();       //  Windowの位置とサイズを復元

            CbGenre.ItemsSource = mGenreTitle;
            CbGenre.SelectedIndex = 0;

            mYamaData.loadData();
            mRouteData.loadData();
            mGuideRouteData.loadData();
            mYamaListData.loadData();
            setDataList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TbSearchWord.Text = mCoordinate;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mYamaData != null)
                mYamaData.saveData();
            if (mRouteData != null)
                mRouteData.saveData();
            if (mGuideRouteData != null)
                mGuideRouteData.saveData();
            if (mYamaListData != null)
                mYamaListData.saveData();

            WindowFormSave();       //  ウィンドの位置と大きさを保存
        }

        /// <summary>
        /// Windowサイズ変更時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            //sampleGraphInit();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.YamaRecoListWidth < 100 ||
                   Properties.Settings.Default.YamaRecoListHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.YamaRecoListHeight) {
                Properties.Settings.Default.YamaRecoListWidth = mWindowWidth;
                Properties.Settings.Default.YamaRecoListHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.YamaRecoListTop;
                this.Left = Properties.Settings.Default.YamaRecoListLeft;
                this.Width = Properties.Settings.Default.YamaRecoListWidth;
                this.Height = Properties.Settings.Default.YamaRecoListHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.YamaRecoListTop = this.Top;
            Properties.Settings.Default.YamaRecoListLeft = this.Left;
            Properties.Settings.Default.YamaRecoListWidth = this.Width;
            Properties.Settings.Default.YamaRecoListHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [ヘルプ]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtHelp_Click(object sender, RoutedEventArgs e)
        {
            ylib.fileExecute(mHelpFile);
        }

        /// <summary>
        /// [データ取得]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtDataRequest_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Content.ToString().CompareTo(mGetDataButtonLabel[0]) == 0) {
                //  データ取得
                int sp = Math.Max(1, ylib.intParse(TbDataStart.Text));
                int ep = ylib.intParse(TbDataEnd.Text);
                if (0 < sp && sp <= ep) {
                    mDownloadMode = DOWNLOADMODE.normal;
                    mGetInfoDataAbort = false;
                    getYamaRecoData(sp, ep);
                    bt.Content = mGetDataButtonLabel[1];
                }
            } else if (bt.Content.ToString().CompareTo(mGetDataButtonLabel[1]) == 0) {
                //  中断
                mGetInfoDataAbort = true;
                bt.Content = mGetDataButtonLabel[0];
            }
        }

        /// <summary>
        /// [次検索]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtNextSearch_Click(object sender, RoutedEventArgs e)
        {
            int pos = DgDataList.SelectedIndex < 0 ? 0 : DgDataList.SelectedIndex + 1;
            int n = nextSearchData(TbSearchWord.Text, pos);
            if (0 < n) {
                DgDataList.SelectedIndex = n;
                string[] item = (string[])DgDataList.Items[n];
                DgDataList.ScrollIntoView(item);
            }
        }

        /// <summary>
        /// [前検索]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtPrevSearch_Click(object sender, RoutedEventArgs e)
        {
            int pos = DgDataList.SelectedIndex < 0 ? DgDataList.Items.Count - 1 : DgDataList.SelectedIndex - 1;
            int n = prevSearchData(TbSearchWord.Text, pos);
            if (0 < n) {
                DgDataList.SelectedIndex = n;
                string[] item = (string[])DgDataList.Items[n];
                DgDataList.ScrollIntoView(item);
            }
        }

        /// <summary>
        /// [検索]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtSearch_Click(object sender, RoutedEventArgs e)
        {
            string category = CbCategory.SelectedIndex <= 0 ? "山の分類" : CbCategory.Items[CbCategory.SelectedIndex].ToString();
            string detail = CbDetail.SelectedIndex <= 0 ? "山の種別" : CbDetail.Items[CbDetail.SelectedIndex].ToString();
            List<string[]> listData;
            switch (mGenreMode) {
                case GENREMODE.yamadata:
                    listData = mYamaData.getFilterongDataLsit(category, detail, TbSearchWord.Text);
                    break;
                case GENREMODE.route:
                    listData = mRouteData.getFilterongDataList(category, TbSearchWord.Text);
                    break;
                case GENREMODE.guide:
                    listData = mGuideRouteData.getFilterongDataList(category, TbSearchWord.Text);
                    break;
                case GENREMODE.yamalist:
                    listData = mYamaListData.getFilterongDataList(category, TbSearchWord.Text);
                    break;
                default: return;
            }
            setData(listData, mDispCol, mDetailCol);
        }

        /// <summary>
        /// [再表示]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtRefresh_Click(object sender, RoutedEventArgs e)
        {
            mDownloadMode = DOWNLOADMODE.normal;
            setYamaRecoData();
        }

        /// <summary>
        /// [ジャンル]YamaRecoのジャンル
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbGenre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mGenreChangeEnabled) {
                if (CbGenre.SelectedIndex == 0) {
                    mGenreMode = GENREMODE.yamadata;
                    setDataList();
                } else if (CbGenre.SelectedIndex == 1) {
                    mGenreMode = GENREMODE.route;
                    setDataList();
                } else if (CbGenre.SelectedIndex == 2) {
                    mGenreMode = GENREMODE.guide;
                    setDataList();
                } else if (CbGenre.SelectedIndex == 3) {
                    mGenreMode = GENREMODE.yamalist;
                    setDataList();
                }
            }
            mGenreChangeEnabled = true;
        }

        /// <summary>
        /// [山の分類]コンボボックス選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbCategory.SelectedIndex && mGenreMode == GENREMODE.yamadata) {
                List<string[]> listData = mYamaData.getFilterongDataLsit(
                    CbCategory.SelectedIndex <= 0 ? "山の分類" : CbCategory.Items[CbCategory.SelectedIndex].ToString(),
                    CbDetail.SelectedIndex <= 0 ? "山の種別" : CbDetail.Items[CbDetail.SelectedIndex].ToString());
                setData(listData, mDispCol, mDetailCol);
            }
        }

        /// <summary>
        /// [山の種別]コンボボックス選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbDetail_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbDetail.SelectedIndex && mGenreMode == GENREMODE.yamadata) {
                List<string[]> listData = mYamaData.getFilterongDataLsit(
                    CbCategory.SelectedIndex <= 0 ? "山の分類" : CbCategory.Items[CbCategory.SelectedIndex].ToString(),
                    CbDetail.SelectedIndex <= 0 ? "山の種別" : CbDetail.Items[CbDetail.SelectedIndex].ToString());
                setData(listData, mDispCol, mDetailCol);
            }
        }

        /// <summary>
        /// [マウスダブルクリック]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgDataList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //  地図を座標値に移動
            if (0 <= DgDataList.SelectedIndex) {
                if (mGenreMode == GENREMODE.yamadata) {
                    string coordinate = ((string[])DgDataList.Items[DgDataList.SelectedIndex])[titleNo("座標")];
                    mMainWindow.setMoveCtrCoordinate(ylib.cnvCoordinate(coordinate));
                } else if (mGenreMode == GENREMODE.yamalist) {
                    int col = findListHeaderCol("URL");
                    string url = ((string[])DgDataList.Items[DgDataList.SelectedIndex])[col];
                    setDetailData(url);
                }
            }
        }

        /// <summary>
        /// [データリストコンテキストメニュー]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (DgDataList.SelectedIndex < 0)
                return;

            int col = findListHeaderCol("URL");
            string url = ((string[])DgDataList.Items[DgDataList.SelectedIndex])[col];

            if (menuItem.Name.CompareTo("DgMoveMenu") == 0 && mGenreMode == GENREMODE.yamadata) {
                //  地図を座標値に移動
                string coordinate = ((string[])DgDataList.Items[DgDataList.SelectedIndex])[titleNo("座標")];
                mMainWindow.setMoveCtrCoordinate(ylib.cnvCoordinate(coordinate));
            } else if (menuItem.Name.CompareTo("DgDispMenu") == 0) {
                //  詳細表示
                (string buf, string title) = detailDisp(url);
                messageBox(buf, title);
            } else if (menuItem.Name.CompareTo("DgOpenMenu") == 0) {
                //  開く
                ylib.openUrl(url);
            } else if (menuItem.Name.CompareTo("DgMarkMenu") == 0 && mGenreMode == GENREMODE.yamadata) {
                //  マークを追加
                addMark((string[])DgDataList.Items[DgDataList.SelectedIndex]);
            } else if (menuItem.Name.CompareTo("DgDetailMenu") == 0) {
                //  詳細データ(登山口,山小屋)抽出
                setDetailData(url);
            } else if (menuItem.Name.CompareTo("DgRouteMenu") == 0) {
                //  登山ルート
                setRouteData(url);
            } else if (menuItem.Name.CompareTo("DgGuideMenu") == 0) {
                //  おすすめルート
                setGuideData(url);
            } else if (menuItem.Name.CompareTo("DgYamaListMenu") == 0) {
                //  山リスト抽出
                setYamaListData(url);
            } else if (menuItem.Name.CompareTo("DgRemoveMenu") == 0) {
                //  削除
                int urlNo = titleNo("URL");
                if (0 <= DgDataList.SelectedItems.Count) {
                    foreach (string[] item in DgDataList.SelectedItems) {
                        int n = mDataList.FindIndex(p => p[urlNo].CompareTo(item[urlNo]) == 0);   //  URL
                        if (0 <= n)
                            mDataList.RemoveAt(n);
                    }
                    setYamaRecoData();
                }
            }
        }

        /// <summary>
        /// 詳細データの取得
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public (string, string) detailDisp(string url)
        {
            switch (mGenreMode) {
                case GENREMODE.yamadata: return mYamaData.detailDisp(url);
                case GENREMODE.route: return mRouteData.detaiilDisp(url);
                case GENREMODE.guide: return mGuideRouteData.detaiilDisp(url);
                case GENREMODE.yamalist: return mYamaListData.detaiilDisp(url);
                default: return ("", "");
            }
        }

        /// <summary>
        /// [分類リスト]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbCategoryContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("CbGetListFilterMenu") == 0) {
                //  山リストのフィルタ設定
                InputBox dialog = new InputBox();
                dialog.mMainWindow = this;
                dialog.Title = "山リストフィルタ設定";
                var result = dialog.ShowDialog();
                if (result == true) {
                    mYamaData.mYamaListFilter = dialog.mEditText;
                    mYamaData.setCategoryList();
                    setCategoryList(mYamaData.mCategoryList);
                }
            } else {
                if (CbCategory.SelectedIndex <= 0 || mGenreMode != GENREMODE.yamadata)
                    return;
                if (menuItem.Name.CompareTo("CbCategoryOpenMenu") == 0) {
                    //  開く
                    string title = CbCategory.Items[CbCategory.SelectedIndex].ToString();
                    string[] url = mYamaData.mCategoryList.Find(p => p[0].CompareTo(title) == 0);
                    if (url.Length == 2)
                        ylib.openUrl(url[1]);
                } else if (menuItem.Name.CompareTo("CbGetMapListMenu") == 0) {
                    //  分類リストからデータ取得
                    string title = CbCategory.Items[CbCategory.SelectedIndex].ToString();
                    string[] url = mYamaData.mCategoryList.Find(p => p[0].CompareTo(title) == 0);
                    if (url.Length == 2 && 0 < url[1].Length) {
                        mYamaData.getCategoryMapList(url[1]);
                        if (0 < mYamaData.mCategoryMapList.Count)
                            getMapListDownloadData(mYamaData.mCategoryMapList);
                    }
                }
            }
        }

        /// <summary>
        /// [プログレスバー] 終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PbGetInfoData_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PbGetInfoData.Value == PbGetInfoData.Maximum || mGetInfoDataAbort)
                progressTerminate();
        }

        /// <summary>
        /// DataGridのソート
        /// 標高とURLのカラムを数値としてソート処理するようにした
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgDataList_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;                           //  カスタムソート設定
            var sortDir = e.Column.SortDirection;
            if (ListSortDirection.Ascending != sortDir)
                sortDir = ListSortDirection.Ascending;  //  昇順
            else
                sortDir = ListSortDirection.Descending; //  降順

            //  ソートカラムno
            int col = ylib.intParse(ylib.string2StringNumber(e.Column.SortMemberPath));
            //  データリスト
            List<string[]> listData;
            if (mGenreMode == GENREMODE.yamadata) {
                //  山の分類フィルタ
                listData = mYamaData.getFilterongDataLsit(
                    CbCategory.SelectedIndex <= 0 ? "山の分類" : CbCategory.Items[CbCategory.SelectedIndex].ToString(),
                    CbDetail.SelectedIndex <= 0 ? "山の種別" : CbDetail.Items[CbDetail.SelectedIndex].ToString(),
                    TbSearchWord.Text);
            } else {
                listData = mDataList;
            }

            //  ソート処理
            if (ListSortDirection.Ascending == sortDir) {
                if (mNumVal[col]) {
                    listData.Sort((a, b) => (int)(ylib.doubleParse(ylib.string2StringNumber(a[col])) - ylib.doubleParse(ylib.string2StringNumber(b[col]))));
                } else {
                    listData.Sort((a, b) => a[col].CompareTo(b[col]));
                }
            } else {
                if (mNumVal[col]) {
                    listData.Sort((b, a) => (int)(ylib.doubleParse(ylib.string2StringNumber(a[col])) - ylib.doubleParse(ylib.string2StringNumber(b[col]))));
                } else {
                    listData.Sort((b, a) => a[col].CompareTo(b[col]));
                }
            }
            //  DataGridに設定
            setData(listData, mDispCol, mDetailCol);

            //  ソート方向を設定
            foreach (var column in DgDataList.Columns) {
                if (column.SortMemberPath == e.Column.SortMemberPath) {
                    column.SortDirection = sortDir;
                }
            }
        }

        /// <summary>
        /// DataGridにタイトルとデータを設定する
        /// 内部でジャンル対応
        /// </summary>
        private void setDataList(bool dataSet = true)
        {
            if (mGenreMode == GENREMODE.yamadata) {
                mDataTitle = mYamaData.mDataTitle;
                mDataList = mYamaData.mDataList;
                mSplitWord = mYamaData.mSplitWord;
                mDispCol = mYamaData.mDispCol;
                mColWidth = mYamaData.mColWidth;
                mNumVal = mYamaData.mNumVal;
                mDetailCol = mYamaData.mDetailCol; 
                mDetailUrlList = mYamaData.mDetailUrlList;
                CbCategory.IsEnabled = true;
                CbDetail.IsEnabled = true;
                DgMoveMenu.IsEnabled = true;
                DgMarkMenu.IsEnabled = true;
                DgRouteMenu.IsEnabled = true;
                DgGuideMenu.IsEnabled = true;
                DgYamaListMenu.IsEnabled = true;
            } else if (mGenreMode == GENREMODE.route) {
                mDataTitle = mRouteData.mDataTitle;
                mDataList = mRouteData.mDataList;
                mSplitWord = mRouteData.mSplitWord;
                mDispCol = mRouteData.mDispCol;
                mColWidth = mRouteData.mColWidth;
                mNumVal = mRouteData.mNumVal;
                mDetailCol = mRouteData.mDetailCol;
                mDetailUrlList = mRouteData.mDetailUrlList;
                CbCategory.IsEnabled = false;
                CbDetail.IsEnabled = false;
                DgMoveMenu.IsEnabled = false;
                DgMarkMenu.IsEnabled = false;
                DgRouteMenu.IsEnabled = false;
                DgGuideMenu.IsEnabled = false;
                DgYamaListMenu.IsEnabled = false;
            } else if (mGenreMode == GENREMODE.guide) {
                mDataTitle = mGuideRouteData.mDataTitle;
                mDataList = mGuideRouteData.mDataList;
                mSplitWord = mGuideRouteData.mSplitWord;
                mDispCol = mGuideRouteData.mDispCol;
                mColWidth = mGuideRouteData.mColWidth;
                mNumVal = mGuideRouteData.mNumVal;
                mDetailCol = mGuideRouteData.mDetailCol;
                mDetailUrlList = mGuideRouteData.mDetailUrlList;
                CbCategory.IsEnabled = false;
                CbDetail.IsEnabled = false;
                DgMoveMenu.IsEnabled = false;
                DgMarkMenu.IsEnabled = false;
                DgRouteMenu.IsEnabled = false;
                DgGuideMenu.IsEnabled = false;
                DgYamaListMenu.IsEnabled = false;
            } else if (mGenreMode == GENREMODE.yamalist) {
                mDataTitle = mYamaListData.mDataTitle;
                mDataList = mYamaListData.mDataList;
                mSplitWord = mYamaListData.mSplitWord;
                mDispCol = mYamaListData.mDispCol;
                mColWidth = mYamaListData.mColWidth;
                mNumVal = mYamaListData.mNumVal;
                mDetailCol = mYamaListData.mDetailCol;
                mDetailUrlList = mYamaListData.mDetailUrlList;
                CbCategory.IsEnabled = false;
                CbDetail.IsEnabled = false;
                DgMoveMenu.IsEnabled = false;
                DgMarkMenu.IsEnabled = false;
                DgRouteMenu.IsEnabled = false;
                DgGuideMenu.IsEnabled = false;
                DgYamaListMenu.IsEnabled = false;
            }

            setTitle(mDataTitle, mDispCol, mColWidth);

            if (dataSet) {
                if (mDataList != null && 0 < mDataList.Count) {
                    setData(mDataList, mDispCol, mDetailCol);
                    mYamaData.setCategoryList();
                    setCategoryList(mYamaData.mCategoryList);
                    mYamaData.setDetailList();
                    setDetailList(mYamaData.mDetailList);
                } else {
                    mDataList = new List<string[]>();
                    DgDataList.Items.Clear();
                }
            }

        }

        /// <summary>
        /// 周辺情報(登山口、山小屋)や山リストのURLを山データを抽出して表示する
        /// </summary>
        /// <param name="url">山データのURL</param>
        private void setDetailData(string url)
        {
            if (mGenreMode == GENREMODE.yamadata) {
                mSelectListData = mYamaData.getSelectUrlList(url);          //  山データの周辺データのURLリスト
            } else if (mGenreMode == GENREMODE.route) {
                mSelectListData = mRouteData.getSelectUrlList(url);         //  登山ルートデータの周辺データのURLリスト
            } else if (mGenreMode == GENREMODE.guide) {
                mSelectListData = mGuideRouteData.getSelectUrlList(url);    //  おすすめルートデータの周辺データのURLリスト
            } else if (mGenreMode == GENREMODE.yamalist) {
                mSelectListData = mYamaListData.getSelectUrlList(url);      //  山リストのURLリスト
            } else {
                return;
            }
            mDownloadMode = DOWNLOADMODE.select;            //  ダウンロードモードのの設定(非同期処理のため)
            getMapListDownloadData(mSelectListData);        //  非同期処理による山データのダウンロード
            mGenreMode = GENREMODE.yamadata;
        }

        /// <summary>
        /// 山データから登山ルートデータの抽出表示
        /// </summary>
        /// <param name="url"></param>
        private void setRouteData(string url)
        {
            if (mGenreMode == GENREMODE.yamadata)
                mSelectListData = mYamaData.getRouteSelectUrlList(url);    //  周辺データのURLリスト
            else
                return;
            mDownloadMode = DOWNLOADMODE.select;             //  ダウンロードモードのの設定(非同期処理のため)
            getMapListDownloadData(mSelectListData);        //  非同期処理による山データのダウンロード
        }

        /// <summary>
        /// 山データのおすすめルートの抽出と表示
        /// </summary>
        /// <param name="url"></param>
        private void setGuideData(string url)
        {
            if (mGenreMode == GENREMODE.yamadata)
                mSelectListData = mYamaData.getGuideSelectUrlList(url);    //  周辺データのURLリスト
            else
                return;
            mDownloadMode = DOWNLOADMODE.select;             //  ダウンロードモードのの設定(非同期処理のため)
            getMapListDownloadData(mSelectListData);        //  非同期処理による山データのダウンロード
        }

        /// <summary>
        /// 山データの山リストの抽出と表示
        /// </summary>
        /// <param name="url"></param>
        private void setYamaListData(string url)
        {
            if (mGenreMode == GENREMODE.yamadata)
                mSelectListData = mYamaData.getYamaListSelectUrlList(url);    //  山リストデータのURLリスト
            else
                return;
            mDownloadMode = DOWNLOADMODE.select;             //  ダウンロードモードのの設定(非同期処理のため)
            getMapListDownloadData(mSelectListData);        //  非同期処理による山データのダウンロード
        }

        /// <summary>
        /// YamaRecoデータからマークを登録追加
        /// </summary>
        /// <param name="listData"></param>
        private void addMark(string[] listData)
        {
            //  マークデータ設定
            MapMark mapMark = new MapMark();
            mapMark.mTitle = listData[titleNo("山名")];
            Point coordinate = ylib.cnvCoordinate(listData[titleNo("座標")]);
            mapMark.mLocation = MapData.coordinates2BaseMap(coordinate);
            mapMark.mLink = listData[titleNo("URL")];
            mapMark.mComment = listData[titleNo("概要")];
 
            //  マークデータをダイヤログに表示
            MarkInput markInput = new MarkInput();
            markInput.mMapMark = mapMark;
            markInput.mMarkList = mMarkList;
            var result = markInput.ShowDialog();
            if (result == true) {
                mMarkList.add(mapMark);
                mMainWindow.mapDisp(false);
            }
        }


        /// <summary>
        /// 分類リストからYamaRecoの山データの取得
        /// </summary>
        /// <param name="listData">(URL,山名)リスト</param>
        private void getMapListDownloadData(List<string[]> listData)
        {
            if (BtDataRequest.Content.ToString().CompareTo(mGetDataButtonLabel[0]) == 0) {
                //  データのダウンロードと山データの抽出
                mGetInfoDataAbort = false;
                getYamaRecoData(listData);
                BtDataRequest.Content = mGetDataButtonLabel[1];
            } else if (BtDataRequest.Content.ToString().CompareTo(mGetDataButtonLabel[1]) == 0) {
                //  ダウンロードの中断処理
                mGetInfoDataAbort = true;
                BtDataRequest.Content = mGetDataButtonLabel[0];
            }
        }

        /// <summary>
        /// 非同期処理によるYamaRecoデータをNo順に取得
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        private void getYamaRecoData(int sp, int ep)
        {
            List<string> urlList = new List<string>();
            for (int i = sp; i <= ep; i++) {
                string url = mYamaBaseUrl + mGenreUrlWord[(int)mGenreMode] + i.ToString();
                urlList.Add(url);
            }
            getYamaRecoData(urlList);
        }

        /// <summary>
        /// 非同期処理によるYamaRecoデータをリストを元に取得
        /// </summary>
        /// <param name="listData">取得データ(URL,山名)リスト</param>
        private void getYamaRecoData(List<string[]> listData)
        {
            List<string> urlList = new List<string>();
            foreach (var data in listData)
                urlList.Add(data[0]);

            getYamaRecoData(urlList);
        }

        /// <summary>
        /// 非同期処理によるYamaRecoWebページデータの取り込みとデータ登録
        /// </summary>
        /// <param name="urlList">URLリスト</param>
        private void getYamaRecoData(List<string> urlList)
        {
            PbGetInfoData.Maximum = urlList.Count;
            PbGetInfoData.Minimum = 0;
            PbGetInfoData.Value = 0;
            //  非同期処理
            Task.Run(() => {
                for (int i = 0; i < urlList.Count; i++) {
                    if (mGetInfoDataAbort)                          //  中断フラグ
                        break;

                    switch(genreOfUrl(urlList[i])) {
                        case GENREMODE.yamadata :
                            mYamaData.getYamaRecoData(urlList[i]);
                            break;
                        case GENREMODE.route :
                            mRouteData.getYamaRecoData(urlList[i]);
                            break;
                        case GENREMODE.guide:
                            mGuideRouteData.getYamaRecoData(urlList[i]);
                            break;
                        case GENREMODE.yamalist:
                            mYamaListData.getYamaRecoData(urlList[i]);
                            break;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        PbGetInfoData.Value++;
                        LbGetDataCount.Content = (PbGetInfoData.Value) + " / " + PbGetInfoData.Maximum;
                    });
                }
                Application.Current.Dispatcher.Invoke(() => {
                    if (PbGetInfoData.Value < PbGetInfoData.Maximum)
                        PbGetInfoData.Value = PbGetInfoData.Maximum;
                });
            });
        }

        /// <summary>
        /// 下部ステータスバーにデータ取得の進捗表示
        /// </summary>
        private void progressTerminate()
        {
            PbGetInfoData.Value = 0;
            BtDataRequest.Content = mGetDataButtonLabel[0];
            setYamaRecoData();
            mGenreChangeEnabled = true;
        }

        /// <summary>
        /// データ取得/検索/周辺情報 取得後の表示処理
        /// </summary>
        private void setYamaRecoData()
        {
            setDataList(false);
            if (mGenreMode == GENREMODE.yamadata) {
                setYamaData();
            } else if (mGenreMode == GENREMODE.route) {
                setRouteData();
            } else if (mGenreMode == GENREMODE.guide) {
                setGuideData();
            } else if (mGenreMode == GENREMODE.yamalist) {
                setYamaListData();
            }
        }

        /// <summary>
        /// 山データ取得/検索/周辺情報 取得後の表示処理
        /// </summary>
        private void setYamaData()
        {
            if (mDownloadMode == DOWNLOADMODE.normal) {
                //  通常時
                if (mYamaData.mDataList != null) {
                    setData(mYamaData.mDataList, mDispCol, mDetailCol);
                    mYamaData.setCategoryList();
                    setCategoryList(mYamaData.mCategoryList);
                    mYamaData.setDetailList();
                    setDetailList(mYamaData.mDetailList);
                }
            } else if (mDownloadMode == DOWNLOADMODE.select) {
                setSelectData(mSelectListData);
            }
        }

        /// <summary>
        /// ルートデータ取得/検索/周辺情報 取得後の表示処理
        /// </summary>
        private void setRouteData()
        {
            if (mDownloadMode == DOWNLOADMODE.normal) {
                //  通常時
                setTitle(mRouteData.mDataTitle, mDispCol, mColWidth);
                if (mRouteData.mDataList != null) {
                    setData(mRouteData.mDataList, mDispCol, mDetailCol);
                }
            } else if (mDownloadMode == DOWNLOADMODE.select) {
                setSelectData(mSelectListData);
            }
        }

        /// <summary>
        /// おすすめルートデータ取得/検索/周辺情報 取得後の表示処理
        /// </summary>
        private void setGuideData()
        {
            if (mDownloadMode == DOWNLOADMODE.normal) {
                //  通常時
                setTitle(mGuideRouteData.mDataTitle, mDispCol, mColWidth);
                if (mGuideRouteData.mDataList != null) {
                    setData(mGuideRouteData.mDataList, mDispCol, mDetailCol);
                }
            } else if (mDownloadMode == DOWNLOADMODE.select) {
                setSelectData(mSelectListData);
            }
        }

        /// <summary>
        /// 山リストデータの表示処理
        /// </summary>
        private void setYamaListData()
        {
            if (mDownloadMode == DOWNLOADMODE.normal) {
                //  通常時
                setTitle(mYamaListData.mDataTitle, mDispCol, mColWidth);
                if (mYamaListData.mDataList != null) {
                    setData(mYamaListData.mDataList, mDispCol, mDetailCol);
                }
            } else if (mDownloadMode == DOWNLOADMODE.select) {
                setSelectData(mSelectListData);
            }
        }

        /// <summary>
        /// 選択されたURLデータリストを各データに追加する(DataGridに追加)
        /// </summary>
        /// <param name="selectdata">周辺データURLリスト</param>
        private void setSelectData(List<string[]> selectdata)
        {
            if (0 < selectdata.Count) {
                GENREMODE genreMode = genreOfUrl(selectdata[0][0]);
                switch (genreMode) {
                    case GENREMODE.yamadata: {
                            //  周辺情報データからの山データリスト取得時
                            mGenreMode = GENREMODE.yamadata;
                            setDataList(false);
                            List<string[]> yamaDataList = mYamaData.extractListdata(selectdata);
                            if (0 < yamaDataList.Count) {
                                setData(yamaDataList, mDispCol, mDetailCol);
                            }
                            mGenreChangeEnabled = false;
                            CbGenre.SelectedIndex = 0;
                        }
                        break;
                    case GENREMODE.route: {
                            //  登山ルートデータから登山ルートデータを取得
                            mGenreMode = GENREMODE.route;
                            setDataList(false);
                            List<string[]> routeDataList = mRouteData.extractListdata(selectdata);
                            if (0 < routeDataList.Count) {
                                setData(routeDataList, mDispCol, mDetailCol);
                            }
                            mGenreChangeEnabled = false;
                            CbGenre.SelectedIndex = 1;
                        }
                        break;
                    case GENREMODE.guide: {
                            //  おすすめルートデータからおすすめルートデータを取得
                            mGenreMode = GENREMODE.guide;
                            setDataList(false);
                            List<string[]> guideDataList = mGuideRouteData.extractListdata(selectdata);
                            if (0 < guideDataList.Count) {
                                setData(guideDataList, mDispCol, mDetailCol);
                            }
                            mGenreChangeEnabled = false;
                            CbGenre.SelectedIndex = 2;
                        }
                        break;
                    case GENREMODE.yamalist:
                        //  山リスト
                        mGenreMode = GENREMODE.yamalist;
                        setDataList(false);
                        List<string[]> yamalistDataList = mYamaListData.extractListdata(selectdata);
                        if (0 < yamalistDataList.Count) {
                            setData(yamalistDataList, mDispCol, mDetailCol);
                        }
                        mGenreChangeEnabled = false;
                        CbGenre.SelectedIndex = 3;
                        break;
                }
            }
        }

        /// <summary>
        /// 下部ステースパーにデータ情報を表示
        /// </summary>
        private void setInfoData()
        {
            LbDataCount.Content = "データ数 " + DgDataList.Items.Count;
        }

        /// <summary>
        /// 山の分類データをコンボボックスに設定する
        /// </summary>
        /// <param name="categoryList">分類リスト</param>
        private void setCategoryList(List<string[]> categoryList)
        {
            CbCategory.Items.Clear();
            CbCategory.Items.Add("山の分類");
            foreach (var itemName in categoryList)
                CbCategory.Items.Add(itemName[0]);
            CbCategory.SelectedIndex = 0;
        }

        /// <summary>
        /// 山の種別データをコンボボックスに設定
        /// </summary>
        /// <param name="detailList">種別リスト</param>
        private void setDetailList(List<string> detailList)
        {
            CbDetail.Items.Clear();
            CbDetail.Items.Add("山の種別");
            foreach (var itemName in detailList)
                CbDetail.Items.Add(itemName);
            CbDetail.Items.Add("");
            CbDetail.SelectedIndex = 0;
        }

        /// <summary>
        /// 次検索
        /// </summary>
        /// <param name="searchWord"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int nextSearchData(string searchWord, int pos)
        {
            (Point searchCoordinate, double searchDistance) = mYamaData.getSearchCoordinate(searchWord);
            if (0 < DgDataList.Items.Count) {
                pos = pos < 0 ? 0 : pos;
                for (int i = pos; i < DgDataList.Items.Count; i++) {
                    if (mYamaData.searchDataChk((string[])DgDataList.Items[i], searchWord, searchCoordinate, searchDistance))
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 前検索
        /// </summary>
        /// <param name="searchWord"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int prevSearchData(string searchWord, int pos)
        {
            (Point searchCoordinate, double searchDistance) = mYamaData.getSearchCoordinate(searchWord);
            if (0 < DgDataList.Items.Count) {
                pos = pos < 0 ? 0 : pos;
                for (int i = pos; 0 <= i; i--) {
                    if (mYamaData.searchDataChk((string[])DgDataList.Items[i], searchWord, searchCoordinate, searchDistance))
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// データのタイトルをDataGridに設定する
        /// </summary>
        /// <param name="title">タイトル配列</param>
        /// <param name="dispCol">表示列配列</param>
        /// <param name="colWidth">列幅配列</param>
        private void setTitle(string[] title, bool[] dispCol, int[] colWidth)
        {
            DgDataList.Columns.Clear();
            for (int i = 0; i < title.Length; i++) {
                if (dispCol[i]) {
                    var column = new DataGridTextColumn();
                    column.Header = title[i];
                    column.Binding = new Binding($"[{i}]");
                    if (0 <= colWidth[i])
                        column.Width = colWidth[i];
                    DgDataList.Columns.Add(column);
                }
            }
        }

        /// <summary>
        /// DataGridにデータを設定
        /// </summary>
        /// <param name="dataList">データリスト</param>
        /// <param name="dispCol">表示列リスト</param>
        /// <param name="detailCol">詳細表示列リスト</param>
        private void setData(List<string[]> dataList, bool[] dispCol, bool[] detailCol)
        {
            int dispSize = dispCol.Count(item => item == true);
            DgDataList.Items.Clear();
            for (int i = 0; i < dataList.Count; i++) {
                string[] buf = new string[dispSize];
                int col = 0;
                for (int j = 0; j < dataList[i].Length; j++) {
                    if (dispCol[j]) {
                        if (detailCol[j]) {
                            //  詳細データ簡略表示
                            string detail = "";
                            string[] details = dataList[i][j].Split('\t');  //  複数の分類に分轄
                            if (0 < details.Length) {
                                //  [詳細データ]のタイトルのみ抽出
                                foreach (string data in details) {
                                    if (0 < data.Length) {
                                        int n = 0 < data.IndexOf(mSplitWord) ? data.IndexOf(mSplitWord) :
                                             0 < data.IndexOf(" ") ? data.IndexOf(" ") : data.Length;
                                        detail += (detail.Length > 0 ? "," : "") + data.Substring(0, n);
                                    }
                                }
                                buf[col++] = detail.Substring(0, Math.Min(detail.Length, mCellDispSize));
                            }
                        } else {
                            buf[col++] = dataList[i][j].Substring(0, Math.Min(dataList[i][j].Length, mCellDispSize));
                        }
                    }
                }
                DgDataList.Items.Add(buf);
            }
            setInfoData();
        }

        /// <summary>
        /// URLからデータのジャンルを求める
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>ジャンル</returns>
        private GENREMODE genreOfUrl(string url)
        {
            if (0 <= url.IndexOf(mGenreUrlWord[0])) {
                return GENREMODE.yamadata;
            } else if (0 <= url.IndexOf(mGenreUrlWord[1])) {
                return GENREMODE.route;
            } else if (0 <= url.IndexOf(mGenreUrlWord[2])) {
                return GENREMODE.guide;
            } else if (0 <= url.IndexOf(mGenreUrlWord[3])) {
                return GENREMODE.yamalist;
            }
            return GENREMODE.yamadata;
        }

        /// <summary>
        /// データリストのヘッダーのカラム位置を検索する
        /// </summary>
        /// <param name="title">カラムタイトル</param>
        /// <returns>カラム位置</returns>
        private int findListHeaderCol(string title)
        {
            for (int i = 0; i < DgDataList.Columns.Count; i++) {
                if (DgDataList.Columns[i].Header.ToString().CompareTo(title) == 0)
                    return i;
            }
            return -1;
        }


        /// <summary>
        /// データリストのタイトル名から配列位置を求める
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private int titleNo(string title)
        {
            if (mGenreMode == GENREMODE.yamadata)
                return mYamaData.titleNo(title);
            else if (mGenreMode == GENREMODE.route)
                return mRouteData.titleNo(title);
            else if (mGenreMode == GENREMODE.guide)
                return mGuideRouteData.titleNo(title);
            else if (mGenreMode == GENREMODE.yamalist)
                return mYamaListData.titleNo(title);
            return mYamaData.titleNo(title);
        }

        /// <summary>
        /// メッセージ表示
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="title"></param>
        private void messageBox(string buf, string title)
        {
            InputBox dlg = new InputBox();
            dlg.Title = title;
            dlg.mWindowSizeOutSet = true;
            dlg.mWindowWidth = 600.0;
            dlg.mWindowHeight = 400.0;
            dlg.mMultiLine = true;
            dlg.mReadOnly = true;
            dlg.mEditText = buf;
            dlg.ShowDialog();
        }
    }
}
