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
        public string mBaseFolder = "Map";              //  保存先フォルダ

        public string mMapUrl = MapInfoData.mGsiUrl;    //  地図データURL(ディフォルトは地理院地図)
        public string mDataIdName = "std";              //  データ種別名(std...)
        public string mExt = "png";                     //  タイル画像の拡張子
        public string mTileOrder = "";                  //  タイルデータの座標順(していない時は{z}/{x}/{y})
        public string mWebUrl = MapInfoData.mHelpUrl;   //  地図データ提供先URL

        public int mDataId = 0;                         //  地図データの種別
        public int mZoom = 0;                           //  ズームレベル
        public Point mStart = new Point(0, 0);          //  表示開始位置(MAP座標(タイル画像単位))
        public int mColCount = 4;                       //  表示するタイル画像の列数
        public int mRowCount = 4;                       //  表示するタイル画像の行数
        public double mCellSize = 256;                  //  タイル画像の大きさ(一辺の長さ)
        public Size mView = new Size(1000, 1000);       //  表示するViewの大きさ
        public int mUseCol = 0;                         //  使用した列数
        public int mUseRow = 0;                         //  使用した行数
        public const int mMaxZoom = 18;
        public int mElevatorDataNo = 0;                 //  使用標高データのNo

        private YLib ylib = new YLib();

        public MapData()
        {

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
            mColCount = Math.Min(Math.Max(mColCount, 1), 20);
            mColCount = (int)Math.Min(mColCount, maxColCount);
            mCellSize = getTileSize();
            mRowCount = (int)getRowCount();
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
                string url = mMapUrl.Replace("{z}", mZoom.ToString());
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
            return mBaseFolder + "\\" + mDataIdName + "\\" + mZoom + "\\" + x + "\\" + y + "." + mExt;
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
        /// 国土地理院地図の標高データのWebアドレスの取得
        /// </summary>
        /// <param name="x">X座標(Map座標)</param>
        /// <param name="y">Y座標(Map座標)</param>
        /// <returns>Webアドレス</returns>
        public string getElevatorWebAddress(double x, double y)
        {
            int elevatorZoom = ylib.intParse(MapInfoData.mMapElevatorData[mElevatorDataNo][4]);  //  標高データの最大ズーム値
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
            int elevatorZoom = ylib.intParse(MapInfoData.mMapElevatorData[mElevatorDataNo][4]);  //  標高データの最大ズーム値
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
        /// 標高データの座標を取得
        /// ズームレベルが指定以上の場合は最大ズームレベルの座標(Map座標)に変換
        /// </summary>
        /// <param name="mp"></param>
        /// <returns></returns>
        public Point cnvElevatorPos(Point mp)
        {
            int elevatorZoomMax = ylib.intParse(MapInfoData.mMapElevatorData[mElevatorDataNo][4]);
            if (elevatorZoomMax < mZoom) {
                //  標高データはズームレベル15(DEM5)までなのでそれ以上は15のデータを取得
                return cnvMapPostionZoom(elevatorZoomMax, mp);
            } else {
                return mp;
            }
        }


        /// <summary>
        /// タイルデータをWebからダウンロードする
        /// </summary>
        /// <param name="url">Webアドレス</param>
        /// <param name="downloadPath">ダウンロードパス</param>
        /// <param name="autoOffline">データ取得モード(true:update/null:auto/false:offline)</param>
        /// <returns>ダウンロード結果(false:なし true:成功 null: 失敗)</returns>
        public bool? getDownLoadFile(string url, string downloadPath, bool? autoOffline)
        {
            bool? result = false;
            string folder = Path.GetDirectoryName(downloadPath);    //  フォルダ名

            //  ファイルがなければダウンロードする
            if (!File.Exists(downloadPath) || autoOffline == true) {
                //  Web上にないファイルはダウンロードに行かない
                if (autoOffline != false || !mImageFileSet.Contains(url)) {
                    Directory.CreateDirectory(folder);
                    System.Diagnostics.Debug.WriteLine($"getDownLoadFile: {url} {downloadPath}");
                    if (!ylib.webFileDownload(url, downloadPath)) {
                        if (ylib.getError()) {
                            System.Diagnostics.Debug.WriteLine($"getDownLoadFile: {url} {ylib.getErrorMessage()}");
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
        /// 標高データの取得
        /// ダウンロードしたテキストファイル(256x256)から標高データを取得
        /// </summary>
        /// <param name="mp">座標(Map値)</param>
        /// <param name="autoOffline">データ取得モード(true:update/null:auto/false:offline)</param>
        /// <returns>標高(m)</returns>
        public double getMapElavtor(Point mp, bool? autoOffline)
        {
            string elevatorUrl = getElevatorWebAddress(mp.X, mp.Y);
            string downloadPath = downloadElevatorPath(mp.X, mp.Y);
            bool ? result = getDownLoadFile(elevatorUrl, downloadPath, autoOffline);
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
        public void setZoom(int nextZoom, Point ctr)
        {
            if (mMaxZoom < nextZoom)
                return;
            mStart.X = ctr.X * Math.Pow(2, nextZoom - mZoom) - mColCount / 2.0;
            mStart.Y = ctr.Y * Math.Pow(2, nextZoom - mZoom) - getRowCountF() / 2.0;
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
    }
}
