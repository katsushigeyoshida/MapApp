using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// WikiList.xaml の相互作用ロジック
    /// </summary>
    public partial class WikiList : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅

        private WikiUrlList mWikiUrlList = new WikiUrlList();
        private DataGridTextColumn[] mDataGridColumn = new DataGridTextColumn[20];
        private string[] mGetDataButtonLabel = { "詳細取得", "中断" };
        private enum PROGRESSMODE { NON, GETDETAIL, SEARCHFILE};
        private PROGRESSMODE mProgressMode = PROGRESSMODE.NON;

        private WikiDataList mWikiDataList = new WikiDataList();
        private bool mGetInfoDataAbort = true;      //  詳細データ取得
        private bool mInfoDataUpdate = true;        //  詳細データ更新
        private string mAppFolder;                  //  アプリケーションフォルダ
        private string mDataFolder;
        private string mCurTitle = "";
        private string mUrlListPath = "";
        public string mCoordinate = "";
        public MainWindow mMainWindow;
        public MarkList mMarkList;

        YLib ylib = new YLib();

        public WikiList()
        {
            InitializeComponent();

            WindowFormLoad();                       //  Windowの位置とサイズを復元
            mAppFolder = ylib.getAppFolderPath();   //  アプリフォルダ
            mDataFolder = Path.Combine(mAppFolder, "WikiData");
            mUrlListPath = Path.Combine(mAppFolder, "WikiDataUrlList.csv");
            BtGetData.Content = mGetDataButtonLabel[0];

            //  DataGridTextColumnを配列に置換える
            mDataGridColumn[0] = DhData1;
            mDataGridColumn[1] = DhData2;
            mDataGridColumn[2] = DhData3;
            mDataGridColumn[3] = DhData4;
            mDataGridColumn[4] = DhData5;
            mDataGridColumn[5] = DhData6;
            mDataGridColumn[6] = DhData7;
            mDataGridColumn[7] = DhData8;
            mDataGridColumn[8] = DhData9;
            mDataGridColumn[9] = DhData10;
            mDataGridColumn[10] = DhData11;
            mDataGridColumn[11] = DhData12;
            mDataGridColumn[12] = DhData13;
            mDataGridColumn[13] = DhData14;
            mDataGridColumn[14] = DhData15;
            mDataGridColumn[15] = DhData16;
            mDataGridColumn[16] = DhData17;
            mDataGridColumn[17] = DhData18;
            mDataGridColumn[18] = DhData19;
            mDataGridColumn[19] = DhData20;

            mWikiUrlList.loadUrlList(mUrlListPath);
            setUrlList();
            CbSeachForm.ItemsSource = mWikiDataList.mSearchFormTitle;
            CbSeachForm.SelectedIndex = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TbSearch.Text = mCoordinate;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mWikiUrlList. saveUrlList(mUrlListPath);
            curWikiListSave();
            WindowFormSave();       //  ウィンドの位置と大きさを保存
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.WikiListWidth < 100 ||
                Properties.Settings.Default.WikiListHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.WikiListHeight) {
                Properties.Settings.Default.WikiListWidth = mWindowWidth;
                Properties.Settings.Default.WikiListHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.WikiListTop;
                Left = Properties.Settings.Default.WikiListLeft;
                Width = Properties.Settings.Default.WikiListWidth;
                Height = Properties.Settings.Default.WikiListHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.WikiListTop = Top;
            Properties.Settings.Default.WikiListLeft = Left;
            Properties.Settings.Default.WikiListWidth = Width;
            Properties.Settings.Default.WikiListHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [タイトル]コンボボックス URLの切替得
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbTitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbTitle.SelectedIndex && mProgressMode == PROGRESSMODE.NON) {
                //  URLまたはファイルからWikiリストを取得
                LbUrlAddress.Content = mWikiUrlList.mUrlList[CbTitle.SelectedIndex][1];
                getWikiDataList(mWikiUrlList.mUrlList[CbTitle.SelectedIndex][1]);
            }
        }

        /// <summary>
        /// [一覧データ抽出形式]コンボボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbSeachForm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            setListSearchForm();
        }

        /// <summary>
        /// [一覧更新]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtUpdateData_Click(object sender, RoutedEventArgs e)
        {
            if (0 <= CbTitle.SelectedIndex) {
                updateWikiList();
            }
        }

        /// <summary>
        /// [詳細取得]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtGetData_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Content.ToString().CompareTo(mGetDataButtonLabel[0]) == 0) {
                CbTitle.IsEnabled = false;
                mGetInfoDataAbort = false;
                mInfoDataUpdate = true;
                getInfoData();
                bt.Content = mGetDataButtonLabel[1];
            } else if (bt.Content.ToString().CompareTo(mGetDataButtonLabel[1]) == 0) {
                //  登録処理中断のフラグを設定
                CbTitle.IsEnabled = true;
                mGetInfoDataAbort = true;
                bt.Content = mGetDataButtonLabel[0];
                //setFormatData();
                //setDispWikiData();
            }
        }

        /// <summary>
        /// [詳細表示]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtInfoData_Click(object sender, RoutedEventArgs e)
        {
            reversDescriptionDisp();
        }

        /// <summary>
        /// [検索URL]ダフルクリックで一覧リストの Webページを開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbUrlAddress_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LbUrlAddress.Content.ToString().Length != 0) {
                System.Diagnostics.Process.Start(LbUrlAddress.Content.ToString());
            }
        }

        /// <summary>
        /// [URLアドレス]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbUrlContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("LbUrlCopyMenu") == 0) {
                //  URLのコピー
                Clipboard.SetText(LbUrlAddress.Content.ToString());
            } else if (menuItem.Name.CompareTo("LbUrlOpenMenu") == 0) {
                //  URLを開く
                System.Diagnostics.Process.Start(LbUrlAddress.Content.ToString());
            } else if (menuItem.Name.CompareTo("LbUrlAddMenu") == 0) {
                //  URLの追加
                if (mWikiUrlList.addUrlList())
                    setUrlList();
            } else if (menuItem.Name.CompareTo("LbUrlRemoveMenu") == 0) {
                //  URLの削除
                if (0 <= CbTitle.SelectedIndex) {
                    var result = MessageBox.Show("[" + mWikiUrlList.mUrlList[CbTitle.SelectedIndex][0] + "] を削除します", "削除確認", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK) {
                        mWikiUrlList.mUrlList.RemoveAt(CbTitle.SelectedIndex);
                        setUrlList();
                    }
                }
            }
        }

        /// <summary>
        /// [前検索]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtPrevSearch_Click(object sender, RoutedEventArgs e)
        {
            int n = mWikiDataList.prevSearchData(TbSearch.Text.ToString(), DgDataList.SelectedIndex);
            if (0 <= n) {
                DgDataList.SelectedIndex = n;
                WikiData item = (WikiData)DgDataList.Items[n];
                DgDataList.ScrollIntoView(item);
            }
        }

        /// <summary>
        /// [次検索]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtNextSearch_Click(object sender, RoutedEventArgs e)
        {
            int n = mWikiDataList.nextSearchData(TbSearch.Text.ToString(), DgDataList.SelectedIndex);
            if (0 <= n) {
                DgDataList.SelectedIndex = n;
                WikiData item = (WikiData)DgDataList.Items[n];
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
            if (0 < TbSearch.Text.Length) {
                mGetInfoDataAbort = false;      //  中断フラグ
                string fileName = CbTitle.SelectedIndex <= 0 ? "" : mWikiUrlList.mUrlList[CbTitle.SelectedIndex][0];
                getSearchFileWikiData(TbSearch.Text, mDataFolder, fileName);
                //mWikiDataList.getSearchAllWikiData(TbSearch.Text, mDataFolder, fileName);
                //getSearchFileTextTermnate();
            }
        }

        /// <summary>
        /// [データリスト]ダブルクリック 選択された位置へ移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgWikiList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //  地図位置
            WikiData wikiData = (WikiData)DgDataList.SelectedItem;
            mMainWindow.setMoveCtrCoordinate(stringCoordinate2Point(wikiData));
        }

        private void DgWikiList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        /// <summary>
        /// [データリスト]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgWikiListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("DgMapPositionMenu") == 0) {
                //  地図位置
                WikiData wikiData = (WikiData)DgDataList.SelectedItem;
                mMainWindow.setMoveCtrCoordinate(stringCoordinate2Point(wikiData));
            } else if (menuItem.Name.CompareTo("DgDispMenu") == 0) {
                //  詳細表示
                dispWikiSelectData();
            } else if (menuItem.Name.CompareTo("DgMarkAddMenu") == 0) {
                WikiData listData = (WikiData)DgDataList.SelectedItem;
                if (listData != null) {
                    addMark(listData);
                }
            } else if (menuItem.Name.CompareTo("DgCopyMenu") == 0) {
                //  選択データのコピー
                if (0 < DgDataList.SelectedItems.Count) {
                    string buffer = ylib.array2csvString(mWikiDataList.getFormatTitleData());
                    foreach (WikiData data in DgDataList.SelectedItems) {
                        buffer += "\n";
                        buffer += ylib.array2csvString(data.getStringData());
                    }
                    Clipboard.SetText(buffer);
                }
            } else if (menuItem.Name.CompareTo("DgOpenMenu") == 0) {
                //  選択アイテムのURLを開く
                WikiData listData = (WikiData)DgDataList.SelectedItem;
                if (listData != null) {
                    try {
                        System.Diagnostics.Process.Start(listData.mUrl);
                    } catch (Exception err) {
                        MessageBox.Show(err.Message, "例外エラー");
                    }
                }
            } else if (menuItem.Name.CompareTo("DgRemoveMenu") == 0) {
                //  データ削除
                if (0 < DgDataList.SelectedItems.Count) {
                    foreach (WikiData data in DgDataList.SelectedItems) {
                        mWikiDataList.mDataList.Remove(data);
                    }
                    curWikiListSave();
                    setDispWikiData();
                }
            }
        }

        /// <summary>
        /// [プログレスバー]詳細データの取得完了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PbGetInfoData_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PbGetInfoData.Value == PbGetInfoData.Maximum || mGetInfoDataAbort) {
                progressTerminate();
            }
        }

        /// <summary>
        /// [ヘルプ]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mWikiListHelp;
            help.Show();
        }

        /// <summary>
        /// URLのデータリストのタイトルをCbTitleのコンボボックスに設定
        /// </summary>
        private void setUrlList()
        {
            CbTitle.Items.Clear();
            foreach (string[] data in mWikiUrlList.mUrlList)
                CbTitle.Items.Add(data[0]);
            CbTitle.SelectedIndex = 0;
            if (0 <= CbTitle.SelectedIndex)
                LbUrlAddress.Content = mWikiUrlList.mUrlList[CbTitle.SelectedIndex][1];
            else
                LbUrlAddress.Content = "";
        }

        /// <summary>
        /// 一覧Webページのリストデータを取得して表示
        /// </summary>
        /// <param name="title">一覧タイトル</param>
        /// <param name="url">一覧のURL</param>
        private void getWikiDataList(string url)
        {
            //  ヘッダ初期化、ファイル名設定
            initHeader();
            if (0 <= url.IndexOf("http")) {
                string title = url.Substring(url.LastIndexOf("/") + 1).Replace(':', '_');
                string filePath = Path.Combine(mDataFolder, title + ".csv");
                setListSearchForm();
                if (File.Exists(filePath)) {
                    //  ファイルからデータを取得
                    mWikiDataList.loadData(filePath);
                    setFormatData(false);
                    if (mWikiDataList.mDataList.Count == 0)
                        mWikiDataList.getWikiDataList(title, url);
                } else {
                    //  Webページからデータを取得
                    mWikiDataList.getWikiDataList(title, url);
                    setFormatData();
                }
                //  データの表示
                setDispWikiData();
                mCurTitle = title;
            } else {
                //  空データ
                mWikiDataList.mDataList.Clear();
                setDispWikiData();
                mCurTitle = "";
            }
        }

        /// <summary>
        /// 一覧ページのWebから再取得
        /// </summary>
        private void updateWikiList()
        {
            //  ヘッダーを初期化
            initHeader();
            //  一覧ページのリストの取得
            setListSearchForm();
            mWikiDataList.getWikiDataList(mWikiUrlList.mUrlList[CbTitle.SelectedIndex][0], mWikiUrlList.mUrlList[CbTitle.SelectedIndex][1]);
            //  データを保存
            mCurTitle = mWikiUrlList.mUrlList[CbTitle.SelectedIndex][0].Replace(':', '_');
            curWikiListSave();
            //  リストデータに反映
            setFormatData();
            setDispWikiData();
        }

        /// <summary>
        /// Webソースから一覧リストを抽出る方法を設定する
        /// </summary>
        private void setListSearchForm()
        {
            mWikiDataList.mSearchForm = (WikiDataList.SEARCHFORM)Enum.ToObject(typeof(WikiDataList.SEARCHFORM), CbSeachForm.SelectedIndex);
        }

        /// <summary>
        /// WikiDataを表示データとして再設定
        /// </summary>
        private void setDispWikiData()
        {
            List<WikiData> wikiDataList = mWikiDataList.mDataList;
            if (wikiDataList != null) {
                DgDataList.ItemsSource = new ReadOnlyCollection<WikiData>(wikiDataList);
                LbSearchForm.Content = 0 < wikiDataList.Count ? wikiDataList[0].mSearchForm : "";
                LbGetDataProgress.Content = "データ数 " + mWikiDataList.mDataList.Count;
            } else {
                LbSearchForm.Content = "";
                LbGetDataProgress.Content = "";
            }
        }

        /// <summary>
        /// 詳細表示を反転する
        /// </summary>
        private void reversDescriptionDisp()
        {
            if (mDataGridColumn[0].Visibility == Visibility.Visible) {
                for (int i = 0; i < mDataGridColumn.Length && i < mWikiDataList.mFormatTitle.Count; i++)
                    mDataGridColumn[i].Visibility = Visibility.Hidden;
            } else {
                for (int i = 0; i < mDataGridColumn.Length && i < mWikiDataList.mFormatTitle.Count; i++)
                    mDataGridColumn[i].Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// ヘッダーを初期化
        /// </summary>
        private void initHeader()
        {
            for (int i = 0; i < mDataGridColumn.Length; i++)
                mDataGridColumn[i].Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 取得したデータからヘッダータイトルを求めてヘッダに反映
        /// </summary>
        /// <param name="webTitle">タイトル更新の有無</param>
        private void setFormatData(bool webTitle = true)
        {
            if (webTitle)
                mWikiDataList.getWebFormatTitle();     //  基本情報の項目取得
            for (int i = 0; i < mDataGridColumn.Length; i++) {
                if (i < mWikiDataList.mFormatTitle.Count)
                    mDataGridColumn[i].Header = mWikiDataList.mFormatTitle[i];
                else {
                    mDataGridColumn[i].Header = "Hidden";
                    mDataGridColumn[i].Visibility = Visibility.Hidden;
                }
            }
            mWikiDataList.setWikiInfoData();
        }

        /// <summary>
        /// 全ファイルの中から検索する
        /// </summary>
        /// <param name="searchText">検索文字列</param>
        /// <param name="dataFoleder">検索ファイルのフォルダ</param>
        /// <param name="fileName">検索ファイル名</param>
        public void getSearchFileWikiData(string searchText, string dataFoleder, string fileName = "")
        {
            //  検索が座標の場合
            Point searchCoordinate = new Point();
            double searchDistance = 20.0;
            if (0 < ylib.getCoordinatePattern(searchText).Length) {
                searchCoordinate = ylib.cnvCoordinate(searchText);
                int n = searchText.IndexOf(' ');
                if (0 < n)
                    searchDistance = ylib.string2double(searchText.Substring(n));
            }

            //  対象ファイルの検索
            LbSearchForm.Content = "";
            string[] fileList = ylib.getFiles(dataFoleder + "\\" + (fileName.Length == 0 ? "*" : fileName) + ".csv");
            if (fileList != null) {
                mWikiDataList.mDataList.Clear();
                //  一覧選択を無効にする
                mCurTitle = "";
                CbTitle.SelectedIndex = -1;
                LbUrlAddress.Content = "";
                //  プログレスバー初期化
                PbGetInfoData.Maximum = fileList.Length;
                PbGetInfoData.Minimum = 0;
                PbGetInfoData.Value = 0;
                mProgressMode = PROGRESSMODE.SEARCHFILE;
                //  非同期処理
                Task.Run(() => {
                    int count = 1;
                    foreach (string path in fileList) {
                        if (mGetInfoDataAbort)                          //  中断フラグ
                            break;
                        //  ファイルごとのデータ検索
                        if (searchCoordinate.X == 0 && searchCoordinate.Y == 0) {
                             mWikiDataList.getSerchWikiDataFile(searchText, path);
                        } else {
                            mWikiDataList.getSerchWikiDataFile(searchCoordinate, searchDistance, path);
                        }
                        Application.Current.Dispatcher.Invoke(() => {
                            PbGetInfoData.Value = count;
                            LbGetDataProgress.Content = "検出数 " + mWikiDataList.mDataList.Count;
                        });
                        count++;
                    }
                    //  検索結果の処理
                    if (0 < mWikiDataList.mDataList.Count) {
                        //  項目に距離があれば距離でソートする
                        int disPos = -1;
                        for (int i = 0; i < mWikiDataList.mDataList[0].mTagSetData.Count; i++) {
                            if (0 <= mWikiDataList.mDataList[0].mTagSetData[i][0].IndexOf("距離")) {
                                disPos = i;
                                break;
                            }
                        }
                        //  距離でソート
                        if (0 <= disPos)
                            mWikiDataList.mDataList.Sort((a, b) => Math.Sign(double.Parse(a.mTagSetData[disPos][1]) - double.Parse(b.mTagSetData[disPos][1])));
                    }
                    Application.Current.Dispatcher.Invoke(() => {
                        PbGetInfoData.Value = PbGetInfoData.Maximum;
                    });
                });
            }
        }

        /// <summary>
        /// ファイル内検索終了処理
        /// </summary>
        private void getSearchFileTextTermnate()
        {
            setFormatData();
            setDispWikiData();
            reversDescriptionDisp();
        }

        /// <summary>
        /// Webからのデータの取得進捗の表示(非同期処理)
        /// </summary>
        private void getInfoData()
        {
            LbSearchForm.Content = "";
            PbGetInfoData.Maximum = mWikiDataList.mDataList.Count - 1;
            PbGetInfoData.Minimum = 0;
            PbGetInfoData.Value = 0;
            mProgressMode = PROGRESSMODE.GETDETAIL;
            //  非同期処理
            Task.Run(() => {
                for (int i = 0; i < mWikiDataList.mDataList.Count; i++) {
                    if (mGetInfoDataAbort)                          //  中断フラグ
                        break;
                    mWikiDataList.mDataList[i].getTagSetData();     //  基本情報の取得
                    Application.Current.Dispatcher.Invoke(() => {
                        PbGetInfoData.Value = i;
                        LbGetDataProgress.Content = "進捗 " + (i + 1) + " / " + mWikiDataList.mDataList.Count;
                    });
                }
                Application.Current.Dispatcher.Invoke(() => {
                    PbGetInfoData.Value = PbGetInfoData.Maximum;
                });
            });
        }

        /// <summary>
        /// Webからのデータ取得の終了処理
        /// </summary>
        private void getInfoDataTerminate()
        {
            BtGetData.Content = mGetDataButtonLabel[0];
            CbTitle.IsEnabled = true;
            if (mInfoDataUpdate) {
                //  ヘッダの更新
                setFormatData();
                mInfoDataUpdate = false;
            }
            //  データの保存と表示
            curWikiListSave();
            setDispWikiData();
            reversDescriptionDisp();
        }

        /// <summary>
        /// 詳細データ取得または検索処理後の処理
        /// </summary>
        private void progressTerminate()
        {
            //  プログレスバー,メッセージ、ボタン名などの初期化
            if (mProgressMode != PROGRESSMODE.NON) {
                PbGetInfoData.Value = 0;
                LbGetDataProgress.Content = "完了";
                mGetInfoDataAbort = false;
            }
            if (mProgressMode == PROGRESSMODE.GETDETAIL) {
                getInfoDataTerminate();
            } else if (mProgressMode == PROGRESSMODE.SEARCHFILE) {
                getSearchFileTextTermnate();
            }
            PbGetInfoData.Value = 0;
            mProgressMode = PROGRESSMODE.NON;
        }

        /// <summary>
        /// リストの選択データの詳細を表示
        /// </summary>
        private void dispWikiSelectData()
        {
            //  詳細表示
            WikiData wikiData = (WikiData)DgDataList.SelectedItem;
            if (wikiData != null) {
                string[] data = wikiData.getStringData();
                string buffer = "";
                string[] wikiFormatTitle = mWikiDataList.getFormatTitleData();
                for (int i = 0; i < wikiFormatTitle.Length && i < data.Length; i++) {
                    if (wikiFormatTitle[i].CompareTo("Hidden") != 0 && 0 < data[i].Length) {
                        buffer += wikiFormatTitle[i] + " : ";
                        buffer += data[i] + "\n";
                    }
                }
                messageBox(buffer, "詳細表示");
                //MessageBox.Show(buffer, "詳細表示");
            }
        }

        /// <summary>
        /// Wikiデータでマーク編集ダイヤログに表示して追加
        /// </summary>
        /// <param name="wikiData"></param>
        private void addMark(WikiData wikiData)
        {
            //  マークの追加
            MapMark mapMark = new MapMark();
            mapMark.mLocation = mMainWindow.mMapData.getCenter();
            mapMark.mTitle = wikiData.mTitle;
            mapMark.mLocation = MapData.coordinates2BaseMap(stringCoordinate2Point(wikiData));
            mapMark.mLink = wikiData.mUrl;
            mapMark.mComment = wikiData.mComment;

            //  マークデータをダイヤ六表示
            MarkInput markInput = new MarkInput();
            markInput.mMapMark = mapMark;
            markInput.mGroups = mMarkList.getGroupList().ToArray();
            var result = markInput.ShowDialog();
            if (result == true) {
                mMarkList.add(mapMark);
                mMainWindow.mapDisp(false);
            }
        }

        /// <summary>
        /// Wikiデータの座標文字列を数値データに変換
        /// </summary>
        /// <param name="data">Wikiデータ</param>
        /// <returns>座標値(Point)</returns>
        private Point stringCoordinate2Point(WikiData data)
        {
            foreach (string item in data.mTag) {
                if (item != null && 0 <= item.IndexOf("北緯")) {
                    return ylib.cnvCoordinate(item);
                }
            }
            if (0 <= data.mComment.IndexOf("北緯")) {
                return ylib.cnvCoordinate(data.mComment);
            }
            return new Point(0.0, 0.0);
        }

        /// <summary>
        /// Wikiリストをファイルに保存
        /// </summary>
        private void curWikiListSave()
        {
            if (0 < mCurTitle.Length) {
                string filePath = Path.Combine(mDataFolder, mCurTitle + ".csv");
                mWikiDataList.saveData(filePath);
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
            dlg.mWindowWidth  = 500.0;
            dlg.mWindowHeight = 400.0;
            dlg.mMultiLine = true;
            dlg.mReadOnly = true;
            dlg.mEditText = buf;
            dlg.ShowDialog();
        }
    }
}
