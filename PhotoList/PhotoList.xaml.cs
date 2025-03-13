using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WpfLib;

namespace MapApp
{
    /// <summary>
    /// 写真データをリストに登録するためのクラス
    /// </summary>
    public class Photo
    {
        public string title { get; set; }
        public string path { get; set; }
        public BitmapImage image { get; set; }
    }

    /// <summary>
    /// PhotoList.xaml の相互作用ロジック
    /// 写真データファイルの一覧表示と座標操作
    /// フォルダ操作: フォルダ選択、追加、削除、開く、貼り付け
    /// 写真操作: 地図位置表示(ダブルクリック)
    ///           開く、表示、マーク登録、座標登録、座標位置指定、GPS座標追加、
    ///           コメント登録、属性表示
    /// </summary>
    public partial class PhotoList : Window
    {
        private double mWindowWidth;            //  ウィンドウの高さ
        private double mWindowHeight;           //  ウィンドウ幅

        public MainWindow mMainWindow;
        public MarkList mMarkList;

        public List<Photo> Photos;

        public int mThumbnailWidth = 80;        //  サムネイル表示のための画像縮小サイズ
        public int mThumbnailHeight = 50;
        public int mDataFolderMax = 100;        //  登録フォルダの最大数
        public bool mRecursiveFolder = true;    //  フォルダのデータを再帰検索
        public string[] mSortMenu = {           //  ソートメニュー
            "パス", "ファイル名", "日付", "サイズ"
        };
        public enum SORTTYPE {                      //  ソートタイプ
            path, filename, date, size
        }
        public SORTTYPE mSortType = SORTTYPE.path;  //  ソート
        public bool mSortReverse = false;           //  逆順
        public enum DOUBLECLICK { disp, coonrdinate, non }          //  ダブルクリック時の処理タイプ
        public DOUBLECLICK mDoubleClikDefualt = DOUBLECLICK.disp;   //  ダブルクリック時の処理方法
        private List<string> mDataFolders = new List<string>();     //  検索フォルダリスト
        private string mDataFolderListPath = "PhotoListFolders.csv";
        private string mGpxFolder = "";
        private GpxReader mGpxReader;
        private FitReader mFitReader;

        private YLib ylib = new YLib();
        private ImageView mImageView;

        public PhotoList()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            WindowFormLoad();

            folderListLoad();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mImageView != null)
                mImageView.Close();
            folderListSave();
            Properties.Settings.Default.PhotoDataFolder = mDataFolders[CbPhotoFolder.SelectedIndex];
            WindowFormSave();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (0 < mDataFolders.Count) {
                string folder = Properties.Settings.Default.PhotoDataFolder;
                int n = mDataFolders.IndexOf(folder);
                CbPhotoFolder.SelectedIndex = 0 < n ? n : 0;
            }
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.PhotoListWidth < 100 ||
                Properties.Settings.Default.PhotoListHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.PhotoListHeight) {
                Properties.Settings.Default.PhotoListWidth = mWindowWidth;
                Properties.Settings.Default.PhotoListHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.PhotoListTop;
                Left = Properties.Settings.Default.PhotoListLeft;
                Width = Properties.Settings.Default.PhotoListWidth;
                Height = Properties.Settings.Default.PhotoListHeight;
            }
            mGpxFolder = Properties.Settings.Default.GpxFolder;
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            Properties.Settings.Default.GpxFolder = mGpxFolder;
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.PhotoListTop = Top;
            Properties.Settings.Default.PhotoListLeft = Left;
            Properties.Settings.Default.PhotoListWidth = Width;
            Properties.Settings.Default.PhotoListHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [写真フォルダ]切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbPhotoFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbPhotoFolder.SelectedIndex) {
                var folder = mDataFolders[CbPhotoFolder.SelectedIndex];
                setPhotoData(folder);
                setFolderList(folder);
            }
        }

        /// <summary>
        /// [写真フォルダ]ダブルクリック フォルダ選択ダイヤログ表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbPhotoFolder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //  フォルダの選択ダイヤログを開く
            folderSelect();
        }

        /// <summary>
        ///  [写真フォルダ]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbPhotoFolderMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("CbPhotoFolderAddMenu") == 0) {
                //  フォルダの選択ダイヤログを開く
                folderSelect();
            } else if (menuItem.Name.CompareTo("CbPhotoFolderDelMenu") == 0) {
                //  フォルダを削除する
                folderRemove(CbPhotoFolder.Text);
            } else if (menuItem.Name.CompareTo("CbPhotoFolderOpenMenu") == 0) {
                //  フォルダを開く
                ylib.openUrl(CbPhotoFolder.Text);
            } else if (menuItem.Name.CompareTo("CbPhotoFolderPasteMenu") == 0) {
                //  クリップボードのフォルダパスを貼り付ける
                string folder = ylib.stripControlCode(Clipboard.GetText());
                if (Directory.Exists(folder)) {
                    if (setPhotoData(folder))
                        setFolderList(folder);
                    CbPhotoFolder.Text = folder;
                }
            }
        }

        /// <summary>
        /// フォルダリストから削除
        /// </summary>
        /// <param name="folder">削除フォルダ</param>
        private void folderRemove(string folder)
        {
            if (0 < folder.Length) {
                int n = mDataFolders.IndexOf(folder);
                if (0 <= n) {
                    mDataFolders.RemoveAt(n);
                    CbPhotoFolder.ItemsSource = new ReadOnlyCollection<string>(mDataFolders);
                    CbPhotoFolder.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// フォルダの選択追加
        /// </summary>
        private void folderSelect()
        {
            string dataFolder = "";
            if (0 < CbPhotoFolder.Text.Length) {
                dataFolder = CbPhotoFolder.Text;
                if (Path.GetExtension(dataFolder).Length <= 0) {
                    dataFolder = Path.GetDirectoryName(dataFolder + "\\");
                } else {
                    dataFolder = Path.GetDirectoryName(dataFolder);
                }
            }
            //  フォルダ選択
            dataFolder = ylib.folderSelect(dataFolder);
            if (0 < dataFolder.Length) {
                if (setPhotoData(dataFolder))
                    setFolderList(dataFolder);
                CbPhotoFolder.Text = dataFolder;
            }
        }

        /// <summary>
        /// [写真リスト]ダブルクリック 地図位置移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvPhotoList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (0 <= LvPhotoList.SelectedIndex) {
                //  ファイル選択
                int index = LvPhotoList.SelectedIndex;
                if (mDoubleClikDefualt == DOUBLECLICK.coonrdinate) {
                    //  GPS座標移動
                    ExifInfo exifInfo = new ExifInfo(Photos[index].path);
                    Point coodinate = exifInfo.getExifGpsCoordinate();
                    if (!coodinate.isEmpty()) {
                        mMainWindow.setMoveCtrCoordinate(coodinate);
                    } else {
                        MessageBox.Show("位置座標が設定されていません");
                    }
                } else if (mDoubleClikDefualt == DOUBLECLICK.disp) {
                    dispPhotoData(Photos[index].path);
                } else {
                    ylib.fileExecute(Photos[index].path);
                }
            }
        }

        /// <summary>
        /// [写真リスト]選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvPhotoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= LvPhotoList.SelectedIndex) {
                //  ファイル選択
                int index = LvPhotoList.SelectedIndex;
                setPhotoInfo(Photos[index].path);
            }
        }

        /// <summary>
        /// [写真リスト]コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            List<string> fileList = new List<string>();
            if (0 < LvPhotoList.SelectedItems.Count) {
                foreach (var item in LvPhotoList.SelectedItems) {
                    Photo photo = (Photo)item;
                    fileList.Add(photo.path);
                }
            }
            if (0 <= LvPhotoList.SelectedIndex) {
                //  ファイル選択
                int index = LvPhotoList.SelectedIndex;
                if (menuItem.Name.CompareTo("LvOpenMenu") == 0) {
                    //  開く
                    ylib.fileExecute(Photos[index].path);
                } else if (menuItem.Name.CompareTo("LvDispMenu") == 0) {
                    //  イメージ表示
                    dispPhotoData(Photos[index].path);
                } else if (menuItem.Name.CompareTo("LvMarkMenu") == 0) {
                    //  マークに登録
                    addMark(Photos[index].path);
                } else if (menuItem.Name.CompareTo("LvCoordMenu") == 0) {
                    //  座標登録
                    editCoordinate(Photos[index].path);
                } else if (menuItem.Name.CompareTo("LvCoordLocMenu") == 0) {
                    //  座標位置指定
                    if (MessageBox.Show("位置をマウスで指定してください", "座標登録", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                        mMainWindow.mSetPhotLocFile = Photos[index].path;
                        mMainWindow.mPhotoLoacMode = true;
                    }
                } else if (menuItem.Name.CompareTo("LvGpsCoordMenu") == 0) {
                    //  GPS座標追加
                    addGpsCoordinate(fileList);
                } else if (menuItem.Name.CompareTo("LvCommentMenu") == 0) {
                    //  コメント登録
                    editComment(Photos[index].path);
                } else if (menuItem.Name.CompareTo("LvPropertyMenu") == 0) {
                    //  プロパティ表示
                    string buf = ylib.getIPTCall(Photos[index].path);
                    ExifInfo exifInfo = new ExifInfo(Photos[index].path);
                    Point cood = exifInfo.getExifGpsCoordinate();
                    if (!cood.isEmpty())
                        buf += "\nGPS座標: " + cood.X.ToString("f6") + "," + cood.Y.ToString("f6");
                    buf += "\n" + exifInfo.getExifInfoAll();
                    messageBox(buf, "属性表示[" + Path.GetFileName(Photos[index].path) + "]");
                }
            }
        }

        /// <summary>
        /// [ソート]ボタン 写真リストのソートメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtSort_Click(object sender, RoutedEventArgs e)
        {
            MenuDialog sortMenu = new MenuDialog();
            sortMenu.mMenuList = mSortMenu.ToList();
            sortMenu.mMainWindow = this;
            sortMenu.Title = "ソート";
            sortMenu.mOneClick = true;
            sortMenu.ShowDialog();
            int index = mSortMenu.FindIndex(sortMenu.mResultMenu);
            if (index < 0) return;
            if ((int)mSortType == index)
                mSortReverse = !mSortReverse;
            else
                mSortType = (SORTTYPE)Enum.ToObject(typeof(SORTTYPE), index);
            if (0 <= CbPhotoFolder.SelectedIndex) {
                var folder = mDataFolders[CbPhotoFolder.SelectedIndex];
                setPhotoData(folder);
            }
        }

        /// <summary>
        /// 地図上にマークの追加
        /// </summary>
        /// <param name="path"></param>
        private void addMark(string path)
        {
            if (!File.Exists(path))
                return;
            ExifInfo exifInfo = new ExifInfo(path);
            BitmapImage bmpImage = ylib.getBitmapImage(path);
            //  マークの追加
            MapMark mapMark = new MapMark();
            mapMark.mLocation = mMainWindow.mMapData.getCenter();
            mapMark.mTitle = Path.GetFileName(path);
            mapMark.mLocation = MapData.coordinates2BaseMap(exifInfo.getExifGpsCoordinate());
            mapMark.mLink = path;
            mapMark.mComment = exifInfo.getDateTime();
            mapMark.mComment += " [" + bmpImage.PixelWidth + "x" + bmpImage.PixelHeight + "]";
            mapMark.mComment += " " + exifInfo.getCamera("カメラ {0} {1}");
            mapMark.mComment += " " + exifInfo.getCameraSetting(" 1/{0} s F{1} ISO {2} 焦点距離 {3} mm");

            //  マークデータをダイヤログ表示
            MarkInput markInput = new MarkInput();
            markInput.mMapMark = mapMark;
            markInput.mMarkList = mMarkList;
            var result = markInput.ShowDialog();
            if (result == true) {
                mMarkList.add(mapMark);
                mMainWindow.mapDisp(false);
            }
        }

        /// <summary>
        /// イメージファイルをダイヤログ表示
        /// </summary>
        /// <param name="path"></param>
        private void dispPhotoData(string path)
        {
            if (mImageView != null) {
                mImageView.Close();
            }
            mImageView = new ImageView();
            mImageView.mImageList = Photos.ConvertAll(p => p.path);
            mImageView.mImagePath = path;
            mImageView.Show();
        }

        /// <summary>
        /// 座標データの追加・編集
        /// </summary>
        /// <param name="path"></param>
        private void editCoordinate(string path)
        {
            ExifInfo exifInfo = new ExifInfo(path);
            Point coord = exifInfo.getExifGpsCoordinate();
            InputBox dlg = new InputBox();
            dlg.Title = "座標登録";
            dlg.mEditText = coord.Y + "," + coord.X;
            if (dlg.ShowDialog() == true) {
                string[] data = dlg.mEditText.Split(',');
                if (1 <= data.Length) {
                    coord.X = ylib.string2double(data[1]);
                    coord.Y = ylib.string2double(data[0]);
                    if (exifInfo.setExifGpsCoordinate(coord))
                        exifInfo.save();
                }
            }
        }

        /// <summary>
        /// GPXファイルから座標を設定
        /// </summary>
        /// <param name="fileList">ファイルパスリスト</param>
        private void addGpsCoordinate(List<string> fileList)
        {
            string gpxPath = ylib.fileSelect(mGpxFolder, "gpx,fit");
            if (0 < gpxPath.Length) {
                mGpxFolder = Path.GetDirectoryName(gpxPath);
                loadGpsData(gpxPath);
                if (mGpxReader == null && mFitReader == null)
                    return;
                int count = 0;

                PbLoadPhoto.Minimum = 0;
                PbLoadPhoto.Maximum = fileList.Count;
                PbLoadPhoto.Value = 0;

                foreach (string path in fileList) {
                    ExifInfo exifInfo = new ExifInfo(path);
                    string datetime = exifInfo.getDateTime();
                    char[] sp = new char[] { ':', ' ' };
                    string[] ta = datetime.Split(sp);
                    datetime = string.Format("{0}/{1}/{2} {3}:{4}:{5}", ta[0], ta[1], ta[2], ta[3], ta[4], ta[5]);
                    DateTime dt = DateTime.Parse(datetime);
                    Point pos;
                    if (mGpxReader != null) {
                        pos = mGpxReader.getCoordinate(dt);
                    } else if (mFitReader != null) {
                        pos = mFitReader.getCoordinate(dt);
                    } else {
                        break;
                    }
                    if (!pos.isEmpty()) {
                        if (exifInfo.setExifGpsCoordinate(pos)) {
                            exifInfo.save();
                            count++;
                        }
                    }
                    PbLoadPhoto.Value++;
                    ylib.DoEvents();
                }
                MessageBox.Show($"{count}/{fileList.Count}の座標を設定");
                PbLoadPhoto.Value = 0;
            }
        }

        /// <summary>
        /// GPSファイルを読み込む
        /// </summary>
        /// <param name="path"></param>
        private void loadGpsData(string path)
        {
            string ext = Path.GetExtension(path);
            if (ext != null && ext.ToLower().CompareTo(".gpx") == 0) {
                mGpxReader = new GpxReader(path, GpxReader.DATATYPE.gpxData);
                if (mGpxReader.mListGpsData.Count == 0)
                    return;
                mGpxReader.dataChk();                                    //  エラーデータチェック
                mFitReader = null;
            } else if (ext != null && ext.ToLower().CompareTo(".fit") == 0) {
                mFitReader = new FitReader(path);
                mFitReader.getDataRecordAll(FitReader.DATATYPE.gpxData);
                if (mFitReader.mListGpsData.Count == 0)
                    return;
                mFitReader.dataChk();
                mGpxReader = null;
            }
        }

        /// <summary>
        /// コメントデータの追加・編集
        /// </summary>
        /// <param name="path"></param>
        private void editComment(string path)
        {
            ExifInfo exifInfo = new ExifInfo(path);
            string comment = exifInfo.getUserComment();
            if (comment.Length <= 0)
                comment += ylib.getIPTC(path)[4];
            InputBox dlg = new InputBox();
            dlg.Title = "コメント登録";
            dlg.mEditText = comment;
            if (dlg.ShowDialog() == true) {
                if (exifInfo.setUserComment(dlg.mEditText))
                    if (!exifInfo.save()) {
                        MessageBox.Show(exifInfo.mErrorMsg);
                    }
            }
        }

        /// <summary>
        /// 写真ファイルのExif情報をステータスバーに表示
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void setPhotoInfo(string path)
        {
            if (!File.Exists(path))
                return;
            Title = "フォトリスト [" + Path.GetFileName(path) + "]";
            //  ファイルプロパティ表示
            BitmapImage bmpImage = ylib.getBitmapImage(path);
            ExifInfo exifInfo = new ExifInfo(path);
            Point coodinate = exifInfo.getExifGpsCoordinate();
            string[] datetime = exifInfo.getDateTime().Split(':');
            TbFolderInfo.Text = datetime.Length == 5 ?
                $"{datetime[0]}/{datetime[1]}/{datetime[2]}:{datetime[3]}:{datetime[4]}" : exifInfo.getDateTime();
            if (coodinate.isEmpty())
                TbFolderInfo.Text += " (座標なし)";
            TbFolderInfo.Text += " " + ylib.getIPTC(path)[4] + exifInfo.getUserComment();
            TbFolderInfo.Text += " [" + bmpImage.PixelWidth + "x" + bmpImage.PixelHeight + "]";
            TbFolderInfo.Text += " " + exifInfo.getCamera("カメラ {0} {1}");
            TbFolderInfo.Text += " " + exifInfo.getCameraSetting(" 1/{0} s F{1} ISO {2} 焦点距離 {3} mm");
        }

        /// <summary>
        /// ListViewにフォルダ内の画像ファイルを設定
        /// </summary>
        /// <param name="folder">フォルダパス</param>
        private bool setPhotoData(string folder)
        {
            if (Photos == null) {
                Photos = new List<Photo>();
            }
            folder = folder.Replace("\n", "");
            folder = folder.Replace("\r", "");
            Photos.Clear();
            string[] files = ylib.getFiles(Path.Combine(folder, "*.jpg"), mRecursiveFolder);
            TbFolderInfo.Text = "ファイル数: " + files.Length;
            if (files == null || files.Length == 0)
                return false;
            List<string> fileList = sortFiles(files, mSortType, mSortReverse);

            PbLoadPhoto.Minimum = 0;
            PbLoadPhoto.Maximum = files.Length;
            PbLoadPhoto.Value = 0;
            foreach (string file in fileList) {
                Photo photo = new Photo();
                //photo.image = ylib.getBitmapImage(file, mThumbnailWidth);
                photo.image = ylib.getThumbnailImage(file, mThumbnailWidth, mThumbnailHeight);
                photo.title = Path.GetFileName(file);
                photo.path = file;
                Photos.Add(photo);
                PbLoadPhoto.Value++;
                ylib.DoEvents();
            }
            LvPhotoList.ItemsSource = new ReadOnlyCollection<Photo>(Photos);
            PbLoadPhoto.Value = 0;
            return true;
        }

        /// <summary>
        /// 写真リストのソート
        /// </summary>
        /// <param name="files">ファイルリスト</param>
        /// <param name="sortType">ソートタイプ</param>
        /// <param name="sortReverse">逆順</param>
        /// <returns>ソートしたファイルリスト</returns>
        private List<string> sortFiles(string[] files, SORTTYPE sortType, bool sortReverse)
        {
            List<FileInfo> fileList = new List<FileInfo>();
            foreach (string path in files)
                fileList.Add(new FileInfo(path));
            switch (sortType) {
                case SORTTYPE.path:     //  フルパスで比較
                    if (sortReverse)
                        fileList.Sort((b, a) => a.FullName.CompareTo(b.FullName));
                    else
                        fileList.Sort((a, b) => a.FullName.CompareTo(b.FullName));
                    break;
                case SORTTYPE.filename: //  拡張子を除くファイル名で比較
                    if (sortReverse)
                        fileList.Sort((b, a) => Path.GetFileNameWithoutExtension(a.Name).CompareTo(Path.GetFileNameWithoutExtension(b.Name)));
                    else
                        fileList.Sort((a, b) => Path.GetFileNameWithoutExtension(a.Name).CompareTo(Path.GetFileNameWithoutExtension(b.Name)));
                    break;
                case SORTTYPE.date:     //  ファイル日付で比較
                    if (sortReverse)
                        fileList.Sort((b, a) => a.LastWriteTime.CompareTo(b.LastWriteTime));
                    else
                        fileList.Sort((a, b) => a.LastWriteTime.CompareTo(b.LastWriteTime));
                    break;
                case SORTTYPE.size:     //  ファイルサイズで比較
                    if (sortReverse)
                        fileList.Sort((b, a) => a.Length.CompareTo(b.Length));
                    else
                        fileList.Sort((a, b) => a.Length.CompareTo(b.Length));
                    break;
            }
            return fileList.ConvertAll(p => p.FullName);
        }

        /// <summary>
        /// フォルダ名の登録
        /// </summary>
        /// <param name="folder"></param>
        private void setFolderList(string folder)
        {
            if (!Directory.Exists(folder))
                return;
            if (mDataFolders.Contains(folder)) {
                mDataFolders.Remove(folder);
            }
            mDataFolders.Insert(0, folder);
            CbPhotoFolder.ItemsSource = new ReadOnlyCollection<string>(mDataFolders);
        }

        /// <summary>
        /// ファイルからフォルダ名リストを取得
        /// </summary>
        private void folderListLoad()
        {
            if (File.Exists(mDataFolderListPath)) {
                List<string> dataFolders = ylib.loadListData(mDataFolderListPath);
                mDataFolders.Clear();
                foreach (string folder in dataFolders) {
                    string[] files = ylib.getFiles(Path.Combine(folder, "*.jpg"), mRecursiveFolder);
                    if (files != null && 0 < files.Length)
                        mDataFolders.Add(folder);
                    if (mDataFolderMax < mDataFolders.Count)
                        break;
                }
                mDataFolders.Sort();
                CbPhotoFolder.ItemsSource = new ReadOnlyCollection<string>(mDataFolders);
            }
        }

        /// <summary>
        /// フォルダ名リストをファイルに保存
        /// </summary>
        private void folderListSave()
        {
            ylib.saveListData(mDataFolderListPath, mDataFolders);
        }

        /// <summary>
        /// メッセージ表示ダイヤログ
        /// </summary>
        /// <param name="buf">メッセージ</param>
        /// <param name="title">タイトル</param>
        private void messageBox(string buf, string title)
        {
            InputBox dlg = new InputBox();
            //dlg.mMainWindow = this;           //  親Windowの中心に表示
            dlg.Title = title;
            dlg.mWindowSizeOutSet = true;
            dlg.mWindowWidth = 500.0;
            dlg.mWindowHeight = 400.0;
            dlg.mMultiLine = true;
            dlg.mReadOnly = true;
            dlg.mEditText = buf;
            dlg.ShowDialog();
        }
    }
}
