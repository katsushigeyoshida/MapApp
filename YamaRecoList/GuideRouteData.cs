using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfLib;

namespace MapApp
{
    public class GuideRouteData
    {
        private Encoding[] mEncoding = {
            Encoding.UTF8, Encoding.GetEncoding("shift_jis"), Encoding.GetEncoding("euc-jp")
        };
        private int mEncordType = 2;                                    //  EUC(ファイルアクセス)
        private string mAppFolder;                                      //  アプリフォルダ
        private string mDataSaveFolder = "YamaRecoData";                //  データフォルダ
        private string mGuideRouteDataPath = "GuideRouteData.csv";      //  データファイル名
        private string mYamaBaseUrl = "https://www.yamareco.com/modules/yamainfo/";
        private string mGuideRouteUrl = "https://www.yamareco.com/modules/yamainfo/guide_detail.php?route_id=";
        public string mSplitWord = " : ";                               //  分類データの分轄ワード
        public char mSeparatorChar = '\t';                              //  項目分轄char

        public readonly string[] mDataTitle = {                                  //  データタイトル
            "タイトル",     "ルート長",        "登り標高差",     "下り標高差",       "行程概要", 
            "分類",         "ルートメモ",      "モデルプラン",   "登山計画書作成",   "山と高原地図",
            "コース概要",   "計画書提出先",    "宿泊",           "交通",　           "駐車場",
            "アドバイス",   "サブコース",     "エスケープルート","入浴",             "おすすめ周辺情報",
            "登る山／通過する場所",   "URL"
        };
        public int[] mColWidth = {
            -1,             -1,                 -1,               -1,                  200,
            -1,             300,                300,              -1,                  -1,
            300,            200,                200,              200,                 300,
            300,            300,                200,              300,                 300,
            300,                       -1
        };
        public readonly bool[] mDispCol = {                             //  表示カラムフラグ
            true,           true,               true,            true,               true,
            true,           true,               true,            true,               true,
            true,           true,               true,            true,               true,
            true,           true,               true,            true,               true,
            true,           true
        };
        public readonly bool[] mNumVal = {                              //  数値データの有無(ソート用)
            false,          true,               true,            true,               false,
            false,          false,              false,           false,              false,
            false,          false,              false,           false,              false,
            false,          false,              false,           false,              false,
            false,          true
        };
        public readonly bool[] mDetailCol = {                           //  詳細簡略表示判定
            false,          false,              false,           false,              false,
            false,          false,              false,           false,              false,
            false,          false,              false,           false,              false,
            false,          false,              false,           false,              false,
            true,           false
        };
        public List<string[]> mDataList = new List<string[]>();         //  山データリスト
        public List<string[]> mDetailUrlList = new List<string[]>();    //  詳細データの(URL,項目)リスト

        private YLib ylib = new YLib();

        public GuideRouteData()
        {
            mAppFolder = ylib.getAppFolderPath();                               //  アプリフォルダ
            mDataSaveFolder = Path.Combine(mAppFolder, mDataSaveFolder);        //  HTMLデータ保存フォルダ
            mGuideRouteDataPath = Path.Combine(mAppFolder, mGuideRouteDataPath);
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
            string url = mGuideRouteUrl + n.ToString();
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
        /// おすすめルートデータから周辺情報データ(登る山／通過する場所)をリスト化(URL,山名)
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>周辺情報データリスト(URL,山名)</returns>
        public List<string[]> getSelectUrlList(string url)
        {
            int m = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(url) == 0);
            List<string[]> listData = new List<string[]>();
            string[] buf = new string[2];
            string[] text = mDataList[m][titleNo("登る山／通過する場所")].ToString().Split(mSeparatorChar);
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
                    buf += "\n" + mDataTitle[i] + ":";
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
                            buf[j] = mDataList[i][j];
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
            List<string[]> guideListdata = new List<string[]>();
            if (mDataList != null && 0 < mDataList.Count) {
                for (int i = 0; i < listData.Count; i++) {
                    int n = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(listData[i][0]) == 0);
                    if (0 <= n)
                        guideListdata.Add(mDataList[n]);
                }
            }

            return guideListdata;
        }

        /// <summary>
        /// HTMLソースからおすすめルートデータを抽出し配列に入れる
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <param name="url">URL</param>
        /// <returns>データ配列</returns>
        private string[] getListData(string html, string url)
        {
            //  HTMLソースからデータの抽出
            List<string[]> listData = getListData(html);
            string[] data = Enumerable.Repeat<string>("", mDataTitle.Length).ToArray();

            //  タイトルに合わせたデータ配列に置換える
            if (mDataList != null && 0 <= mDataList.Count) {
                for (int i = 0; i < mDataTitle.Count(); i++) {
                    int n = listData.FindIndex(p => p[0].CompareTo(mDataTitle[i]) == 0);
                    if (0 <= n)
                        data[i] = listData[n][1];
                    else if (mDataTitle[i].CompareTo("URL") == 0)
                        data[i] = url;
                }
            }
            return data;
        }

        /// <summary>
        /// おすすめルートのHTMLソースからデータを抽出
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>データリスト(タイトル,データ)</returns>
        private List<string[]> getListData(string html)
        {
            List<string[]> listData = new List<string[]>();
            string bodyData, title, tagData;

            //  head データ
            (string tagPara, string headData, string nextSrc) = ylib.getHtmlTagData(html, "head");
            //  body データ
            (tagPara, bodyData, nextSrc) = ylib.getHtmlTagData(nextSrc, "body");
            //  headタイトル
            (tagPara, title, nextSrc) = ylib.getHtmlTagData(headData, "title");

            string[] buf = new string[2];
            buf[0] = "タイトル";
            buf[1] = ylib.stripControlCode(ylib.stripHtmlTagData(ylib.getHtmlTagSrc(bodyData, "div", "id", "pagetitle"))).Trim();
            listData.Add(buf);
            buf = new string[2];
            buf[0] = "分類";
            buf[1] = getCategoryGuideRoute(bodyData).Trim();
            listData.Add(buf);
            buf = new string[2];
            buf[0] = "ルートメモ";
            buf[1] = ylib.stripControlCode(ylib.stripHtmlTagData(ylib.getHtmlTagSrc(bodyData, "div", "class", "route_note"))).Trim();
            listData.Add(buf);
            //  ルートデータ(ルート長、登り標高差、下り標高差、行程概要)
            string boxRouteData = ylib.getHtmlTagSrc(bodyData, "div", "class", "box route_data");
            List<string[]> subListData = getBoxGuideRouteData(boxRouteData);
            listData.AddRange(subListData);
            //  スキップ
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(html, "div", "class", "box");
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "div", "class", "box");
            // 詳細解説
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "div", "class", "box");
            //  表データ
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(tagData, "table");
            subListData = getTableGuideRouteList(tagData);
            listData.AddRange(subListData);
            // 登る山／通過する場所
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(bodyData, "div", "class", "box place");
            buf = new string[2];
            buf[0] = "登る山／通過する場所";
            buf[1] = "";
            subListData = getPlaceGuideRouteList(tagData);
            foreach (var place in subListData) {
                buf[1] += (0 < buf[1].Length ? mSeparatorChar.ToString() : "") + place[0] + " : " + place[1];
            }
            listData.Add(buf);

            return listData;
        }

        /// <summary>
        /// 分類(中級、日帰り、エリア)
        /// </summary>
        /// <param name="html">HTMLソース(bodyData)</param>
        /// <returns>抽出データ</returns>
        private string getCategoryGuideRoute(string html)
        {
            string buf = ylib.stripControlCode(ylib.stripHtmlTagData(ylib.getHtmlTagSrc(html, "span", "class", "label2 grade2"))).Trim();
            buf += " " + ylib.stripControlCode(ylib.stripHtmlTagData(ylib.getHtmlTagSrc(html, "span", "class", "type label2"))).Trim();
            buf += " " + ylib.stripControlCode(ylib.stripHtmlTagData(ylib.getHtmlTagSrc(html, "span", "class", "location label2"))).Trim();
            buf += " " + ylib.stripControlCode(ylib.stripHtmlTagData(ylib.getHtmlTagSrc(html, "span", "class", "label2 item"))).Trim();
            return buf;
        }

        /// <summary>
        /// ルートデータ(ルート長、登り標高差,下り標高差,行程概要)
        /// </summary>
        /// <param name="html">HTML(boxRouteData)</param>
        /// <returns></returns>
        private List<string[]> getBoxGuideRouteData(string html)
        {
            List<string[]> listData = new List<string[]>();
            string tagPara, tagData, inner, nextSrc;
            string[] buf = new string[2];
            //  ルートデータ
            (tagPara, inner, nextSrc) = ylib.getHtmlTagData(html, "div", "class", "inner");
            do {
                (tagPara, tagData, inner) = ylib.getHtmlTagData(inner, "div", "class", "value");
                List<string> routeData = ylib.getHtmlTagDataAll(tagData);
                if (routeData.Count == 0)
                    break;
                buf = new string[2];
                buf[0] = routeData[0];
                for (int i = 1; i < routeData.Count; i++)
                    buf[1] += " " + routeData[i].Trim();
                listData.Add(buf);
            } while (0 < tagPara.Length && 0 < inner.Length);
            //  行程概要
            buf = new string[2];
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "div");
            buf[0] = "行程概要";
            buf[1] = ylib.stripControlCode(ylib.stripHtmlTagData(tagData));
            buf[1] = buf[1].Replace("行程概要:", "").Trim();
            listData.Add(buf);

            return listData;
        }

        /// <summary>
        /// 表データ
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private List<string[]> getTableGuideRouteList(string table)
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
                buf[0] = ylib.stripControlCode(tagData).Trim();
                (tagPara, tagData, nextTag) = ylib.getHtmlTagData(ylib.cnvHtmlSpecialCode(nextTag), "td");
                buf[1] = ylib.stripHtmlTagData(ylib.stripControlCode(ylib.cnvHtmlSpecialCode(tagData))).Trim();
                listData.Add(buf);
            } while (0 < trData.Length && 0 < nextHtml.Length);

            return listData;
        }

        /// <summary>
        /// 登る山／通過する場所
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private List<string[]> getPlaceGuideRouteList(string table)
        {
            List<string[]> listData = new List<string[]>();
            string tagData, tagPara, liData, nextTag;
            string nextHtml = table;
            do {
                (tagPara, liData, nextHtml) = ylib.getHtmlTagData(nextHtml, "li");
                if (liData.Length <= 0)
                    break;
                string[] buf = new string[2];
                (tagPara, tagData, nextTag) = ylib.getHtmlTagData(liData, "a");
                string url = ylib.stripHtmlParaData(tagPara, "href");
                buf[0] = ylib.stripHtmlTagData(ylib.stripControlCode(ylib.cnvHtmlSpecialCode(tagData))).Trim();
                buf[1] = ylib.stripControlCode(url).Trim();
                listData.Add(buf);
            } while (0 < liData.Length && 0 < nextHtml.Length);

            return listData;
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
            mDataList = ylib.loadCsvData(mGuideRouteDataPath, mDataTitle);
        }

        /// <summary>
        /// データリストをファイルに保存
        /// </summary>
        public void saveData()
        {
            if (mDataList != null && 0 < mDataList.Count)
                ylib.saveCsvData(mGuideRouteDataPath, mDataTitle, mDataList);
        }
    }
}
