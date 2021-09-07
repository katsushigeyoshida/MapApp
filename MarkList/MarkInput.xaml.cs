using System;
using System.Windows;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// MarkInput.xaml の相互作用ロジック
    /// 
    /// マークデータの入力ダイヤログ
    /// 
    /// </summary>
    public partial class MarkInput : Window
    {
        public MapMark mMapMark;
        public string[] mGroups;
        YLib ylib = new YLib();

        public MarkInput()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (mMapMark != null) {
                TbTitle.Text = mMapMark.mTitle;
                ChkTitleDisp.IsChecked = mMapMark.mTitleVisible;
                CbGroup.ItemsSource = mGroups;
                CbGroup.SelectedIndex = Array.IndexOf(mGroups, mMapMark.mGroup);
                CbMarkType.ItemsSource = mMapMark.mMarkName;
                CbMarkType.SelectedIndex = mMapMark.mMarkType;
                CbSize.ItemsSource = mMapMark.mSizeName;
                Point cp = MapData.baseMap2Coordinates(mMapMark.mLocation);
                TbCoordinates.Text = cp.Y.ToString() + "," + cp.X.ToString();
                CbSize.SelectedIndex = Array.IndexOf(mMapMark.mSizeName, mMapMark.mSize.ToString());
                TbComment.Text = ylib.strControlCodeRev(mMapMark.mComment);
                TbLink.Text = mMapMark.mLink;
            }
        }

        /// <summary>
        /// リンクデータを開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOpen_Click(object sender, RoutedEventArgs e)
        {
            if (0 < TbLink.Text.Length) {
                System.Diagnostics.Process p =
                        System.Diagnostics.Process.Start(TbLink.Text);
            }
        }

        private void BtOk_Click(object sender, RoutedEventArgs e)
        {
            //  データの保存
            mMapMark.mTitle = TbTitle.Text;
            mMapMark.mTitleVisible = ChkTitleDisp.IsChecked == true;
            mMapMark.mGroup = CbGroup.Text;
            mMapMark.mMarkType = CbMarkType.SelectedIndex;
            mMapMark.mSize = int.Parse(CbSize.Items[CbSize.SelectedIndex].ToString());
            mMapMark.mComment = ylib.strControlCodeCnv(TbComment.Text);
            mMapMark.mLink = Uri.UnescapeDataString(TbLink.Text);
            //  緯度経度座標をBaseMap座標に変換
            string[] coord = TbCoordinates.Text.Split(',');
            if (2 == coord.Length) {
                double x, y;
                if (double.TryParse(coord[1], out x) && double.TryParse(coord[0], out y)) {
                    if (-180.0 <= x && x <= 180.0 && -85.0 <= y && y <= 85.0) {
                        Point cp = new Point(x, y);
                        mMapMark.mLocation = MapData.coordinates2BaseMap(cp);
                    }
                }
            } 
            if (mMapMark.mLocation.X < 0.83 || 0.93 < mMapMark.mLocation.X ||
                mMapMark.mLocation.Y < 0.35 || 0.45 < mMapMark.mLocation.Y) {
                MessageBox.Show("緯度経度座標が範囲外です");
                return;
            }
            if (mMapMark.mTitle.Length < 1) {
                MessageBox.Show("タイトルが設定されていません");
                return;
            }


            DialogResult = true;
            Close();
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Wikiリストで「コピー」したデータを貼り付ける
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtPast_Click(object sender, RoutedEventArgs e)
        {
            string[] buffer = Clipboard.GetText().Split('\n');
            if (1 < buffer.Length) {
                string[] titles = ylib.seperateString(buffer[0]);
                string[] data = ylib.seperateString(buffer[1]);
                string title = "";
                string coordinate = "";
                string link = "";
                string comment = "";
                for (int i = 0; i < titles.Length; i++) {
                    if (titles[i].CompareTo("タイトル") == 0) {
                        title = data[i];
                    } else if (titles[i].CompareTo("座標") == 0) {
                        Point coord = ylib.cnvCoordinate(data[i]);
                        coordinate = coord.Y.ToString() + "," + coord.X.ToString();
                    } else if (titles[i].CompareTo("URL") == 0) {
                        link = data[i];
                    } else if (titles[i].CompareTo("Hidden") != 0 &&
                        titles[i].CompareTo("リストタイトル") != 0 &&
                        titles[i].CompareTo("親リストタイトル") != 0 &&
                        titles[i].CompareTo("親リストURL") != 0 &&
                        titles[i].CompareTo("一覧抽出方法") != 0 ) {
                        if (0 < data[i].Length)
                            comment += (0 < comment.Length ? "\n" : "") + titles[i] + ": " + data[i];
                    }
                }
                if (TbTitle.Text.Length == 0)
                    TbTitle.Text = title;
                if (TbCoordinates.Text.Length == 0)
                    TbCoordinates.Text = coordinate;
                if (TbLink.Text.Length == 0)
                    TbLink.Text = link;
                if (TbComment.Text.Length == 0)
                    TbComment.Text = comment;
            }
        }
    }
}
