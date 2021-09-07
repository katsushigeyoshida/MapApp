﻿namespace MapApp
{
    class HelpText
    {
        public static string mMapAppHelp =
            "【地図データ表示】\n" +
            "国土地理院が公開している地図データを表示する\n" +
            "\n" +
            "■表示できるデータ\n" +
            "国土地理院が硬化しているデータは17種類でそのうち4種類を除く13種類の表示を行うことがでる。\n" +
            "データの種類によって日本全国を表示できるものと特定の地域のみの物がある。\n" +
            "また、表示できる倍率の範囲もデータによって異なる。\n" +
            "\n" +
            "1.標準地図(std)、2.淡色地図(pale)、3.数値地図2500(土地条件)(lcm25k)、4.沿岸海域土地条件図(ccm1)、\n" +
            "5.火山基本図(vbm)、、6.火山土地条件図(vlcd)、7.標準地図(画像なし)、8.淡色地図(画像なし)、\n" +
            "9.白地図(blank)、10.湖沼図(lake2)(画像不明)、11.航空写真全国最新写真(シームレス)(seamlessphoto)、\n" +
            "12.色別標高図(relief)、13.活断層図(都市圏活断層)(afm)、14.宅地利用動向調査成果(lum4bl_capital1994)<\n" +
            "15.全国植生指標データ(ndvi_250m_2010_10)、16.基準点(画像なし)、17.磁気図(jikizu2015_chijiki_h)\n" +
            "上記以外にも国土地理院では年代別の空中写真、地震や水害などの画像も公開しており、追加で登録することができる\n" +
            "\n" +
            "■地図データのしくみ\n" +
            "地図はメルカトル図法によるタイル状にビットマップデータを配置して表示する。\n" +
            "地図の種類、ズームレベル、タイル座標(X,Y)、画像列数の組合せで表示します。\n" +
            "この中でズームレベルは0で全世界を1枚の画像データ表し、1で2x2の4枚、2で4x4の16枚と\n" +
            "2~(ZoomLevel)x2~(ZoomLevel)の数の画像データに分解して表す。\n" +
            "実際の表示は分解したタイル画像データを底る座標位置から列数分だけ表示する。\n" +
            "\n" +
            "■画面操作\n" +
            "・上部バー\n" +
            "  1.地図名 : 表示する地図の種類を指定(右ボタンメニューで追加・削除ができる)\n" +
            "  2.ズームレベル : 表示する地図の拡大率 0-9だと世界地図、10以上だと日本地図のみ\n" +
            "  3.タイル座標位置(X,Y) : タイル表示の開始位置(右上)、同じ位置でもズームレベルよって異なる\n" +
            "  4.画像列数 : 表示するタイル画像の列数(数字が小さいと粗くなる)\n" +
            "  5.自動オンラインチェックボックス :地図データをWeb上から取り込状態設定\n" +
            "    ①チェック　オン (オンライン)     : 常にWeb上からデータをダウンロードして表示\n" +
            "    ②チェック 未確定(自動オンライン) : ダウンロードしてないデータのみをWeb上から取得\n" +
            "    ③チェック　オフ (オフライン)     : ダウンロードしてあるデータのみを表示、ないデータは表示されない\n" +
            "  6.ヘルプ[?] : ヘルプファイルを表示\n" +
            "・左サイドバー\n" +
            "  1.縮尺 : ズームレベルに合わせて変わる(目安レベル)\n" +
            "  2.[+]ボタン : 地図を拡大(ズームレベルを1段階上げる)\n" +
            "  3.[-]ボタン : 地図を拡大(ズームレベルを1段階下げる)\n" +
            "  4.[↑]ボタン : 表示領域を上に移動する\n" +
            "  5.[←]ボタン : 表示領域を左に移動する\n" +
            "  6.[→]ボタン : 表示領域を右に移動する\n" +
            "  7.[→]ボタン : 表示領域を下に移動する\n" +
            "  8.画面登録コンボボックス : 登録した画面に切り替える\n" +
            "  9.[登録]ボタン : 現在の表示画面を名前を付けて登録する\n" +
            " 10.[削除]ボタン : コンボボックスに表示されている登録名を削除する\n" +
            " 11.[マーク表示]チェックボックス : チェックを入れると登録したマークが表示される\n" +
            " 12.[マーク編集]ボタン : マークの一覧が表示され、項目をダブルクリックするとその位置に画面が移動する。" +
            " また選択項目のマークの変更や削除を行うことができる\n" +
            " 13.[GPS軌跡]チェックボックス : チェックを入れると登録したGPSのトレースを表示する\n" +
            " 14.[GPSリスト]ボタン : GPSのトレースリストを表示、リストに対してコンテキストメニューで 追加/編集/削除/移動ができる\n" +
            " 15.[Wikiリスト]ボタン : Wikipediaの一覧リストダイヤログの表示、リストから位置を設定する\n" +
            " 16.説明 : 現在表示されている地図の簡単な説明\n" +
            "・下部ステータスバー\n" +
            "  1.画像取り込 : 画像をWebから取り込んで表示する時の進捗を示す\n" +
            "\n" +
            "■地図上でのコンテキストメニュー(右ボタンメニュー)\n" +
            "・地図名コンボボックス\n" +
            "　[データの追加] : 地図管理データを追加するためのダイヤログを表示する\n" +
            "　[データの編集] : 現在の表示中の地図管理データをダイヤログで編集する\n" +
            "　[データの削除] : 現在表示中の地図の管理データを削除する\n" +
            "・地図表示画面\n" +
            "　[地図画像コピー : 地図画像をクリップボードにコピーする\n" +
            "　[座標のコピー] : カーソル位置での座標値をクリップボードにコピーする\n" +
            "　[補正前値のコピー] : 地図座標の補正を行う前の座標値をクリップボードにコピーする\n" +
            "  [マークの追加] : カーソル位置のマークを追加するための、その属性をを入力するダイヤログを表示する\n" +
            "  [マークの編集] : カーソルをマークの位置で選択するとそのマークの属性を表示し変更することができる\n" +
            "  [マークの削除] : カーソル位置のマークを削除する\n" +
            "　[Wikiリスト検索] : カーソル位置の座標を取得してWikiリストから近傍の名所や施設を検索する\n" +
            "  [測定開始] : 地図上の位置をクリックして最後にコンテキストメニューで[測定終了]を選択すると" +
            "地図上に表示された線の距離が表示される、\n" +
            "■下部ステータスバー\n" +
            "　画像読込プログレスバー : 地図を更新する時にWebからの地図データの取得と表示の進捗状態を表示する\n" +
            "　緯度経度座標 : マウスの位置の緯度経度の座標を表示\n" +
            "\n" +
            "■キー操作\n" +
            " 1.上カーソルキー : 地図領域を上に移動する\n" +
            " 2.左カーソルキー : 地図領域を左に移動する\n" +
            " 3.右カーソルキー : 地図領域を右に移動する\n" +
            " 4.下カーソルキー : 地図領域を下に移動する\n" +
            " 5.PageUpキー   : 地図を拡大(ズームレベルを1段階上げる)\n" +
            " 6.PageDownキー : 地図を拡大(ズームレベルを1段階下げる)\n" +
            " 7.F5キー : 地図を再表示する\n" +
            "\n" +
            "■マウス操作\n" +
            "画面移動 : マウスの左ボタンを押しながらマウスを移動すると\n" +
            "縮小拡大 : マウスのホイールを回すとズームレベルの変更で拡大縮小を行う\n" +
            "\n\n" +
            "■地図データの登録\n" +
            "地図データの追加登録は「地図名コンボボックス」のコンテキストメニューで「データの追加」を選択\n" +
            "登録できるデータは国土地理院のホームページ(https://maps.gsi.go.jp/development/ichiran.html)に公開している\n" +
            "登録するデータは「タイトル」、「データＩＤ」、「ファイルの拡張子」、地図の種類」、「ズーム範囲」\n" +
            "「整備範囲」、「概要」でこのうち「タイトル」、「データＩＤ」、「ファイルの拡張子」は必須です\n" +
            "例えば「北海道胆振東部地震 安平地区」の画像ではURLが\n" +
            "https://cyberjapandata.gsi.go.jp/xyz/20180906hokkaido_abira_0911do/{z}/{x}/{y}.png \n" +
            "「データＩＤ」はxyz/に続く[20180906hokkaido_abira_0911do]となり、ファイルの拡張子」は末尾の[png]と" +
            "なります。「タイトル」は他と重ならない任意の文字列です\n" +
            "\n\n" +
            "■地図データの簡単な説明\n" +
            "1.標準地図(std) : 日本全国(ズームレベル 9-18)および全世界(ズームレベル 0-8)\n" +
            "　道路、建物などの電子地図上の位置の基準である項目と植生、崖、岩、構造物などの土地の状況を" +
            "表す項目を一つにまとめたデータをもとに作られた。国土地理院の提供している一般的な地図。\n" +
            "2.淡色地図(pale) : 日本全国\n" +
            "　標準地図を淡い色調で表したもの\n" +
            "3.数値地図2500(土地条件)(lcm25k)(ズームレベル 4-9, 10-16) : 一部地域\n" +
            "　防災対策や土地利用/土地保全/地域開発などの計画の策定に必要な土地の自然条件などに関する" +
            "基礎資料提供する目的で、昭和30年代から実施している土地条件調査の成果を基に地形分類" +
            "(山地、台地・段丘、低地、水部、人口地形など)について可視化したもの、基本測量成果、" +
            "センサーデータの可視化の下絵などに使用\n" +
            "4.沿岸海域土地条件図(ccm1)(ズームレベル 14-16) : 一部地域(瀬戸内海)\n" +
            "　陸部、解部の地形条件、標高、水深、底質、堆積層、沿岸関連施設、機関、区域などを可視化したもの。" +
            "5.火山基本図(vbm)(ズームレベル 16-18) : 一部地域(雄阿寒/雌阿寒岳、十勝岳、有珠山、樽前山、北海道駒ヶ岳、" +
            "八幡平、秋田駒ヶ岳、岩手山、鳥海山、栗駒山、磐梯山、安達太良山、妙高、草津白根山、" +
            "御嶽山、富士山、大島、三宅島、久住山、阿蘇山、普賢岳、韓国岳、桜島、鬼界ヶ島、竹島)\n" +
            "　噴火の防災計画、緊急対策用のほか、火山の研究や火山噴火予知などの基礎資料として整備した。" +
            "火山の地形を精密に表す等高線や火山防災施設などを示した縮尺 1/2500-1/10000の地形図" +
            "(地図データ)\n" +
            "6.火山土地条件図(vlcd)(ズームレベル 13-16) : 一部地域(同火山基本図))\n" +
            "　火山災害の予測や防災対策立案に利用されている他、地震災害対策、土地保全/利用計画立案や" +
            "各種の調査/研究、教育のための基礎資料としてあるいは地域や強度の理解を深めるための資料としても" +
            "活用することを目的として整備した。火山の地形分類を示した縮尺 1/10000-1/50000の地図。" +
            "過去の火山活動によって形成された地形や噴出物の分布(溶岩流、火砕流、スコリア丘、岩屑なだれなど)" +
            "防災関連施設・機関、救護保安施設、河川工作物、観光施設などをわかりやすく表示したもの\n" +
            "7.標準地図(画像なし) : 日本全国\n" +
            "　国土地理院が提供している一般的な地図、縮尺のちいさいもの\n" +
            "8.淡色地図(画像なし) : 日本全国\n" +
            "　標準地図をグレー・スケールにしたもの、縮尺のちいさいもの\n" +
            "9.白地図(blank) : 日本全国\n" +
            "　全国の白地図\n" +
            "10.湖沼図(lake2) : 主要な湖および沼(不明)\n" +
            "　湖及び沼とその周辺における、道路、主要施設、底質、推進、地形などを示したもの\n" +
            "11.航空写真全国最新写真(シームレス)(seamlessphoto) : 日本全国\n" +
            "　電子国土基本図(オルソ画像)、東日本大震災後正射画像、森林(国有林)の空中写真、" +
            "簡易空中写真、国土画像情報を組み合わせ、全国をシームレスに閲覧でるようにしたもの。\n" +
            "12.色別標高図(relief)(ズームレベル 6-15) : 日本全国\n" +
            "　基礎地図情報(数値標高モデル)および日本海洋データ・センタが提供する500mメッシュ" +
            "海底地形データをもとに作成。標高の変化を色の変化を用いて視覚的に表現したもの。\n" +
            "13.活断層図(都市圏活断層)(afm)(ズームレベル 7-10,11-16) : 日本全国の活断層\n" +
            "　地震被害の軽減に向けて整備された。地形図、活断層とその状態、地形分類を可視化したもの\n" +
            "14.宅地利用動向調査成果(lum4bl_capital1994)(ズームレベル 6-12,13-16) : 首都圏、(中部圏、近畿圏)\n" +
            "　宅地利用動向調査の結果(山林・荒地、田、畑・その他の農地、造成中地、空地、工業用地" +
            "一般低層住宅地、密集低層住宅地、中・高層住宅、商業・業務用地、道路用地、公園・緑地など、" +
            "その他の公共施設用地、河川・湖沼など、その他、海、対象地域外)を可視化したもの" +
            "首都圏は1994年、中部圏は1997年、近畿圏は1996年のデータが最新である。\n" +
            "15.全国植生指標データ(ndvi_250m_2010_10)(ズームレベル 6-10) : 日本とその周辺\n" +
            "　植生指標とは植物による光の反射の特徴を生かし衛星データを使って簡易な計算式で" +
            "植生の状況を把握することを目的として考案された指標で植物の量や活力を表している\n" +
            "16.基準点(画像なし) : 日本全国\n" +
            "　電子基準点、三角点、水準点を示したもの。RTK-GNSSなどの高精度測位をする際の基準局選定に\n" +
            "17.磁気図(jikizu2015_chijiki_h)(ズームレベル 6-8) : 日本全国\n" +
            "　時期の偏角、伏角、全磁力、水平分力、鉛直分力を示したもの\n" +
            "\n\n";

        public static string mMarkListHelp =
            "■マークリストダイヤログ\n" +
            "  マークリストダイヤログは画面左側の[マーク編集]ボタンを押すと表示される\n" +
            "マークリストのダイヤログ画面では上部のコンボボックスでグループを選択し、表示内容を絞ることができるる\n" +
            "リスト上で右クリックするとコンテキストメニューが表示される。またリストの項目をダブルクリックすると\n" +
            "マークの位置を地図の中心に移動する\n" +
            "・コンテキストメニュー\n" +
            "　[編集]: 選択したマークをマーク編集ダイヤログを表示して行う\n" +
            "　[追加]: 新たにマークを追加する、マーク編集画面を表示してデータを入力、位置は緯度経度を度で入力する\n" +
            "　[削除]: 選択したマークを探所する\n" +
            "　[ソート]: リストをソートする\n" +
            "　     [ソートなし]: 登録順に表示\n" +
            "　     [昇順]: A→Zの順で表示\n" +
            "　     [降順]: Z→Aの順で表示\n" +
            "　     [距離順]: 地図の中心との距離順で表示\n" +
            "　[インポート]: CSV形式のマークリストデータファイルを読み込んで追加する\n" +
            "　[エキスポート]: マークリストデータをcsv形式でファイルに出力\n" +
            "\n" +
            "　■マークデータ編集ダイヤログ\n" +
            "　　地図上でのコンテキストメニューの編集/追加、またはマーリストダイヤログのコンテキストメニューの編集/追加で表示される\n" +
            "　　入力項目はタイトル、グループ、マークタイプ、サイズ、座標、コメント、リンクでタイトルと座標は必須入力となる\n" +
            "　　座標は緯度、経度をカンマ区切りで入力するn" +
            "　　リンクはWebのURLまたはファイルパスを入力しておくと[開く]ボタンでそのURLまたはファイルを開く\n" +
            "\n\n";

        public static string mGpsTraceListHelp =
            "■GPSトレースリストダイヤログ\n" +
            "GPXファイルで保存されたGPSトレースデータの登録、編集、削除、グラフ表示を行います。\n" +
            "「GPSリスト」ボタンを押すと上部にグループのコンボボックスがあり、チェックボックス付きのリストが表示される\n" +
            "リストのチェックボックスにチェックが入っていると地図上にGPSデータの軌跡が表示される\n" +
            "リスト上で右ボタンを押すとコンテキストメニューが表示されどれを選択して実行する\n" +
            "・コンテキストメニュー\n" +
            "  [追加]: GPSデータの設定ダイヤログを表示しGPSデータを登録する\n" +
            "  [編集]: 選択したGPSデータの編集ダイヤログが表示され登録内容を変更することができる\n" +
            "  [削除]: 選択したGPSデータを登録リストから削除しする\n" +
            "  [移動]: 選択したGPSデータの表示位置に地図の位置を移動する\n" +
            "  [グラフ表示]: 選択したGPSデータを別ウィンドウにグラフ表示する\n" +
            "  [すべてにチェックを入れる]: 表示されているリストのすべてのチェックボックスにチェックを入れる\n" +
            "  [すべてのチェックを外す]: 表示されているリストのすべてのチェックボックスのチェックを外す\n" +
            "\n" +
            "　■GPSデータ編集ダイヤログ\n" +
            "　　タイトル    : GPSデータのタイトルを設定\n" +
            "　　グループ名  : GPSデータのグループを設定、既に登録されているグループの選択もできる\n" +
            "　　線の色      : GPSトレースの線の色を選択\n" +
            "　　線の太さ    : GPSトレースの線の太さを選択\n" +
            "　　ファイルパス: GPSデータのファイルを選択\n" +
            "　　コメント    : コメントの入力\n" +
            "　　概略データ  : 既に登録されているGPSデータを編集するときにGPSデータの情報を表示\n" +
            "　　\n" +
            "　■GPSデータグラフ表示\n" +
            "　　グラフの種類:\n" +
            "　　　縦軸選択: 標高、標高さ、速度\n" +
            "　　　横軸選択: 距離、経過時間、時刻\n" +
            "　　　移動平均: なし、0-50" +
            "\n\n";

        public static string mWikiListHelp =
            "【Wikipedia一覧リスト表示】\n" +
            "フリー百科事典のWikipediaは山や観光地、博物館などの紹介ページが多数登録されてます。\n" +
            "その紹介ページに基本情報として所在地と一緒に座標が登録されており、その座標値を取得できれば" +
            "その位置の地図を表示することができます。\n" +
            "またこれらのページは一覧としてまとめたページがあり、そのページのリストから各対象ページをアクセスして" +
            "座標データを取得すれば、一覧から目的の位置の地図が表示できます\n" +
            "例えば、法隆寺の場所の地図を見たい時は、まずWikipediaの「日本の寺院一覧」のページを取得し、そのリストに" +
            "ある各寺の紹介ページのURL取得して各寺の紹介ページを参照して位置座標を取得します。\n" +
            "取得した位置座標から地図の位置を特定しその位置の地図を表示します。\n" +
            "\n" +
            "■画面説明\n" +
            "タイトル: 一覧のタイトル\n" +
            "ＵＲＬ  : 一覧のWikipediaのWebアドレス\n" +
            "　　ダブルクリック: WebアドレスをダブルクリックするとWikipediaの一覧ページを表示\n" +
            "　　コンテキストメニュー:\n" +
            "　　　[コピー] : Webアドレスをクリップボードにコピーする\n" +
            "　　　[開く]   : Webアドレスのページを開く\n" +
            "　　　[URL追加]: Wikipediaの一覧ページを追加登録する、タイトルを抜くとアドレスの最後がtitleになる\n" +
            "　　　[URL削除]: 表示されている項目をリストから削除する\n" +
            "[一覧更新]ボタン: 一覧リストをWebから取得して更新します。(座標などの詳細データは更新されません)\n" +
            "[詳細取得]ボタン: 一覧リストのWebアドレスのページから基本情報を取得する\n" +
            "[詳細表示]ボタン: 一覧リストの詳細データの表示非表示を切り替える。\n" +
            "一覧抽出方法: 一覧リストを更新する際に一覧の抽出方法を選択する\n" +
            "　　[自動]      : 取得方法を自動で決める。これでうまく取得できなければ他の方法を試してみる\n" +
            "　　[箇条書き・制限あり]: 箇条書き形式で書かれたものから取得、脚注,References,関連項目,参考文献のキーワードで取得を打ち切る\n" +
            "　　[箇条書き]  : 箇条書き形式で書かれたものから取得、取得制限はつけない\n" +
            "　　[表形式]    : 表形式で記述されたものから取得する\n" +
            "　　[ループ形式]: グループでまとめられたもの(例えばタイトルと写真とコメントをひとまとめにしたもの)から取得\n" +
            "　　[参照]      : キーワードにWebアドレスが関連付けられているものから取得する\n" +
            "検索    : 検索する文字列の入力\n" +
            "　[前検索]ボタン: 表示されているリストから検索文字を上方向に検索する\n" +
            "　[後検索]ボタン: 表示されているリストから検索文字を下方向に検索する\n" +
            "　[検索]ボタン: ファイルに保存されている検索リストを検索して検索リストに表示する\n" +
            "一覧リスト: Wikipediaから取得した一覧データを表示する\n" +
            "　　ダブルクリック: 座標位置が含まれている項目を選択しダブルクリックするとその位置に地図を移動する\n" +
            "　　コンテキストメニュー:  項目を選択し右ボタンを押すとメニューを表示する\n" +
            "　　　[地図位置]: 選択した項目に座標情報が含まれていればその位置に地図を移動する\n" +
            "　　　[詳細表示]: 選択した項目の詳細内容を表示する\n" +
            "　　　[コピー]  : 選択した項目の詳細内容をクリップボードにコピーする\n" +
            "　　　[]開く]   : 選択した内容のWebページを開く\n" +
            "　　　[削除]    : 選択した項目をリストから削除する\n" +
            "ステータスバー\n" +
            "  データ数 : 表示されているリストの項目数を表示\n" +
            "  進捗バー : 詳細項目をWebから取得する時の進捗を表示\n" +
            "  抽出方法 : 一覧項目をWebページから取得する時に使用した抽出方法を表示\n" +
            "  [?]ボタン　: ヘルプボタン\n" +
            "\n" +
            "■操作方法\n" +
            "\n" +
            "\n";
    }
}
