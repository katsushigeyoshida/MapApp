using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private string mHelpFile = "MapAppManual.pdf";          //  PDFのヘルプファイル

        private string[] mGetDataButtonLabel = { "データ取得", "中断" };
        private bool mGetInfoDataAbort = true;                  //  データ取得中断フラグ

        private YamaRecoData mYamaRecoData = new YamaRecoData();
        private int mDispSize = 6;

        private YLib ylib = new YLib();

        public YamaRecoList()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;
            WindowFormLoad();       //  Windowの位置とサイズを復元

            setTitle(mYamaRecoData.mDataTitle);
            mYamaRecoData.loadData();
            setYamaRecoData();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TbSearchWord.Text = mCoordinate;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mYamaRecoData.saveData();
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
                int sp = ylib.intParse(TbDataStart.Text);
                int ep = ylib.intParse(TbDataEnd.Text);
                if (0 < sp && sp <= ep) {
                    mGetInfoDataAbort = false;
                    getYamaRecoData(sp, ep);
                    bt.Content = mGetDataButtonLabel[1];
                }
            } else if (bt.Content.ToString().CompareTo(mGetDataButtonLabel[1]) == 0) {
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
            List<string[]> listData = mYamaRecoData.getFilterongDataLsit(
                    CbCategory.SelectedIndex <= 0 ? "" : CbCategory.Items[CbCategory.SelectedIndex].ToString(),
                    TbSearchWord.Text, mDispSize);
            setData(listData);
            setInfoData();
        }

        /// <summary>
        /// [山の分類]コンボボックス選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbCategory.SelectedIndex) {
                List<string[]> listData = mYamaRecoData.getFilterongDataLsit(
                    CbCategory.SelectedIndex <= 0 ? "" : CbCategory.Items[CbCategory.SelectedIndex].ToString(),
                    TbSearchWord.Text, mDispSize);
                setData(listData);
                setInfoData();
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
                string coordinate = ((string[])DgDataList.Items[DgDataList.SelectedIndex])[2];
                mMainWindow.setMoveCtrCoordinate(ylib.cnvCoordinate(coordinate));
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
            if (menuItem.Name.CompareTo("DgMoveMenu") == 0) {
                //  地図を座標値に移動
                string coordinate = ((string[])DgDataList.Items[DgDataList.SelectedIndex])[titleNo("座標")];
                mMainWindow.setMoveCtrCoordinate(ylib.cnvCoordinate(coordinate));
            } else if (menuItem.Name.CompareTo("DgDispMenu") == 0) {
                //  詳細表示
                detaiilDisp((string[])DgDataList.Items[DgDataList.SelectedIndex]);
            } else if (menuItem.Name.CompareTo("DgOpenMenu") == 0) {
                //  開く
                string url = ((string[])DgDataList.Items[DgDataList.SelectedIndex])[titleNo("URL")];
                ylib.openUrl(url);
            } else if (menuItem.Name.CompareTo("DgMarkMenu") == 0) {
                //  マークを追加
                addMark((string[])DgDataList.Items[DgDataList.SelectedIndex]);
            } else if (menuItem.Name.CompareTo("DgRemoveMenu") == 0) {
                //  削除
                int urlNo = titleNo("URL");
                if (0 <= DgDataList.SelectedItems.Count) {
                    foreach (string[] item in DgDataList.SelectedItems) {
                        int n = mYamaRecoData.mDataList.FindIndex(p => p[urlNo].CompareTo(item[urlNo]) == 0);   //  URL
                        if (0 <= n)
                            mYamaRecoData.mDataList.RemoveAt(n);
                    }
                    setYamaRecoData();
                }
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
            if (CbCategory.SelectedIndex <= 0)
                return;
            if (menuItem.Name.CompareTo("CbCategoryOpenMenu") == 0) {
                //  開く
                string title = CbCategory.Items[CbCategory.SelectedIndex].ToString();
                string[] url = mYamaRecoData.mCategoryList.Find(p => p[0].CompareTo(title) == 0);
                if (url.Length == 2)
                    ylib.openUrl(url[1]);
            } else if (menuItem.Name.CompareTo("CbGetMapListMenu") == 0) {
                //  分類リストからデータ取得
                string title = CbCategory.Items[CbCategory.SelectedIndex].ToString();
                string[] url = mYamaRecoData.mCategoryList.Find(p => p[0].CompareTo(title) == 0);
                if (url.Length == 2 && 0 < url[1].Length) {
                    mYamaRecoData.getCategoryMapList(url[1]);
                    if (0 < mYamaRecoData.mCategoryMapList.Count)
                        getCategoryMapListData(mYamaRecoData.mCategoryMapList);
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
        /// 1と4カラムを数値としてソート処理するようにした
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgDataList_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            var sortDir = e.Column.SortDirection;
            if (ListSortDirection.Ascending != sortDir)
                sortDir = ListSortDirection.Ascending;  //  昇順
            else
                sortDir = ListSortDirection.Descending; //  降順

            int col = ylib.intParse(ylib.string2StringNumber(e.Column.SortMemberPath));
            if (ListSortDirection.Ascending == sortDir) {
                if (col == 1 || col == 5) {
                    mYamaRecoData.mDataList.Sort((a, b) => (int)(ylib.doubleParse(ylib.string2StringNumber(a[col])) - ylib.doubleParse(ylib.string2StringNumber(b[col]))));
                } else {
                    mYamaRecoData.mDataList.Sort((a, b) => a[col].CompareTo(b[col]));
                }
            } else {
                if (col == 1 || col == 5) {
                    mYamaRecoData.mDataList.Sort((b, a) => (int)(ylib.doubleParse(ylib.string2StringNumber(a[col])) - ylib.doubleParse(ylib.string2StringNumber(b[col]))));
                } else {
                    mYamaRecoData.mDataList.Sort((b, a) => a[col].CompareTo(b[col]));
                }
            }
            //  DataGridに設定
            List<string[]> listData = mYamaRecoData.getFilterongDataLsit(
                CbCategory.SelectedIndex <= 0 ? "" : CbCategory.Items[CbCategory.SelectedIndex].ToString(),
                TbSearchWord.Text, mDispSize);
            setData(listData);
            setInfoData();
            //  ソート方向を設定
            foreach (var column in DgDataList.Columns) {
                if (column.SortMemberPath == e.Column.SortMemberPath) {
                    column.SortDirection = sortDir;
                }
            }
        }

        /// <summary>
        /// YamaRecoデータからマークを登録追加
        /// </summary>
        /// <param name="listData"></param>
        private void addMark(string[] listData)
        {
            //  マークデータ設定
            MapMark mapMark = new MapMark();
            mapMark.mTitle = listData[0];
            Point coordinate = ylib.cnvCoordinate(listData[2]);
            mapMark.mLocation = MapData.coordinates2BaseMap(coordinate);
            mapMark.mLink = listData[5];
            mapMark.mComment = listData[3];
 
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
        /// <param name="listData"></param>
        private void getCategoryMapListData(List<string[]> listData)
        {
            if (BtDataRequest.Content.ToString().CompareTo(mGetDataButtonLabel[0]) == 0) {
                //  データのダウンロードと山データの値湧出
                mGetInfoDataAbort = false;
                int sp = ylib.intParse(TbDataStart.Text);
                int ep = ylib.intParse(TbDataEnd.Text);
                getYamaRecoData(listData);
                BtDataRequest.Content = mGetDataButtonLabel[1];
            } else if (BtDataRequest.Content.ToString().CompareTo(mGetDataButtonLabel[1]) == 0) {
                //  ダウンロードの中断処理
                mGetInfoDataAbort = true;
                BtDataRequest.Content = mGetDataButtonLabel[0];
            }
        }

        /// <summary>
        /// 下部ステータスバーにデータ取得の進捗表示
        /// </summary>
        private void progressTerminate()
        {
            BtDataRequest.Content = mGetDataButtonLabel[0];
            setYamaRecoData();
        }

        /// <summary>
        /// データの詳細表示
        /// </summary>
        /// <param name="listData"></param>
        private void detaiilDisp(string[] listData)
        {
            string title = listData[0];
            int selIndex = mYamaRecoData.mDataList.FindIndex(p => p[0].CompareTo(title) == 0);
            string buf = "山名: " + mYamaRecoData.mDataList[selIndex][titleNo("山名")];
            buf += "\n" + "標高: " + mYamaRecoData.mDataList[selIndex][titleNo("標高")];
            buf += "\n" + "座標: " + mYamaRecoData.mDataList[selIndex][titleNo("座標")];
            if (2 < mYamaRecoData.mDataList[selIndex].Length && 0 < mYamaRecoData.mDataList[selIndex][titleNo("種別")].Length) {
                buf += "\n" + "種別:";
                string[] text = mYamaRecoData.mDataList[selIndex][titleNo("種別")].ToString().Split('\t');
                for (int j = 0; j < text.Length; j++) {
                    buf += "\n  " + text[j].Trim();
                }
            }
            buf += "\n" + "概要:\n  " + mYamaRecoData.mDataList[selIndex][titleNo("概要")];
            buf += "\n" + "Web URL: " + mYamaRecoData.mDataList[selIndex][titleNo("URL")];
            if (5 < mYamaRecoData.mDataList[selIndex].Length && 0 < mYamaRecoData.mDataList[selIndex][titleNo("分類")].Length) {
                buf += "\n" + "分類:";
                string[] text = mYamaRecoData.mDataList[selIndex][titleNo("分類")].ToString().Split(',');
                for (int j = 0; j < text.Length; j++) {
                    buf += "\n  " + text[j].Trim();
                }
            }
            if (6 < mYamaRecoData.mDataList[selIndex].Length && 0 < mYamaRecoData.mDataList[selIndex][titleNo("登山口")].Length) {
                buf += "\n" + "登山口:";
                string[] text = mYamaRecoData.mDataList[selIndex][titleNo("登山口")].ToString().Split(',');
                for (int j = 0; j < text.Length; j++) {
                    buf += "\n  " + text[j].Trim();
                }
            }
            if (7 < mYamaRecoData.mDataList[selIndex].Length && 0 < mYamaRecoData.mDataList[selIndex][titleNo("山小屋")].Length) {
                buf += "\n" + "山小屋:";
                string[] text = mYamaRecoData.mDataList[selIndex][titleNo("山小屋")].ToString().Split(',');
                for (int j = 0; j < text.Length; j++) {
                    buf += "\n  " + text[j].Trim();
                }
            }
            messageBox(buf, title);
        }

        /// <summary>
        /// 非同期処理によるYamaRecoデータをNo順に取得
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        private void getYamaRecoData(int sp, int ep)
        {
            PbGetInfoData.Maximum = ep - sp + 1;
            PbGetInfoData.Minimum = 0;
            PbGetInfoData.Value = 0;
            if (mYamaRecoData.mDataList == null)
                mYamaRecoData.mDataList = new List<string[]>();
            //  非同期処理
            Task.Run(() => {
                for (int i = sp; i <= ep; i++) {
                    if (mGetInfoDataAbort)                          //  中断フラグ
                        break;
                    mYamaRecoData.getYamaRecoData(i);               //  データ取得
                    Application.Current.Dispatcher.Invoke(() => {
                        PbGetInfoData.Value++;
                        LbGetDataCount.Content = (PbGetInfoData.Value) + " / " + PbGetInfoData.Maximum;
                    });
                }
                Application.Current.Dispatcher.Invoke(() => {
                    PbGetInfoData.Value = PbGetInfoData.Maximum;
                });
            });
        }

        /// <summary>
        /// 非同期処理によるYamaRecoデータをリストを元に取得
        /// </summary>
        /// <param name="listData">取得データリスト</param>
        private void getYamaRecoData(List<string[]> listData)
        {
            PbGetInfoData.Maximum = listData.Count;
            PbGetInfoData.Minimum = 0;
            PbGetInfoData.Value = 0;
            if (mYamaRecoData.mDataList == null)
                mYamaRecoData.mDataList = new List<string[]>();
            //  非同期処理
            Task.Run(() => {
                for (int i = 0; i < listData.Count; i++) {
                    if (mGetInfoDataAbort)                          //  中断フラグ
                        break;
                    int n = ylib.intParse(ylib.string2StringNumber(listData[i][0]));
                    mYamaRecoData.getYamaRecoData(n);               //  データ取得
                    Application.Current.Dispatcher.Invoke(() => {
                        PbGetInfoData.Value++;
                        LbGetDataCount.Content = (PbGetInfoData.Value) + " / " + PbGetInfoData.Maximum;
                    });
                }
                Application.Current.Dispatcher.Invoke(() => {
                    PbGetInfoData.Value = PbGetInfoData.Maximum;
                });
            });
        }

        /// <summary>
        /// YamaRecoの取得したデータをDataGridに設定する
        /// </summary>
        private void setYamaRecoData()
        {
            if (mYamaRecoData.mDataList != null) {
                setData(mYamaRecoData.mDataList);
                mYamaRecoData.setCategoryList();
                setCategoryList(mYamaRecoData.mCategoryList);
                setInfoData();
                PbGetInfoData.Value = 0;
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
        /// 次検索
        /// </summary>
        /// <param name="searchWord"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int nextSearchData(string searchWord, int pos)
        {
            (Point searchCoordinate, double searchDistance) = mYamaRecoData.getSearchCoordinate(searchWord);
            if (0 < DgDataList.Items.Count) {
                pos = pos < 0 ? 0 : pos;
                for (int i = pos; i < DgDataList.Items.Count; i++) {
                    if (mYamaRecoData.searchDataChk((string[])DgDataList.Items[i], searchWord, searchCoordinate, searchDistance))
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
            (Point searchCoordinate, double searchDistance) = mYamaRecoData.getSearchCoordinate(searchWord);
            if (0 < DgDataList.Items.Count) {
                pos = pos < 0 ? 0 : pos;
                for (int i = pos; 0 <= i; i--) {
                    if (mYamaRecoData.searchDataChk((string[])DgDataList.Items[i], searchWord, searchCoordinate, searchDistance))
                        return i;
                }
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
            return mYamaRecoData.titleNo(title);
        }

        /// <summary>
        /// データのタイトルをDataGridに設定する
        /// </summary>
        /// <param name="title">タイトル配列</param>
        private void setTitle(string[] title)
        {
            DgDataList.Columns.Clear();
            int dataSize = Math.Min(title.Length, mDispSize);
            for (int i = 0; i < dataSize; i++) {
                var column = new DataGridTextColumn();
                column.Header = title[i];
                column.Binding = new Binding($"[{i}]");
                DgDataList.Columns.Add(column);
            }
        }

        /// <summary>
        /// DataGridにデータを設定
        /// </summary>
        /// <param name="dataList">データリスト</param>
        private void setData(List<string[]> dataList)
        {
            string splitWord = mYamaRecoData.mSplitWord;
            int detailNo = titleNo("種別");
            mDispSize = Math.Min(DgDataList.Columns.Count, mDispSize);
            DgDataList.Items.Clear();
            for (int i = 0; i < dataList.Count; i++) {
                string[] buf = new string[Math.Min(dataList[0].Length, mDispSize)];
                for (int j = 0; j < buf.Length; j++) {
                    if (j == detailNo && 0 < dataList[i][j].Length) {
                        //  [種別]]詳細分類データ
                        string detail = "";
                        string[] details = dataList[i][j].Split('\t');  //  複数の分類に分轄
                        if (0 < details.Length) {
                            //  [種別]のタイトルのみ抽出
                            foreach (string data in details) {
                                if (0 < data.Length) {
                                    int n = data.IndexOf(splitWord) < 0 ? data.Length : data.IndexOf(splitWord);
                                    detail += (detail.Length > 0 ? "," : "") + data.Substring(0, n);
                                }
                            }
                            buf[j] = detail;
                        }
                    } else {
                        buf[j] = dataList[i][j].Substring(0, Math.Min(dataList[i][j].Length, 100));
                    }
                }
                DgDataList.Items.Add(buf);
            }
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
            dlg.mWindowWidth = 500.0;
            dlg.mWindowHeight = 400.0;
            dlg.mMultiLine = true;
            dlg.mReadOnly = true;
            dlg.mEditText = buf;
            dlg.ShowDialog();
        }
    }
}
