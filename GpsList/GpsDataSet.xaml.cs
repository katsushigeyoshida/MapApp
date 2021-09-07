using System;
using System.Windows;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// GpsDataSet.xaml の相互作用ロジック
    /// 
    /// GpsFileDataの編集ダイヤログ
    /// </summary>
    public partial class GpsDataSet : Window
    {
        public GpsFileData mGpsFileData;                //  GpsFileData
        public string[] mGroups;                        //  グループリスト
        private string[] mLineThicknes = new string[] { //  トレース線の太さリスト
            "1", "2", "3", "4", "5" };
        private string mDataFolder = "";                //  データフォルダ

        YLib ylib = new YLib();
        YDrawingShapes ydraw = new YDrawingShapes();
        

        public GpsDataSet()
        {
            InitializeComponent();

            //  初期設定
            Properties.Settings.Default.Reload();
            mDataFolder = Properties.Settings.Default.GpsDataFolder;
            CbColorType.ItemsSource = ydraw.getColor15Title();
            CbColorType.SelectedIndex = 7;
            CbThickness.ItemsSource = mLineThicknes;
            CbThickness.SelectedIndex = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //  GPsFileDataをコントロールに設定
            CbGroup.ItemsSource = mGroups;
            if (mGpsFileData != null) {
                TbTitle.Text = mGpsFileData.mTitle;
                if (mGroups != null)
                    CbGroup.SelectedIndex = Array.IndexOf(mGroups, mGpsFileData.mGroup);
                else
                    CbGroup.Text = mGpsFileData.mGroup;
                CbThickness.SelectedIndex = CbThickness.Items.IndexOf(mGpsFileData.mLineThickness.ToString());
                CbColorType.SelectedIndex = CbColorType.Items.IndexOf(mGpsFileData.mLineColor);
                TbFilePath.Text = mGpsFileData.mFilePath;
                TbComment.Text = mGpsFileData.mComment;
                LbDiscription.Content = getGpsDataDiscription();
            }
        }

        private string getGpsDataDiscription()
        {
            string buffer = "";
            DateTime startTime = mGpsFileData.mFirstTime;
            DateTime endTime = mGpsFileData.mLastTime;
            TimeSpan spanTime = endTime - startTime;
            buffer += "開始時間: " + startTime.ToString("yyyy/MM/dd HH:mm:ss") + " 終了時間: " + endTime.ToString("yyyy/MM/dd HH:mm:ss") +
                " 経過時間: " + ((spanTime.TotalMinutes < 60.0 * 24.0) ? spanTime.ToString(@"hh\:mm\:ss") : spanTime.ToString(@"d\d\a\y\ hh\:mm\:ss"));
            buffer += "\n移動距離: " + mGpsFileData.mDistance.ToString("#,##0.## km") + " 速度: " + (mGpsFileData.mDistance / spanTime.TotalHours).ToString("##0.# km/s");
            buffer += "\n最大標高: " + mGpsFileData.mMaxElevation.ToString("#,##0 m") + " 最小標高: " + mGpsFileData.mMinElevation.ToString("#,##0 m") +
                " 標高差: " + (mGpsFileData.mMaxElevation - mGpsFileData.mMinElevation).ToString("#,##0 m");

            return buffer;
        }

        private void setGpsFileData(string path)
        {
            if (mGpsFileData == null) {
                mGpsFileData = new GpsFileData(path);
            }
         }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.GpsDataFolder = mDataFolder;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [開く]ボタン GPXファイルの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOpen_Click(object sender, RoutedEventArgs e)
        {
            string filePath = ylib.fileSelect("", "gpx");
            if (0 < filePath.Length) {
                TbFilePath.Text = filePath;
                if (TbTitle.Text.Length == 0) {
                    TbTitle.Text = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    setGpsFileData(filePath);
                    LbDiscription.Content = getGpsDataDiscription();
                }

            }
        }

        /// <summary>
        /// [OK]ボタン データの登録
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOk_Click(object sender, RoutedEventArgs e)
        {
            mGpsFileData.mTitle = TbTitle.Text;
            mGpsFileData.mGroup = CbGroup.Text;
            mGpsFileData.setFilePath(TbFilePath.Text);
            mGpsFileData.mLineColor = CbColorType.Text;
            mGpsFileData.mLineThickness = double.Parse(CbThickness.Text);
            mGpsFileData.mComment = TbComment.Text;

            DialogResult = true;
            Close();

        }

        /// <summary>
        /// [Cancel]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
