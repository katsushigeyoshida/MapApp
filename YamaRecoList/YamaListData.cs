using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WpfLib;

namespace MapApp
{
    public class YamaListData
    {
        private Encoding[] mEncoding = {
            Encoding.UTF8, Encoding.GetEncoding("shift_jis"), Encoding.GetEncoding("euc-jp")
        };
        private int mEncordType = 2;                                    //  EUC(ファイルアクセス)
        private string mAppFolder;                                      //  アプリフォルダ
        private string mDataSaveFolder = "YamaRecoData";                //  データフォルダ
        private string mYamaListDataPath = "YamaListData.csv";          //  データファイル名
        private string mYamaBaseUrl = "https://www.yamareco.com/modules/yamainfo/";
        private string mYamaListUrl = "https://www.yamareco.com/modules/yamainfo/ptlist.php?groupid=";
        public string mSplitWord = "（";                               //  分類データの分轄ワード
        public char mSeparatorChar = '\t';                              //  項目分轄char

        public string[] mDataTitle = {                                  //  データタイトル
            "山リスト名", "登録数", "概要", "登録山名", "URL"
        };
        public int[] mColWidth = {
            -1,          -1,        300,       300,     -1
        };
        public bool[] mDispCol = {                                      //  表示カラムフラグ
            true,        true,      true,      true,     true
        };
        public bool[] mNumVal = {                                       //  数値データの有無(ソート用)
            false,       true,      false,     false,    true
        };
        public bool[] mDetailCol = {                                    //  詳細簡略表示判定
            true,        true,      true,      true,       true,
        };
        public List<string[]> mDataList = new List<string[]>();         //  山データリスト
        public List<string[]> mDetailUrlList = new List<string[]>();    //  詳細データの(URL,項目)リスト

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public YamaListData()
        {
            mAppFolder = ylib.getAppFolderPath();                               //  アプリフォルダ
            mDataSaveFolder = Path.Combine(mAppFolder, mDataSaveFolder);        //  HTMLデータ保存フォルダ
            mYamaListDataPath = Path.Combine(mAppFolder, mYamaListDataPath);
        }

        /// <summary>
        /// 番号指定してWebデータを取り込む
        /// </summary>
        /// <param name="n"></param>
        public void getYamaRecoData(int n)
        {
            string url = mYamaListUrl + n.ToString();
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
                    System.Diagnostics.Debug.WriteLine($"getYamaListData: {buf[0]} {url}");
                    mDataList.Add(buf);
                }
            }
        }

        /// <summary>
        /// HTMLソースから登山ルートデータを抽出し配列に入れる
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
        /// [0]山リスト名,[1]概要,[2]登録山名,[3]URL
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
            buf = new string[2] { "山リスト名", ylib.stripControlCode(pageTitle) };
            listData.Add(buf);
            //  表データ(山名リスト、URLリスト)抽出
            (tagPara, table, nextSrc) = ylib.getHtmlTagData(bodyData, "table");
            List<string[]> tableList = getYamaListInfoList(table);
            //  登録数
            buf = new string[2] { "登録数", (tableList.Count - 1).ToString() };
            listData.Add(buf);
            //  概要
            string basicInfoText = ylib.stripHtmlTagData(ylib.getHtmlTagSrc(bodyData, "div", "class", "content1"));
            buf = new string[2] { "概要", ylib.stripControlCode(basicInfoText) };
            listData.Add(buf);
            //  登録山名
            string detailList = "";
            for (int i = 1; i < tableList.Count; i++) {
                for (int j = 0; j < tableList[i].Length; j++) {
                    if (tableList[0][j] == "名前") {
                        detailList += tableList[i][j];
                    } else if (tableList[0][j] == "標高") {
                        detailList += $"{tableList[i][j]} ";
                    } else if (tableList[0][j] == "山行記録") { //  山のURL
                        detailList += $": {tableList[i][j]}";
                    }
                }
                detailList += mSeparatorChar;
            }
            detailList = detailList.TrimEnd(mSeparatorChar);
            buf = new string[2] { "登録山名", detailList };
            listData.Add(buf);
            return listData;
        }

        /// <summary>
        /// 登録されている山ごとのデータ(山名、標高、URL)リスト
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private List<string[]> getYamaListInfoList(string table)
        {
            List<string[]> listData = new List<string[]>();
            string tagData, tagPara, trData, nextTag;
            string nextHtml = table;
            int bufSize = 5;
            do {
                (tagPara, trData, nextHtml) = ylib.getHtmlTagData(nextHtml, "tr");
                if (trData.Length <= 0)
                    break;
                List<string> dataList = ylib.getHtmlTagDataAll(trData);
                string[] buf = new string[bufSize];
                if (0 < dataList.Count && dataList[0] == "項番") {
                    bufSize = dataList.Count;
                    buf = new string[bufSize];
                    for (int i = 0; i < dataList.Count; i++) {
                        buf[i] = dataList[i];
                    }
                    listData.Add(buf);
                } else {
                    int i = 0;
                    do {
                        (tagPara, tagData, nextTag) = ylib.getHtmlTagData(trData, "td");
                        if (listData[0][i] == "標高") {
                            string temp = ylib.stripHtmlTagData(ylib.stripControlCode(ylib.cnvHtmlSpecialCode(tagData)), "script");
                            buf[i++] = $" {temp.Substring(0, temp.IndexOf('<')).Trim()}";
                        } else if (listData[0][i] == "山行記録") {
                            string temp = ylib.getHtmlTagPara(tagData, "href=");
                            buf[i++] = temp.Substring(0, temp.IndexOf("#"));
                        } else
                            buf[i++] = ylib.stripHtmlTagData(ylib.stripControlCode(ylib.cnvHtmlSpecialCode(tagData)));
                        trData = nextTag;
                    } while (0 < trData.Length && i < buf.Length && i < listData[0].Length);
                    listData.Add(buf);
                }
            } while (0 < trData.Length && 0 < nextHtml.Length);

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
                if (mDetailCol[i] && mDataTitle[i] == "登録山名") {
                    string[] text = mDataList[selIndex][i].ToString().Split(mSeparatorChar);
                    buf += "\n山名リスト:";
                    for (int j = 0; j < text.Length; j++) {
                        buf += "\n  " + text[j].Trim();
                    }
                } else if (mDetailCol[i]) {
                    buf += (0 < buf.Length ? "\n" : "") + mDataTitle[i] + " : " + mDataList[selIndex][i];
                }
            }
            return (buf, title);
        }

        /// <summary>
        /// 山名リスト
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public List<string[]> getSelectUrlList(string url)
        {
            int m = mDataList.FindIndex(p => p[titleNo("URL")].CompareTo(url) == 0);
            List<string[]> listData = new List<string[]>();
            string[] buf = new string[2];
            string[] text = mDataList[m][titleNo("登録山名")].ToString().Split(mSeparatorChar);
            for (int j = 0; j < text.Length; j++) {
                buf = new string[2];
                int n = text[j].IndexOf(mSplitWord);
                if (0 < n) {
                    buf[1] = text[j].Substring(0, text[j].IndexOf(mSplitWord)).Trim();  //  山名(登山口,山小屋)
                    buf[0] = text[j].Substring(text[j].IndexOf(":") + 1).Trim();    //  URL
                    listData.Add(buf);
                }
            }
            return listData;
        }


        /// <summary>
        /// 抽出URLリストからルートデータリストを抽出する
        /// </summary>
        /// <param name="listData">抽出データリスト(URL,ルート名)</param>
        /// <returns>ルートデータリスト</returns>
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
            mDataList = ylib.loadCsvData(mYamaListDataPath, mDataTitle);
        }

        /// <summary>
        /// データリストをファイルに保存
        /// </summary>
        public void saveData()
        {
            if (mDataList != null && 0 < mDataList.Count)
                ylib.saveCsvData(mYamaListDataPath, mDataTitle, mDataList);
        }
    }
}
