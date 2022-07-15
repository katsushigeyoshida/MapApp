using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// 地図管理データクラス
    /// 
    ///  地図の位置情報
    ///  国土地理院のタイル画像を画面上に配置するためのクラス
    ///  
    ///  ZoomLevel : 0-20 (使われているのは20まで) 世界全体を表すためのレベルで数字が大きいほど詳細が見れる
    ///              0だと一枚のタイル画像で世界全体を表示, 1だと2x2の4枚で表し,2だと3x3の9枚で表示する
    ///  MapTitle : 地図の種類、標準図や写真図などいろいろあり、MapInfoDataに記載
    ///  Ext : タイル画像の拡張子、主にpngで写真データはjpgになっている(MapInfoDataに記載)
    ///  CellSize : タイル画像の表示の大きさ、元データは256x256pixel
    ///  ColCount : 描画領域に表示するタイル画像の列数
    ///  RowCount : 描画領域に表示するタイル画像の行数、描画領域とColCountで決まる
    ///  Start : 描画領域に表示する左上のタイル画像の開始番号
    ///  View : 美容が領域のサイズ
    ///  
    ///  座標系
    ///  Screen : 描画領域のスクリーン座標
    ///  BaseMap : ZoomLevel= 0 の時の座標、赤道の周長を1に換算して表す
    ///  Map : ZoomLevel ごとの座標、 BaseMap * 2^ZoomLevel となる
    ///  Coordinates : メルカトル図法による緯度経度座標(度)
    ///  
    ///  メルカトル図法の緯度変換
    ///  参考: https://qiita.com/Seo-4d696b75/items/aa6adfbfba404fcd65aa
    ///  幾何学的な円筒投影法だと f(Φ) = R x tan(Φ) であるが (Φ : 緯度(rad))
    ///  メルカトル図法では f(Φ) = R x ln(tan(π/4 + Φ/2)) で表される
    ///  逆変換は Φ = 2 x arcTan(exp(y/R)) - π/2
    /// </summary>
    public class MapData
    {
        private HashSet<string> mImageFileSet = new HashSet<string>();  //  地図イメージデータ取得リスト
        private Dictionary<string, List<string[]>> mElevatorDataList = new Dictionary<string, List<string[]>>();    //  標高データ
        public Dictionary<string, string> mColorLegend;         //  色凡例データ
        private string[] mLegendTitle = { "RGB", "comment" };   //  色凡例データタイトル
        public string mBaseFolder = "Map";              //  保存先フォルダ

        public string mMapUrl = MapInfoData.mGsiUrl;    //  地図データURL(ディフォルトは地理院地図)
        private string mMapUrl2 = "";                   //  日時データ置き換え後のURL(z,x,yの置換え用)
        private string mDateTimeFolder = "";            //  日時データ用のフォルダ名

        public string mDataIdName = "std";              //  データ種別名(std...)
        public string mExt = "png";                     //  タイル画像の拡張子
        public string mTileOrder = "";                  //  タイルデータの座標順(していない時は{z}/{x}/{y})
        public string mWebUrl = MapInfoData.mHelpUrl;   //  地図データ提供先URL

        public int mDataId = 0;                         //  地図データの種別
        public int mZoom = 0;                           //  ズームレベル
        public Point mStart = new Point(0, 0);          //  表示開始位置(MAP座標(タイル画像単位))
        public int mColCount = 4;                       //  表示するタイル画像の列数
        public int mRowCount = 4;                       //  表示するタイル画像の行数
        private int mMaxColCount = 30;                  //  表示できる最大列数
        public double mCellSize = 256;                  //  タイル画像の大きさ(一辺の長さ)
        public Size mView = new Size(1000, 1000);       //  表示するViewの大きさ
        public int mUseCol = 0;                         //  使用した列数
        public int mUseRow = 0;                         //  使用した行数
        public const int mMaxZoom = 18;                 //  最大ズームレベル
        public int mElevatorDataNo = 0;                 //  使用標高データのNo

        public string mBaseDataIDName = "";             //  重ね合わせBase地図ID
        public System.Drawing.Color[] mTransportColors; //  重ねるデータの透過色
        public bool mBaseMapOver = false;               //  ベースマップの重ねる順番で上になる

        public MapData mBaseMap = null;                 //  重ね合わせるベースの地図
        public DateTime[] mChangeMapDateTime;           //  地図を切り替えた時の時間
        public string[] mDateTimeForm = {               //  地図切替時間をURLとPATHに設定するフォーム
            "yyyyMMddHHmmss", 
            "yyyyMMddHHmmss_UTC", "yyyyMMddHHmmss_UTC0",//  雨雲レーダー用
            "yyyyMMddHHmmss_UTC1", "yyyyMMddHHmmss_UTC2"//  天気分布予報用
        };
        public List<DateTime> mDispMapDateTime = new List<DateTime>();  //  画面表示時間(日本時間)
        public DateTime mDispMapPreDateTime = DateTime.MinValue;
        public int mDateTimeInc = 0;                    //  表示時間の増加数
        public int mDateTimeInterval = 0;               //  日時追加のインターバル時間(分)

        private YDrawingShapes ydraw = new YDrawingShapes();
        private YLib ylib = new YLib();

        public MapData()
        {
            setDateTime();
        }


        public MapData(string mapID)
        {
            int mapDataIdNo = MapInfoData.mMapData.FindIndex(n => n[1].CompareTo(mapID) == 0);
            if (0 <= mapDataIdNo)
                setMapInfoData(mapDataIdNo);
            setDateTime();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="mapData">MapData</param>
        public MapData(MapData mapData)
        {
            mMapUrl = mapData.mMapUrl;
            mDataId = mapData.mDataId;
            mDataIdName = mapData.mDataIdName;
            mExt = mapData.mExt;
            mTileOrder = mapData.mTileOrder;
            mWebUrl = mapData.mMapUrl;
            mZoom = mapData.mZoom;
            mStart =  new Point(mapData.mStart.X, mapData.mStart.Y);
            mColCount = mapData.mColCount;
            mRowCount = mapData.mRowCount;
            mCellSize = mapData.mCellSize;
            mView = new Size(mapData.mView.Width, mapData.mView.Height);

            loadLegendData();
        }


        /// <summary>
        ///  取得時間設定 (雨雲レーダーに合わせ5分おきに設定)
        /// </summary>
        public void setDateTime(bool forth = false)
        {
            mMapUrl2 = mMapUrl;
            mDateTimeInterval = 0;
            mDispMapDateTime.Clear();
            if (isDateTimeData()) {
                setDateTime2(mMapUrl);
                if (forth || (0 < mDateTimeFolder.Length && mDispMapDateTime[0] != mDispMapPreDateTime)) {
                    //  過去データを削除
                    removeMapData(false);
                    mDispMapPreDateTime = mDispMapDateTime[0];
                }
            } else {
                mDateTimeFolder = "";
            }
            return;
        }

        /// <summary>
        /// 日時形式の変換文字列の入った地図URLで日時データの置き換えをする
        /// </summary>
        /// <param name="mapUrl">日時置換えURL</param>
        public void setDateTime2(string mapUrl)
        {
            List<string[]> transData = new List<string[]>();
            mDispMapDateTime.Clear();

            DateTime dateJpn = DateTime.Now;            //  日本時間
            DateTime dateUtc = DateTime.UtcNow;         //  UTC時間
            List<string> convForm = ylib.extractBrackets(mapUrl);
            for (int i = 0; i < convForm.Count; i++) {
                if (0 <= convForm[i].IndexOf("yyyy")) {
                    string[] dateBuf = convForm[i].Split('_');
                    string convData = transDate(dateBuf, dateJpn, dateUtc);
                    transData.Add(new string[] { "{" + convForm[i] + "}", convData });
                }
            }
            foreach (string[] data in transData)
                mMapUrl2 = mMapUrl2.Replace(data[0], data[1]);
        }

        /// <summary>
        /// 日時形式から日時データを設定する
        /// </summary>
        /// <param name="form">日時形式</param>
        /// <param name="dateJpn">日本時間</param>
        /// <param name="dateUtc">世界時間</param>
        /// <returns>日時データ文字列</returns>
        private string transDate(string[] form, DateTime dateJpn, DateTime dateUtc)
        {
            DateTime dateTime = DateTime.UtcNow;
            if (form.Length == 1) {
                dateTime = new DateTime(dateJpn.Year, dateJpn.Month, dateJpn.Day, dateJpn.Hour, dateJpn.Minute / 5 * 5, 0);
                mDispMapDateTime.Add(dateTime);
            } else if (form.Length == 2) {
                //  yyyyMMddHHmmss_UTCx
                if (form[1].CompareTo("UTC") == 0) {
                    //  10分単位の時間
                    dateTime = ylib.roundDateTimeMin(dateUtc, 10);
                } else if (form[1].CompareTo("UTC0") == 0) {
                    //  15分単位で時間をかえる
                    mDateTimeInterval = 15;
                    dateUtc = dateUtc.Add(new TimeSpan(0, 15 * mDateTimeInc, 0));
                    dateTime = ylib.roundDateTimeMin(dateUtc, 15);
                } else if (form[1].CompareTo("UTC1") == 0) {
                    dateTime = roundDateTime(dateUtc);
                } else if (form[1].CompareTo("UTC2") == 0) {
                    //  3時間単位で時間を変える
                    mDateTimeInterval = 3 * 60;
                    dateUtc = dateUtc.Add(new TimeSpan(3 * mDateTimeInc, 0, 0));
                    dateTime = ylib.roundDateTimeMin(dateUtc, 180);
                } else if (ylib.IsNumberString(form[1])) {
                    //  {yyyyMMddHHmmss_n}
                    mDateTimeInterval = ylib.intParse(form[2]);
                    dateTime = ylib.roundDateTimeMin(dateJpn, mDateTimeInterval);
                }
            } else if (form.Length == 3) {
                //  yyyyMMddHHmmss_UTCx_Interval(delay)
                mDateTimeInterval = ylib.intParse(form[2]);
                int delay = ylib.intParse(form[2]);
                if (form[1].CompareTo("UTC") == 0) {
                    dateTime = ylib.roundDateTimeMin(dateUtc, mDateTimeInterval);
                } else if (form[1].CompareTo("UTC0") == 0) {
                    dateUtc = dateUtc.Add(new TimeSpan(0, mDateTimeInterval * mDateTimeInc, 0));
                    dateTime = ylib.roundDateTimeMin(dateUtc, mDateTimeInterval);
                } else if (form[1].CompareTo("UTC1") == 0) {
                    dateUtc = dateUtc.Add(new TimeSpan(0, -delay, 0));
                    dateTime = roundDateTime(dateUtc);
                } else if (form[1].CompareTo("UTC2") == 0) {
                    //  3時間単位で時間を変える
                    mDateTimeInterval = 3 * 60;
                    dateUtc = dateUtc.Add(new TimeSpan(0, -delay, 0));
                    dateUtc = dateUtc.Add(new TimeSpan(3 * mDateTimeInc, 0, 0));
                    dateTime = ylib.roundDateTimeMin(dateUtc, 180);
                }
            } else if (form.Length == 4) {
                //  yyyyMMddHHmmss_UTCx_Interval_Delay
                mDateTimeInterval = ylib.intParse(form[2]);
                int delay = ylib.intParse(form[3]);
                if (form[1].CompareTo("UTC") == 0) {
                    dateUtc = dateUtc.Add(new TimeSpan(0, -delay, 0));
                    dateTime = ylib.roundDateTimeMin(dateUtc, mDateTimeInterval);
                } else if (form[1].CompareTo("UTC0") == 0) {
                    dateUtc = dateUtc.Add(new TimeSpan(0, -delay, 0));
                    dateUtc = dateUtc.Add(new TimeSpan(0, mDateTimeInterval * mDateTimeInc, 0));
                    dateTime = ylib.roundDateTimeMin(dateUtc, mDateTimeInterval);
                }
            } else {
                //  10分単位の時間
                mDateTimeInterval = 10;
                dateTime = dateJpn.Add(new TimeSpan(0, mDateTimeInterval * mDateTimeInc, 0));
                dateTime = ylib.roundDateTimeMin(dateTime, mDateTimeInterval);
            }
            //  地図に表示する日時データ(日本時間に変換)
            DateTime dateTimeJpn = (0 <= form[1].IndexOf("UTC")) ? dateTime.Add(new TimeSpan(9, 0, 0)) : dateTime;
            mDispMapDateTime.Add(dateTimeJpn);
            //  日時データの追加フォルダ名
            mDateTimeFolder = "\\" + dateTime.ToString(form[0]);

            return dateTime.ToString(form[0]);
        }

        /// <summary>
        /// 時間データを2時、8時、20時に丸める
        /// </summary>
        /// <param name="date">時間</param>
        /// <returns>丸めた時間</returns>
        private DateTime roundDateTime(DateTime date)
        {
            DateTime roundDate;
            if (2 < date.Hour && date.Hour < 8)
                roundDate = new DateTime(date.Year, date.Month, date.Day, 2, 0, 0);
            else if (8 < date.Hour && date.Hour < 20)
                roundDate = new DateTime(date.Year, date.Month, date.Day, 8, 0, 0);
            else if (20 < date.Hour && date.Hour < 24)
                roundDate = new DateTime(date.Year, date.Month, date.Day, 20, 0, 0);
            else {
                date = date.Add(new TimeSpan(-1, 0, 0, 0));
                roundDate = new DateTime(date.Year, date.Month, date.Day, 20, 0, 0);
            }
            return roundDate;
        }


        /// <summary>
        /// MapInfoDataの値を設定する
        /// </summary>
        /// <param name="mapDataID">MapInfoDataのNo</param>
        public void setMapInfoData(int mapDataIdNo)
        {
            mDataId = mapDataIdNo;                                  //  MapInfoDataのNo
            mMapUrl = MapInfoData.mMapData[mDataId][7];             //  国土地理院データ以外のURL
            mDataIdName = MapInfoData.mMapData[mDataId][1];         //  データID
            mExt = MapInfoData.mMapData[mDataId][2];                //  データファイルの拡張子
            mTileOrder = MapInfoData.mMapData[mDataId][8];          //  {z}/{x}/{y}以外のタイル座標順 → ヘルプ参照先URL
            mElevatorDataNo = getElevatorDataNo(MapInfoData.mMapData[mDataId][10]); //
                                                                                    //  
            mBaseDataIDName = MapInfoData.mMapData[mDataId][11];    //  重ね合わせのベースマップID
            if (0 <= mBaseDataIDName.Length && mDataIdName.CompareTo(mBaseDataIDName) != 0)
                mBaseMap = new MapData(mBaseDataIDName);
            //  透過色の設定
            if (0 < MapInfoData.mMapData[mDataId][12].Length) {
                string[] stringColors = MapInfoData.mMapData[mDataId][12].Split(',');
                mTransportColors = new System.Drawing.Color[stringColors.Length];
                for (int i = 0; i < stringColors.Length; i++) {
                    mTransportColors[i] = ylib.hexString2Color(stringColors[i]);
                }
            } else {
                //  設定が空の時
                mTransportColors = new System.Drawing.Color[1];
                mTransportColors[0] = System.Drawing.Color.White;
            }
            mBaseMapOver = MapInfoData.mMapData[mDataId][13].ToLower().CompareTo("true") == 0;  //  BaseMapの上下

            loadLegendData();                                       //  凡例データ読込
        }

        /// <summary>
        /// Viewサイズを設定
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void setViewSize(double width, double height)
        {
            mView = new Size(width, height);
            normarized();
        }

        /// <summary>
        /// データのコピーを作成
        /// </summary>
        /// <returns></returns>
        public MapData Copy()
        {
            MapData mapData = new MapData();
            mapData.mMapUrl = mMapUrl;
            mapData.mDataId = mDataId;
            mapData.mDataIdName = mDataIdName;
            mapData.mExt = mExt;
            mapData.mTileOrder = mTileOrder;
            mapData.mWebUrl = mWebUrl;
            mapData.mZoom = mZoom;
            mapData.mStart = new Point(mStart.X, mStart.Y);
            mapData.mColCount = mColCount;
            mapData.mRowCount = mRowCount;
            mapData.mCellSize = mCellSize;
            mapData.mView = new Size(mView.Width, mView.Height);
            mapData.mBaseDataIDName = mBaseDataIDName;
            mapData.mTransportColors = mTransportColors;
            mapData.mBaseMapOver = mBaseMapOver;
            return mapData;
        }

        /// <summary>
        /// データを正規化する
        /// </summary>
        public void normarized()
        {
            mZoom = Math.Min(Math.Max(mZoom, 0), mMaxZoom);
            double maxColCount = getMaxColCount();
            mStart.X = Math.Min(Math.Max(mStart.X, 0), maxColCount);
            mStart.Y = Math.Min(Math.Max(mStart.Y, 0), maxColCount);
            mColCount = Math.Min(Math.Max(mColCount, 1), mMaxColCount);
            mColCount = (int)Math.Min(mColCount, maxColCount);
            mCellSize = getTileSize();
            mRowCount = (int)getRowCount();
        }

        /// <summary>
        /// 日時データを使う地図かの確認
        /// </summary>
        /// <returns></returns>
        public bool isDateTimeData()
        {
            for (int i = 0; i< mDateTimeForm.Length; i++) {
                if (0 <= mMapUrl.IndexOf(mDateTimeForm[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 標高データファイルの取得
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <param name="autoOnline">データ取得モード(true:update/null:auto/false:offline)</param>
        public void getElevatorDataFile(int x, int y, bool? autoOnline)
        {
            //  標高データの取得
            string elevatorUrl = getElevatorWebAddress(x, y);
            string downloadPath = downloadElevatorPath(x, y);
            getDownLoadFile(elevatorUrl, downloadPath, autoOnline);
        }

        /// <summary>
        /// 国土地理院地図の標高データのWebアドレスの取得
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <returns>Webアドレス</returns>
        public string getElevatorWebAddress(double x, double y)
        {
            int elevatorZoom = getMaxZoom(MapInfoData.mMapElevatorData[mElevatorDataNo][4]);  //  標高データの最大ズーム値
            Point pos = new Point(x, y);
            if (elevatorZoom < mZoom) {
                //  標高データはズームレベル15(DEM5)までなのでそれ以上は15のデータを取得
                pos = cnvMapPostionZoom(elevatorZoom, new Point(x, y));
            } else {
                elevatorZoom = mZoom;
            }
            return MapInfoData.mGsiUrl + MapInfoData.mMapElevatorData[mElevatorDataNo][1] + "/" + elevatorZoom +
                "/" + (int)pos.X + "/" + (int)pos.Y + "." + MapInfoData.mMapElevatorData[mElevatorDataNo][2];
        }

        /// <summary>
        /// 国土地理院地図の標高データのダウンロード先パスの取得
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <returns>ファイルパス</returns>
        public string downloadElevatorPath(double x, double y)
        {
            int elevatorZoom = getMaxZoom(MapInfoData.mMapElevatorData[mElevatorDataNo][4]);  //  標高データの最大ズーム値
            Point pos = new Point(x, y);
            if (elevatorZoom < mZoom) {
                //  標高データはズームレベル15(DEM5)までなのでそれ以上は15のデータを取得
                pos = cnvMapPostionZoom(elevatorZoom, new Point(x, y));
            } else {
                elevatorZoom = mZoom;
            }
            return mBaseFolder + "\\" + MapInfoData.mMapElevatorData[mElevatorDataNo][1] + "\\" + elevatorZoom +
                "\\" + (int)pos.X + "\\" + (int)pos.Y + "." + MapInfoData.mMapElevatorData[mElevatorDataNo][2];
        }

        /// <summary>
        /// 標高データのIDからデータNoを取得
        /// </summary>
        /// <param name="id">データID</param>
        /// <returns>データo</returns>
        public int getElevatorDataNo(string id)
        {
            for (int i = 0; i < MapInfoData.mMapElevatorData.Count; i++) {
                if (MapInfoData.mMapElevatorData[i][1].CompareTo(id) == 0)
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// 標高データの座標を取得
        /// ズームレベルが指定以上の場合は最大ズームレベルの座標(Map座標)に変換
        /// </summary>
        /// <param name="mp"></param>
        /// <returns></returns>
        public Point cnvElevatorPos(Point mp)
        {
            int elevatorZoomMax = getMaxZoom(MapInfoData.mMapElevatorData[mElevatorDataNo][4]);
            if (elevatorZoomMax < mZoom) {
                //  標高データはズームレベル15(DEM5)までなのでそれ以上は15のデータを取得
                return cnvMapPostionZoom(elevatorZoomMax, mp);
            } else {
                return mp;
            }
        }

        /// <summary>
        /// MapInfoのZoom情報から最大のZoomレベルを求める
        /// </summary>
        /// <param name="zoom">zoom情報</param>
        /// <returns>最大Zoomレベル</returns>
        private int getMaxZoom(string zoom)
        {
            if (zoom.Length < 1)
                return 14;
            List<string> zoomlist = ylib.string2StringNumbers(zoom);
            int maxzoom = 0;
            foreach (var str in zoomlist) {
                maxzoom = Math.Max(maxzoom, Math.Abs(int.Parse(str)));
            }
            return maxzoom;
        }


        /// <summary>
        /// 標高データの取得
        /// ダウンロードしたテキストファイル(256x256)から標高データを取得
        /// </summary>
        /// <param name="mp">座標(Map値)</param>
        /// <param name="autoOnline">データ取得モード(true:update/null:auto/false:offline)</param>
        /// <returns>標高(m)</returns>
        public double getMapElavtor(Point mp, bool? autoOnline)
        {
            string elevatorUrl = getElevatorWebAddress(mp.X, mp.Y);
            string downloadPath = downloadElevatorPath(mp.X, mp.Y);
            bool? result = getDownLoadFile(elevatorUrl, downloadPath, autoOnline);
            if (result == null) {
                return 0.0;
            } else {
                mp = cnvElevatorPos(mp);
                return getMapElevatorFile(downloadPath, (int)(256.0 * (mp.X % 1.0)), (int)(256.0 * (mp.Y % 1.0)));
            }
        }

        /// <summary>
        /// 標高データファイルから標高値を取得
        /// データファイルで標高がない部分は'e'が記載されている
        /// </summary>
        /// <param name="path">データファイルパス</param>
        /// <param name="x">データファイル配列の列数</param>
        /// <param name="y">データファイル配列の行数</param>
        /// <returns>標高値(m)</returns>
        private double getMapElevatorFile(string path, int x, int y)
        {
            List<string[]> eleList;
            if (mElevatorDataList.ContainsKey(path)) {
                eleList = mElevatorDataList[path];
            } else {
                eleList = ylib.loadCsvData(path);
                mElevatorDataList.Add(path, eleList);
            }
            if (0 < eleList.Count)
                return ylib.string2double(eleList[y][x]);
            else
                return 0;
        }

        //  Webからのダウンロード結果(true:Download OK, false:データ既存, null:失敗)
        private bool? mMapDataDwonloadResult = false;

        /// <summary>
        /// 地図データを取得する
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <param name="autoOnline">データ取得モード(true:update/null:auto/false:offline)</param>
        /// <returns>ファイルパス(ファイルがない時はnull)</returns>
        public string getMapData(int x, int y, bool? autoOnline)
        {
            string downloadFilePath;
            if (isMergeData()) {
                //  重ね合わせデータの表示する場合
                downloadFilePath = getMergeMapData(x, y, autoOnline);
                if (downloadFilePath != null)
                    mMapDataDwonloadResult = true;
                else
                    mMapDataDwonloadResult = null;
            } else {
                //  単独のMapDataの表示する場合
                downloadFilePath = getMapDataDownload(x, y, autoOnline);
            }
            if (mMapDataDwonloadResult == null)
                return null;
            else
                return downloadFilePath;
        }

        /// <summary>
        /// 重ね合わせた地図データの取得
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <param name="autoOnline">データ取得モード(true:update / null:auto / false:offline)</param>
        /// <returns>重ね合わせたデータファイルパス</returns>
        public string getMergeMapData(int x, int y, bool? autoOnline)
        {
            string mergeDataPath = downloadMergeDataPath(x, y);
            if (File.Exists(mergeDataPath) && autoOnline != true)
                return mergeDataPath;                                                  //  既にファイルが存在する

            mBaseMap.mZoom = mZoom;                                                    //  BaseMapのZoomを設定
            string baseMapDataPath = mBaseMap.getMapDataDownload(x, y, autoOnline);    //  BaseMapのデータ取得
            string lapMapDataPath = getMapDataDownload(x, y, autoOnline);              //  重ねるデータの取得
            if (!ylib.createPathFolder(mergeDataPath))
                return null;
            if (mBaseMapOver) {
                //  BaseMapを上にする時
                mergeDataPath = ydraw.imageOverlap(lapMapDataPath, baseMapDataPath, mergeDataPath, mTransportColors);   //  重ね合わせた処理
            } else {
                //  BaseMapを下にする時
                mergeDataPath = ydraw.imageOverlap(baseMapDataPath, lapMapDataPath, mergeDataPath, mTransportColors);   //  重ね合わせた処理
            }
            return mergeDataPath;
        }

        /// <summary>
        /// 重ね合わせの地図データの確認
        /// BaseMapIDがあれば重ね合わせデータとする
        /// </summary>
        /// <returns></returns>
        private bool isMergeData()
        {
            return 0 < mBaseDataIDName.Length;
        }

        /// <summary>
        /// 地図データをダウンロードする
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <param name="autoOnline">データ取得モード(true:update/null:auto/false:offline)</param>
        /// <returns>ダウンロードファイルパス</returns>
        public string getMapDataDownload(int x, int y, bool? autoOnline)
        {
            string url = getWebAddress(x, y);
            string downloadFilePath = downloadPath(x, y);
            mMapDataDwonloadResult = getDownLoadFile(url, downloadFilePath, autoOnline);
            return downloadFilePath;
        }

        /// <summary>
        /// 地図データの取得結果を返すす
        /// 結果: ダウンロードOK : true  ダウンロードなし(ファイルが既に存在する) : false ダウンロード失敗 : null
        /// </summary>
        /// <returns>(false:なし true:成功 null: 失敗)</returns>
        public bool? getMapDataResult()
        {
            return mMapDataDwonloadResult;
        }

        /// <summary>
        /// 地図データの削除
        /// <param name="msg">確認メッセージを出す</param>
        /// </summary>
        public void removeMapData(bool msg = true)
        {
            string path = Path.GetFullPath(getDownloadDataBaseFolder());
            if (!msg || MessageBox.Show(path + " を削除します", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                try {
                    if (Directory.Exists(path)) {
                        Directory.Delete(path, true);
                        removeImageFileList();
                    }
                    if (isMergeData()) {
                        //  重ね合わせデータ
                        path = Path.GetFullPath(getDownloadDataBaseFolder(false));
                        if (Directory.Exists(path)) {
                            Directory.Delete(path, true);
                            removeImageFileList();
                        }
                    }
                } catch (Exception e) {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// 日時管理地図データの削除(残すファイルを指定)
        /// </summary>
        /// <param name="withoutName">削除しない部分ファイル名</param>
        public void removeMapDataWithoutFolder(string withoutName)
        {
            string path = Path.GetFullPath(getDownloadDataBaseFolder(false));
            string[] dirList = Directory.GetDirectories(path);
            foreach (string dir in dirList) {
                string folder = Path.GetFileName(dir);
                if (folder.IndexOf(withoutName) < 0) {
                    if (Directory.Exists(dir)) {
                        Directory.Delete(dir, true);

                    }
                }
            }
        }

        /// <summary>
        /// ImageFileListから自分のURLを削除する
        /// </summary>
        public void removeImageFileList()
        {
            string url;
            if (0 < mMapUrl.Length) {
                url = mMapUrl.Substring(0, ylib.lastIndexCountOf(mMapUrl, "/", 3));
            } else {
                url = MapInfoData.mGsiUrl + mDataIdName;
            }

            var removeList = new List<string>();
            foreach (var item in mImageFileSet) {
                if (0 <= item.IndexOf(url))
                    removeList.Add(item);
            }
            foreach (var item in removeList)
                mImageFileSet.Remove(item);
        }

        /// <summary>
        /// ダウンロード先のWebアドレスの取得
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <returns>Webアドレス</returns>
        public string getWebAddress(int x, int y)
        {
            if (0 < mMapUrl.Length) {
                //  国土地理院地図以外
                string url = mMapUrl2.Replace("{z}", mZoom.ToString());
                url = url.Replace("{x}", x.ToString());
                url = url.Replace("{y}", y.ToString());
                return url;
            } else {
                //  国土地理院地図
                return MapInfoData.mGsiUrl + mDataIdName + "/" + mZoom + "/" + x + "/" + y + "." + mExt;
            }
        }

        /// <summary>
        /// ダウンロードした先のパス
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <returns>ファイルパス</returns>
        public string downloadPath(int x, int y)
        {
            return mBaseFolder + "\\" + mDataIdName + mDateTimeFolder + "\\" + mZoom + "\\" + x + "\\" + y + "." + mExt;
        }

        /// <summary>
        /// マージしたデータファイル名のパス(重ね合わせた地図データ)
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <returns>ファイルパス</returns>
        public string downloadMergeDataPath(int x, int y)
        {
            return mBaseFolder + "\\" + mDataIdName + "_" + mBaseDataIDName + (mBaseMapOver ? "_up" : "")
                    + "\\" + mZoom + "\\" + x + "\\" + y + "." + mExt;
        }

        /// <summary>
        /// 地図データを保存するフォルダ名の取得
        /// </summary>
        /// <param name="merge">true: 重ね合わせ先パス名</param>
        /// <returns>ファイルパス</returns>
        public string getDownloadDataBaseFolder(bool merge = true)
        {
            if (merge && isMergeData()) {
                return mBaseFolder + "\\" + mDataIdName + "_" + mBaseDataIDName + (mBaseMapOver ? "_up" : "");
            } else {
                return mBaseFolder + "\\" + mDataIdName;
            }
        }

        /// <summary>
        /// タイルデータをWebからダウンロードする
        /// </summary>
        /// <param name="url">Webアドレス</param>
        /// <param name="downloadPath">ダウンロードパス</param>
        /// <param name="autoOnline">データ取得モード(true:update/null:auto/false:offline)</param>
        /// <returns>ダウンロード結果(false:なし true:成功 null: 失敗)</returns>
        public bool? getDownLoadFile(string url, string downloadPath, bool? autoOnline)
        {
            bool? result = false;
            string folder = Path.GetDirectoryName(downloadPath);    //  フォルダ名

            //  オンラインの時はファイル登録を一度削除する
            bool imageFIleset = mImageFileSet.Contains(url);
            if (autoOnline == true && imageFIleset)
                mImageFileSet.Remove(url);

            //  ファイルがなければダウンロードする
            if (!File.Exists(downloadPath) || autoOnline == true) {
                //  Web上にないファイルはダウンロードに行かない
                if (autoOnline == true || !imageFIleset) {
                    Directory.CreateDirectory(folder);
                    //System.Diagnostics.Debug.WriteLine($"getDownLoadFile: {url} {downloadPath}");
                    if (!ylib.webFileDownload(url, downloadPath)) {
                        if (ylib.getError()) {
                            System.Diagnostics.Debug.WriteLine($"Error getDownLoadFile: FileSet[{imageFIleset}] Online[{autoOnline}] {url} {ylib.getErrorMessage()}");
                        }
                        //  Web上にないファイルを登録
                        mImageFileSet.Add(url);
                        result = null;
                    } else {
                        result = true;
                    }
                } else {
                    result = null;
                }
            }
            return result;
        }

        /// <summary>
        ///  Web上にないイメージファイルのリストの保存
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void saveImageFileSet(string path)
        {
            if (mImageFileSet.Count == 0)
                return;
            List<string[]> dataList = new List<string[]>();
            foreach (string value in mImageFileSet) {
                string[] data = new string[1];
                data[0] = value;
                dataList.Add(data);
            }
            ylib.saveCsvData(path, dataList);
        }

        /// <summary>
        /// Web上にないイメージファイルのリストの取り込み
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void loadImageFileSet(string path)
        {
            if (File.Exists(path)) {
                List<string[]> dataList = new List<string[]>();
                dataList = ylib.loadCsvData(path);
                foreach (string[] data in dataList) {
                    if (0 < data.Length) {
                        if (!mImageFileSet.Contains(data[0])) {
                            mImageFileSet.Add(data[0]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 画面の移動(タイル座標での移動量)
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void setMove(double dx, double dy)
        {
            mStart.X += dx;
            mStart.Y += dy;
            normarized();
        }

        /// <summary>
        /// 指定座標がセンターになるように起点を設定する
        /// </summary>
        /// <param name="ctr"></param>
        public void setMoveCenter(Point ctr)
        {
            Point mapCtr = baseMap2Map(ctr);
            mStart.X = mapCtr.X - mColCount / 2.0;
            mStart.Y = mapCtr.Y - getRowCountF() / 2.0;
        }

        /// <summary>
        /// ズームレベルを変更した時の左上タイル座標を設定
        /// </summary>
        /// <param name="nextZoom">変更後のズームレベル</param>
        public void setZoom(int nextZoom)
        {
            Point ctr = new Point(mStart.X + (double)mColCount / 2.0, mStart.Y + getRowCountF() / 2.0);
            setZoom(nextZoom, ctr);
        }

        /// <summary>
        /// ズームレベルの変更 (指定位置を中心に拡大縮小する)
        /// </summary>
        /// <param name="nextZoom">変更後のズームレベル</param>
        /// <param name="ctr">ズームの中心位置(MAP座標)</param>
        public void setZoom(int nextZoom, Point pos)
        {
            if (mMaxZoom < nextZoom)
                return;

            double zoom = Math.Pow(2, nextZoom - mZoom);
            Point ctr = getMapCenter();
            Point v = pos.vector(ctr);
            v.scale(1.0 / zoom - 1.0);
            ctr.Offset(v.X, v.Y);

            mStart.X = ctr.X * zoom - mColCount / 2.0;
            mStart.Y = ctr.Y * zoom - getRowCountF() / 2.0;
            mZoom = nextZoom;
        }

        /// <summary>
        /// ズームレベルを変更したときのMap座標を求める
        /// </summary>
        /// <param name="nextZoom">変更するズームレベル</param>
        /// <param name="mp">Map座標</param>
        /// <returns>変換後のMap座標</returns>
        public Point cnvMapPostionZoom(int nextZoom, Point mp)
        {
            Point cmp = new Point();
            cmp.X = mp.X * Math.Pow(2, nextZoom - mZoom);
            cmp.Y = mp.Y * Math.Pow(2, nextZoom - mZoom);
            return cmp;
        }

        /// <summary>
        /// 開始位置の小数点以下の値を求める
        /// </summary>
        /// <returns></returns>
        public Point getOffset()
        {
            return new Point(mStart.X - Math.Floor(mStart.X), mStart.Y - Math.Floor(mStart.Y));
        }

        /// <summary>
        /// タイル画像のサイズを求める(一辺のながさ)
        /// </summary>
        /// <returns></returns>
        public double getTileSize()
        {
            if (mColCount == 1) {
                if (mView.Width < mView.Height)
                    return mView.Width;
                else
                    return mView.Height;
            } else {
                return mView.Width / mColCount;
            }
        }

        /// <summary>
        /// 画面縦方向のタイル数を求める
        /// </summary>
        /// <returns>タイル数(切上げ)</returns>
        private double getRowCount()
        {
            return Math.Ceiling(mColCount * mView.Height / mView.Width);
        }

        /// <summary>
        /// 画面縦方向のタイル数を求める
        /// </summary>
        /// <returns>タイル数(端数含む)</returns>
        public double getRowCountF()
        {
            return mColCount * mView.Height / mView.Width;
        }

        /// <summary>
        /// ズームレベルでの最大列数(一周分)
        /// </summary>
        /// <returns></returns>
        private int getMaxColCount()
        {
            return (int)Math.Pow(2, mZoom);
        }

        /// <summary>
        /// 表示領域をBaseMap座標で求める
        /// </summary>
        /// <returns></returns>
        public Rect getArea()
        {
            Point bsp = map2BaseMap(mStart);
            Point bep = map2BaseMap(new Point(mStart.X + mColCount, mStart.Y + (mColCount * mView.Height / mView.Width)));
            return new Rect(bsp, bep);
        }

        /// <summary>
        /// 中心座標(BasMap)の取得
        /// </summary>
        /// <returns></returns>
        public Point getCenter()
        {
            return map2BaseMap(new Point(mStart.X + (double)mColCount / 2.0, mStart.Y + getRowCountF() / 2.0));
        }

        /// <summary>
        /// 中心座標(Map)の取得
        /// </summary>
        /// <returns></returns>
        public Point getMapCenter()
        {
            return new Point(mStart.X + (double)mColCount / 2.0, mStart.Y + getRowCountF() / 2.0);
        }

        /// <summary>
        /// スクリーン座標から緯度経度座標に変換
        /// </summary>
        /// <param name="sp">スクリーン座標</param>
        /// <returns>緯度経度座標</returns>
        public Point screen2Coordinates(Point sp)
        {
            Point bp = screen2BaseMap(sp);
            return baseMap2Coordinates(bp);
        }

        /// <summary>
        /// 緯度経度座標からスクリーン座標に変換
        /// </summary>
        /// <param name="cp">緯度経度座標</param>
        /// <returns>スクリーン座標</returns>
        public Point coordinates2Screen(Point cp)
        {
            Point bp = coordinates2BaseMap(cp);
            return baseMap2Screen(bp);
        }

        /// <summary>
        /// MAP座標から緯度経度座標に変換
        /// </summary>
        /// <param name="mp">MAP座標</param>
        /// <returns>緯度経度座標</returns>
        public Point map2Coordinates(Point mp)
        {
            Point bp = map2BaseMap(mp);
            Point cp = baseMap2Coordinates(bp);
            return cp;
        }

        /// <summary>
        /// 緯度経度座標からMAP座標に変換
        /// </summary>
        /// <param name="cp2">緯度経度座標</param>
        /// <returns>MAP座標</returns>
        public Point coordinates2Map(Point cp)
        {
            Point bp = coordinates2BaseMap(cp);
            return baseMap2Map(bp);
        }

        /// <summary>
        /// スクリーン座標をBaseMap座標に変換
        /// </summary>
        /// <param name="sp">スクリーン座標</param>
        /// <returns>BaseMap座標</returns>
        public Point screen2BaseMap(Point sp)
        {
            Point mp = screen2Map(sp);
            return map2BaseMap(mp);
        }

        /// <summary>
        /// BaseMap座標をScreen座標に変換
        /// </summary>
        /// <param name="bp">BaseMap座標</param>
        /// <returns>スクリーン座標</returns>
        public Point baseMap2Screen(Point bp)
        {
            Point mp = baseMap2Map(bp);
            return map2Screen(mp);
        }

        /// <summary>
        /// BaseMap座標をScreen座標に変換
        /// </summary>
        /// <param name="brect">BaseMap座標</param>
        /// <returns>スクリーン座標</returns>
        public Rect baseMap2Screen(Rect brect)
        {
            return new Rect(baseMap2Screen(brect.TopLeft), baseMap2Screen(brect.BottomRight));
        }

        /// <summary>
        /// スクリーン座標からMAP座標に変換する
        /// </summary>
        /// <param name="sp">スクリーン座標</param>
        /// <returns>MAP座標</returns>
        public Point screen2Map(Point sp)
        {
            Point mp = new Point();
            mp.X = mStart.X + sp.X / mCellSize;
            mp.Y = mStart.Y + sp.Y / mCellSize;
            return mp;
        }

        /// <summary>
        /// MAP座標からスクリーン座標に変換
        /// </summary>
        /// <param name="mp">MAP座標</param>
        /// <returns>スクリーン座標</returns>
        public Point map2Screen(Point mp)
        {
            Point sp = new Point();
            sp.X = (mp.X - mStart.X) * mCellSize;
            sp.Y = (mp.Y - mStart.Y) * mCellSize;
            return sp;
        }

        /// <summary>
        /// BaseMap座標(ZoomLevel0のでのMAP座標)に変換する
        /// </summary>
        /// <param name="loc">現在のMAP座標</param>
        /// <returns>BaseMap座標(ZoomLevel0のMAP座標)</returns>
        public Point map2BaseMap(Point mp)
        {
            mp.X /= Math.Pow(2, mZoom);
            mp.Y /= Math.Pow(2, mZoom);
            return mp;
        }

        /// <summary>
        /// BaseMap座標(ZoomLevel0のでのMAP座標)から現在のMAP座標に戻す
        /// </summary>
        /// <param name="bp">BaseMapLocation</param>
        /// <returns>MapLocation</returns>
        public Point baseMap2Map(Point bp)
        {
            Point pos = new Point();
            pos.X = bp.X * Math.Pow(2, mZoom);
            pos.Y = bp.Y * Math.Pow(2, mZoom);
            return pos;
        }

        /// <summary>
        /// 緯度経度座標(度)からメルカトル図法でBaseMAP座標に変換
        /// 参考: https://qiita.com/Seo-4d696b75/items/aa6adfbfba404fcd65aa
        /// cp.X : 経度、 cp.Y : 緯度
        /// </summary>
        /// <param name="cp">緯度経度座標</param>
        /// <returns>BaseMap座標</returns>
        public static Point coordinates2BaseMap(Point cp)
        {
            //  座標変換
            Point bp = new Point();
            bp.X = cp.X / 360.0 + 0.5;
            bp.Y = 0.5 - 0.5 / Math.PI * Math.Log(Math.Tan(Math.PI *(1 / 4.0 + cp.Y / 360.0)));
            return bp;
        }

        /// <summary>
        /// BaseMAP座標からメルカトル図法での緯度経度座標(度)に変換
        /// 参考: https://qiita.com/Seo-4d696b75/items/aa6adfbfba404fcd65aa
        /// bp.X : 経度方向の距離、 bp.Y : 緯度方向の距離
        /// cp.X : 経度 cp.Y : 緯度
        /// </summary>
        /// <param name="bp">BaseMap座標</param>
        /// <returns>緯度経度座標</returns>
        public static Point baseMap2Coordinates(Point bp)
        {
            Point cp = new Point();
            cp.X = Math.PI *(2.0 * bp.X - 1);
            cp.Y = 2.0 * Math.Atan(Math.Exp((0.5 - bp.Y) * 2.0 * Math.PI)) - Math.PI / 2.0;
            //  rad → deg
            cp.X *= 180.0 / Math.PI;
            cp.Y *= 180.0 / Math.PI;
            return cp;
        }

        /// <summary>
        /// Map座標間の距離を求める
        /// </summary>
        /// <param name="mps">始点座標(Map)</param>
        /// <param name="mpe">終点座標(Map)</param>
        /// <returns>距離(km)</returns>
        public double map2Distance(Point mps, Point mpe)
        {
            Point cps = map2Coordinates(mps);
            Point cpe = map2Coordinates(mpe);
            return ylib.coordinateDistance(cps, cpe);
        }

        /// <summary>
        /// 色凡例のコメントを返す
        /// </summary>
        /// <param name="color">色コード</param>
        /// <returns></returns>
        public string getColorLegend(System.Drawing.Color color)
        {
            if (8 <= color.Name.Length && mColorLegend.ContainsKey(color.Name.Substring(2, 6)))
                return mColorLegend[color.Name.Substring(2, 6)];
            else
                return "";
        }

        /// <summary>
        /// 凡例データ読込
        /// ファイル名は "legend_" + データID + ".csv"
        /// </summary>
        private void loadLegendData()
        {
            mColorLegend = null;
            string path = "legend_" + mDataIdName + ".csv";
            if (!File.Exists(path))
                return;

            List<string[]> legendList = ylib.loadCsvData(path, mLegendTitle, false, false);
            if (legendList != null && 0 < legendList.Count) {
                mColorLegend = new Dictionary<string, string>();
                foreach (string[] legend in legendList) {
                    if (1 < legend.Length && legend[0][0] != '#') {
                        mColorLegend.Add(legend[0].ToLower(), legend[1]);
                    }
                }
            }
        }
    }
}
