using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WpfLib;

namespace MapApp
{
    public class WikiData
    {

        //  DgDataListのBindingデータ(固定データ)
        public string mTitle { get; set; }          //  タイトル名
        public string mComment { get; set; }        //  コメント
        public string mUrl { get; set; }            //  URL
        public string mListTitle { get; set; }      //  親リストのタイトル
        public string mListUrl { get; set; }        //  親リストのURL
        public string mSearchForm { get; set; }     //  一覧の抽出方法

        //public string mDataType { get; set; }       //  データの種別

        //  DgDataListのBindingデータ(不定データ)
        public string mData1 { get { return mTag[0]; } }
        public string mData2 { get { return mTag[1]; } }
        public string mData3 { get { return mTag[2]; } }
        public string mData4 { get { return mTag[3]; } }
        public string mData5 { get { return mTag[4]; } }
        public string mData6 { get { return mTag[5]; } }
        public string mData7 { get { return mTag[6]; } }
        public string mData8 { get { return mTag[7]; } }
        public string mData9 { get { return mTag[8]; } }
        public string mData10 { get { return mTag[9]; } }
        public string mData11 { get { return mTag[10]; } }
        public string mData12 { get { return mTag[11]; } }
        public string mData13 { get { return mTag[12]; } }
        public string mData14 { get { return mTag[13]; } }
        public string mData15 { get { return mTag[14]; } }
        public string mData16 { get { return mTag[15]; } }
        public string mData17 { get { return mTag[16]; } }
        public string mData18 { get { return mTag[17]; } }
        public string mData19 { get { return mTag[18]; } }
        public string mData20 { get { return mTag[19]; } }

        public List<string[]> mTagSetData;      //  基本情報の取得データ(タイトルとデータ)
        public string[] mTag = new string[20];  //  DgDataListのBindデータ(mData?)の参照元データ
        public static string[] mDefaultTitle = new string[] {
            "タイトル", "コメント", "URL", "親リストタイトル", "親リストURL", "一覧抽出方法" };

        private RegexOptions mRegexOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline; //  正規表現検索オプション
        private string mHtml;               //  ダウンロードししたHTMLソース
        private int mEncordType = 0;        //  UTF8
        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <param name="comment">コメント</param>
        /// <param name="url">URL</param>
        /// <param name="listTitle">親リストのタイトル</param>
        /// <param name="listUrl">親リストのURL</param>
        /// <param name="infoData">詳細取得有無</param>
        public WikiData(string title, string comment, string url, string listTitle = "", string listUrl = "", string searchForm = "", bool infoData = false)
        {
            mTitle = title;
            mComment = comment;
            mUrl = url;
            mListTitle = listTitle;
            mListUrl = listUrl;
            mSearchForm = searchForm;
            //  詳細データ取得
            if (infoData)
                getInfoData();
        }

        public void setBaseData( string listTitle = "", string listUrl = "", string searchForm = "")
        {
            mListTitle = listTitle;
            mListUrl = listUrl;
            mSearchForm = searchForm;
        }

        /// <summary>
        /// タグのタイトルとデータの取得・設定
        /// </summary>
        public void getTagSetData()
        {
            mTagSetData = getInfoData();
        }

        /// <summary>
        /// リストデータを文字配列で取得
        /// </summary>
        /// <returns></returns>
        public string[] getStringData(bool dispOrder = true)
        {
            int offset = 2;
            string[] data = new string[mDefaultTitle.Length + mTag.Length];
            data[0] = mTitle;
            data[1] = mComment;
            if (!dispOrder) {
                //  ファイルに保存する時の優先
                data[2] = mUrl;
                data[3] = mListTitle;
                data[4] = mListUrl;
                data[5] = mSearchForm;
                offset = mDefaultTitle.Length;
            }
            int i = 0;
            for (; i < mTag.Length; i++)
                data[offset + i] = mTag[i] == null ? "" : mTag[i];
            if (dispOrder) {
                //  画面に表示する時
                data[i + offset] = mUrl;
                data[++i + offset] = mListTitle;
                data[++i + offset] = mListUrl;
                data[++i + offset] = mSearchForm;
            }

            return data;
        }

        /// <summary>
        /// 文字配列データをリストデータに設定
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        public void setStringData(string[] data, int offset)
        {
            //  固定データ
            mTitle      = data[0];
            mComment    = data[1];
            mUrl        = offset < 3 ? "" : data[2];
            mListTitle  = offset < 4 ? "" : data[3];
            mListUrl    = offset < 5 ? "" : data[4];
            mSearchForm = offset < 6 ? "" : data[5];
            //  可変データ
            for (int i = 0; i < mTag.Length && i + offset < data.Length; i++)
                mTag[i] = data[offset + i];
        }

        /// <summary>
        /// 基本情報の項目データをタイトルに合わせて設定
        /// 設定したmTagの配列データはBind設定されたmData?から参照される
        /// </summary>
        /// <param name="titles"></param>
        public void setInfoData(List<string> titles)
        {
            if (mTagSetData != null) {
                //  基本情報をタイトルに合わせて追加
                for (int i = 0; i < titles.Count && i < mTag.Length; i++) {
                    mTag[i] = "";
                    foreach (string[] data in mTagSetData) {
                        if (titles[i].CompareTo(data[0]) == 0) {
                            mTag[i] = data[1];
                            break;
                        }
                    }
                }
                //  基本情報に座標データがあれば座標項目に抽出して追加
                int coordinateNo = titles.IndexOf("座標");
                if (coordinateNo < 0)
                    coordinateNo = titles.IndexOf("位置");
                if (0 <= coordinateNo) {
                    foreach (string[] data in mTagSetData) {
                        string coordinate = ylib.getCoordinatePattern(data[1]);
                        if (0 < coordinate.Length) {
                            mTag[coordinateNo] = coordinate;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 個別の情報ページから基本情報を取り出す
        /// </summary>
        /// <returns></returns>
        public List<string[]> getInfoData()
        {
            ylib.mRegexOption = mRegexOptions;
            if (mUrl == null || mUrl.Length == 0)
                return null;
            //  Webデータの取得
            mHtml = ylib.getWebText(mUrl, mEncordType);
            if (mHtml == null || mHtml.Length == 0)
                return null;

            //  データの冒頭(前書き)部分を抽出
            mComment = getFirstComment(mComment) + " " + getIntroduction(mHtml);
            //  基本情報の取得
            List<string[]> tagDataList = getBaseInfoData(mHtml);
            //  基本情報以外に座標がある場合
            string coord = getCoordinateString(mHtml);
            if (0 < coord.Length) {
                tagDataList.Add(new string[] { "座標", coord });
            }

            return tagDataList;
        }

        /// <summary>
        /// tableデータから基本情報のデータを取得する
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns></returns>
        private List<string[]> getBaseInfoData(string html)
        {
            List<string[]> tagDataList = new List<string[]>();          //  Wiki抽出データ

            //  <table> </table>情報の抽出
            string tagPara, tagData, nextSrc;
            do {
                (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(html, "table");
                html = nextSrc;
            } while (0 < tagPara.Length && tagPara.IndexOf("infobox") < 0 && 0 < html.Length);

            //  表内データの抽出
            html = tagData;
            //  不要データの除去
            html = ylib.stripHtmlTagData(html, "style");
            while (0 < html.Length) {
                //  表の行単位のデータ抽出
                (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(html, "tr");
                if (tagData.Length < 1)
                    break;
                (string title, string data) = getTableRowData(tagData);
                if (0 < title.Length && 0 < data.Length) {
                    data = ylib.stripBrackets(data);
                    tagDataList.Add(new string[] { title, data.Replace("\n", " ") });
                }
                html = nextSrc;
            }
            return tagDataList;
        }

        /// <summary>
        /// 一覧抽出で取得したコメント('['']'で囲まれた部分)のみを抽出
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        private string getFirstComment(string comment)
        {
            if (comment.IndexOf("[") == 0 && 0 < comment.IndexOf("]")) {
                return comment.Substring(0, comment.IndexOf("]") + 1);
            } else {
                return "";
            }
        }

        /// <summary>
        /// データの前書き部分を抽出
        /// 最初の段落(<p> ～　</p>)部分から文字列デーを抽出する
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string getIntroduction(string html)
        {
            //  基本情報のテーブルを終わりを検索
            int bs = html.IndexOf("<table class=\"infobox");
            int be = 0;
            if (0 <= bs) {
                (bs, be) = ylib.getHtmlTagDataPos(html, "table", bs);
            }

            //  段落の検出
            (bs, be) = ylib.getHtmlTagDataPos(html, "p", be);
            if (bs >= be)
                return "";

            //  段落データの抽出
            string introData = html.Substring(bs, be - bs + 1);
            if (0 < introData.IndexOf("<span")) {
                //  最初の段落に座標データの場合があるので、その時は次の段落を使う段落の検出
                (bs, be) = ylib.getHtmlTagDataPos(html, "p", be);
                if (bs >= be)
                    return "";
                introData = html.Substring(bs, be - bs + 1);
            }
            //introData = ylib.stripHtmlTagData(introData, "span");

            //  データを抽出してリスト化
            List<string> tagList = ylib.getHtmlTagDataAll(introData);

            string data = string.Join(" ", tagList);
            data = ylib.cnvHtmlSpecialCode(data);   //  HTML特殊コード変換
            data = data.Replace("\n", " ");
            return ylib.stripBrackets(data);        //  大括弧内の文字列を括弧ごと除く
        }

        /// <summary>
        /// 基本情報とは別に座標情報の取得
        /// ページ上部の段落の中に<span > ～ </span> で記述されている場合
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>座標文字列</returns>
        private string getCoordinateString(string html)
        {
            //int bs = html.IndexOf("<span id=\"coordinates");
            int bs = html.IndexOf("<span class=\"geo-dms");
            int be = 0;
            if (bs < 0)
                return "";

            (bs, be) = ylib.getHtmlTagDataPos(html, "span", bs);
            string coordData = html.Substring(bs, be - bs + 1);
            coordData = ylib.stripHtmlTagData(coordData, "style");
            List<string> tagList = ylib.getHtmlTagDataAll(coordData);
            string data = string.Join(" ", tagList);
            return ylib.getCoordinatePattern(data);
        }

        /// <summary>
        /// リスト一覧で登録したデータ(タイトル,コメント,URL,親リストタイトル,親リストURL)以外の
        /// 基本情報をタイトルとデータをセットで登録
        /// </summary>
        /// <param name="title">タイトル配列</param>
        /// <param name="data">データ配列</param>
        public void setTagSetData(string[] title, string[] data)
        {
            if (mTagSetData == null)
                mTagSetData = new List<string[]>();
            for (int i = 0; i < data.Length; i++) {
                if (0 < data[i].Length) {
                    if (Array.IndexOf(mDefaultTitle, title[i]) < 0)
                        mTagSetData.Add(new string[] { title[i], data[i] });
                }
            }
        }

        /// <summary>
        /// HTMLのテーブルデータの一行から見出しとデータを取得
        /// </summary>
        /// <param name="tagList">タグリストデータ</param>
        /// <param name="pos">データ位置</param>
        /// <returns>(label, data, pos) (データタイトル,データ, 位置)</returns>
        private (string, string, int) getTagTr(List<string> tagList, int pos)
        {
            string label = "";
            string data = "";
            while (tagList[pos].IndexOf("</tr>") < 0 && pos < tagList.Count - 1) {
                string tagName = ylib.getHtmlTagName(tagList[pos]);
                if (tagName.CompareTo("th") == 0) {
                    //  ヘッダー(見出し)
                    while (tagList[pos].IndexOf("</th>") < 0 && tagList[pos].IndexOf("</tr>") < 0 && pos < tagList.Count) {
                        tagName = ylib.getHtmlTagName(tagList[pos]);
                        if (tagName.Length == 0) {
                            label = tagList[pos];
                        }
                        pos++;
                    }
                } else if (tagName.CompareTo("td") == 0) {
                    //  セルのデータ
                    while (tagList[pos].IndexOf("</td>") < 0 && tagList[pos].IndexOf("</tr>") < 0 && pos < tagList.Count) {
                        tagName = ylib.getHtmlTagName(tagList[pos]);
                        if (tagName.Length == 0) {
                            if (0 < data.Length)
                                data += " ";
                            data += tagList[pos];
                        }
                        pos++;
                    }
                }
                pos++;
            }
            return (label, data, pos);
        }

        /// <summary>
        /// HTMLソースから表の1行分のデータ(<tr></tr>)から
        /// <th></th>と<td></td>をタイトルとデータして取得
        /// </summary>
        /// <param name="html">HTMLソース</param>
        /// <returns>(タイトル、データ)</returns>
        private (string title, string data) getTableRowData(string html)
        {
            (string tagPara, string tagData, string nextSrc) = ylib.getHtmlTagData(html, "th");
            List<string> titleList = ylib.getHtmlTagDataAll(tagData);
            (tagPara, tagData, nextSrc) = ylib.getHtmlTagData(nextSrc, "td");
            List<string> dataList = ylib.getHtmlTagDataAll(tagData);

            string title = "";
            foreach (string data in titleList)
                title += (title.Length > 0 ? "," : "") + data;
            string para = "";
            foreach (string data in dataList)
                para += data;
            return (title, para);
        }

    }
}
