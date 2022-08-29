using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using WpfLib;

namespace MapApp
{
    public class YamaRecoData
    {
        private Encoding[] mEncoding = {
            Encoding.UTF8, Encoding.GetEncoding("shift_jis"), Encoding.GetEncoding("euc-jp")
        };
        private int mEncordType = 2;                                    //  EUC
        private string mAppFolder;                                      //  アプリフォルダ
        private string mDataSaveFolder = "YamaRecoData";                //  データフォルダ
        private string mYamaRecoDataPath = "YamaRecoData.csv";
        private string mYamaBaseUrl = "https://www.yamareco.com/modules/yamainfo/";
        private string mYamaRecoUrl = "https://www.yamareco.com/modules/yamainfo/ptinfo.php?ptid=";
        public string mSplitWord = " : ";                               //  分類データの分轄ワード
        public char mSeparatorChar = '\t';                              //  項目分轄char
        public string mYamaListFilter = "";
        public string[] mDataTitle = {                                  //  データのタイトル
            "山名", "標高", "座標", "種別", "概要", "分類", "登山口", "山小屋", "付近の山",
            "登山ルート", "おすすめルート", "URL"
        };
        public int[] mColWidth = {
            -1,     -1,      -1,     200,    300,    200,    200,      200,     300,
            300,           300,           -1
        };
        public bool[] mDispCol = {                                      //  表示カラムフラグ
            true,    true,   true,   true,   true,   true,   true,     true,      true,
            true,         true,            true
        };
        public bool[] mNumVal = {                                       //  数値データ判定
            false,   true,    false, false,  false,  false,  false,    false,      false,
            false,         false,            true
        };
        public bool[] mDetailCol = {                                    //  詳細簡略表示判定
            false,   false,   false, true,   false,  true,   true,     true,        true,
            true,          true,             false
        };
        public List<string[]> mDataList = new List<string[]>();         //  山データリスト
        public List<string[]> mDetailUrlList = new List<string[]>();    //  詳細データの(URL,項目)リスト

        public List<string[]> mCategoryList = new List<string[]>();     //  分類データリスト(分類名, URL))
        public List<string[]> mCategoryMapList = new List<string[]>();  //  分類ごとの(URL,山名)リスト

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public YamaRecoData()
        {
            mAppFolder = ylib.getAppFolderPath();                               //  アプリフォルダ
            mDataSaveFolder = Path.Combine(mAppFolder, mDataSaveFolder);        //  HTMLデータ保存フォルダ
            mYamaRecoDataPath = Path.Combine(mAppFolder, mYamaRecoDataPath);
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
            string url = mYamaRecoUrl + n.ToString();
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
            //  URLで登録済みチェック
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
        ///  山データから周辺情報データ(登山口,山小屋)をリスト化(URL,山名)
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>周辺情報データリスト(URL,山名)</returns>
        public List<string[]> getSelectUrlList(string url)
        {
            int n = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(url) == 0);
            return getDetailUrlList(mDataList[n]);
        }

        /// <summary>
        /// 山データから登山ルートデータをリスト化(URL,ルート名)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public List<string[]> getRouteSelectUrlList(string url)
        {
            int n = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(url) == 0);
            return getRouteUrlList(mDataList[n]);
        }

        /// <summary>
        /// 山データからおすすめルートデータをリスト化(URL,ルート名)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public List<string[]> getGuideSelectUrlList(string url)
        {
            int n = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(url) == 0);
            return getGuideUrlList(mDataList[n]);
        }

        /// <summary>
        /// 詳細表示用データの取得
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>(データ,タイトル)</returns>
        public (string, string) detailDisp(string url)
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
        /// <param name="category">分類</param>
        /// <param name="searchWord">検索ワード</param>
        /// <returns>データリスト</returns>
        public List<string[]> getFilterongDataLsit(string category = "", string searchWord = "")
        {
            List<string[]> dataList = new List<string[]>();
            (Point searchCoordinate, double searchDistance) = getSearchCoordinate(searchWord);
            int catgoryNo = mDataTitle.FindIndex(p => p.CompareTo("分類") == 0);
            int dispSize = mDispCol.Count(item => item == true);

            for (int i = 0; i < mDataList.Count; i++) {
                if ((category.Length == 0 || getCategoryChk(mDataList[i][catgoryNo], category)) &&
                    (searchWord.Length == 0 || searchDataChk(mDataList[i], searchWord, searchCoordinate, searchDistance))) {
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
        /// 指定のURLのWebデータから山データの山分類のダウンロードリストを作成
        /// </summary>
        /// <param name="url">WebデータのURL</param>
        public void getCategoryMapList(string url)
        {
            string tagPara, bodyData, nextSrc;
            mCategoryMapList.Clear();
            string html= getWebData(url);
            //  body データ
            (tagPara, bodyData, nextSrc) = ylib.getHtmlTagData(html, "body");
            mCategoryMapList = getMapList(bodyData);
        }

        /// <summary>
        /// 分類データをデータリストから抽出してリスト化する
        /// mYamaListFilter でフィルタを設定
        /// </summary>
        public void setCategoryList()
        {
            mCategoryList.Clear();
            if (mDataList != null && 0 < mDataList.Count) {
                foreach (var data in mDataList) {
                    string[] category = data[titleNo("分類")].Split(mSeparatorChar);     //  分類
                    foreach (var item in category) {
                        string[] itemData = new string[2];
                        int n = item.IndexOf(mSplitWord);
                        if (0 < n) {
                            itemData[0] = item.Substring(0, n).Trim();                      //  タイトル
                            itemData[1] = item.Substring(n + mSplitWord.Length).Trim();     //  URL
                            if (0 < itemData.Length && (mYamaListFilter.Length == 0 || 0 <= itemData[0].IndexOf(mYamaListFilter)) &&
                                0 > mCategoryList.FindIndex(p => p[0].CompareTo(itemData[0]) == 0)) {
                                mCategoryList.Add(itemData);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 詳細データリストから山データリストを抽出する
        /// </summary>
        /// <param name="listData">詳細データリスト(URL,山名他)</param>
        /// <returns>山データリスト</returns>
        public List<string[]> extractListdata(List<string[]> listData)
        {
            List<string[]> yamaListdata = new List<string[]>();
            if (mDataList != null && 0 < mDataList.Count) {
                for (int i = 0; i < listData.Count; i++) {
                    int n = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(listData[i][0]) == 0);
                    if (0 <= n)
                        yamaListdata.Add(mDataList[n]);
                }
            }

            return yamaListdata;
        }

        /// <summary>
        /// 座標検索ワードから座標値と範囲距離(km)を求める
        /// </summary>
        /// <param name="searchWord"></param>
        /// <returns>(座標,距離)</returns>
        public (Point, double) getSearchCoordinate(string searchWord)
        {
            Point searchCoordinate = new Point();
            double searchDistance = 10.0;
            if (0 < ylib.getCoordinatePattern(searchWord).Length) {
                //  検索が座標の場合
                searchCoordinate = ylib.cnvCoordinate(searchWord);
                int n = searchWord.IndexOf(' ');
                if (0 < n) {
                    searchDistance = ylib.string2double(searchWord.Substring(n));
                }
            }
            return (searchCoordinate, searchDistance);
        }

        /// <summary>
        /// リストデータから検索ワードの有無か座標検索をおこなう
        /// </summary>
        /// <param name="listData">リストデータ</param>
        /// <param name="searchWord">検索ワード</param>
        /// <param name="searchCoordinate">座標</param>
        /// <param name="searchDistance">範囲距離</param>
        /// <returns>有無</returns>
        public bool searchDataChk(string[] listData, string searchWord, Point searchCoordinate, double searchDistance)
        {
            if (searchCoordinate.isEmpty()) {
                //  ワード検索
                if (0 <= listData.FindIndex(p => 0 <= p.IndexOf(searchWord)))
                    return true;
            } else {
                //  座標検索(距離が指定範囲内)
                Point tpos = ylib.cnvCoordinate(listData[2]);
                if (!tpos.isEmpty()) {
                    double dis = ylib.coordinateDistance(searchCoordinate, tpos);
                    if (dis < searchDistance)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 山データから周辺情報データ(登山口,山小屋)を抽出しリスト化(URL,山名)
        /// </summary>
        /// <param name="yamaData">山データ</param>
        /// <returns>周辺情報データリスト(URL,山名)</returns>
        private List<string[]> getDetailUrlList(string[] yamaData)
        {
            List<string[]> listData = new List<string[]>();
            string[] buf = new string[2];
            buf[0] = yamaData[titleNo("URL")];
            buf[1] = yamaData[titleNo("山名")];
            listData.Add(buf);
            string[] text = yamaData[titleNo("登山口")].ToString().Split(mSeparatorChar);
            for (int j = 0; j < text.Length; j++) {
                buf = new string[2];
                int n = text[j].IndexOf(mSplitWord);
                if (0 < n) {
                    buf[1] = text[j].Substring(0, n).Trim();                    //  山名(登山口,山小屋)
                    buf[0] = text[j].Substring(n + mSplitWord.Length).Trim();   //  URL
                    listData.Add(buf);
                }
            }
            text = yamaData[titleNo("山小屋")].ToString().Split(mSeparatorChar);
            for (int j = 0; j < text.Length; j++) {
                buf = new string[2];
                int n = text[j].IndexOf(mSplitWord);
                if (0 < n) {
                    buf[1] = text[j].Substring(0, n).Trim();                    //  山名(登山口,山小屋)
                    buf[0] = text[j].Substring(n + mSplitWord.Length).Trim();   //  URL
                    listData.Add(buf);
                }
            }
            text = yamaData[titleNo("付近の山")].ToString().Split(mSeparatorChar);
            for (int j = 0; j < text.Length; j++) {
                buf = new string[2];
                int n = text[j].IndexOf(mSplitWord);
                if (0 < n) {
                    buf[1] = text[j].Substring(0, n).Trim();                    //  山名(付近の山)
                    buf[0] = text[j].Substring(n + mSplitWord.Length).Trim();   //  URL
                    listData.Add(buf);
                }
            }

            return listData;
        }

        /// <summary>
        /// 山データから登山ルートのURLリストを抽出しリスト化(URL,山名)
        /// </summary>
        /// <param name="yamaData">山データ</param>
        /// <returns>登山ルートURLリスト</returns>
        private List<string[]> getRouteUrlList(string[] yamaData)
        {
            List<string[]> listData = new List<string[]>();
            string[] buf = new string[2];
            string[] text = yamaData[titleNo("登山ルート")].ToString().Split(mSeparatorChar);
            for (int j = 0; j < text.Length; j++) {
                buf = new string[2];
                int n = text[j].IndexOf(mSplitWord);
                if (0 < n) {
                    buf[1] = text[j].Substring(0, n).Trim();                    //  登山ルート名
                    buf[0] = text[j].Substring(n + mSplitWord.Length).Trim();   //  URL
                    listData.Add(buf);
                }
            }

            return listData;
        }

        /// <summary>
        /// 山データからおすすめルートのURLリストを抽出しリスト化(URL,山名)
        /// </summary>
        /// <param name="yamaData">山データ</param>
        /// <returns>おすすめルートURLリスト</returns>
        private List<string[]> getGuideUrlList(string[] yamaData)
        {
            List<string[]> listData = new List<string[]>();
            string[] buf = new string[2];
            string[] text = yamaData[titleNo("おすすめルート")].ToString().Split(mSeparatorChar);
            for (int j = 0; j < text.Length; j++) {
                buf = new string[2];
                int n = text[j].IndexOf(mSplitWord);
                if (0 < n) {
                    buf[1] = text[j].Substring(0, n).Trim();                    //  おすすめルート名
                    buf[0] = text[j].Substring(n + mSplitWord.Length).Trim();   //  URL
                    listData.Add(buf);
                }
            }

            return listData;
        }

        /// <summary>
        /// HTMLソースからデータを抽出し配列に入れる
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string[] getListData(string html, string url)
        {
            List<string[]> listData = getListData(html);

            //  タイトルに合わせたデータ配列に置換える
            string[] data = Enumerable.Repeat<string>("", mDataTitle.Length).ToArray();
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
            string bodyData, title;
            string[] buf = new string[2];

            //  head データ
            (string tagPara, string headData, string nextSrc) = ylib.getHtmlTagData(html, "head");
            //  body データ
            (tagPara, bodyData, nextSrc) = ylib.getHtmlTagData(nextSrc, "body");
            //  headタイトル
            (tagPara, title, nextSrc) = ylib.getHtmlTagData(headData, "title");

            //  基本情報エリア
            string basicInfo = ylib.getHtmlTagSrc(bodyData, "div", "class", "basic_info_area mb20");
            string pageTitle = ylib.getHtmlTagSrc(bodyData, "div", "id", "pagetitle");
            buf = new string[2] { "山名", string.Join(",", ylib.getHtmlTagData(pageTitle)) };
            listData.Add(buf);
            //  座標と標高
            string basicInfoSeparator = string.Join("\n", ylib.getHtmlTagDataList(basicInfo, "table", "class", "basic_info"));
            (string ele, string coord) = seperateBasicInfo(basicInfoSeparator);
            buf = new string[2] { "標高", ele };
            listData.Add(buf);
            buf = new string[2] { "座標", coord };
            listData.Add(buf);
            //  種別(詳細)
            List<string[]> detail = getDetail(basicInfo);
            string detailText = "";
            foreach (string[] det in detail) {
                detailText += (0 < detailText.Length ? mSeparatorChar.ToString() : "") + det[0] + " : " + det[1];
            }
            buf = new string[2] { "種別", detailText };
            listData.Add(buf);
            //  概要(Wikpedia)
            string basicInfoText = string.Join("", ylib.getHtmlTagDataList(basicInfo, "div", "class", "basic_info_explain mytips"));
            basicInfoText += (basicInfoText.Length > 0 ? " " : "") + string.Join("", ylib.getHtmlTagDataList(basicInfo, "div", "class", "mb10"));
            buf = new string[2] { "概要", basicInfoText };
            listData.Add(buf);
            //  山のカテゴリ
            List<string[]> category = getCategory(bodyData);
            string catText = "";
            foreach (string[] cat in category) {
                catText += (0 < catText.Length ? mSeparatorChar.ToString() : "") + cat[1] + " : " + cat[0];
            }
            buf = new string[2] { "分類", catText };
            listData.Add(buf);
            //  登山口と山小屋
            string officialInfo = ylib.getHtmlTagSrc(html, "table", "class", "official-info");
            List<string[]> startMountainList = getStartMountain(officialInfo, "登山口");
            string startMountText = "";
            foreach (string[] start in startMountainList) {
                startMountText += (0 < startMountText.Length ? mSeparatorChar.ToString() : "") + start[1] + " : " + start[0];
            }
            buf = new string[2] { "登山口", startMountText };
            listData.Add(buf);
            List<string[]> hutList = getStartMountain(officialInfo, "周辺の山小屋");
            string hutText = "";
            foreach (string[] hut in hutList) {
                hutText += (0 < hutText.Length ? mSeparatorChar.ToString() : "") + hut[1] + " : " + hut[0];
            }
            buf = new string[2] { "山小屋", hutText };
            listData.Add(buf);

            List<string[]> nearMount = getNearMountain(bodyData);
            string nearMountText = "";
            foreach (string[] hut in nearMount) {
                nearMountText += (0 < nearMountText.Length ? mSeparatorChar.ToString() : "") + hut[1] + " : " + hut[0];
            }
            buf = new string[2] { "付近の山", nearMountText };
            listData.Add(buf);

            List<string[]> yamaRoute = getYamaRoute(bodyData);
            string yamaRoutetext = "";
            foreach (string[] hut in yamaRoute) {
                yamaRoutetext += (0 < yamaRoutetext.Length ? mSeparatorChar.ToString() : "") + hut[1] + " : " + hut[0];
            }
            buf = new string[2] { "登山ルート", yamaRoutetext };
            listData.Add(buf);

            List<string[]> guideRoute = getYamaGuideRoute(bodyData);
            string yamaGuideRoutetext = "";
            foreach (string[] hut in guideRoute) {
                yamaGuideRoutetext += (0 < yamaGuideRoutetext.Length ? mSeparatorChar.ToString() : "") + hut[1] + " : " + hut[0];
            }
            buf = new string[2] { "おすすめルート", yamaGuideRoutetext };
            listData.Add(buf);

            return listData;
        }

        /// <summary>
        /// [種別](詳細情報)の抽出
        /// </summary>
        /// <param name="html">HTML</param>
        /// <returns>リスト</returns>
        private List<string[]> getDetail(string html)
        {
            List<string[]> dataList = new List<string[]>();
            string tagPara, tagData, tagData2, nextHtml, nextHtml2;
            nextHtml = ylib.getHtmlTagSrc(html, "table", "class", "basic_info_detail");
            do {
                (tagPara, tagData, nextHtml) = ylib.getHtmlTagData(nextHtml, "tr");
                if (tagPara.Length == 0 && 0 < tagData.Length) {
                    (tagPara, tagData2, nextHtml2) = ylib.getHtmlTagData(tagData, "th");
                    if (0 < tagData2.Length) {
                        string[] paraData = new string[2];
                        paraData[0] = ylib.stripHtmlTagData(tagData2);
                        (tagPara, tagData2, nextHtml2) = ylib.getHtmlTagData(tagData, "td");
                        paraData[1] = ylib.getHtmlTagData(tagData2);
                        dataList.Add(paraData);
                    }
                }
            } while (0 < nextHtml.Length && 0 < tagData.Length);

            return dataList;
        }

        /// <summary>
        /// [登山口]や[山小屋]リスト取得
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <param name="title">検索対象タイトル</param>
        /// <returns></returns>
        private List<string[]> getStartMountain(string html, string title)
        {
            List<string[]> dataList = new List<string[]>();
            string nextHtml = html;
            string tagPara, trData;
            do {
                //  trの領域を取得
                (tagPara, trData, nextHtml) = ylib.getHtmlTagData(nextHtml, "tr");
                if (trData.Length <= 0)
                    break;
                //  th データ取得
                (int sp, int ep) = ylib.getHtmlTagDataPos(trData, "th", 0);
                if (sp < ep) {
                    string tagData = trData.Substring(sp, ep - sp + 1);
                    tagPara = ylib.getHtmlTagParaDataTitle(tagData, "class", "active");
                    string data = ylib.getHtmlTagData(tagData);
                    if (data.CompareTo(title) != 0)
                        continue;
                    //  a 参照データの取得
                    do {
                        (tagPara, tagData, trData) = ylib.getHtmlTagData(trData, "a");
                        if (0 < tagData.Length) {
                            string[] paraData = new string[2];
                            paraData[0] = ylib.stripHtmlParaData(tagPara, "href");
                            paraData[1] = ylib.cnvHtmlSpecialCode(tagData);
                            dataList.Add(paraData);
                        }
                    } while (0 < nextHtml.Length && 0 < tagData.Length);
                }
            } while (0 < nextHtml.Length);

            return dataList;
        }

        /// <summary>
        /// 付近の山
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private List<string[]> getNearMountain(string html)
        {
            List<string[]> listData = new List<string[]>();
            string tagPara, tagData, nextHtml;
            nextHtml = ylib.getHtmlTagSrc(html, "div", "class", "box neighbor");
            do {
                (tagPara, tagData, nextHtml) = ylib.getHtmlTagData(nextHtml, "a");
                if (0 < tagData.Length) {
                    string[] buf = new string[2];
                    buf[0] = ylib.stripHtmlParaData(tagPara, "href");
                    buf[1] = ylib.cnvHtmlSpecialCode(tagData);
                    listData.Add(buf);
                }
            } while (0 < nextHtml.Length && 0 < tagData.Length);

            return listData;
        }

        /// <summary>
        /// 山データの登山ルート抽出
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private List<string[]> getYamaRoute(string html)
        {
            List<string[]> listData = new List<string[]>();
            string tagPara, tagData, tagData2, nextSrc, nextTag;
            //  登山ルート
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(html, "div", "class", "route_box");
            nextSrc = tagData;
            do {
                (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "div", "class", "block");
                if (0 < tagData.Length) {
                    string[] buf = new string[2];
                    (tagPara, tagData2, nextTag) = ylib.getHtmlTagData(tagData, "a");
                    if (0 < tagPara.Length) {
                        buf[0] = ylib.stripHtmlParaData(tagPara, "href");
                    }
                    (tagPara, tagData2, nextTag) = ylib.getHtmlTagData(tagData, "img");
                    if (0 < tagPara.Length) {
                        buf[1] = ylib.stripHtmlParaData(tagPara, "alt");
                    }
                    if (0 < buf[0].Length && 0 < buf[1].Length)
                        listData.Add(buf);
                }
            } while (0 < tagData.Length && 0 < nextSrc.Length);

            return listData;
        }

        /// <summary>
        /// 山データのおすすめルート抽出
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private List<string[]> getYamaGuideRoute(string html)
        {
            List<string[]> listData = new List<string[]>();
            string tagPara, tagData, tagData2, nextSrc, nextTag;
            //  登山ルート
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(html, "div", "id", "route_guide");
            nextSrc = tagData;
            do {
                (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "li", "class", "route_list clearfix");
                (tagPara, tagData, nextTag) = ylib.getHtmlTagData(tagData, "div", "class", "route_title");
                if (0 < tagData.Length) {
                    string[] buf = new string[2];
                    (tagPara, tagData2, nextTag) = ylib.getHtmlTagData(tagData, "a");
                    if (0 < tagPara.Length && 0 < tagData2.Length) {
                        buf[0] = ylib.stripHtmlParaData(tagPara, "href");
                        buf[0] = mYamaBaseUrl + buf[0].Substring(buf[0].LastIndexOf('/') + 1);
                        buf[1] = ylib.stripHtmlTagData(tagData2);
                    }
                    if (0 < buf[0].Length && 0 < buf[1].Length)
                        listData.Add(buf);
                }
            } while (0 < tagData.Length && 0 < nextSrc.Length);

            return listData;
        }

        /// <summary>
        /// [分類](山のカテゴリ)リストの抽出
        /// 山分類リスト(日本百名山,花の百名山....)
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>リスト(URL,分類タイトル)</returns>
        private List<string[]> getCategory(string html)
        {
            List<string[]> dataList = new List<string[]>();
            string tagPara, tagData, nextHtml;
            nextHtml = ylib.getHtmlTagSrc(html, "div", "id", "ptinfo_category_links");
            do {
                (tagPara, tagData, nextHtml) = ylib.getHtmlTagData(nextHtml, "a");
                if (0 < tagData.Length) {
                    string[] paraData = new string[2];
                    paraData[0] = ylib.stripHtmlParaData(tagPara, "href");
                    paraData[1] = tagData;
                    dataList.Add(paraData);
                }
            } while (0 < nextHtml.Length && 0 < tagData.Length);

            return dataList;
        }

        /// <summary>
        /// [分類]山のカテゴリリストの取得(ダウンロード用リスト)
        /// </summary>
        /// <param name="html">山名リストHTMLソース</param>
        /// <returns>(URL,山名)</returns>
        private List<string[]> getMapList(string html)
        {
            List<string[]> dataList = new List<string[]>();
            string tagPara, tagData, nextHtml, nextHtml2;
            nextHtml = ylib.getHtmlTagSrc(html, "table", "class", "reset table tr_hover ptlist");
            do {
                (tagPara, tagData, nextHtml) = ylib.getHtmlTagData(nextHtml, "tr");
                if (tagPara.Length == 0 && 0 < tagData.Length) {
                    (tagPara, tagData, nextHtml2) = ylib.getHtmlTagData(tagData, "a");
                    if (0 < tagData.Length) {
                        string[] paraData = new string[2];
                        paraData[0] = mYamaBaseUrl + ylib.stripHtmlParaData(tagPara, "href");  //  URL
                        paraData[1] = tagData;                                  //  山名
                        dataList.Add(paraData);
                    }
                }
            } while (0 < nextHtml.Length && 0 < tagData.Length);

            return dataList;
        }

        /// <summary>
        /// 指定の分類に該当するかを確認
        /// </summary>
        /// <param name="categoryData">分類データ</param>
        /// <param name="category">分類名</param>
        /// <returns>有無</returns>
        private bool getCategoryChk(string categoryData, string category)
        {
            string[] categorys = categoryData.Split(mSeparatorChar);
            foreach (var item in categorys) {
                int n = item.IndexOf(mSplitWord);
                if (0 <= n) {
                    string itemName = item.Substring(0, n);
                    if (itemName.CompareTo(category) == 0)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 標高と座標データを抽出
        /// </summary>
        /// <param name="basicInfo"></param>
        /// <returns>(標高,座標)</returns>
        private (string, string) seperateBasicInfo(string basicInfo)
        {
            string elevate = "", coordinatee = "";
            int sp = basicInfo.IndexOf("標高");
            int ep = basicInfo.IndexOf("m");
            if (0 <= sp && 0 <= ep) {
                elevate = basicInfo.Substring(sp + 3, ep - sp - 2);
            }
            sp = basicInfo.IndexOf("北緯");
            ep = basicInfo.IndexOf("東経");
            if (0 <= sp && 0 <= ep) {
                ep = basicInfo.IndexOf("秒", ep);
                if (0 <= ep)
                    coordinatee = basicInfo.Substring(sp, ep - sp + 1);
            }
            return (elevate, coordinatee);
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
            mDataList = ylib.loadCsvData(mYamaRecoDataPath, mDataTitle);
        }

        /// <summary>
        /// データリストをファイルに保存
        /// </summary>
        public void saveData()
        {
            if (mDataList != null && 0 < mDataList.Count)
                ylib.saveCsvData(mYamaRecoDataPath, mDataTitle, mDataList);
        }

    }
}
