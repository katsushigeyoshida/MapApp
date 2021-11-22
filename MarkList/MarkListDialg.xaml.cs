using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// MarkListDialg.xaml の相互作用ロジック
    /// マークリスト表示ダイヤログの処理
    /// </summary>
    public partial class MarkListDialg : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private double mPrevWindowHeight;                       //  変更前のウィンドウ高さ
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        public MarkList mMarkList;
        public MainWindow mMainWindow;
        private string[] mMarkSizeRate = { "0.5", "0.8", "1.0", "1.2", "1.5", "1.8", "2.0", "2.5", "3.0" };
        private YLib ylib = new YLib();

        public MarkListDialg()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;
            mPrevWindowHeight = mWindowHeight;
            WindowFormLoad();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //  グループとマークデータを設定
            dataLoad();
            TbSort.Text = mMarkList.mSortName[(int)mMarkList.mListSort];
            setSort((int)MarkList.SORTTYPE.Distance);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            WindowFormSave();
        }

        /// <summary>
        /// 
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
            double dy = mWindowHeight - mPrevWindowHeight;
            mPrevWindowWidth = mWindowWidth;
            mPrevWindowHeight = mWindowHeight;
            CbGroup.Width += dx;
            LbMarkList.Height += dy;
            //  表示の更新
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MarkListWidth < 100 ||
                Properties.Settings.Default.MarkListHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.MarkListHeight) {
                Properties.Settings.Default.MarkListWidth = mWindowWidth;
                Properties.Settings.Default.MarkListHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MarkListTop;
                Left = Properties.Settings.Default.MarkListLeft;
                Width = Properties.Settings.Default.MarkListWidth;
                Height = Properties.Settings.Default.MarkListHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MarkListTop = Top;
            Properties.Settings.Default.MarkListLeft = Left;
            Properties.Settings.Default.MarkListWidth = Width;
            Properties.Settings.Default.MarkListHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [タイトルリストボックス]　ダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbMarkList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            selectTitleOpen();
        }

        /// <summary>
        /// [グループ]コンボボックスの変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbGroup_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int index = CbGroup.SelectedIndex;
            if (0 <= index) {
                string group = CbGroup.Items[index].ToString();
                if (group.CompareTo("すべて") == 0)
                    group = "";
                mMarkList.mFilterGroup = group;
                setSort((int)mMarkList.mListSort, group);
                //LbMarkList.ItemsSource = mMarkList.getTitleList(group).ToArray();
                mMainWindow.mapDisp(false);
            }
        }

        /// <summary>
        /// [タイトルリストボックス]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbMarkListMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            int index = LbMarkList.SelectedIndex;
            string group = CbGroup.Text.CompareTo("すべて") == 0 ? "" : CbGroup.Text;
            if (0 <= index && menuItem.Name.CompareTo("LbMarkListMenuEdit") == 0) {
                //  マーク編集
                MapMark mapMark = mMarkList.getMapMark(LbMarkList.Items[index].ToString(), group);
                if (mapMark != null) {
                    MarkInput markInput = new MarkInput();
                    markInput.mMapMark = mapMark;
                    markInput.mGroups = mMarkList.getGroupList().ToArray();
                    var result = markInput.ShowDialog();
                    if (result == true) {
                        dataLoad();
                        mMainWindow.mapDisp(false);
                    }
                }
            } else if (menuItem.Name.CompareTo("LbMarkListMenuAdd") == 0) {
                //  マークの追加
                MapMark mapMark = new MapMark();
                mapMark.mLocation = mMainWindow.mMapData.getCenter();
                MarkInput markInput = new MarkInput();
                markInput.mMapMark = mapMark;
                markInput.mGroups = mMarkList.getGroupList().ToArray();
                var result = markInput.ShowDialog();
                if (result == true) {
                    mMarkList.add(mapMark);
                    dataLoad();
                    mMainWindow.mapDisp(false);
                }
            } else if (menuItem.Name.CompareTo("LbMarkListMenuDelete") == 0) {
                //  マーク削除
                MapMark mapMark = mMarkList.getMapMark(LbMarkList.Items[index].ToString(), group);
                MessageBoxResult result = MessageBox.Show("[" + mapMark.mTitle + "] 削除します", "確認", MessageBoxButton.OKCancel);
                if (mapMark != null && result == MessageBoxResult.OK) {
                    mMarkList.remove(mapMark);
                    dataLoad();                     //  グループを含めてマークリストの再表示
                    mMainWindow.mapDisp(false);
                }
            } else if (menuItem.Name.CompareTo("LbMarkListMenuSortNon") == 0) {
                setSort(0);
            } else if (menuItem.Name.CompareTo("LbMarkListMenuSortNormal") == 0) {
                setSort(1);
            } else if (menuItem.Name.CompareTo("LbMarkListMenuSortReverse") == 0) {
                setSort(2);
            } else if (menuItem.Name.CompareTo("LbMarkListMenuSortDistance") == 0) {
                setSort(3);
            } else if (menuItem.Name.CompareTo("LbMarkListMenuMarkSize") == 0) {
                setMarkSize();
            } else if (menuItem.Name.CompareTo("LbMarkListMenuImport") == 0) {
                importSortList();
            } else if (menuItem.Name.CompareTo("LbMarkListMenuExport") == 0) {
                exportSortFile();
            }
        }

        /// <summary>
        /// マークサイズの設定メニューをだす
        /// </summary>
        private void setMarkSize()
        {
            SelectMenu selectMenu = new SelectMenu();
            selectMenu.Title = "マークの倍率設定";
            selectMenu.mMenuList = mMarkSizeRate;
            selectMenu.mSelectIndex = Array.IndexOf(mMarkSizeRate, mMarkList.mSizeRate.ToString("#0.0"));
            if (selectMenu.ShowDialog() == true) {
                mMarkList.mSizeRate = ylib.doubleParse(selectMenu.mSelectItem);
                mMainWindow.mapDisp(false);
            }
        }

        /// <summary>
        /// タイトル選択による地図の移動
        /// </summary>
        private void selectTitleOpen()
        {
            int index = LbMarkList.SelectedIndex;
            if (index < 0)
                return;
            MapMark mapMark = mMarkList.getMapMark(LbMarkList.Items[index].ToString(), "");
            if (mapMark != null)
                mMainWindow.setMoveCtr(mapMark.mLocation);
        }

        /// <summary>
        /// マークのタイトルとグループデータをコントロールに設定する
        /// </summary>
        private void dataLoad()
        {
            List<string> groupList = mMarkList.getGroupList();
            groupList.Insert(0, "すべて");
            CbGroup.ItemsSource = groupList.ToArray();
            CbGroup.SelectedIndex = 0;
            LbMarkList.ItemsSource = mMarkList.getTitleList("").ToArray();
            mMarkList.mFilterGroup = "";
        }

        /// <summary>
        /// マークリストをソートして表示
        /// </summary>
        private void setSort(int sortType, string group = null)
        {
            if (group == null)
                group = CbGroup.Text.CompareTo("すべて") == 0 ? "" : CbGroup.Text;
            mMarkList.mCenter = MapData.baseMap2Coordinates(mMainWindow.mMapData.getCenter());
            switch (sortType) {
                case 1: mMarkList.mListSort = MarkList.SORTTYPE.Normal; break;
                case 2: mMarkList.mListSort = MarkList.SORTTYPE.Reverse; break;
                case 3 : mMarkList.mListSort = MarkList.SORTTYPE.Distance; break;
                default: mMarkList.mListSort = MarkList.SORTTYPE.Non; break;
            }
            TbSort.Text = mMarkList.mSortName[(int)mMarkList.mListSort];
            LbMarkList.ItemsSource = mMarkList.getTitleList(group).ToArray();
        }

        /// <summary>
        /// マークリストをインポートする
        /// </summary>
        private void importSortList()
        {
            string filePath = ylib.fileSelect("", "csv");
            if (0 < filePath.Length) {
                mMarkList.loadMarkFile(filePath, true);
                dataLoad();
            }
        }

        /// <summary>
        /// マークリストをエクスポートする
        /// </summary>
        private void exportSortFile()
        {
            string filePath = ylib.saveFileSelect("", "csv");
            if (0 < filePath.Length) {
                mMarkList.saveMarkFile(filePath);
            }
        }
    }
}
