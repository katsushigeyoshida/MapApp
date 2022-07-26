using System;
using System.Collections.Generic;
using System.IO;
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
        private int mEncordType = 2;        //  EUC
        private string mAppFolder;          //  アプリフォルダ
        private string mDataSaveFolder = "YamaRecoData";
        private string mYamaRecoDataPath = "YamaRecoData.csv";
        private string mYamaRecoUrl = "https://www.yamareco.com/modules/yamainfo/ptinfo.php?ptid=";
        public string mSplitWord = " : ";              //  分類データの分轄ワード

        public string[] mDataTitle = {
            "山名", "標高", "座標", "種別", "概要", "URL", "分類", "登山口", "山小屋", "付近の山"
        };
        public List<string[]> mDataList = new List<string[]>();         //  山データリスト
        public List<string[]> mCategoryList = new List<string[]>();     //  分類データリスト(分類名, URL))
        public List<string[]> mCategoryMapList = new List<string[]>();  //  分類ごとの(URL,山名)リスト
        public List<string[]> mDetailUrlList = new List<string[]>();    //  詳細データの(URL,項目)リスト

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
            //  URLで登録済みチェック
            if (mDataList.FindIndex(p => p[5].CompareTo(url) == 0) < 0) {
                string html = getWebData(url);
                string[] buf = getListData(html, url);
                mDataList.Add(buf);
            }
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
        /// 指定のURLのWebデータから山データのリストを作成
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
        /// 詳細データリストから山データリストを抽出する
        /// </summary>
        /// <param name="listData">詳細データリスト(URL,山名他)</param>
        /// <returns>山データリスト</returns>
        public List<string[]> extractListdata(List<string[]> listData)
        {
            List<string[]> yamaListdata = new List<string[]>();
            for (int i = 0; i < listData.Count; i++) {
                int n = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(listData[i][0]) == 0);
                if (0 <= n)
                    yamaListdata.Add(mDataList[n]);
            }

            return yamaListdata;
        }

        /// <summary>
        /// 山データから周辺情報データ(登山口,山小屋)をリスト化(URL,山名)
        /// </summary>
        /// <param name="yamaData">山データ</param>
        /// <returns>周辺情報データリスト(URL,山名)</returns>
        public List<string[]> getDetailUrlList(string[] yamaData)
        {
            List<string[]> listData = new List<string[]>();
            string[] buf = new string[2];
            buf[0] = yamaData[titleNo("URL")];
            buf[1] = yamaData[titleNo("山名")];
            listData.Add(buf);
            string[] text = yamaData[titleNo("登山口")].ToString().Split(',');
            for (int j = 0; j < text.Length; j++) {
                buf = new string[2];
                int n = text[j].IndexOf(mSplitWord);
                if (0 < n) {
                    buf[1] = text[j].Substring(0, n).Trim();                    //  山名(登山口,山小屋)
                    buf[0] = text[j].Substring(n + mSplitWord.Length).Trim();   //  URL
                    listData.Add(buf);
                }
            }
            text = yamaData[titleNo("山小屋")].ToString().Split(',');
            for (int j = 0; j < text.Length; j++) {
                buf = new string[2];
                int n = text[j].IndexOf(mSplitWord);
                if (0 < n) {
                    buf[1] = text[j].Substring(0, n).Trim();                    //  山名(登山口,山小屋)
                    buf[0] = text[j].Substring(n + mSplitWord.Length).Trim();   //  URL
                    listData.Add(buf);
                }
            }
            text = yamaData[titleNo("付近の山")].ToString().Split(',');
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
        /// HTMLソースから山データを抽出し配列に入れる
        /// [0]山名,[1]標高,[2]座標,[3]種別,[4]概要,[5]URL,[6]分類,[7]登山口,[8]山小屋
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>データ配列</returns>
        private string[] getListData(string html, string url)
        {
            string[] data = new string[mDataTitle.Length];
            string bodyData, title;
            //  head データ
            (string tagPara, string headData, string nextSrc) = ylib.getHtmlTagData(html, "head");
            //  body データ
            (tagPara, bodyData, nextSrc) = ylib.getHtmlTagData(nextSrc, "body");
            //  headタイトル
            (tagPara, title, nextSrc) = ylib.getHtmlTagData(headData, "title");

            //  基本情報エリア(basic_info_area mb20)
            string basicInfo = ylib.getHtmlTagSrc(bodyData, "div", "class", "basic_info_area mb20");
            //  山名(pagetitle)
            string pageTitle = ylib.getHtmlTagSrc(bodyData, "div", "id", "pagetitle");
            data[titleNo("山名")] = string.Join(",", ylib.getHtmlTagData(pageTitle));    //  山名
            //  基本情報(basic_info)
            string basicInfoSeparator = string.Join("\n", ylib.getHtmlTagData(basicInfo, "table", "class", "basic_info"));
            //  座標と標高
            (data[titleNo("標高")], data[titleNo("座標")]) = seperateBasicInfo(basicInfoSeparator);
            //  詳細情報(basic_info_detail) 分類
            List<string[]> detail = getDetail(basicInfo);
            List<string> buf = new List<string>();
            foreach (string[] det in detail) {
                buf.Add(det[0] + " : " + det[1]);
            }
            data[titleNo("種別")] = string.Join("\t", buf);
            //  概要(個人登録とWikpedia)(basic_info_explain mytips, pt_wiki mb10)
            data[titleNo("概要")] = string.Join("", ylib.getHtmlTagData(basicInfo, "div", "class", "basic_info_explain mytips"));
            data[titleNo("概要")] += (data[3].Length > 0 ? " " : "") + string.Join("", ylib.getHtmlTagData(basicInfo, "div", "class", "mb10"));
            //  URL
            data[titleNo("URL")] = url;
            //  山のカテゴリ
            List<string[]> category = getCategory(bodyData);
            buf = new List<string>();
            foreach (string[] cat in category) {
                buf.Add(cat[1] + mSplitWord + cat[0]);
            }
            data[titleNo("分類")] = string.Join(",", buf);
            //  登山口と山小屋
            string officialInfo = ylib.getHtmlTagSrc(html, "table", "class", "official-info");
            List<string[]> startMountainList = getStartMountain(officialInfo, "登山口");
            buf = new List<string>();
            foreach (string[] start in startMountainList) {
                buf.Add(start[1] + mSplitWord + start[0]);
            }
            data[titleNo("登山口")] = string.Join(",", buf);
            List<string[]> hutList = getStartMountain(officialInfo, "周辺の山小屋");
            buf = new List<string>();
            foreach (string[] hut in hutList) {
                buf.Add(hut[1] + mSplitWord + hut[0]);
            }
            data[titleNo("山小屋")] = string.Join(",", buf);
            //  付近の山
            List<string[]> nearMount = getNearMountain(bodyData);
            buf = new List<string>();
            foreach (string[] near in nearMount) {
                buf.Add(near[1] + mSplitWord + near[0]);
            }
            data[titleNo("付近の山")] = string.Join(",", buf);

            return data;
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
        /// [分類](山のカテゴリ)リストの抽出
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>リスト</returns>
        public List<string[]> getCategory(string html)
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
                            paraData[1] = tagData;
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
                    buf[1] = tagData;
                    listData.Add(buf);
                }
            } while (0 < nextHtml.Length && 0 < tagData.Length);

            return listData;
        }

        /// <summary>
        /// [分類]山のカテゴリリストの取得
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
                        paraData[0] = ylib.stripHtmlParaData(tagPara, "href");  //  URL
                        paraData[1] = tagData;                                  //  山名
                        dataList.Add(paraData);
                    }
                }
            } while (0 < nextHtml.Length && 0 < tagData.Length);

            return dataList;
        }

        /// <summary>
        /// [分類]と検索ワードでフィルタリングしたデータリストを作成
        /// </summary>
        /// <param name="category">分類</param>
        /// <param name="searchWord">検索ワード</param>
        /// <param name="dispSize">取得サイズ</param>
        /// <returns>データリスト</returns>
        public List<string[]> getFilterongDataLsit(string category = "", string searchWord = "", int dispSize = 0)
        {
            List<string[]> dataList = new List<string[]>();
            (Point searchCoordinate, double searchDistance) = getSearchCoordinate(searchWord);
            int catgoryNo = mDataTitle.FindIndex(p => p.CompareTo("分類") == 0);

            for (int i = 0; i < mDataList.Count; i++) {
                if ((category.Length == 0 || getCategoryChk(mDataList[i][catgoryNo], category)) &&
                    (searchWord.Length == 0 || searchDataChk(mDataList[i], searchWord, searchCoordinate, searchDistance))) {
                    string[] buf = new string[dispSize <= 0 ? mDataList[0].Length : Math.Min(mDataList[i].Length, dispSize)];
                    for (int j = 0; j < buf.Length; j++) {
                        buf[j] = mDataList[i][j].Substring(0, Math.Min(mDataList[i][j].Length, 100));
                    }
                    dataList.Add(buf);
                }
            }
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
            string[] categorys = categoryData.Split(',');
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
        /// 分類データをデータリストから抽出してリスト化する
        /// </summary>
        public void setCategoryList()
        {
            mCategoryList.Clear();
            foreach (var data in mDataList) {
                string[] category = data[6].Split(',');     //  分類
                foreach (var item in category) {
                    string[] itemData = new string[2];
                    int n = item.IndexOf(mSplitWord);
                    if (0 < n) {
                        itemData[0] = item.Substring(0, n).Trim();
                        itemData[1] = item.Substring(n + mSplitWord.Length).Trim();
                        if (0 < itemData.Length && 0 > mCategoryList.FindIndex(p => p[0].CompareTo(itemData[0]) == 0)) {
                            mCategoryList.Add(itemData);
                        }
                    }
                }
            }
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
            ylib.saveCsvData(mYamaRecoDataPath, mDataTitle, mDataList);
        }

    }
}
