using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using WpfLib;

namespace MapApp
{

    public class WikiDataList
    {
        public List<WikiData> mDataList = new List<WikiData>(); //  詳細データリスト
        public List<string> mFormatTitle = new List<string>();  //  固定データを除くデータのタイトル

        private int mTagCount = 20;     //  最大表示項目数
        public enum SEARCHFORM { 
            NON, LISTLIMIT, LISTUNLIMIT, TABLE, TABLE2, GROUP, REFERENCELIMIT, REFERENCE,
            TABLE_LIST, TABLE2_LIST
        };
        public SEARCHFORM mSearchForm = SEARCHFORM.NON;
        public string[] mSearchFormTitle = { 
            "自動", "箇条書き,制限あり", "箇条書き", "表形式", "表形式2", "グループ形式",
            "参照,制限あり", "参照", "表・箇条書き", "表2・箇条書き" };
        private string[] mStopTagData = new string[] {          //  一覧ページの読込中断キーワード
            "脚注", "References", "関連項目", "参考文献", "外部リンク" };

        YLib ylib = new YLib();

        public WikiDataList()
        {
            ylib.mRegexOption = RegexOptions.None;
        }

        /// <summary>
        /// リストデータのタイトル取得
        /// ファイル保存用と表示用でタイトル順をかえている
        /// </summary>
        /// <param name="dispOrder">表示時のタイトル</param>
        /// <returns></returns>
        public string[] getFormatTitleData(bool dispOrder = true)
        {
            int offset = 2;
            string[] formatTitle = new string[WikiData.mDefaultTitle.Length + mTagCount];
            formatTitle[0] = WikiData.mDefaultTitle[0]; // "タイトル";
            formatTitle[1] = WikiData.mDefaultTitle[1]; // "コメント";
            if (!dispOrder) {
                //  ファイルに保存する時の優先
                formatTitle[2] = WikiData.mDefaultTitle[2]; //  "URL";
                formatTitle[3] = WikiData.mDefaultTitle[3]; //  "親リストタイトル";
                formatTitle[4] = WikiData.mDefaultTitle[4]; //  "親リストURL";
                formatTitle[5] = WikiData.mDefaultTitle[5]; //  "一覧抽出方法";
                offset = 6;
            }
            int i = 0;
            for ( ; i < mTagCount; i++) {
                if (i < mFormatTitle.Count)
                    formatTitle[i + offset] = mFormatTitle[i];
                else
                    formatTitle[i + offset] = "Hidden";
            }
            if (dispOrder) {
                //  表示の時
                formatTitle[  i + offset] = WikiData.mDefaultTitle[2];  //  "URL";
                formatTitle[++i + offset] = WikiData.mDefaultTitle[3];  //  "親リストタイトル";
                formatTitle[++i + offset] = WikiData.mDefaultTitle[4];  //  "親リストURL";
                formatTitle[++i + offset] = WikiData.mDefaultTitle[5];  //  "一覧抽出方法";
            }
            return formatTitle;
        }

        /// <summary>
        /// リストヘッダ用のタイトル設定(ファイルから取得したデータ)
        /// </summary>
        /// <param name="data">タイトルデータ</param>
        public void setFormatTitleData(string[] data)
        {
            if (6 <= data.Length) {
                if (data[0].CompareTo(WikiData.mDefaultTitle[0]) == 0 &&
                    data[1].CompareTo(WikiData.mDefaultTitle[1]) == 0) {
                    mFormatTitle.Clear();
                    for (int i = WikiData.mDefaultTitle.Length; i < data.Length; i++) {
                        if (data[i].CompareTo("Hidden") == 0)
                            break;
                        mFormatTitle.Add(data[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Web取得した基本情報から表示項目をタイトルとして抽出
        /// </summary>
        public void getWebFormatTitle()
        {
            if (mDataList != null) {
                //webFormatTitleNpnSort();
                webFortmatTitleSort();
            }
        }

        /// <summary>
        /// 詳細データのタイトルをデータ数順にソートする
        /// </summary>
        private void webFortmatTitleSort()
        {
            //  取得したデータのタイトルと出現数を取得
            Dictionary<string, int> titleCount = new Dictionary<string, int>();
            foreach (WikiData data in mDataList) {
                if (data.mTagSetData != null) {
                    foreach (string[] tagData in data.mTagSetData) {
                        if (titleCount.ContainsKey(tagData[0])) {
                            titleCount[tagData[0]]++;
                        } else {
                            titleCount.Add(tagData[0], 1);
                        }
                    }
                }
            }
            //  タイトルを数でソートしてmFormatTitleにコピー
            mFormatTitle.Clear();
            mFormatTitle.Add("座標");
            foreach (var keyVal in titleCount.OrderByDescending(c => c.Value)) {
                if (keyVal.Key.CompareTo("座標") != 0) {
                    mFormatTitle.Add(keyVal.Key);
                }
            }
        }

        /// <summary>
        /// 基本情報の項目データをタイトルに合わせて設定
        /// </summary>
        public void setWikiInfoData()
        {
            foreach (WikiData data in mDataList) {
                data.setInfoData(mFormatTitle);
            }
        }

        /// <summary>
        /// Wikipediaの一覧リストの取得
        /// </summary>
        /// <param name="listTitle">一覧里レストのタイトル</param>
        /// <param name="url">一覧リストのURL</param>
        /// <returns>一覧リスト</returns>
        public void getWikiDataList(string listTitle, string url)
        {
            SEARCHFORM searchForm = SEARCHFORM.NON;
            mDataList.Clear();
            mFormatTitle.Clear();
            if (url.Length <= 0)
                return;
            string baseUrl = url.Substring(0, url.IndexOf('/', 10));
            string html = ylib.getWebText(url);
            if (html != null) {
                //  箇条書き,制限あり
                if (mSearchForm == SEARCHFORM.LISTLIMIT || mSearchForm == SEARCHFORM.NON) {
                    searchForm = SEARCHFORM.LISTLIMIT;
                    mDataList = getWikiDataListItem(html, baseUrl, listTitle, url, "li", searchForm);
                }
                //  表形式
                if (mSearchForm == SEARCHFORM.TABLE || (mSearchForm == SEARCHFORM.NON && mDataList.Count < 25)) {
                    searchForm = SEARCHFORM.TABLE;
                    mDataList = getWikiDataListItem(html, baseUrl, listTitle, url, "tr", searchForm, false);
                }
                //  グループ形式
                if (mSearchForm == SEARCHFORM.GROUP|| (mSearchForm == SEARCHFORM.NON && mDataList.Count < 25)) {
                    searchForm = SEARCHFORM.GROUP;
                    mDataList = getWikiDataListItem(html, baseUrl, listTitle, url, "span", searchForm, false);
                }
                // 箇条書き,制限なし
                if (mSearchForm == SEARCHFORM.LISTUNLIMIT || (mSearchForm == SEARCHFORM.NON && mDataList.Count < 25)) {
                    searchForm = SEARCHFORM.LISTUNLIMIT;
                    mDataList = getWikiDataListItem(html, baseUrl, listTitle, url, "li", searchForm, false);
                }
                // 参照,制限あり
                if (mSearchForm == SEARCHFORM.REFERENCELIMIT || (mSearchForm == SEARCHFORM.NON && mDataList.Count < 25)) {
                    searchForm = SEARCHFORM.REFERENCELIMIT;
                    mDataList = getWikiDataListItem(html, baseUrl, listTitle, url, "a", searchForm, true);
                }
                // 参照,制限なし
                if (mSearchForm == SEARCHFORM.REFERENCE || (mSearchForm == SEARCHFORM.NON && mDataList.Count < 25)) {
                    searchForm = SEARCHFORM.REFERENCE;
                    mDataList = getWikiDataListItem(html, baseUrl, listTitle, url, "a", searchForm, false);
                }
                //  表＋箇条書き
                if (mSearchForm == SEARCHFORM.TABLE_LIST || (mSearchForm == SEARCHFORM.NON && mDataList.Count < 25)) {
                    searchForm = SEARCHFORM.TABLE_LIST;
                    mDataList = getWikiDataListItem(html, baseUrl, listTitle, url, "tr", searchForm);    //  表形式
                    mDataList.AddRange(getWikiDataListItem(html, baseUrl, listTitle, url, "li", searchForm));   //  箇条書き,制限あり
                }
                //  表形式2
                if (mSearchForm == SEARCHFORM.TABLE2 || (mSearchForm == SEARCHFORM.NON && mDataList.Count < 25)) {
                    searchForm = SEARCHFORM.TABLE2;
                    mDataList = getWikiDataListItem(html, baseUrl, listTitle, url, "tr", searchForm, false, true);
                }
                //  表2＋箇条書き
                if (mSearchForm == SEARCHFORM.TABLE2_LIST || (mSearchForm == SEARCHFORM.NON && mDataList.Count < 25)) {
                    searchForm = SEARCHFORM.TABLE2_LIST;
                    mDataList = getWikiDataListItem(html, baseUrl, listTitle, url, "tr", searchForm, false, true);    //  表形式2
                    mDataList.AddRange(getWikiDataListItem(html, baseUrl, listTitle, url, "li", searchForm));   //  箇条書き,制限あり
                }
                mSearchForm = searchForm;
            }
        }


        /// <summary>
        /// 箇条書きや表形式などのデータからリストを抽出
        /// フィルタリングタグ li : 箇条書き, tr : 表形式, span : グループ形式, a : 参照のみ
        /// 検索制限　HTMLソース中に"脚注", "References", "関連項目", "参考文献"のキーワードが
        ///           検出されたところでリストの抽出を辞める
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <param name="baseUrl">基準URL</param>
        /// <param name="listTitle">一覧リストタイトル</param>
        /// <param name="url">一覧リストURL</param>
        /// <param name="filterTag">フィルタリングタグ(抽出方法)</param>
        /// <param name="searchForm">一覧抽出方法</param>
        /// <param name="limitOn">検索制限を有無</param>
        /// <param name="secondTitle">表の2列目をタイトルとする</param>
        /// <returns>一覧リスト</returns>
        private List<WikiData> getWikiDataListItem(string html, string baseUrl, string listTitle, string listUrl,
                                string filterTag, SEARCHFORM searchForm, bool limitOn = true, bool secondTitle = false)
        {
            //  検索終了位置を求める
            int limitPos = limitOn ? limitDataPosition(html) : -1;
            //  Wikiデータの検索
            List<WikiData> dataList;
            if (filterTag.CompareTo("tr") == 0) {
                int pos = 0;
                int sp, ep;
                string tagPara, tagData;
                dataList = new List<WikiData>();
                do {
                    (tagPara, tagData, html, sp, ep) = ylib.getHtmlTagData(html, "tbody", pos);
                    if (0 < tagData.Length)
                        dataList.AddRange(getWikiDataItem(baseUrl, tagData, filterTag, limitPos, secondTitle));
                    else
                        break;
                } while (0 < html.Length);
            } else {
                dataList = getWikiDataItem(baseUrl, html, filterTag, limitPos, secondTitle);
            }
            //  一覧リストのタイトル、URL、抽出方法を追加
            for (int i = 0; i < dataList.Count; i++)
                dataList[i].setBaseData(listTitle, listUrl, mSearchFormTitle[(int)searchForm]);
            return dataList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUrl">基準URL</param>
        /// <param name="html">HTMLソース</param>
        /// <param name="filterTag">抽出タグ名</param>
        /// <param name="limitPos">抽出打ち切り位置</param>
        /// <param name="secondTitle">表の抽出タイトルを2列目にする</param>
        /// <returns>一覧リスト</returns>
        private List<WikiData> getWikiDataItem(string baseUrl, string html, string filterTag, int limitPos, bool secondTitle = false)
        {
            List<WikiData> dataList = new List<WikiData>();
            int pos = 0;
            int sp, ep;
            string tagPara, tagData, nextSrc;
            int row = secondTitle ? -2 : 0;

            while ((limitPos < 0 || pos < limitPos) && pos < html.Length) {
                (tagPara, tagData, nextSrc, sp, ep) = ylib.getHtmlTagData(html, filterTag, pos);
                if (0 < tagData.Length && sp < ep) {
                    if ((filterTag.CompareTo("li") == 0 || filterTag.CompareTo("tr") == 0) &&
                            (0 <= tagData.IndexOf("<" + filterTag + ">") || 0 <= tagData.IndexOf("<" + filterTag + " "))) {
                        listSetTagData(dataList, tagData, tagPara, filterTag, baseUrl, ref row);
                        //  入れ子データを再帰処理
                        dataList.AddRange(getWikiDataItem(baseUrl, tagData, filterTag, limitPos, secondTitle));
                    } else {
                        listSetTagData(dataList, tagData, tagPara, filterTag, baseUrl, ref row);
                    }
                } else
                    break;
                pos = ep + 1;
            }
            return dataList;
        }

        /// <summary>
        /// データをリストに登録
        /// </summary>
        /// <param name="dataList">登録リスト</param>
        /// <param name="tagData">対象タグデータ</param>
        /// <param name="tagPara">タグのパラメータ</param>
        /// <param name="filterTag">フィルタのタグ名</param>
        /// <param name="baseUrl">基準URL</param>
        /// <param name="secondTitle">タイトル列(0: 自動　0<n: n列目 n<0: 1行目の参照n列目</param>
        private void listSetTagData(List<WikiData> dataList, string tagData, string tagPara,
                                    string filterTag, string baseUrl, ref int row)
        {
            tagData = ylib.stripHtmlTagData(tagData, "style");
            tagData = ylib.stripHtmlTagData(tagData, "rb");             //  ルビもとを除外、ただしルビ(rt)は残す)
            if (filterTag.CompareTo("a") == 0) {
                //  参照のみに対応(日本の港湾一覧)
                if (0 < tagPara.Length) {
                    string title = ylib.stripHtmlParaData(tagPara, "title");
                    string urlAddress = Uri.UnescapeDataString(baseUrl + ylib.stripHtmlParaData(tagPara, "href"));
                    string comment = tagData;
                    if (0 < comment.Length)
                        comment = "[" + comment + "]";
                    if (0 < title.Length)
                        dataList.Add(new WikiData(title, comment, urlAddress, "", "", ""));
                }
            } else if (filterTag.CompareTo("tr") == 0) {
                //  表形式
                WikiData wikiData = getTableWikiData(tagData, baseUrl, ref row);
                if (wikiData != null)
                    dataList.Add(wikiData);
            } else {
                //  箇条書き、その他
                string title = ylib.stripHtmlParaData(tagData, "title");    //  タイトルのあるタグを検出
                if (0 < title.Length) {
                    string paraData = ylib.getHtmlTagParaDataTitle(tagData, "title");   //  検出したタイトルのタグを取得
                    string urlAddress = Uri.UnescapeDataString(baseUrl + ylib.stripHtmlParaData(paraData, "href"));
                    string comment = string.Join("", ylib.getHtmlTagDataAll(tagData));  //  表の行全体のコメントを抽出
                    if (0 < comment.Length)
                        comment = "[" + comment + "]";
                    dataList.Add(new WikiData(title, comment, urlAddress, "", "", ""));
                }
            }
        }

        /// <summary>
        /// 表形式<tr >のタグの処理
        /// 対象列 row = n : n列目確定　0 : 自動  -n : 1行目の参照n列目   
        /// </summary>
        /// <param name="html">HTMLソース(1行分のデータ)</param>
        /// <param name="baseUrl">基準URL</param>
        /// <param name="row">対象列</param>
        /// <returns>WikiData</returns>
        private WikiData getTableWikiData(string html, string baseUrl, ref int row)
        {
            int pos = 0;
            int sp = 0;
            int ep = -1;
            int rowCount = 0;                   //  列数
            int refCount = 0;                   //  参照タグの出現列数
            string tagPara, tagData, nextSrc;
            string title = "";
            string urlAddress = "";
            html = html.Replace("\n", "");

            while (0 <= sp && sp != ep) {
                (tagPara, tagData, nextSrc, sp, ep) = ylib.getHtmlTagData(html, "td", pos); //  列データ
                if (tagData.Length == 0)
                    break;
                string caption = ylib.stripHtmlParaData(tagData, "title");  //  参照データ(<a)の有無
                if (0 < caption.Length)
                    refCount++;
                if ((row == 0 && 0 < refCount) ||
                    (row < 0 && refCount == -row) ||
                    (row > 0 && rowCount == row)) {
                    title = ylib.getHtmlTagData(tagData, tagData.IndexOf("<a "));
                    urlAddress = Uri.UnescapeDataString(baseUrl + ylib.stripHtmlParaData(tagData, "href"));
                    if (row < 0)
                        row = rowCount;
                    break;
                }
                pos = ep + 1;
                rowCount++;
            }
            if (0 < title.Length) {
                string comment = string.Join("", ylib.getHtmlTagDataAll(html));
                if (0 < comment.Length)
                    comment = "[" + comment + "]";
                return new WikiData(title, comment, urlAddress, "", "", "");
            }  else
                return null;
        }

        /// <summary>
        /// 検索終了位置を決める
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>終了位置</returns>
        private int limitDataPosition(string html)
        {
            int endPos = -1;
            foreach (string data in mStopTagData) {
                int p = html.LastIndexOf("id=\"" + data);
                if (endPos < 0) {
                    endPos = p;
                } else {
                    if (p < 0)
                        p = html.LastIndexOf("#" + data);
                    if (0 <= p)
                        endPos = Math.Min(endPos, p);
                }
            }
            int footerPos = html.LastIndexOf("<div class=\"printfooter");
            int navboxPos = html.LastIndexOf("<div class=\"navbox");
            if (endPos < 0) {
                if (0 < footerPos)
                    endPos = footerPos;
                if (0 < navboxPos)
                    endPos = endPos < navboxPos ? endPos : navboxPos;
            } else {
                if (0 < footerPos)
                    endPos = endPos < footerPos ? endPos : footerPos;
                if (0 < navboxPos)
                    endPos = endPos < navboxPos ? endPos : navboxPos;
            }
            return endPos;
        }

        /// <summary>
        /// データの検索(前検索)
        /// タイトルとコメント列から検索
        /// </summary>
        /// <param name="searchText">検索ワード</param>
        /// <param name="order">検索方向 昇順/降順</param>
        /// <returns>検索位置</returns>
        public int searchData(string searchText, int searchIndex, bool order)
        {
            if (mDataList == null || mDataList.Count < 1)
                return -1;

            Point searchCoordinate = new Point();
            double searchDistance = 20.0;
            int coordinatePos = -1;
            if (0 < ylib.getCoordinatePattern(searchText).Length) {
                //  検索が座標の場合
                searchCoordinate = ylib.cnvCoordinate(searchText);
                int n = searchText.IndexOf(' ');
                if (0 < n) {
                    searchDistance = ylib.string2double(searchText.Substring(n));
                    coordinatePos = getFormatTitleData().FindIndex("座標");
                }
            }

            for (int i = order ? Math.Max(searchIndex + 1, 0) : Math.Min(searchIndex - 1, mDataList.Count - 1);
                    order ? i < mDataList.Count : 0 <= i; i += order ? 1 : -1) {
                if (searchCoordinate.X == 0 && searchCoordinate.Y == 0) {
                    //  通常の検索
                    if (0 <= mDataList[i].mTitle.IndexOf(searchText) ||
                        0 <= mDataList[i].mComment.IndexOf(searchText)) {
                        return i;
                    }
                } else {
                    //  座標の距離検索
                    if (0 <= coordinatePos) {
                        Point pos = ylib.cnvCoordinate(mDataList[i].getStringData()[coordinatePos]);
                        if (pos.X != 0 && pos.Y != 0) {
                            double dis = ylib.coordinateDistance(searchCoordinate, pos);
                            if (dis < searchDistance)
                                return i;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 全ファイルの中から検索する
        /// </summary>
        /// <param name="searchText">検索文字列</param>
        /// <param name="dataFoleder">検索ファイルのフォルダ</param>
        /// <param name="fileName">検索ファイル名</param>
        public void getSearchAllWikiData(string searchText, string dataFoleder, string fileName = "")
        {
            Point searchCoordinate = new Point();
            double searchDistance = 20.0;
            if (0 < ylib.getCoordinatePattern(searchText).Length) {
                //  検索が座標の場合
                searchCoordinate = ylib.cnvCoordinate(searchText);
                int n = searchText.IndexOf(' ');
                if (0 < n)
                    searchDistance = ylib.string2double(searchText.Substring(n));
            }
            //  対象ファイルの検索
            string[] fileList = ylib.getFiles(dataFoleder + "\\" +(fileName.Length == 0 ? "*" : fileName) + ".csv");
            if (fileList != null) {
                mDataList.Clear();
                foreach (string path in fileList) {
                    //  ファイルごとのデータ検索
                    if (searchCoordinate.X == 0 && searchCoordinate.Y == 0) {
                        getSerchWikiDataFile(searchText, path);
                    } else {
                        getSerchWikiDataFile(searchCoordinate, searchDistance, path);
                    }
                }

                //  検索結果の処理
                if (0 < mDataList.Count) {
                    //  項目に距離があれば距離でソートする
                    int disPos = -1;
                    for (int i = 0; i < mDataList[0].mTagSetData.Count; i++) {
                        if (0 <= mDataList[0].mTagSetData[i][0].IndexOf("距離")) {
                            disPos = i;
                            break;
                        }
                    }
                    //  距離でソート
                    if (0 <= disPos)
                        mDataList.Sort((a, b) => Math.Sign(double.Parse(a.mTagSetData[disPos][1]) - double.Parse(b.mTagSetData[disPos][1])));
                }
            }
        }

        /// <summary>
        /// 検索ファイルから用語を検索しListに保存
        /// </summary>
        /// <param name="searchText">検索文字列</param>
        /// <param name="filePath">検索ファイル名</param>
        /// <returns>検出データリスト</returns>
        public void getSerchWikiDataFile(string searchText, string filePath)
        {
            List<string[]> wikiDataList = ylib.loadCsvData(filePath);
            if (wikiDataList != null && 0 < wikiDataList.Count) {
                int urlPos = Array.IndexOf(wikiDataList[0], "URL");
                for (int i = 1; i < wikiDataList.Count; i++) {
                    for (int j = 0; j < wikiDataList[i].Length; j++) {
                        if (0 <= wikiDataList[i][j].IndexOf(searchText, StringComparison.OrdinalIgnoreCase)) {
                            WikiData wikiData = new WikiData(wikiDataList[i][0], wikiDataList[i][1],
                                wikiDataList[i][urlPos], wikiDataList[i][urlPos + 1], wikiDataList[i][urlPos + 2]);
                            wikiData.setTagSetData(wikiDataList[0], wikiDataList[i]);
                            mDataList.Add(wikiData);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 検索ファイルから距離を求めて指定距離内であれば登録する
        /// </summary>
        /// <param name="searchPos">座標</param>
        /// <param name="searchDistance">範囲距離(km)</param>
        /// <param name="filePath">検索ファイル名</param>
        public void getSerchWikiDataFile(Point searchPos, double searchDistance, string filePath)
        {
            List<string[]> wikiDataList = ylib.loadCsvData(filePath);
            if (wikiDataList != null && 0 < wikiDataList.Count) {
                int urlPos = Array.IndexOf(wikiDataList[0], "URL");
                //  タイトルに「距離」を追加してコピー
                string[] title = new string[wikiDataList[0].Length + 1];
                int coordinatePos = 0;
                for (int i = 0, j = 0; i < wikiDataList[0].Length; i++) {
                    if (wikiDataList[0][i].CompareTo("座標") == 0) {
                        coordinatePos = i;
                        title[j++] = wikiDataList[0][i];
                        title[j++] = "距離(km)";
                    } else {
                        title[j++] = wikiDataList[0][i];
                    }
                }
                //  指定距離内であれば登録する
                for (int i = 1; i < wikiDataList.Count; i++) {
                    if (coordinatePos < wikiDataList[i].Length &&
                        0 < wikiDataList[i][coordinatePos].Length) {
                        //  座標データが存在する時、距離を求める
                        Point pos = ylib.cnvCoordinate(wikiDataList[i][coordinatePos]);
                        if (pos.X != 0 && pos.Y != 0) {
                            double dis = ylib.coordinateDistance(searchPos, pos);
                            if (dis < searchDistance) {
                                WikiData wikiData = new WikiData(wikiDataList[i][0], wikiDataList[i][1],
                                    wikiDataList[i][urlPos], wikiDataList[i][urlPos + 1], wikiDataList[i][urlPos + 2]);
                                //  データに「距離」を追加してコピー
                                string[] data = new string[wikiDataList[i].Length + 1];
                                for (int k = 0, l = 0; k < wikiDataList[i].Length; k++) {
                                    if (wikiDataList[0][k].CompareTo("座標") == 0) {
                                        data[l++] = wikiDataList[i][k];
                                        data[l++] = dis.ToString("#,##0.##");
                                    } else {
                                        data[l++] = wikiDataList[i][k];
                                    }
                                }
                                wikiData.setTagSetData(title, data);
                                mDataList.Add(wikiData);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// データリストの保存
        /// </summary>
        /// <param name="filePath">保存パス</param>
        public void saveData(string filePath)
        {
            List<string[]> dataList = new List<string[]>();
            foreach (WikiData data in mDataList)
                dataList.Add(data.getStringData(false));
            ylib.saveCsvData(filePath, getFormatTitleData(false), dataList);
        }

        /// <summary>
        /// ファイルからデータリストを読み込む
        /// </summary>
        /// <param name="filePath">ファイル名</param>
        public void loadData(string filePath)
        {
            mDataList.Clear();
            List<string[]> wikiDataList = ylib.loadCsvData(filePath);
            if (wikiDataList != null && 0 < wikiDataList.Count) {
                //  タイトルの設定
                setFormatTitleData(wikiDataList[0]);
                //  固定タイトルの数
                int urlPos = Array.IndexOf(wikiDataList[0], "URL");
                int offset = WikiData.mDefaultTitle.Length;
                for (int i = 0; i < WikiData.mDefaultTitle.Length; i++) {
                    if (wikiDataList[0][i].CompareTo(WikiData.mDefaultTitle[i]) != 0) {
                        offset = i;
                        break;
                    }
                }
                //  詳細情報の取得
                for (int i = 1; i < wikiDataList.Count; i++) {
                    if (urlPos + 2 < wikiDataList[i].Length) {
                        WikiData wikiData = new WikiData(wikiDataList[i][0], wikiDataList[i][1],
                            wikiDataList[i][urlPos], wikiDataList[i][urlPos + 1], wikiDataList[i][urlPos + 2]);
                        wikiData.setStringData(wikiDataList[i], offset);
                        mDataList.Add(wikiData);
                    }
                }
            }
        }
    }
}
