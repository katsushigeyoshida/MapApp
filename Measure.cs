using System.Collections.Generic;
using System.Windows;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// トレースした距離を測定する
    /// </summary>
    class Measure
    {
        private List<Point> mPositionList = new List<Point>();  //  計測地点リスト
        public bool mMeasureMode = false;                       //  計測中のモード

        private YLib ylib = new YLib();

        public Measure()
        {

        }

        /// <summary>
        /// 計測地点の追加
        /// </summary>
        /// <param name="pos">BaseMap座標</param>
        public void add(Point pos)
        {
            mPositionList.Add(pos);
        }

        /// <summary>
        /// 計測地点を一つ戻す
        /// </summary>
        public void decriment()
        {
            if (0 < mPositionList.Count)
                mPositionList.RemoveAt(mPositionList.Count - 1);
        }

        /// <summary>
        /// 登録した地点数
        /// </summary>
        /// <returns></returns>
        public int getCount()
        {
            return mPositionList.Count;
        }

        /// <summary>
        /// 計測地点をクリア
        /// </summary>
        public void clear()
        {
            mPositionList.Clear();
        }

        /// <summary>
        /// 計測経路を地図上に表示
        /// </summary>
        /// <param name="ydraw"></param>
        /// <param name="mapData"></param>
        public void draw(YGButton ydraw, MapData mapData)
        {
            if (1 < mPositionList.Count) {
                ydraw.setThickness(1);
                ydraw.setColor("Green");
                Point sp = mapData.baseMap2Screen(mPositionList[0]);
                for (int i = 1; i < mPositionList.Count; i++) {
                    Point ep = mapData.baseMap2Screen(mPositionList[i]);
                    ydraw.drawLine(sp, ep);
                    sp = ep;
                }
            }
        }

        /// <summary>
        /// 計測地点間の距離の合計(km)
        /// </summary>
        /// <param name="mapData"></param>
        /// <returns></returns>
        public double measure(MapData mapData)
        {
            double dis = 0;
            for (int i = 0; i < mPositionList.Count - 1; i++) {
                Point ps = mapData.map2Coordinates(mapData.baseMap2Map(mPositionList[i]));
                Point pe = mapData.map2Coordinates(mapData.baseMap2Map(mPositionList[i+1]));
                dis += distance(ps, pe);
            }
            return dis;
        }

        /// <summary>
        /// 緯度経度の座標による距離を求める
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="pe"></param>
        /// <returns></returns>
        private double distance(Point ps, Point pe)
        {
            return ylib.coordinateDistance(ps.X, ps.Y, pe.X, pe.Y);
        }

    }
}
