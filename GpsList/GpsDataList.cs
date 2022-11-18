using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using WpfLib;

namespace MapApp
{
    public class GpsDataList
    {
        public List<GpsFileData> mGpsDataList = new List<GpsFileData>();
        public string mFilterGroup = "";
        public bool mVisible = true;

        YLib ylib = new YLib();

        public GpsDataList()
        {
        }

        /// <summary>
        /// GPXファイル名を指定してデータを登録
        /// </summary>
        /// <param name="path"></param>
        public void addFile(string path)
        {
            GpsFileData gpsFileData = new GpsFileData(path);
            mGpsDataList.Add(gpsFileData);
        }

        /// <summary>
        /// GpsFileDataをリストに追加
        /// </summary>
        /// <param name="gpsFileData"></param>
        public void addData(GpsFileData gpsFileData)
        {
            mGpsDataList.Add(gpsFileData);
        }

        /// <summary>
        /// GpsFileDataをリストから削除
        /// </summary>
        /// <param name="gpsFileData"></param>
        public void removeData(GpsFileData gpsFileData)
        {
            mGpsDataList.Remove(gpsFileData);
        }

        /// <summary>
        /// リスト上のGPSトレースを表示する
        /// </summary>
        /// <param name="ydraw"></param>
        /// <param name="mapData"></param>
        public void draw(YGButton ydraw, MapData mapData)
        {
            if (0 < mGpsDataList.Count && mVisible) {
                foreach (GpsFileData gpsFileData in mGpsDataList) {
                    gpsFileData.draw(ydraw, mapData, mFilterGroup);

                }
            }
        }

        /// <summary>
        /// タイトルとクループ名からGPXデータを検索
        /// </summary>
        /// <param name="title"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        public GpsFileData findDataTitle(string title, string group)
        {
            foreach(GpsFileData gpsFileData in mGpsDataList) {
                if (gpsFileData.mTitle.CompareTo(title) == 0 &&
                    (group.Length== 0 || gpsFileData.mGroup.CompareTo(group) == 0)) {
                    return gpsFileData;
                }
            }
            return null;
        }

        /// <summary>
        /// GPXのリストデータからタイトル名を取得
        /// </summary>
        /// <returns></returns>
        public List<string> getTitleList()
        {
            List<string> titleList = new List<string>();
            foreach(GpsFileData gpsFileData in mGpsDataList) {
                titleList.Add(gpsFileData.mTitle);
            }
            return titleList;
        }

        /// <summary>
        /// GPXのリストデータからグループ名を抽出する
        /// </summary>
        /// <returns></returns>
        public List<string> getGroupList()
        {
            List<string> groupList = new List<string>();
            foreach (GpsFileData gpsFileData in mGpsDataList) {
                if (!groupList.Contains(gpsFileData.mGroup))
                    groupList.Add(gpsFileData.mGroup);
            }
            return groupList;
        }

        /// <summary>
        /// GPXのリストデータをファイルに保存
        /// </summary>
        /// <param name="path"></param>
        public void saveGpsFile(string path)
        {
            if (mGpsDataList.Count == 0)
                return;
            List<string[]> dataList = new List<string[]>();
            foreach (GpsFileData gpsFileData in mGpsDataList) {
                string[] data = gpsFileData.getStringData();
                dataList.Add(data);
            }
            ylib.saveCsvData(path, GpsFileData.mFormatTitle, dataList);
        }

        /// <summary>
        /// ファイルからGPXリストデータを読み込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void loadGpsFile(string path)
        {
            if (File.Exists(path)) {
                List<string[]> dataList = new List<string[]>();
                dataList = ylib.loadCsvData(path, GpsFileData.mFormatTitle);
                //  GPSデータの読込
                mGpsDataList.Clear();
                foreach (string[] data in dataList) {
                    GpsFileData gpsFileData = new GpsFileData();
                    gpsFileData.setStringData(data);
                    mGpsDataList.Add(gpsFileData);
                }
            }
        }
    }

    /// <summary>
    /// GPXファイルの位置情報データクラス
    /// </summary>
    public class GpsFileData
    {
        private List<Point> mLocData = new List<Point>();   //  座標リスト
        private Rect mLocArea;                  //  トレースエリア(BaseMap)
        private double mGpsDispRate = 10.0;     //  GPSトレースの間引き割合
        //public List<GpsData> mGpsDataList;
        public string mTitle = "";              //  タイトル
        public string mGroup = "";              //  グループ
        public string mLineColor = "Green";     //  トレースカラー
        public double mLineThickness = 1;       //  トレースの太さ
        public string mFilePath = "";           //  GPXファイルのパス
        public string mComment = "";            //  コメント
        public bool mVisible = true;            //  表示可否
        public static string[] mFormatTitle = new string[] {
            "Title", "Group", "Comment", "FilePath", "Visible", "Color", "Thickness",
            "Left", "Top", "Right", "Bottom", "Distance", "MinElevator", "MaxElevator",
            "FirstTime", "LastTime"
        };
        public double mDistance = 0.0;          //  移動距離
        public double mMinElevation = 0.0;      //  最小標高
        public double mMaxElevation = 0.0;      //  最大標高
        public DateTime mFirstTime;             //  開始時間
        public DateTime mLastTime;              //  終了時間

        private YLib ylib = new YLib();

        public GpsFileData()
        {

        }

        public GpsFileData(string path)
        {
            setFilePath(path);
        }

        /// <summary>
        /// パラメータを文字配列データに変換する
        /// </summary>
        /// <returns>GPXリストのデータ</returns>
        public string[] getStringData()
        {
            string[] data = new string[mFormatTitle.Length];
            data[0] = mTitle;
            data[1] = mGroup;
            data[2] = mComment;
            data[3] = mFilePath;
            data[4] = mVisible.ToString();
            data[5] = mLineColor;
            data[6] = mLineThickness.ToString();

            data[7] = mLocArea.Left.ToString();
            data[8] = mLocArea.Top.ToString();
            data[9] = mLocArea.Right.ToString();
            data[10] = mLocArea.Bottom.ToString();
            data[11] = mDistance.ToString();
            data[12] = mMinElevation.ToString();
            data[13] = mMaxElevation.ToString();
            data[14] = mFirstTime.ToString();       //  yyyy/MM/dd hh:mm:ss
            data[15] = mLastTime.ToString();

            return data;
        }

        /// <summary>
        /// 文字配列データでパラメータを設定する
        /// </summary>
        /// <param name="data">GPXリストのデータ</param>
        public void setStringData(string[] data)
        {
            mTitle = data[0];
            mGroup = data[1];
            mComment = data[2];
            mFilePath = data[3];
            //setFilePath(mFilePath);                                     //  GPSデータの読込
            mVisible = data[4].Length > 0 ? bool.Parse(data[4]) : true;
            mLineColor = data[5];
            mLineThickness = double.Parse(data[6]);
            //  setFilePath()を実行した時は下記は除外する
            mLocArea.X = data[7].Length == 0 ? 0.0 : double.Parse(data[7]);
            mLocArea.Y = data[8].Length == 0 ? 0.0 : double.Parse(data[8]);
            mLocArea.Width = data[9].Length == 0 ? 0.0 : double.Parse(data[9]) - mLocArea.X;
            mLocArea.Height = data[10].Length == 0 ? 0.0 : double.Parse(data[10]) - mLocArea.Y;
            mDistance = data[11].Length == 0 ? 0.0 : double.Parse(data[11]);
            mMinElevation = data[12].Length == 0 ? 0.0 : double.Parse(data[12]);
            mMaxElevation = data[13].Length == 0 ? 0.0 : double.Parse(data[13]);
            mFirstTime = data[14].Length == 0 ? DateTime.MinValue : DateTime.Parse(data[14]);
            mLastTime = data[7].Length == 0 ? DateTime.MinValue : DateTime.Parse(data[15]);
        }

        /// <summary>
        /// 指定したGPXファイルを読み込んでトレース座標データと領域を設定する
        /// </summary>
        /// <param name="path">GPXファイルパス</param>
        public bool setFilePath(string path)
        {
            mFilePath = path;
            GpxReader gpsReader = new GpxReader(path, GpxReader.DATATYPE.gpxSimpleData);
            if (gpsReader.mListGpsPointData.Count == 0)
                return false;
            gpsReader.dataChk();                                    //  エラーデータチェック

            mLocData = gpsReader.mListGpsPointData;
            mDistance = gpsReader.mGpsInfoData.mDistance;
            mMinElevation = gpsReader.mGpsInfoData.mMinElevator;
            mMaxElevation = gpsReader.mGpsInfoData.mMaxElevator;
            mFirstTime = gpsReader.mGpsInfoData.mFirstTime;
            mLastTime = gpsReader.mGpsInfoData.mLastTime;
            //  データ領域をBaseMapに変換
            Point sp = MapData.coordinates2BaseMap(gpsReader.mGpsInfoData.mArea.TopLeft);
            Point ep = MapData.coordinates2BaseMap(gpsReader.mGpsInfoData.mArea.BottomRight);
            mLocArea = new Rect(sp, ep);
            return true;
        }

        /// <summary>
        /// トレースを表示する
        /// </summary>
        /// <param name="ydraw">描画Lib</param>
        /// <param name="mapData">MapData</param>
        /// <param name="filterGroup">グループのフィルタ文字</param>
        public void draw(YGButton ydraw, MapData mapData, string filterGroup)
        {
            if (mVisible && (filterGroup.Length < 1 || filterGroup.CompareTo(mGroup) == 0)) {
                //  表示フラグ、グループ表示有効
                ydraw.setColor(mLineColor);
                ydraw.setThickness(mLineThickness);
                //  表示エリアチェック
                if (mLocArea.Width != 0.0 && mLocArea.Height != 0.0 && !insideChk(mapData.getArea(), mLocArea))
                    return;
                //  エリアサイズチェック
                Rect area = mapData.baseMap2Screen(mLocArea);
                if (area.Width < 10.0 || area.Height < 10) {
                    //  簡易表示
                    //System.Diagnostics.Debug.WriteLine($"GpsFileData: draw: 簡易表示 {mTitle}");
                    ydraw.drawWRectangle(area, 0.0);
                } else {
                    //  正規表示
                    if (mLocData.Count == 0 || mLocArea.Width == 0.0 || mLocArea.Height == 0.0) {
                        //  データがロードされていない
                        //System.Diagnostics.Debug.WriteLine($"GpsFileData: draw: データロード {mTitle}");
                        setFilePath(mFilePath);
                    }
                    if (1 < mLocData.Count && insideChk(mapData.getArea(), mLocArea)) {
                        int skipCount = skipLocCount(mapData.getArea(), mapData.mScreen);   //  データの間引き数
                        //System.Diagnostics.Debug.WriteLine($"SkipCount: {skipCount} {mLocData.Count} {mLocData.Count/skipCount}");
                        //System.Diagnostics.Debug.WriteLine($"GpsFileData: draw: データ表示 {mTitle}");
                        Point sp = mapData.coordinates2Screen(mLocData[0]);
                        Point ep = new Point();
                        int i = 1;
                        for( ; i < mLocData.Count; i += skipCount) {
                            if (mLocData[i].X != 0 && mLocData[i].Y != 0) {
                                ep = mapData.coordinates2Screen(mLocData[i]);
                                ydraw.drawWLine(sp, ep);
                                sp = ep;
                            }
                        }
                        if (i < mLocData.Count - 1) {
                            ep = mapData.coordinates2Screen(mLocData[mLocData.Count - 1]);
                            ydraw.drawWLine(sp, ep);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 表示領域の大きさに合わせてGPSデータの間引き数を求める
        /// </summary>
        /// <param name="dispArea">表示領域</param>
        /// <param name="windowSize">Windowサイズ</param>
        /// <returns>間引き数</returns>
        private int skipLocCount(Rect dispArea, Size windowSize)
        {
            double sizeRate = Math.Max(mLocArea.Width / dispArea.Width, mLocArea.Height / dispArea.Height);
            //System.Diagnostics.Debug.WriteLine($"sizeRate: {sizeRate} {(int)windowSize.Width} {mGpsDispRate}");
            return Math.Max((int)(mLocData.Count / (windowSize.Width * sizeRate) * mGpsDispRate), 1);
        }

        /// <summary>
        /// Rectの内外判定
        /// 一部でもArea内にあればTRUE
        /// </summary>
        /// <param name="area">判定エリア</param>
        /// <param name="data">対象データ</param>
        /// <returns>判定結果</returns>
        private bool insideChk(Rect area, Rect data)
        {
            if (data.Right < area.Left || area.Right < data.Left ||
                    data.Bottom < area.Top || area.Bottom < data.Top)
                return false;
            else
                return true;
        }

        /// <summary>
        /// トレース領域を拡張する
        /// </summary>
        /// <param name="pos">拡張座標(BaseMap)</param>
        /// <param name="rect">領域</param>
        /// <returns></returns>
        private Rect rectExtension(Point pos, Rect rect)
        {
            Point sp = new Point(Math.Min(pos.X, rect.X), Math.Min(pos.Y, rect.Y));
            Point ep = new Point(Math.Max(pos.X, rect.Right), Math.Max(pos.Y, rect.Bottom));
            return new Rect(sp, ep);
        }

        /// <summary>
        /// トレース領域の中心座標を求める
        /// </summary>
        /// <returns>BaseMap座標</returns>
        public Point getCenter()
        {
            return new Point(mLocArea.X + mLocArea.Width / 2.0, mLocArea.Y + mLocArea.Height / 2.0);
        }
    }
}
