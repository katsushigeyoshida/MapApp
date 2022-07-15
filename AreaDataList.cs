using System.Collections.Generic;
using System.IO;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// 地図画面情報をリストで管理するクラス
    /// </summary>
    class AreaDataList
    {
        private Dictionary<string, MapData> mMapAreaList = new Dictionary<string, MapData>();    //  地図画面保存リスト
        private string mFilePath;
        public static string[] mPositionListFormat = new string[] { //  文字配列に変換するときのタイトル
            "title", "DataID", "DataIDName", "ZoomLevel", "StartX", "StartY", "ColCount"
        };


        private YLib ylib = new YLib();

        public AreaDataList()
        {

        }

        /// <summary>
        /// ファイル保存先のパス設定
        /// </summary>
        /// <param name="path"></param>
        public void setSvaePath(string path)
        {
            mFilePath = path;
        }


        /// <summary>
        /// リストに地図画面情報を追加
        /// </summary>
        /// <param name="title"></param>
        /// <param name="mapData"></param>
        public void add(string title, MapData mapData)
        {
            if (!mMapAreaList.ContainsKey(title)) {
                mMapAreaList.Add(title, mapData);
            } else {
                mMapAreaList[title] = mapData;
            }
        }

        /// <summary>
        /// リストから地図画面情報を削除
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public bool remove(string title)
        {
            if (mMapAreaList.ContainsKey(title)) {
                return mMapAreaList.Remove(title);
            }
            return false;
        }

        /// <summary>
        /// タイトルが地図画面情報(MapData)を取得
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <returns></returns>
        public MapData getData(string title)
        {
            return mMapAreaList[title];
        }

        /// <summary>
        /// 画面情報のタイトルリストを取得
        /// </summary>
        /// <returns></returns>
        public List<string> getTitleList()
        {
            List<string> titleList = new List<string>();
            foreach (string title in mMapAreaList.Keys) {
                titleList.Add(title);
            }
            return titleList;
        }


        /// <summary>
        /// 保存用データの文字配列化
        /// </summary>
        /// <param name="key">データのタイトル</param>
        /// <returns>文字配列データ</returns>
        public string[] getStrinData(string key, MapData mapData)
        {
            string[] data = new string[mPositionListFormat.Length];
            data[0] = key;
            data[1] = mapData.mDataId.ToString();
            data[2] = mapData.mDataIdName;
            data[3] = mapData.mZoom.ToString();
            data[4] = mapData.mStart.X.ToString();
            data[5] = mapData.mStart.Y.ToString();
            data[6] = mapData.mColCount.ToString();
            
            return data;
        }

        /// <summary>
        /// 文字配列データから設定
        /// </summary>
        /// <param name="data">文字配列データ</param>
        public MapData setStringData(string[] data)
        {
            MapData mapData = new MapData();
            mapData.mDataId = int.Parse(data[1]);
            mapData.mDataIdName = data[2];
            mapData.mZoom = int.Parse(data[3]);
            mapData.mStart.X = double.Parse(data[4]);
            mapData.mStart.Y = double.Parse(data[5]);
            mapData.mColCount = int.Parse(data[6]);

            return mapData;
        }

        /// <summary>
        /// 画面情報をファイルに保存
        /// </summary>
        public void saveData()
        {
            saveAreaData(mFilePath);
        }

        /// <summary>
        /// 画面情報をファイルから取得
        /// </summary>
        public void loadData()
        {
            loadAreaData(mFilePath);
        }

        /// <summary>
        /// 位置情報リストをファイルに保存
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void saveAreaData(string path)
        {
            if (mMapAreaList.Count == 0)
                return;
            List<string[]> dataList = new List<string[]>();
            foreach (KeyValuePair<string, MapData> keyValue in mMapAreaList) {
                string[] data = getStrinData(keyValue.Key, keyValue.Value);
                dataList.Add(data);
            }
            ylib.saveCsvData(path, mPositionListFormat, dataList);
        }

        /// <summary>
        /// 位置情報リストをファイルから取り込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void loadAreaData(string path)
        {
            if (File.Exists(path)) {
                List<string[]> dataList;
                dataList = ylib.loadCsvData(path, mPositionListFormat);
                foreach (string[] data in dataList) {
                    if (mPositionListFormat.Length <= data.Length) {
                        string key = data[0];
                        MapData mapData;
                        if (0 < data[6].Length) {
                            //  列数が設定されている既存データ
                            mapData = setStringData(data);
                        } else {
                            //  旧データ
                            mapData = new MapData();
                            mapData.mDataId = int.Parse(data[1]);
                            if (MapInfoData.mMapData.Count <= mapData.mDataId)
                                mapData.mDataId = 0;
                            mapData.mDataIdName = MapInfoData.mMapData[mapData.mDataId][1];
                            mapData.mExt = MapInfoData.mMapData[mapData.mDataId][2];
                            mapData.mZoom = int.Parse(data[2]);
                            mapData.mStart.X = double.Parse(data[3]);
                            mapData.mStart.Y = double.Parse(data[4]);
                            mapData.mColCount = int.Parse(data[5]);
                            mapData.normarized();
                        }
                        if (!mMapAreaList.ContainsKey(key)) {
                            mMapAreaList.Add(key, mapData);
                        } else {
                            mMapAreaList[key] = mapData;
                        }
                    }
                }
            }
        }

    }
}
