# MapApp
## 国土地理院地図を表示するアプリ

国土地理院のタイル画像を表示するために作成したアプリソフトですがその他のさまざまな地図データも表示する。

![MainWindow画像](Image/MainImage.png)

使い方などは[説明書](Document/MapAppManual.pdf)を参照。  
実行方法は[MapApp.zip](MapApp.zip)をダウンロードし適当なフォルダーに展開してMapApp.exeを実行する。  

#### ■おもな機能
**1.Web上で公開されている地図表示**  
　国土地理院の地図と同じ方式が使える他の公開している地図の登録・表示ができる。  
  オープンストリートマップなどを利用すれば海外の地域も同じように見ることができる。(かなり粗いが標高データもあるので三次元表示することも可能)  

**標準地図**  
<img src="Image/MapStdImage.png" width="80%">  
**色別標高図**  
<img src="Image/MapLerifImage.png" width="80%">  
**20万分の1日本シームレス地質図V2** (マウス位置の地質名を下部ステータスバーに表示)  
<img src="Image/MspSeamlessV2.png" width="80%">  
  
**地図の重ね合わせ**  
<img src="Image/淡色地図(地すべり地形分布).png" width="45%"> <img src="Image/地すべり地形分布地図.png" width="45%">  
標準地図と地すべり地形分布図  

<img src="Image/地すべり地形分布地図・淡色地図.png" width="80%">  

標準地図 + 地すべり地形分布図 の重ね合わせた地図  

<img src="Image/雨雲レーダー.png" width="80%">  

気象庁の雨雲レーダーと淡色地図の重ね合わせ  


**2.地図の解像度変更対応**  
　地図の表示は256x256のタイル画像を並べて表示しているので表示する画像の数を増やすと解像度を高くすることができる(画像列数で設定)のでPCの解像度以上のビットマップデータが作成できる。  

**3.マーク表示**  
　特定の座標を登録でき、その位置に移動することやコメントや参照などを登録できる。
  WikiListで検索したデータもマークとして登録できる。  
    
**4.GPSデータの登録・表示**  
　GPS機能を持った機器でトレースしたGPXデータの登録やトレース表示を行う。  
<img src="Image/MapGpsTrace.png" width="80%">  
<img src="Image/MapGpsElevatorGraph.png" width="80%">  
  
**5.Wikipediaのデータ参照**  
　Wikipediaには史跡や観光地、博物館、百名山などに位置情報を含むデーが登録されている。  
　これらの情報の一覧を取り込んでそこから位置座標を取出し、その位置に地図を移動させたり、マークの登録を行うことができる。  
<img src="Image/Wiki百山の一覧.png" width="45%"><img src="Image/Wiki利尻山.png" width="45%">  

Wikipediaの一覧ページとその個別ページから一覧リストを作成。　  
<img src="Image/WikiListImage.png" width="90%">  
一覧リストからその地図位置への移動や座標からリストの検索ができる。  

**6.三次元表示機能**  
　国土地理院の標高データを利用して地図の表示エリアを三次元表示する。  
<img src="Image/Map3DImage.png" width="80%">  
八ヶ岳  
<img src="Image/ヒマラヤ周辺.png" width="80%">  
ヒマラヤ(Outdoors map)  
<img src="Image/ヒマラヤ周辺3D.png" width="80%">  
ヒマラヤ(3D)


### ■実行環境
Windows10で動作の確認をおこなっている。
ソフトの実行方法は MapApp.zip をダウンロードして適当なフォルダに展開し、フォルダ内の MapApp.exe をダブルクリックして実行します。  
初回は画像が乱れることがあるので、その場合は F5 キーで再表示するとなおります。

### ■開発環境  
開発ソフト : Microsoft Visual Studio 2022  
開発言語　 : C# 7.3 Windows アプリケーション  
フレームワーク　 : .NET framework 4.7.2  
NuGetライブラリ : OpenTK(3.3.2),OpenTK.GLControl(3.1.0)  
自作ライブラリ  : WpfLib, Wpf3DLib