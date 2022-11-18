using System;
using System.Collections.Generic;
using System.Windows;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// マークデータクラス
    /// </summary>
    public class MapMark
    {
        public string mTitle = "";                                  //  マーク名
        public string mGroup = "";                                  //  グループ名
        public int mMarkType = 1;                                   //  表示する記号の種類
        public string mComment = "";                                //  コメント
        public string mLink = "";                                   //  リンクデータ
        public Point mLocation = new Point(0.882022, 0.393773);     //  マークの中心位置(BaseMap座標)(皇居の座標)
        public double mSize = 10;                                   //  記号のサイズ
        public double mSizeRate = 1.0;                              //  記号のサイズ倍率
        public bool mVisible = true;                                //   表示非表示
        public bool mTitleVisible = true;                           //  タイトルの表示/非表示

        public string[] mMarkPath;
        public string mMarkName;

        public static string[] mMarkDataFormat = {
            "Title", "Group", "MarkType", "Size", "Comment", "Link", "Visible", "TitleVisible", "XLocation", "YLocation",
        };

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MapMark()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="ygbutton">YGButton class</param>
        /// <param name="title">タイトル</param>
        /// <param name="location">位置(ZoomLevel0のMAP座標)</param>
        /// <param name="markType">記号の種類</param>
        /// <param name="mapData">MapData class</param>
        public MapMark(string title, Point location, int markType)
        {
            mTitle = title;
            //location.Offset(-10, -10);
            mLocation = location;
            mMarkType = markType;
        }

        /// <summary>
        /// マークを描画する
        /// 記号パターンを読み込んで表示
        /// </summary>
        /// <param name="mapData"></param>
        public void draw(YGButton ydraw, MapData mapData, string filterGroup)
        {
            if (insideChk(mapData.getArea(), mLocation) && 
                (filterGroup.Length < 1 || containGroup(filterGroup))) {
                ydraw.setThickness(1.0);
                for (int i = 1; i < mMarkPath.Length; i++) {
                    switch (mMarkPath[i][0]) {
                        case 'C': setColor(ydraw, mMarkPath[i]); break;
                        case 'L': drawLine(ydraw, mMarkPath[i], mapData); break;
                        case 'R': drawRect(ydraw, mMarkPath[i], mapData); break;
                        case 'P': drawPolyLine(ydraw, mMarkPath[i], mapData); break;
                        case 'A': drawArc(ydraw, mMarkPath[i], mapData); break;
                        default: break;
                    }
                }
                if (0 < mTitle.Length && mTitleVisible) {
                    drawTitle(ydraw, mapData);
                }
            }
        }

        /// <summary>
        /// 座標が表示領域内に入っているかの判定
        /// </summary>
        /// <param name="area">表示領域(BaseMap)</param>
        /// <param name="data">マーク座標(BaseMap)</param>
        /// <returns></returns>
        private bool insideChk(Rect area, Point data)
        {
            if (data.X < area.Left || area.Right < data.X ||
                    data.Y < area.Top || area.Bottom < data.Y)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 指定のグループの有無を確認
        /// 指定グループ名が空の場合はすべて該当
        /// </summary>
        /// <param name="group">グループ名</param>
        /// <returns>グループの有無</returns>
        public bool containGroup(string group)
        {
            if (group == null || group.Length == 0)
                return true;
            string[] groups = mGroup.Split(',');
            for (int i=0; i < groups.Length; i++) {
                if (groups[i].Trim().CompareTo(group) == 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// タイルを表示する
        /// </summary>
        /// <param name="mapData"></param>
        private void drawTitle(YGButton ydraw, MapData mapData)
        {
            ydraw.setColor("Black");
            ydraw.setTextSize(mSize * mSizeRate * 2);
            Point pos = mapData.baseMap2Screen(mLocation);
            pos.Y += mSize * mSizeRate * 2 - 10 * mSizeRate;
            ydraw.drawWText(mTitle, pos, 0);
        }

        /// <summary>
        /// 色の設定
        /// </summary>
        /// <param name="command">コマンド</param>
        private void setColor(YGButton ydraw, string command)
        {
            string color = command.Substring(command.IndexOf('=') + 1);
            ydraw.setColor(color.Trim());
            ydraw.setFillColor(null);
        }

        /// <summary>
        /// 線分の表示
        /// </summary>
        /// <param name="command">コマンド</param>
        /// <param name="mapData"></param>
        private void drawLine(YGButton ydraw, string command, MapData mapData)
        {
            string[] data = getParameter(command);

            Point pos = mapData.baseMap2Screen(mLocation);
            double sx =  ylib.doubleParse(data[0]) * mSize * mSizeRate + pos.X;
            double sy = -ylib.doubleParse(data[1]) * mSize * mSizeRate + pos.Y;
            double ex =  ylib.doubleParse(data[2]) * mSize * mSizeRate + pos.X;
            double ey = -ylib.doubleParse(data[3]) * mSize * mSizeRate + pos.Y;
            ydraw.drawWLine(new Point(sx, sy), new Point(ex, ey));
        }

        /// <summary>
        /// 四角形の表示
        /// </summary>
        /// <param name="ydraw"></param>
        /// <param name="command"></param>
        /// <param name="mapData"></param>
        private void drawRect(YGButton ydraw, string command, MapData mapData)
        {
            string[] data = getParameter(command);

            Point pos = mapData.baseMap2Screen(mLocation);
            double sx =  ylib.doubleParse(data[0]) * mSize * mSizeRate + pos.X;
            double sy = -ylib.doubleParse(data[1]) * mSize * mSizeRate + pos.Y;
            double ex =  ylib.doubleParse(data[2]) * mSize * mSizeRate + pos.X;
            double ey = -ylib.doubleParse(data[3]) * mSize * mSizeRate + pos.Y;
            ydraw.drawWRectangle(new Point(sx,sy), new Point(ex, ey), 0.0);
        }

        /// <summary>
        /// 連続線分の表示
        /// </summary>
        /// <param name="ydraw"></param>
        /// <param name="command"></param>
        /// <param name="mapData"></param>
        private void drawPolyLine(YGButton ydraw, string command, MapData mapData)
        {
            string[] data = getParameter(command);
            Point pos = mapData.baseMap2Screen(mLocation);
            Point sp = new Point(ylib.doubleParse(data[0]) * mSize * mSizeRate + pos.X,
                                -ylib.doubleParse(data[1]) * mSize * mSizeRate + pos.Y);
            for (int i = 2; i < data.Length; i += 2) {
                Point ep = new Point(ylib.doubleParse(data[i]) * mSize * mSizeRate + pos.X,
                                    -ylib.doubleParse(data[i+1]) * mSize * mSizeRate + pos.Y);
                ydraw.drawWLine(sp, ep);
                sp = ep;
            }
        }



        /// <summary>
        /// 円弧の描画
        /// </summary>
        /// <param name="command">コマンド</param>
        /// <param name="mapData"></param>
        private void drawArc(YGButton ydraw, string command, MapData mapData)
        {
            string[] data = getParameter(command);
            Point pos = mapData.baseMap2Screen(mLocation);
            double cx =  ylib.doubleParse(data[0]) + pos.X;
            double cy = -ylib.doubleParse(data[1]) + pos.Y;
            double r  =  ylib.doubleParse(data[2]) * mSize * mSizeRate;
            double sa =  ylib.doubleParse(data[3]) * Math.PI / 180;
            double ea =  ylib.doubleParse(data[4]) * Math.PI / 180;
            ydraw.drawWArc(new Point(cx, cy), r, sa, ea);
        }

        /// <summary>
        /// コマンドをパラメータに分解する
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private string[] getParameter(string command)
        {
            string data = command.Substring(command.IndexOf('=') + 1);
            return data.Split(',');
        }

        /// <summary>
        /// 指定座標との距離
        /// </summary>
        /// <param name="p">緯度経度座標</param>
        /// <returns>距離8km)</returns>
        public double distance(Point p)
        {
            double dis =  ylib.coordinateDistance(p, MapData.baseMap2Coordinates(mLocation));
            //System.Diagnostics.Debug.WriteLine($"{mTitle} {dis} {p} {MapData.baseMap2Coordinates(mLocation)}");
            return dis;
        }

        /// <summary>
        /// データを文字配列で取得する
        /// </summary>
        /// <returns></returns>
        public string[] getStrinData()
        {
            string[] data = new string[mMarkDataFormat.Length];
            data[0] = mTitle;
            data[1] = mGroup;
            data[2] = mMarkType.ToString();
            data[3] = mSize.ToString();
            data[4] = mComment;
            data[5] = mLink;
            data[6] = mVisible.ToString();
            data[7] = mTitleVisible.ToString();
            data[8] = mLocation.X.ToString();
            data[9] = mLocation.Y.ToString();
            return data;
        }

        /// <summary>
        /// 文字配列からデータを設定する
        /// </summary>
        /// <param name="data"></param>
        public void setStrinData(string[] data, List<string[]> markPathData)
        {
            mTitle        = data[0];
            mGroup        = data[1];
            mMarkType     = ylib.intParse(data[2], mMarkType);
            mSize         = ylib.doubleParse(data[3], mSize);
            mComment      = data[4];
            mLink         = data[5];
            mVisible      = ylib.boolParse(data[6], mVisible);
            mTitleVisible = ylib.boolParse(data[7], mTitleVisible);
            mLocation.X   = ylib.doubleParse(data[8], mLocation.X);
            mLocation.Y   = ylib.doubleParse(data[9], mLocation.Y);
            mMarkPath     = markPathData[mMarkType < markPathData.Count ? mMarkType : 0];
        }
    }
}
