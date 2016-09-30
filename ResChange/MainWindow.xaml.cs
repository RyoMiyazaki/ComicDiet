//
// 2015/04/01
// V1.0.0.0 - > V1.0.1.0
// tifサポートとtifファイルのJPG品質調整
//
// 2015/04/03
// V1.0.1.0 - > V1.1.0.0
// Gridによる複数ファイル処理対応
//


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO.Compression;
using System.Collections.ObjectModel;

using jp.giraffe.CollectionUtil;

namespace ResChange
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker _backgroundWorker;
        int m_nFiles;

        //string m_CurrentFile;
        ComicFile[] m_ComicList;
        bool bLowQuality;


        public MainWindow()
        {
            InitializeComponent();
            _backgroundWorker = new BackgroundWorker();
            m_nFiles = 0;

            m_ComicList = null;
            bLowQuality = false;

            ObservableCollectionWithItemNotify<ComicFile> data = new ObservableCollectionWithItemNotify<ComicFile>();
            this.MyGrid.ItemsSource = data;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _backgroundWorker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            _backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);
            _backgroundWorker.WorkerReportsProgress = true;

        }


        private void btnChange_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollectionWithItemNotify<ComicFile> data = (ObservableCollectionWithItemNotify<ComicFile>)this.MyGrid.ItemsSource;

            if (1 > data.Count)
            {
                return;
            }

            m_ComicList = new ComicFile[data.Count];
            data.CopyTo(m_ComicList, 0);


            progBar.Minimum = 0;
            //progBar.Maximum = m_nFiles;
            progBar.Value = 0;

            //HandleZipJpg zip = new HandleZipJpg();

            //zip.processOneZipFileByAllFile(txtFileName.Text);
            //zip.processOneZipFile(txtFileName.Text);

            //m_CurrentFile = txtFileName.Text;

            this.btnChange.IsEnabled = false;
            this.btnClearAll.IsEnabled = false;
            bLowQuality = this.chkLowHQ.IsChecked == true ? true : false;
            _backgroundWorker.RunWorkerAsync(this);

        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            HandleZipJpg zip = new HandleZipJpg();

            if (bLowQuality)
            {
                zip.setLowQ();
            }

            int i = 0;
            for (i = 0; i < m_ComicList.Length; i++)
            {
                ComicFile currentFile = m_ComicList[i];
                _backgroundWorker.ReportProgress(0, i);

                zip.processOneZipFile(currentFile.fullName, _backgroundWorker, currentFile.fileCount);
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (0 == e.ProgressPercentage)
            {
                int nIndex = (int)e.UserState;
                progBar.Maximum = m_ComicList[nIndex].fileCount;

                ObservableCollectionWithItemNotify<ComicFile> data = (ObservableCollectionWithItemNotify<ComicFile>)this.MyGrid.ItemsSource;
                data[nIndex].isWorked = 1;
                data[nIndex].fileName = data[nIndex].fileName;

                if (0 < nIndex)
                {
                    data[nIndex - 1].isWorked = 2;
                    data[nIndex - 1].fileName = data[nIndex - 1].fileName;
                }
            }

            progBar.Value = e.ProgressPercentage;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ObservableCollectionWithItemNotify<ComicFile> data = (ObservableCollectionWithItemNotify<ComicFile>)this.MyGrid.ItemsSource;
            data[data.Count - 1].isWorked = 2;
            data[data.Count - 1].fileName = data[data.Count - 1].fileName;
            
            this.btnChange.IsEnabled = true;
            this.btnClearAll.IsEnabled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];

            FileInfo cFileInfo = new FileInfo(files[0]);
            string stExtension = cFileInfo.Extension;

            if (".ZIP" == stExtension.ToUpper())
            {
                txtFileName.Text = cFileInfo.FullName;

                using (ZipArchive archive = ZipFile.Open(txtFileName.Text, ZipArchiveMode.Read))
                {
                    List<ZipArchiveEntry> fileList = archive.Entries.ToList();
                    m_nFiles = fileList.Count;
                }

            }

        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }

        private void MyGrid_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];

            foreach(string filename in files)
            {
                FileInfo cFileInfo = new FileInfo(filename);
                string stExtension = cFileInfo.Extension;

                if (".ZIP" == stExtension.ToUpper())
                {
                    ObservableCollectionWithItemNotify<ComicFile> data = (ObservableCollectionWithItemNotify<ComicFile>)this.MyGrid.ItemsSource;

                    ComicFile file = new ComicFile();

                    file.fileName = System.IO.Path.GetFileName(filename);
                    file.fullName = filename;

                    using (ZipArchive archive = ZipFile.Open(filename, ZipArchiveMode.Read))
                    {
                        List<ZipArchiveEntry> fileList = archive.Entries.ToList();
                        file.fileCount = fileList.Count;
                    }

                    data.Add(file);

                    //txtFileName.Text = cFileInfo.FullName;


                }
            }

        }

        private void MyGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "fileName":
                    e.Column.Header = "名前";
                    e.Column.DisplayIndex = 0;
                    break;
                default:
                    e.Cancel = true;
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollectionWithItemNotify<ComicFile> data = (ObservableCollectionWithItemNotify<ComicFile>)this.MyGrid.ItemsSource;
            data.Clear();
        }



    }
    public class ComicFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _fileName;

        public string fileName
        {
            get { return _fileName; }
            set { _fileName = value; NotifyPropertyChanged(); }
        }

        private string _fullName;

        public string fullName
        {
            get { return _fullName; }
            set { _fullName = value; NotifyPropertyChanged(); }
        }
        private int _fileCount;

        public int fileCount
        {
            get { return _fileCount; }
            set { _fileCount = value; NotifyPropertyChanged(); }
        }
        private int _isWorked;

        public int isWorked
        {
            get { return _isWorked; }
            set { _isWorked = value; NotifyPropertyChanged(); }
        }

        public ComicFile()
        {
            _fileName = string.Empty;
            _fullName = string.Empty;
            _fileCount = 0;
            _isWorked = 0;
        }

        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
