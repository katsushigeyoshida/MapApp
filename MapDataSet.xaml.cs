using System.Windows;

namespace MapApp
{
    /// <summary>
    /// MapDataSet.xaml の相互作用ロジック
    /// 
    /// 地図データ情報設定ダイヤログ
    /// </summary>
    public partial class MapDataSet : Window
    {
        public string[] mDatas = new string[] {  //  地図データ初期値
            "", "", "", "", "", "", "", "", "" , "", "", "", "", ""
        };

        public MapDataSet()
        {
            InitializeComponent();
            Title = "地図データ情報";
        }

        /// <summary>
        /// 起動時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //  データをコントロールに設定
            TbTitle.Text = mDatas[0];
            TbDataID.Text = mDatas[1];
            TbFIleExt.Text = mDatas[2];
            TbMapType.Text = mDatas[3];
            TbZoomLevel.Text = mDatas[4];
            TbMapArea.Text = mDatas[5];
            TbDiscription.Text = mDatas[6];
            TbWebDataAddress.Text = mDatas[7];
            TbRefTitle.Text = mDatas[8];
            TbWebAddress.Text = mDatas[9];
            TbElevatorID.Text = mDatas[10];
            TbBaseID.Text = mDatas[11];
            TbTransportColor.Text = mDatas[12];
            CbBaseOrder.IsChecked = mDatas[13].ToLower().CompareTo("true") == 0;
        }

        /// <summary>
        /// [OK]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            if (TbTitle.Text.Length == 0 || TbDataID.Text.Length == 0 || TbFIleExt.Text.Length == 0) {
                MessageBox.Show("データの設定が不足しています。");
                return;
            }
            //  データIDの重複登録チェック
            if (containsMapId(TbDataID.Text) && mDatas[1].CompareTo(TbDataID.Text) != 0) {
                MessageBox.Show("データIDが既に存在していますので別名を設定してください。");
                return;
            }
            //  データのセット
            mDatas[0] = TbTitle.Text;
            mDatas[1] = TbDataID.Text;
            mDatas[2] = TbFIleExt.Text;
            mDatas[3] = TbMapType.Text;
            mDatas[4] = TbZoomLevel.Text;
            mDatas[5] = TbMapArea.Text;
            mDatas[6] = TbDiscription.Text;
            mDatas[7] = TbWebDataAddress.Text;
            mDatas[8] = TbRefTitle.Text;
            mDatas[9] = TbWebAddress.Text;
            mDatas[10] = TbElevatorID.Text;
            mDatas[11] = TbBaseID.Text;
            mDatas[12] = TbTransportColor.Text;
            mDatas[13] = CbBaseOrder.IsChecked.ToString();
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
        /// データIDの重複チェック
        /// </summary>
        /// <param name="mapId">データID</param>
        /// <returns>重複有無</returns>
        private bool containsMapId(string mapId)
        {
            //  地図データ
            foreach (string[] mapInf in MapInfoData.mMapData) {
                if (mapInf[1].ToLower().CompareTo(mapId.ToLower()) == 0)
                    return true;
            }
            //  標高データ
            foreach (string[] mapEleInf in MapInfoData.mMapElevatorData) {
                if (mapEleInf[1].ToLower().CompareTo(mapId.ToLower()) == 0)
                    return true;
            }
            return false;
        }
    }
}
