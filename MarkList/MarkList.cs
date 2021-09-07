using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// マークリスト管理クラス
    /// </summary>
    public class MarkList
    {
        private List<MapMark> mMapMarkList = new List<MapMark>();   //  マークデータリスト
        private double mTolerance = 200;                            //  マーク記号ピックアップ距離(スクリーン座標の2乗)
        private string mSaveFilePath = "";
        public string mFilterGroup = "";
        public enum SORTTYPE { Non, Normal, Reverse, Distance };
        public string[] mSortName = new string[] {"ソートなし", "昇順", "降順", "距離順" };
        public SORTTYPE mListSort = SORTTYPE.Non;
        public Point mCenter = new Point();
        public double mSizeRate = 1.0;

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MarkList()
        {

        }

        /// <summary>
        /// データ保存ファイルパスの設定
        /// </summary>
        /// <param name="path"></param>
        public void setSaveFilePath(string path)
        {
            mSaveFilePath = path;
        }

        /// <summary>
        /// マークの追加
        /// </summary>
        /// <param name="mapMark"></param>
        public void add(MapMark mapMark)
        {
            mMapMarkList.Add(mapMark);
        }

        /// <summary>
        /// マークの削除
        /// </summary>
        /// <param name="mapMark"></param>
        public void remove(MapMark mapMark)
        {
            mMapMarkList.Remove(mapMark);
        }

        /// <summary>
        /// タイトルとグループからマークを検索する
        /// </summary>
        /// <param name="title">検索タイトル</param>
        /// <param name="group">検索グループ</param>
        /// <returns></returns>
        public MapMark getMapMark(string title , string group)
        {
            foreach ( MapMark mapMark in mMapMarkList) {
                if (mapMark.mTitle.CompareTo(title) == 0 && 
                    (group.Length == 0 || mapMark.mGroup.CompareTo(group) == 0))
                    return mapMark;
            }
            return null;
        }

        /// <summary>
        ///  タイトルリストを取得する
        /// </summary>
        /// <param name="group">グループ名</param>
        /// <returns></returns>
        public List<string> getTitleList(string group)
        {
            List<string> titleList = new List<string>();
            foreach (MapMark mark in mMapMarkList) {
                if (group.Length == 0 || mark.mGroup.CompareTo(group) == 0)
                    if (!titleList.Contains(mark.mTitle))
                        titleList.Add(mark.mTitle);
            }
            if (mListSort == SORTTYPE.Normal)
                titleList.Sort((x, y) => x.CompareTo(y));
            else if (mListSort == SORTTYPE.Reverse)
                titleList.Sort((x, y) => y.CompareTo(x));
            else if (mListSort == SORTTYPE.Distance) {
                titleList.Sort((x, y) => Math.Sign(getMapMark(x, group).distance(mCenter) - getMapMark(y, group).distance(mCenter)));
            }
            return titleList;
        }

        /// <summary>
        /// グループをリストで抽出
        /// </summary>
        /// <returns></returns>
        public List<string> getGroupList()
        {
            List<string> groupList = new List<string>();
            foreach (MapMark mark in mMapMarkList) {
                if (!groupList.Contains(mark.mGroup))
                    groupList.Add(mark.mGroup);
            }
            return groupList;
        }

        /// <summary>
        /// 指定値のマークデータを取得する
        /// </summary>
        /// <param name="location">位置座標(スクリーン座標)</param>
        /// <param name="mapData">MapMarkデータ(見つからない時はnull)</param>
        /// <returns></returns>
        public MapMark getMark(Point location, MapData mapData)
        {
            foreach (MapMark mark in mMapMarkList) {
                Point listPos = mapData.map2Screen(mapData.baseMap2Map(mark.mLocation));
                double dis = (location.X - listPos.X) * (location.X - listPos.X) + (location.Y - listPos.Y) * (location.Y - listPos.Y);
                if (dis < mTolerance && (mFilterGroup.Length < 1 || mFilterGroup.CompareTo(mark.mGroup) == 0)) {
                    return mark;
                }
            }
            return null;
        }

        /// <summary>
        /// マークの表示
        /// </summary>
        /// <param name="ydraw"></param>
        /// <param name="mapData"></param>
        public void draw(YGButton ydraw, MapData mapData)
        {
            foreach (MapMark mark in mMapMarkList) {
                mark.mSizeRate = mSizeRate;
                mark.draw(ydraw, mapData, mFilterGroup);
            }
        }

        /// <summary>
        /// データの保存
        /// </summary>
        public void saveFile()
        {
            saveMarkFile(mSaveFilePath);
        }

        /// <summary>
        /// データの取り込み
        /// </summary>
        public void loadFile()
        {
            loadMarkFile(mSaveFilePath, false);
        }

        /// <summary>
        /// ファイルにデータを保存する
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void saveMarkFile(string path)
        {
            if (mMapMarkList.Count == 0)
                return;
            List<string[]> dataList = new List<string[]>();
            foreach (MapMark mark in mMapMarkList) {
                string[] data = mark.getStrinData();
                dataList.Add(data);
            }
            ylib.saveCsvData(path, MapMark.mMarkDataFormat, dataList);
        }

        /// <summary>
        /// ファイルからデータを読み込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void loadMarkFile(string path, bool add)
        {
            if (File.Exists(path)) {
                List<string[]> dataList = new List<string[]>();
                dataList = ylib.loadCsvData(path, MapMark.mMarkDataFormat);
                if (!add)
                    mMapMarkList.Clear();
                foreach (string[] data in dataList) {
                    MapMark mapMark = new MapMark();
                    mapMark.setStrinData(data);
                    if (getMapMark(mapMark.mTitle, mapMark.mGroup) == null)
                        mMapMarkList.Add(mapMark);
                }
            }
        }
    }
}
