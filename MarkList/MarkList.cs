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

        public List<string[]> mMarkPath = new List<string[]>() {
            //  記号パターン (T:マークタイトル L:線分 R:四角 A:円弧 C:色)
            new string[] { "T=クロス",   "C=Black", "L=-1,0,1,0", "L=0,1,0,-1" },
            new string[] { "T=クロス円", "C=Black", "L=-1,0,1,0", "L=0,1,0,-1", "C=Red", "A=0,0,0.7,0,360" },
            new string[] { "T=三角形",   "C=Black", "L=-1,-1,1,-1", "L=1,-1,0,0", "L=0,0,-1,-1"},
            new string[] { "T=家",       "C=Black", "L=-1,-1,1,-1", "L=1,-1,1,0", "L=1,0,0,1", "L=0,1,-1,0", "L=-1,0,-1,-1", "L=-1,0,1,0" },
            new string[] { "T=ビル",     "C=Black", "R=-0.6, 1,0.6,-1", "R=-0.4,0.7,0.4,0.4", "R=-0.4,0.2,0.4,-0.2", "R=-0.4,-0.4,0.4,-0.7"},
            new string[] { "T=工場",     "C=Black", "R=-1,0.0, 1,-1", "R=-0.7,1,-0.3,0.0"},
            new string[] { "T=橋",       "C=Black", "L=-1.5,0,1.5,0", "A=0,0,1,0,180" },
            new string[] { "T=公園",     "C=Black", "L=0,1.5,-1,0.3", "L=0,1.5,1,0.3", "L=1,0.3,-1,0.3",
                                         "L=0,0.4,-1,-0.8", "L=0,0.4,1,-0.8", "L=1,-0.8,-1,-0.8", "L=0,-0.8,0,-1.5" },
        };
        public string[] mMarkName;
        public string mMarkPathDataFile = "MarkPathData.csv";
        public string[] mSizeName = { "1", "2", "4", "7", "10", "15", "20", "25", "30" };

        private string mAppFolder;
        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MarkList()
        {
            loadPathData();
            setMarkPath2Title();
        }

        /// <summary>
        /// マークパスデータからマーク名だけを抽出する
        /// </summary>
        private void setMarkPath2Title()
        {
            mMarkName = new string[mMarkPath.Count];
            for (int i = 0; i < mMarkPath.Count; i++) {
                int n = mMarkPath[i][0].IndexOf("T=");
                if (0 <= n && n < mMarkPath[i][0].Length)
                    mMarkName[i] = mMarkPath[i][0].Substring(2);
            }
        }

        /// <summary>
        /// マークパスデータをファイルに保存
        /// </summary>
        public void savePathData()
        {
            if (File.Exists(mMarkPathDataFile))
                return;
            List<string> pathData = new List<string>();
            pathData.Add("# マークの形状データ");
            pathData.Add("# 名称　 : T=マーク名称");
            pathData.Add("# 色設定 : C=カラー名称");
            pathData.Add("# 線分　 : L=始点X座標,始点Y座標,終点X座標,終点Y座標");
            pathData.Add("# 四角形 : R=始点X座標,始点Y座標,終点X座標,終点Y座標");
            pathData.Add("# 円弧　 : R=中心点X座標,中心点Y座標,半径,開始角(度),修了角(度)");
            foreach (string[] datas in mMarkPath) {
                foreach (string data in datas) {
                    pathData.Add(data);
                }
            }
            if (0 < pathData.Count)
                ylib.saveListData(mMarkPathDataFile, pathData);
        }

        /// <summary>
        /// マークパスデータをファイルから呼び出す
        /// </summary>
        public void loadPathData()
        {
            if (File.Exists(mMarkPathDataFile)) {
                List<string> pathdata = ylib.loadListData(mMarkPathDataFile);
                mMarkPath.Clear();
                List<string> buf = null;
                foreach (string data in pathdata) {
                    if (2 < data.Length && data[0] != '#') {
                        if (0 == data.IndexOf("T=")) {
                            if (buf != null && 0 < buf.Count)
                                mMarkPath.Add(buf.ToArray());
                            buf = new List<string>();
                            buf.Add(data);
                        } else if (buf != null && data[1] == '=') {
                            buf.Add(data);
                        }
                    }
                }
                if (buf != null && 0 < buf.Count)
                    mMarkPath.Add(buf.ToArray());
            }

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
                if (mapMark.mTitle.CompareTo(title) == 0 && mapMark.containGroup(group))
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
                if (mark.containGroup(group))
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
                string[] group = mark.mGroup.Split(',');
                for (int i = 0; i < group.Length; i++) {
                    if (!groupList.Contains(group[i].Trim()))
                        groupList.Add(group[i].Trim());
                }
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
                    mapMark.setStrinData(data, mMarkPath);
                    if (getMapMark(mapMark.mTitle, mapMark.mGroup) == null)
                        mMapMarkList.Add(mapMark);
                }
            }
        }
    }
}
