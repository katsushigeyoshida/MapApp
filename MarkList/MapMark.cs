using System;
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

        private string[][] mMarkPath = {    //  記号パターン (L:線分 R:四角 A:円弧 C:色)
            new string[] { "C=Black", "L=-1,0,1,0", "L=0,-1,0,1" },                 //  クロス
            new string[] { "C=Black", "L=-1,0,1,0", "L=0,-1,0,1", "C=Red", "A=0,0,0.7,0,360" }, //  クロスに〇
            new string[] { "C=Black", "L=-1,1,1,1", "L=1,1,0,-1", "L=0,-1,-1,1"},   //  △
            new string[] { "C=Black", "L=-1,1,1,1", "L=1,1,1,0", "L=1,0,0,-1", "L=0,-1,-1,0", "L=-1,0,-1,1", "L=-1,0,1,0" },    //  家
            new string[] { "C=Black", "R=-0.6, -1,0.6,1", "R=-0.4,-0.7,0.4,-0.4", "R=-0.4,-0.2,0.4,0.2", "R=-0.4,0.4,0.4,0.7"},    //  ビル
            new string[] { "C=Black", "R=-1,0.0, 1,1", "R=-0.7,-1,-0.3,0.0"},       //  工場
            new string[] { "C=Black", "L=-1.5,0,1.5,0", "A=0,0,1,0,180" },          //  橋
            new string[] { "C=Black", "L=0,-1.5,-1,-0.3", "L=0,-1.5,1,-0.3", "L=1,-0.3,-1,-0.3",
                                      "L=0,-0.4,-1,0.8", "L=0,-0.4,1,0.8", "L=1,0.8,-1,0.8",
                                      "L=0,0.8,0,1.5" },                            //  公園(木)
        };
        public string[] mMarkName = { "クロス", "クロス円", "三角形",　"家", "ビル" , "工場", "橋", "公園" };
        public string[] mSizeName = { "1", "2", "4", "7", "10", "15", "20", "25", "30" };
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
                for (int i = 0; i < mMarkPath[mMarkType].Length; i++) {
                    switch (mMarkPath[mMarkType][i][0]) {
                        case 'C': setColor(ydraw, mMarkPath[mMarkType][i]); break;
                        case 'L': drawLine(ydraw, mMarkPath[mMarkType][i], mapData); break;
                        case 'R': drawRect(ydraw, mMarkPath[mMarkType][i], mapData); break;
                        case 'P': drawPolyLine(ydraw, mMarkPath[mMarkType][i], mapData); break;
                        case 'A': drawArc(ydraw, mMarkPath[mMarkType][i], mapData); break;
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
            ydraw.drawText(mTitle, pos, 0);
        }

        /// <summary>
        /// 色の設定
        /// </summary>
        /// <param name="command">コマンド</param>
        private void setColor(YGButton ydraw, string command)
        {
            string color = command.Substring(command.IndexOf('=') + 1);
            ydraw.setColor(color.Trim());
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
            double sx = double.Parse(data[0]) * mSize * mSizeRate + pos.X;
            double sy = double.Parse(data[1]) * mSize * mSizeRate + pos.Y;
            double ex = double.Parse(data[2]) * mSize * mSizeRate + pos.X;
            double ey = double.Parse(data[3]) * mSize * mSizeRate + pos.Y;
            ydraw.drawLine(sx, sy, ex, ey);
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
            double sx = double.Parse(data[0]) * mSize * mSizeRate + pos.X;
            double sy = double.Parse(data[1]) * mSize * mSizeRate + pos.Y;
            double ex = double.Parse(data[2]) * mSize * mSizeRate + pos.X;
            double ey = double.Parse(data[3]) * mSize * mSizeRate + pos.Y;
            ydraw.drawLine(sx, sy, sx, ey);
            ydraw.drawLine(ex, sy, ex, ey);
            ydraw.drawLine(sx, sy, ex, sy);
            ydraw.drawLine(sx, ey, ex, ey);
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
            double sx = double.Parse(data[0]) * mSize * mSizeRate + pos.X;
            double sy = double.Parse(data[1]) * mSize * mSizeRate + pos.Y;
            for (int i = 2; i < data.Length; i += 2) {
                double ex = double.Parse(data[i]) * mSize * mSizeRate + pos.X;
                double ey = double.Parse(data[i+1]) * mSize * mSizeRate + pos.Y;
                ydraw.drawLine(sx, sy, ex, ey);
                sx = ex;
                sy = ey;
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
            double cx = double.Parse(data[0]) + pos.X;
            double cy = double.Parse(data[1]) + pos.Y;
            double r = double.Parse(data[2]) * mSize * mSizeRate;
            double sa = double.Parse(data[3]) * Math.PI / 180;
            double ea = double.Parse(data[4]) * Math.PI / 180;
            ydraw.drawArc(cx, cy, r, sa, ea);
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
        public void setStrinData(string[] data)
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
        }
    }
}
