using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// リストボックスにチェックマークのデータを反映させるためのクラス
    /// </summary>
    public class CheckBoxListItem
    {
        public bool Checked { get; set; }
        public string Text { get; set; }
        public CheckBoxListItem(bool ch, string text)
        {
            Checked = ch;
            Text = text;
        }
    }

    /// <summary>
    /// GpsListDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class GpsListDialog : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private double mPrevWindowHeight;                       //  変更前のウィンドウ高さ
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)
        private bool mGroupEnabled = true;                      //  不要なグループリストの変更を防ぐためのフラグ
        GpsGraph mGpsGraph = null;

        public GpsDataList mGpsDataList;
        public MainWindow mMainWindow;

        private YLib ylib = new YLib();

        public GpsListDialog()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;
            mPrevWindowHeight = mWindowHeight;
            WindowFormLoad();       //  Windowの位置とサイズを復元

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            groupDataSet();             //  グループリストの設定
            listDataSet();              //  マークリストの設定
            mMainWindow.mapDisp(false); //  地図データの再表示
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (mGpsGraph != null)
                mGpsGraph.Close();
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
            double dy = mWindowHeight - mPrevWindowHeight;
            mPrevWindowWidth = mWindowWidth;
            mPrevWindowHeight = mWindowHeight;
            CbGroup.Width += dx;
            LbGpsList.Height += dy;
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.GpsListWidth < 100 ||
                Properties.Settings.Default.GpsListHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.GpsListHeight) {
                Properties.Settings.Default.GpsListWidth = mWindowWidth;
                Properties.Settings.Default.GpsListHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.GpsListTop;
                this.Left = Properties.Settings.Default.GpsListLeft;
                this.Width = Properties.Settings.Default.GpsListWidth;
                this.Height = Properties.Settings.Default.GpsListHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.GpsListTop = this.Top;
            Properties.Settings.Default.GpsListLeft = this.Left;
            Properties.Settings.Default.GpsListWidth = this.Width;
            Properties.Settings.Default.GpsListHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// グループリストの変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mGroupEnabled) {
                listDataSet();
                mMainWindow.mapDisp(false);
            }
        }

        /// <summary>
        /// マークリストのチェックボックスOn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            int index = LbGpsList.SelectedIndex;
            //System.Diagnostics.Debug.WriteLine($"CheckBox_Checked: {index}");
            visibleDataSet();
            mMainWindow.mapDisp(false);
        }

        /// <summary>
        /// マークリストのチェックボックスff
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            int index = LbGpsList.SelectedIndex;
            //System.Diagnostics.Debug.WriteLine($"CheckBox_Checked: {index}");
            visibleDataSet();
            mMainWindow.mapDisp(false);
        }

        /// <summary>
        /// コンテキストメニューの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbGpsListMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            int index = LbGpsList.SelectedIndex;
            int groupNo = CbGroup.SelectedIndex;
            string group = CbGroup.Text.CompareTo("すべて") == 0 ? "" : CbGroup.Text;
            if (menuItem.Name.CompareTo("LbGpsListMenuAdd") == 0) {
                //  項目の追加
                GpsDataSet gpsDataSet = new GpsDataSet();
                //gpsDataSet.mGpsFileData = new GpsFileData();
                gpsDataSet.mGroups = mGpsDataList.getGroupList().ToArray();
                var result = gpsDataSet.ShowDialog();
                if (result == true) {
                    mGpsDataList.addData(gpsDataSet.mGpsFileData);
                    groupDataSet(groupNo);
                    listDataSet();
                }
            } else if (0 <= index && menuItem.Name.CompareTo("LbGpsListMenuEdit") == 0) {
                //  選択項目のデータを編集
                GpsDataSet gpsDataSet = new GpsDataSet();
                CheckBoxListItem item = (CheckBoxListItem)LbGpsList.Items[index];
                GpsFileData gpsFileData = mGpsDataList.findDataTitle(item.Text, group);
                if (gpsFileData != null) {
                    gpsDataSet.mGpsFileData = gpsFileData;
                    gpsDataSet.mGroups = mGpsDataList.getGroupList().ToArray();
                    if (gpsDataSet.ShowDialog() == true) {
                        groupDataSet(groupNo);
                        listDataSet();
                    }
                }
            } else if (0 <= index && menuItem.Name.CompareTo("LbGpsListMenuDelete") == 0) {
                //  リストボックスから項目削除
                CheckBoxListItem item = (CheckBoxListItem)LbGpsList.Items[index];
                GpsFileData gpsFileData = mGpsDataList.findDataTitle(item.Text, group);
                mGpsDataList.removeData(gpsFileData);
                listDataSet();
            } else if (0 <= index && menuItem.Name.CompareTo("LbGpsListMenuMove") == 0) {
                //  移動
                CheckBoxListItem item = (CheckBoxListItem)LbGpsList.Items[index];
                GpsFileData gpsFileData = mGpsDataList.findDataTitle(item.Text, group);
                if (gpsFileData != null) {
                    mMainWindow.setMoveCtr(gpsFileData.getCenter());
                }
            } else if (0 <= index && menuItem.Name.CompareTo("LbGpsListMenuGraph") == 0) {
                //  標高などのグラフ表示
                CheckBoxListItem item = (CheckBoxListItem)LbGpsList.Items[index];
                GpsFileData gpsFileData = mGpsDataList.findDataTitle(item.Text, group);
                mGpsGraph = new GpsGraph();
                mGpsGraph.mGraphFilePath = gpsFileData.mFilePath;
                mGpsGraph.Show();
                return;
            } else if (0 <= index && menuItem.Name.CompareTo("LbGpsListMenuAllCheck") == 0) {
                //  すべてにチェックを入れる
                visibleDataAllSet(true);
                listDataSet();
            } else if (0 <= index && menuItem.Name.CompareTo("LbGpsListMenuAllUnCheck") == 0) {
                //  すべてのチェックを外す
                visibleDataAllSet(false);
                listDataSet();
            } else {
                return;
            }
            mMainWindow.mapDisp(false);
        }

        /// <summary>
        /// 表示グループ変更で各マークの表示フラグを更新
        /// </summary>
        private void visibleDataSet()
        {
            string group = CbGroup.Text.CompareTo("すべて") == 0 ? "" : CbGroup.Text;
            foreach (CheckBoxListItem checkBoxListItem in LbGpsList.Items) {
                GpsFileData gpsFileData = mGpsDataList.findDataTitle(checkBoxListItem.Text, group);
                if (gpsFileData != null)
                    gpsFileData.mVisible = checkBoxListItem.Checked;
            }
        }

        /// <summary>
        /// 表示しているリストにすべてのチェックの入り切り
        /// </summary>
        /// <param name="check"></param>
        private void visibleDataAllSet(bool check)
        {
            string group = CbGroup.Text.CompareTo("すべて") == 0 ? "" : CbGroup.Text;
            foreach (CheckBoxListItem checkBoxListItem in LbGpsList.Items) {
                GpsFileData gpsFileData = mGpsDataList.findDataTitle(checkBoxListItem.Text, group);
                if (gpsFileData != null)
                    gpsFileData.mVisible = check;
            }
        }

        /// <summary>
        /// グループリストをコンボボックスに設定
        /// </summary>
        private void groupDataSet(int groupNo = 0)
        {
            mGroupEnabled = false;
            CbGroup.Items.Clear();
            CbGroup.Items.Add("すべて");
            List<string> groupList = mGpsDataList.getGroupList();
            foreach (string group in groupList)
                CbGroup.Items.Add(group);
            CbGroup.SelectedIndex = groupNo;
            mGroupEnabled = true;
            mGpsDataList.mFilterGroup = "";
        }

        /// <summary>
        /// リストボックスにデータを設定する
        /// </summary>
        private void listDataSet()
        {
            string group = CbGroup.Items[CbGroup.SelectedIndex].ToString();
            LbGpsList.Items.Clear();
            foreach (GpsFileData gpsFileData in mGpsDataList.mGpsDataList) {
                if (group.CompareTo("すべて") == 0 || group.CompareTo(gpsFileData.mGroup) == 0)
                    LbGpsList.Items.Add(new CheckBoxListItem(gpsFileData.mVisible, gpsFileData.mTitle));
            }
            mGpsDataList.mFilterGroup = group.CompareTo("すべて") == 0 ? "" : group;
        }
    }
}
