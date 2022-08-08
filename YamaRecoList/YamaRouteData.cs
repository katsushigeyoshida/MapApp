using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WpfLib;

namespace MapApp
{
    public class YamaRouteData
    {
        private Encoding[] mEncoding = {
            Encoding.UTF8, Encoding.GetEncoding("shift_jis"), Encoding.GetEncoding("euc-jp")
        };
        private int mEncordType = 2;                                    //  EUC(ファイルアクセス)
        private string mAppFolder;                                      //  アプリフォルダ
        private string mDataSaveFolder = "YamaRecoData";                //  データフォルダ
        private string mYamaRouteDataPath = "YamaRouteData.csv";        //  データファイル名
        private string mYamaBaseUrl = "https://www.yamareco.com/modules/yamainfo/";
        private string mYamaRouteUrl = "https://www.yamareco.com/modules/yamainfo/rtinfo.php?rtid=";
        public string mSplitWord = " : ";                               //  分類データの分轄ワード
        public char mSeparatorChar = '\t';                              //  項目分轄char

        public string[] mDataTitle = {                                  //  データタイトル
            "ルート名", "日程", "エリア", "ジャンル",     "技術レベル",     "体力レベル",         "見どころ",
            "合計距離", "最高点の標高",   "最低点の標高", "累積標高（上り）", "累積標高（下り）",
            "アクセス", "ルート詳細",     "URL"
        };
        public bool[] mDispCol = {                                      //  表示カラムフラグ
            true,        true,   true,     true,           true,             true,                 true,
            true,        true,             true,           true,             true,
            true,        true,             true
        };
        public bool[] mNumVal = {                                       //  数値データの有無(ソート用)
            false,       false,  false,    false,          false,             false,               false,
            true,        true,             true,           true,              true,
            false,       false,            true
        };
        public bool[] mDetailCol = {                                    //  詳細簡略表示判定
            false,       false,  false,    false,          false,              false,              false,
            false,       false,            false,          false,              false,
            false,       true,             false
        };
        public List<string[]> mDataList = new List<string[]>();         //  山データリスト
        public List<string[]> mDetailUrlList = new List<string[]>();    //  詳細データの(URL,項目)リスト

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public YamaRouteData()
        {
            mAppFolder = ylib.getAppFolderPath();                               //  アプリフォルダ
            mDataSaveFolder = Path.Combine(mAppFolder, mDataSaveFolder);        //  HTMLデータ保存フォルダ
            mYamaRouteDataPath = Path.Combine(mAppFolder, mYamaRouteDataPath);
        }

        /// <summary>
        /// 番号指定してWebデータを取り込む
        /// </summary>
        /// <param name="st"></param>
        /// <param name="end"></param>
        public void getYamaRecoList(int st, int end)
        {
            if (mDataList == null)
                mDataList = new List<string[]>();
            for (int i = st; i <= end; i++) {
                getYamaRecoData(i);
            }
        }

        /// <summary>
        /// 番号指定してWebデータを取り込む
        /// </summary>
        /// <param name="n"></param>
        public void getYamaRecoData(int n)
        {
            string url = mYamaRouteUrl + n.ToString();
            getYamaRecoData(url);
        }

        /// <summary>
        /// Webページデータをダウンロードしデータを抽出してリストに追加
        /// </summary>
        /// <param name="url">URL</param>
        public void getYamaRecoData(string url)
        {
            //  Webデータの取得
            if (mDataList == null)
                mDataList = new List<string[]>();
            if (mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(url) == 0) < 0) {
                string html = getWebData(url);
                string[] buf = getListData(html, url);
                if (0 < buf[0].Length) {
                    //System.Diagnostics.Debug.WriteLine($"{buf[0]} {url}");
                    mDataList.Add(buf);
                }
            }
        }

        /// <summary>
        /// 山データから周辺情報データ(登山口,山小屋)をリスト化(URL,山名)
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>周辺情報データリスト(URL,山名)</returns>
        public List<string[]> getSelectUrlList(string url)
        {
            int m = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(url) == 0);
            List<string[]> listData = new List<string[]>();
            string[] buf = new string[2];
            string[] text = mDataList[m][titleNo("ルート詳細")].ToString().Split(mSeparatorChar);
            for (int j = 0; j < text.Length; j++) {
                buf = new string[2];
                int n = text[j].IndexOf(mSplitWord);
                if (0 < n) {
                    buf[1] = text[j].Substring(0, n).Trim();                    //  山名(登山口,山小屋)
                    buf[0] = text[j].Substring(n + mSplitWord.Length).Trim();   //  URL
                    listData.Add(buf);
                }
            }
            return listData;
        }

        /// <summary>
        /// データの詳細表示
        /// URLで合致するデータを取得
        /// </summary>
        /// <param name="url">データのURL</param>
        /// <returns>(データ文字列,タイトル)</returns>
        public (string, string) detaiilDisp(string url)
        {
            string buf = "";
            int col = mDataTitle.FindIndex(p => p.CompareTo("URL") == 0);
            int selIndex = mDataList.FindIndex(p => p[col].CompareTo(url) == 0);
            string title = mDataList[selIndex][0];
            for (int i = 0; i < mDataList[selIndex].Length; i++) {
                if (mDetailCol[i]) {
                    string[] text = mDataList[selIndex][i].ToString().Split(mSeparatorChar);
                    buf += "\nルート詳細:";
                    for (int j = 0; j < text.Length; j++) {
                        buf += "\n  " + text[j].Trim();
                    }
                } else {
                    buf += (0 < buf.Length ? "\n" : "") + mDataTitle[i] + " : " + mDataList[selIndex][i];
                }
            }
            return (buf, title);
        }

        /// <summary>
        /// [分類]と検索ワードでフィルタリングしたデータリストを作成
        /// </summary>
        /// <param name="category">分類(dummy)</param>
        /// <param name="searchWord">検索ワード</param>
        /// <returns>データリスト</returns>
        public List<string[]> getFilterongDataList(string category = "", string searchWord = "")
        {
            List<string[]> dataList = new List<string[]>();
            int dispSize = mDispCol.Count(item => item == true);

            for (int i = 0; i < mDataList.Count; i++) {
                if ((searchWord.Length == 0 || searchDataChk(mDataList[i], searchWord))) {
                    string[] buf = new string[dispSize];
                    for (int j = 0; j < buf.Length; j++) {
                        if (mDispCol[j])
                            buf[j] = mDataList[i][j].Substring(0, Math.Min(mDataList[i][j].Length, 100));
                    }
                    dataList.Add(buf);
                }
            }
            return dataList;
        }

        /// <summary>
        /// リストデータから検索ワードの有無か座標検索をおこなう
        /// </summary>
        /// <param name="listData">リストデータ</param>
        /// <param name="searchWord">検索ワード</param>
        /// <returns>有無</returns>
        private bool searchDataChk(string[] listData, string searchWord)
        {
            //  ワード検索
            if (0 <= listData.FindIndex(p => 0 <= p.IndexOf(searchWord)))
                return true;
            return false;
        }

        /// <summary>
        /// 抽出URLリストからルートデータリストを抽出する
        /// </summary>
        /// <param name="listData">抽出データリスト(URL,ルート名)</param>
        /// <returns>ルートデータリスト</returns>
        public List<string[]> extractListdata(List<string[]> listData)
        {
            List<string[]> routeListdata = new List<string[]>();
            if (mDataList != null && 0 <= mDataList.Count) {
                for (int i = 0; i < listData.Count; i++) {
                    int n = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(listData[i][0]) == 0);
                    if (0 <= n)
                        routeListdata.Add(mDataList[n]);
                }
            }

            return routeListdata;
        }


        /// <summary>
        /// HTMLソースから登山ルートデータを抽出し配列に入れる
        /// [0]ルート名,[1]日程,[2]エリア,[3]ジャンル,[4]技術レベル,[5]体力レベル,[6]合計距離,[7]最高点の標高,[8]最低点の標高
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>データ配列</returns>
        private string[] getListData(string html, string url)
        {
            //  タイトルに合わせたデータ配列に置換える
            string[] data = Enumerable.Repeat<string>("", mDataTitle.Length).ToArray();
            //  データの抽出
            List<string[]> listData = getListData(html);
            //  タイトル順の配列に変更
            for (int i = 0; i < mDataTitle.Count(); i++) {
                int n = listData.FindIndex(p => p[0].CompareTo(mDataTitle[i]) == 0);
                if (0 <= n)
                    data[i] = listData[n][1];
                else if (mDataTitle[i].CompareTo("URL") == 0)
                    data[i] = url;
            }

            return data;

        }

        /// <summary>
        /// HTMLソースからデータを抽出
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>データリスト(タイトル,データ)</returns>
        private List<string[]> getListData(string html)
        {
            List<string[]> listData = new List<string[]>();
            string bodyData, title, table;
            string[] buf = new string[2];
            //  head データ
            (string tagPara, string headData, string nextSrc) = ylib.getHtmlTagData(html, "head");
            //  body データ
            (tagPara, bodyData, nextSrc) = ylib.getHtmlTagData(nextSrc, "body");
            //  headタイトル
            (tagPara, title, nextSrc) = ylib.getHtmlTagData(headData, "title");

            string pageTitle = ylib.stripHtmlTagData(ylib.getHtmlTagSrc(bodyData, "div", "id", "pagetitle"));
            buf = new string[2] { "ルート名", ylib.stripControlCode(pageTitle) };
            listData.Add(buf);
            //  表データ
            (tagPara, table, nextSrc) = ylib.getHtmlTagData(bodyData, "table");
            List<string[]> tableList = getRouteInfoList(table);
            foreach (var text in tableList) {
                buf = new string[2];
                if (text[0].CompareTo("技術レベル") == 0 || text[0].CompareTo("体力レベル") == 0) {
                    buf[0] = text[0];
                    int n = text[1].IndexOf("※");
                    if (0 <= n)
                        buf[1] = text[1].Substring(0, n).Trim();
                    else
                        buf[1] = text[1].Trim();
                } else {
                    buf[0] = text[0];
                    buf[1] = text[1];
                }
                listData.Add(buf);
            }
            //  ルート詳細
            List<string[]> routeList= getRouteDetailList(nextSrc);
            string routes = "";
            foreach (var route in routeList)
                routes += (0 < routes.Length ? mSeparatorChar.ToString() : "") + route[0] + " : " + route[1];
            buf = new string[2] { "ルート詳細", routes };
            listData.Add(buf);

            return listData;
        }

        /// <summary>
        /// ルート情報のテーブルエリアの情報取得
        /// </summary>
        /// <param name="table">tableデータ</param>
        /// <returns>抽出データ</returns>
        private List<string[]> getRouteInfoList(string table)
        {
            List<string[]> listData = new List<string[]>();
            string tagData, tagPara, trData, nextTag;
            string nextHtml = table;
            do {
                (tagPara, trData, nextHtml) = ylib.getHtmlTagData(nextHtml, "tr");
                if (trData.Length <= 0)
                    break;
                string[] buf = new string[2];
                (tagPara, tagData, nextTag) = ylib.getHtmlTagData(trData, "th");
                if (tagData.IndexOf("距離／時間") < 0) {
                    buf[0] = ylib.stripControlCode(tagData);
                    (tagPara, tagData, nextTag) = ylib.getHtmlTagData(ylib.cnvHtmlSpecialCode(nextTag), "td");
                    buf[1] = ylib.stripHtmlTagData(ylib.stripControlCode(ylib.cnvHtmlSpecialCode(tagData)));
                    listData.Add(buf);
                } else {
                    List<string> dataList = ylib.getHtmlTagDataAll(trData);
                    foreach (var data in dataList) {
                        string[] temp = data.Split('：');
                        if (1 < temp.Length) {
                            buf = new string[2];
                            buf[0] = ylib.stripControlCode(temp[0].Trim());
                            buf[1] = ylib.stripControlCode(temp[1].Trim());
                            listData.Add(buf);
                        }
                    }
                }
            } while (0 < trData.Length && 0 < nextHtml.Length);

            return listData;
        }

        /// <summary>
        /// ルート詳細の取得
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>(title,url)のリスト</returns>
        private List<string[]> getRouteDetailList(string html)
        {
            List<string[]> listData = new List<string[]>();
            string tagPara;
            string tagData;
            string nextSrc = html;
            do {
                (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "span");
                if (tagPara.IndexOf("pt-") < 0)
                    break;
                (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "div");
                (string title, string url) = getRouteDetailData(tagData);
                if (0 < title.Length && 0 < url.Length) {
                    string[] buf = new string[2];
                    buf[0] = title;
                    buf[1] = mYamaBaseUrl +  url;
                    listData.Add(buf);
                }
            } while (0 < nextSrc.Length);
            return listData;
        }

        /// <summary>
        /// ルート詳細の取得
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private (string, string) getRouteDetailData(string html)
        {
            string tagPara;
            string tagData;
            string nextSrc = html;
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "span");
            string title = ylib.getHtmlTagData(tagData);
            while (0 < nextSrc.Length && 0 < tagData.Length) {
                (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "a");
                string url = ylib.stripHtmlParaData(tagPara, "href");
                if (0 <= url.IndexOf("ptinfo.php?ptid=")) {
                    return (title, url);
                }
            }
            return (title, "");
        }

        /// <summary>
        /// URLのWebデータの読込
        /// </summary>
        /// <param name="url">URL</param>
        private string getWebData(string url, string downloadFilePath = "")
        {
            string html = "";
            if (url.Substring(0, 4).CompareTo("http") == 0) {
                //  WebからのHTMLソースの取り込
                html = ylib.getWebDownloadString(Uri.UnescapeDataString(url), mEncordType);
                //  HTMLソースをファイルに保存
                if (0 < downloadFilePath.Length) {
                    ylib.saveTextFile(downloadFilePath, html);
                }
            } else {
                if (File.Exists(url))
                    html = ylib.loadTextFile(url);
            }
            return html;
        }

        /// <summary>
        /// データリストのタイトル名から配列位置を求める
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public int titleNo(string title)
        {
            return mDataTitle.FindIndex(p => p.CompareTo(title) == 0);
        }

        /// <summary>
        /// ファイルからリストデータを読み込む
        /// </summary>
        public void loadData()
        {
            mDataList = ylib.loadCsvData(mYamaRouteDataPath, mDataTitle);
        }

        /// <summary>
        /// データリストをファイルに保存
        /// </summary>
        public void saveData()
        {
            if (mDataList != null && 0 < mDataList.Count)
                ylib.saveCsvData(mYamaRouteDataPath, mDataTitle, mDataList);
        }
    }
}
