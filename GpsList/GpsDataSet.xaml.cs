using System;
using System.IO;
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
        public GpsInfoData mGpsInfoData;
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
            BtGpxConv.Visibility = Visibility.Hidden;
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
                if (Path.GetExtension(mGpsFileData.mFilePath).ToLower() == ".fit") {
                    BtGpxConv.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// GPS情報を文字列取り出す
        /// </summary>
        /// <returns></returns>
        private string getGpsDataDiscription()
        {
            if (mGpsFileData != null)
                return mGpsFileData.getGpsInfoData().toString();
            if (mGpsInfoData != null)
                return mGpsInfoData.toString();
            return "";
        }

        /// <summary>
        /// GPSデータファイルから情報を取得
        /// </summary>
        /// <param name="path"></param>
        private void setGpsFileData(string path)
        {
            string ext = Path.GetExtension(path);
            if (ext.ToLower().CompareTo(".gpx") == 0) {
                mGpsFileData = new GpsFileData(path);
                mGpsInfoData = mGpsFileData.getGpsInfoData();
            } else if (ext.ToLower().CompareTo(".fit") == 0) {
                FitReader fitReader = new FitReader(path);
                int count = fitReader.getDataRecordAll(FitReader.DATATYPE.gpxData);
                mGpsInfoData = fitReader.getGpsInfoData();
                mGpsFileData = new GpsFileData();
                mGpsFileData.setGpsInfoData(mGpsInfoData);
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
            string filePath = ylib.fileSelect("", "gpx,fit");
            if (0 < filePath.Length) {
                TbFilePath.Text = filePath;
                if (TbTitle.Text.Length == 0) {
                    TbTitle.Text = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    setGpsFileData(filePath);
                    LbDiscription.Content = getGpsDataDiscription();
                }
                if (Path.GetExtension(filePath).ToLower() == ".fit") {
                    BtGpxConv.Visibility = Visibility.Visible;
                } else {
                    BtGpxConv.Visibility = Visibility.Hidden;
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

        /// <summary>
        /// FIT→GPX変換ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtGpxConv_Click(object sender, RoutedEventArgs e)
        {
            string fitPath = TbFilePath.Text;
            if (Path.GetExtension(fitPath).ToLower() == ".fit") {
                string ext = Path.GetExtension(fitPath);
                string gpxPath = fitPath.Replace(ext, ".gpx");
                if (File.Exists(gpxPath)) {
                    if (MessageBox.Show(gpxPath + "\n上書きしてもいいですか", "確認", 
                        MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                        return;
                }
                convGpx(fitPath, gpxPath);
                MessageBox.Show("GPX変換終了\n" + gpxPath);
            }
        }

        /// <summary>
        /// FitのGPSデータをGPXファイルに変換
        /// </summary>
        /// <param name="fitPath">Fitファイルパス</param>
        /// <param name="gpxPath">GPXファイルパス</param>
        /// <returns>可否</returns>
        private bool convGpx(string fitPath, string gpxPath)
        {
            FitReader fitReader = new FitReader(fitPath);
            int count = fitReader.getDataRecordAll(FitReader.DATATYPE.gpxData);
            if (count == 0)
                return false;
            fitReader.dataChk();    //  エラーデータチェック
            GpxWriter gpxWriter = new GpxWriter(fitReader.mListGpsData, gpxPath);
            return gpxWriter.writeDataAll();
        }
    }
}
