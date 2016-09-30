using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Compression;
using System.Windows.Media.Imaging;
using System.Windows.Media;

using System.Diagnostics;


using jp.giraffe.DirUtil;
using jp.giraffe.StreamUtil;

namespace ResChange
{
    public class HandleZipJpg
    {

        private int processHeight = 1500;

        public int ProcessHeight
        {
            get { return processHeight; }
            set { processHeight = value; }
        }
        private int jpegQuality = 85;

        public int JpegQuality
        {
            get { return jpegQuality; }
            set { jpegQuality = value; }
        }

        public void setLowQ()
        {
            processHeight = 900;
            JpegQuality = 70;
        }
        public void setNormalQ()
        {
            processHeight = 1500;
            JpegQuality = 85;
        }

        public void processOneZipFile(string fullPath, System.ComponentModel.BackgroundWorker bgWorker, int maxProgress)
        {
            int i = 0;
            if (!File.Exists(fullPath))
            {
                return;
            }

            using (var zipReadStream = File.OpenRead(fullPath))
            {
                string newFileName = createWriteFileName(fullPath);

                if (File.Exists(newFileName))
                {
                    return;
                }

                using (FileStream zipWriteStream = File.Create(newFileName))
                {
                    using (ZipArchive writeArchive = new ZipArchive(zipWriteStream, ZipArchiveMode.Create))
                    {
                        using (ZipArchive readArchive = new ZipArchive(zipReadStream, ZipArchiveMode.Read))
                        {
                            foreach (ZipArchiveEntry readEntry in readArchive.Entries)
                            {
                                string Name = readEntry.Name;

                                //string[] nameComponents = Name.Split('/');

                                string useName = string.Empty;

                                //string fullName = string.Empty;

                                if (isGraphicFile(Name))
                                {
                                    useName = createJpgFileName(Name);
                                    if (-1 < readEntry.FullName.IndexOf('/'))
                                    {
                                        int lastPos = readEntry.FullName.LastIndexOf('/');
                                        int oneBeforeLast = readEntry.FullName.LastIndexOf('/', lastPos - 1);

                                        if (-1 < oneBeforeLast)
                                        {
                                            useName = readEntry.FullName.Substring(oneBeforeLast + 1, lastPos - oneBeforeLast) + useName;
                                        }
                                    }
                                }


#if NOTUSE
                                if (1 < nameComponents.Length)
                                {
                                    for (int i = 0; i < nameComponents.Length; i++)
                                    {
                                        if (-1 != nameComponents[i].IndexOf(".jpg") ||
                                            -1 != nameComponents[i].IndexOf(".JPG") ||
                                            -1 != nameComponents[i].IndexOf(".png") ||
                                            -1 != nameComponents[i].IndexOf(".PNG"))
                                        {
                                            useName = nameComponents[i];
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    if (isGraphicFile(nameComponents[0]))
                                    {
                                        useName = nameComponents[0];
                                    }
                                    if (-1 != nameComponents[0].IndexOf(".jpg") ||
                                        -1 != nameComponents[0].IndexOf(".JPG") ||
                                        -1 != nameComponents[0].IndexOf(".png") ||
                                        -1 != nameComponents[0].IndexOf(".PNG"))
                                    {
                                        useName = nameComponents[0];
                                    }

                                }
#endif //NOTUSE

                                if (useName == string.Empty)
                                {
                                    continue;
                                }


                                ZipArchiveEntry WriteEntry = writeArchive.CreateEntry(useName);


                                using (Stream tmPreadEntryStream = readEntry.Open())
                                using (WrappingStream readEntryStream = new WrappingStream(tmPreadEntryStream))
                                {

                                    //TODO:JPGのストリームからの読み込みエラー調査
                                    //FileStream fs0 = new FileStream("0test.jpg", FileMode.Open);
                                    //readEntryStream.CopyTo(fs0);
                                    //fs0.Close();

                                    //fs0.Position = 0;
                                    using (MemoryStream tmPms = new MemoryStream())
                                    using (WrappingStream ms = new WrappingStream(tmPms))
                                    {
                                        readEntryStream.CopyTo(ms);
                                        BitmapImage page = new BitmapImage();
                                        {

                                            //int streamLength = ReadEntireZipStream(readEntryStream, ms);
                                            //ms.SetLength(streamLength);
                                            //readEntryStream.Read(ms.GetBuffer(), 0, (int)streamLength);
                                            ms.Flush();
                                            if (ms.CanSeek)
                                            {
                                                ms.Seek(0, SeekOrigin.Begin);
                                            }

                                            //stream CopyToバグってるぽい？
                                            //fs0.CopyTo(ms);
                                            //fs0.Close();
                                            //fs0.Dispose();


                                            page.BeginInit();
                                            page.DecodePixelHeight = processHeight;
                                            page.CacheOption = BitmapCacheOption.OnLoad;
                                            page.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                                            page.StreamSource = ms;
                                            //page.StreamSource = readEntryStream;
                                            page.EndInit();

#if NOTUSE
                                            BitmapEncoder enc2 = new JpegBitmapEncoder();
                                            FileStream fs = new FileStream("test2.jpg", FileMode.Create);

                                            enc2.Frames.Add(BitmapFrame.Create(page));

                                            enc2.Save(fs);
                                            fs.Close();
                                            fs.Dispose();
#endif
                                            int targetX;
                                            int targetY;

                                            /*
                                            if (page.Height > page.Width)
                                            {
                                                //単ページの場合
                                            }
                                            else
                                            {
                                                //見開きの場合
                                            }
                                             */
                                            if (page.PixelHeight > processHeight)
                                            {
                                                targetY = processHeight;
                                                targetX = (int)page.PixelHeight * processHeight / (int)page.PixelWidth;
                                            }
                                            else
                                            {
                                                targetY = (int)page.PixelHeight;
                                                targetX = (int)page.PixelWidth;
                                            }

                                            System.Windows.Rect rc = new System.Windows.Rect();
                                            rc.Width = targetX;
                                            rc.Height = targetY;

                                            DrawingVisual dv = new DrawingVisual();
                                            using (var dc = dv.RenderOpen())
                                            {
                                                dc.DrawImage(page, rc);
                                            }

                                            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(targetX, targetY, 96.0d, 96.0d, PixelFormats.Pbgra32);
                                            renderBitmap.Render(dv);



                                            JpegBitmapEncoder enc = new JpegBitmapEncoder();
                                            //enc.QualityLevel = 90;
                                            enc.QualityLevel = isTiff(readEntry.Name) ? 70 : 90;
                                            enc.Frames.Add(BitmapFrame.Create(renderBitmap));

                                            //FileStream fs = new FileStream("test2.jpg", FileMode.Create);


                                            //enc.Save(fs);
                                            //fs.Close();
                                            //fs.Dispose();




                                            using (Stream tmPwriteStream = WriteEntry.Open())
                                            using (WrappingStream writeStream = new WrappingStream(tmPwriteStream))
                                            {
                                                using (MemoryStream tmPwriteMs = new MemoryStream())
                                                using (WrappingStream writeMs = new WrappingStream(tmPwriteMs))
                                                {
                                                    enc.Save(writeMs);
                                                    //enc.Save(writeStream);
                                                    if (writeMs.CanSeek)
                                                    {
                                                        writeMs.Seek(0, SeekOrigin.Begin);
                                                    }
                                                    writeMs.CopyTo(writeStream);
                                                }
                                            }
                                            GC.Collect();
                                            GC.WaitForPendingFinalizers();
                                            GC.Collect();
                                        }
                                    }

                                }
                                i++;
                                bgWorker.ReportProgress(i, -1);
                            }
                        }
                    }
                }
                bgWorker.ReportProgress(maxProgress, -1);
            }
        }

        public void processOneZipFileByFile(string fullPath)
        {
            string newPath = createWriteFileDirName(fullPath);
            //ディレクトリが存在する場合は失敗
            Process currentProcess = Process.GetCurrentProcess();
            if (Directory.Exists(newPath))
            {
                return;
            }
            else
            {
                Directory.CreateDirectory(newPath);
            }

            using (var tmPzipReadStream = File.OpenRead(fullPath))
            using (var zipReadStream = new WrappingStream(tmPzipReadStream))
            {
                currentProcess.Refresh();
                Debug.WriteLine("ZipArchive befor open : {0}", currentProcess.WorkingSet64);

                using (ZipArchive readArchive = new ZipArchive(zipReadStream, ZipArchiveMode.Read))
                {
                    currentProcess.Refresh();
                    Debug.WriteLine("ZipArchive after open : {0}", currentProcess.WorkingSet64);
                    using (MemoryStream tmPms = new MemoryStream())
                    using (WrappingStream ms = new WrappingStream(tmPms))
                    {
                        foreach (ZipArchiveEntry readEntry in readArchive.Entries)
                        {
                            currentProcess.Refresh();
                            Debug.WriteLine("ZipArchive after getEntry : {0}", currentProcess.WorkingSet64);

                            string Name = readEntry.Name;
                            string useName = string.Empty;

                            if (isGraphicFile(Name))
                            {
                                useName = createJpgFileName(Name);
                            }
                            if (useName == string.Empty)
                            {
                                continue;
                            }

                            using (Stream tmPreadEntryStream = readEntry.Open())
                            using (WrappingStream readEntryStream = new WrappingStream(tmPreadEntryStream))
                            {
                                currentProcess.Refresh();
                                Debug.WriteLine("ZipArchive after openEntry : {0}", currentProcess.WorkingSet64);
                                //using (MemoryStream ms = new MemoryStream())
                                {
                                    if (ms.CanSeek)
                                    {
                                        ms.Seek(0, SeekOrigin.Begin);
                                        ms.SetLength(0);
                                    }

                                    currentProcess.Refresh();
                                    Debug.WriteLine("ZipArchive after createMemoryStream : {0}", currentProcess.WorkingSet64);

                                    readEntryStream.CopyTo(ms);

                                    currentProcess.Refresh();
                                    Debug.WriteLine("ZipArchive after copy MemoryStream : {0}", currentProcess.WorkingSet64);

                                    BitmapImage page = new BitmapImage();
                                    {
                                        currentProcess.Refresh();
                                        Debug.WriteLine("ZipArchive after construct BitmapImage : {0}", currentProcess.WorkingSet64);

                                        ms.Flush();
                                        if (ms.CanSeek)
                                        {
                                            ms.Seek(0, SeekOrigin.Begin);
                                        }

                                        try
                                        {

                                            page.BeginInit();
                                            page.CacheOption = BitmapCacheOption.OnLoad;
                                            page.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                                            page.StreamSource = ms;
                                            page.EndInit();
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Trace.WriteLine(ex.Message);
                                        }
                                        currentProcess.Refresh();
                                        Debug.WriteLine("ZipArchive after draw to  BitmapImage : {0}", currentProcess.WorkingSet64);

                                        int targetX;
                                        int targetY;

                                        if (page.Height > processHeight)
                                        {
                                            targetY = processHeight;
                                            targetX = (int)page.Height * processHeight / (int)page.Height;
                                        }
                                        else
                                        {
                                            targetY = (int)page.Height;
                                            targetX = (int)page.Width;
                                        }

                                        System.Windows.Rect rc = new System.Windows.Rect();
                                        rc.Width = targetX;
                                        rc.Height = targetY;

                                        currentProcess.Refresh();
                                        Debug.WriteLine("ZipArchive befor draw to  DrawingVisual : {0}", currentProcess.WorkingSet64);

                                        DrawingVisual dv = new DrawingVisual();
                                        using (var dc = dv.RenderOpen())
                                        {
                                            dc.DrawImage(page, rc);
                                        }

                                        currentProcess.Refresh();
                                        Debug.WriteLine("ZipArchive after draw to  DrawingVisual : {0}", currentProcess.WorkingSet64);

                                        RenderTargetBitmap renderBitmap = new RenderTargetBitmap(targetX, targetY, 96.0d, 96.0d, PixelFormats.Pbgra32);
                                        renderBitmap.Render(dv);

                                        dv = null;

                                        currentProcess.Refresh();
                                        Debug.WriteLine("ZipArchive after draw to  renderBitmap : {0}", currentProcess.WorkingSet64);


                                        JpegBitmapEncoder enc = new JpegBitmapEncoder();
                                        enc.QualityLevel = 85;
                                        enc.Frames.Add(BitmapFrame.Create(renderBitmap));

                                        currentProcess.Refresh();
                                        Debug.WriteLine("ZipArchive after enc to  JpegBitmapEncoder : {0}", currentProcess.WorkingSet64);


                                        string fileName = newPath + "\\" + useName;

                                        using (FileStream tmPfs = new FileStream(fileName, FileMode.Create))
                                        using (WrappingStream fs = new WrappingStream(tmPfs))
                                        {
                                            enc.Save(fs);
                                            fs.Close();
                                        }
                                        renderBitmap = null;
                                        enc = null;
                                        currentProcess.Refresh();
                                        Debug.WriteLine("ZipArchive before dispose BitmapImage : {0}", currentProcess.WorkingSet64);

                                    }
                                    page = null;
                                    System.GC.Collect();
                                    GC.WaitForPendingFinalizers();
                                    GC.Collect();
                                    currentProcess.Refresh();
                                    Debug.WriteLine("ZipArchive after dispose BitmapImage : {0}", currentProcess.WorkingSet64);
                                }
                                currentProcess.Refresh();
                                Debug.WriteLine("物理メモリ使用量: {0}", currentProcess.WorkingSet64);
                            }
                        }
                    }
                }
            }
            createZip(newPath + "\\");
            DirUtil.DeleteDirectory(newPath);
        }

        public void processOneZipFileByAllFile(string fullPath)
        {
            string wkPath = createWorkFileDirName(fullPath);
            if (Directory.Exists(wkPath))
            {
                return;
            }
            else
            {
                Directory.CreateDirectory(wkPath);
            }


            string newPath = createWriteFileDirName(fullPath);
            //ディレクトリが存在する場合は失敗
            Process currentProcess = Process.GetCurrentProcess();
            if (Directory.Exists(newPath))
            {
                return;
            }
            else
            {
                Directory.CreateDirectory(newPath);
            }

            UnzipGraphicFile(fullPath, wkPath);

            //            IEnumerable<string> files = Directory.EnumerateFiles(wkPath);
            foreach (string fileName in Directory.EnumerateFiles(wkPath))
            {
                ConvertJpegFile(newPath, fileName);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            createZip(newPath);
            DirUtil.DeleteDirectory(wkPath);
            DirUtil.DeleteDirectory(newPath);
        }

        private void ConvertJpegFile(string newPath, string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                BitmapImage page = new BitmapImage();
                {
                    page.BeginInit();
                    page.CacheOption = BitmapCacheOption.OnLoad;
                    page.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    page.StreamSource = fs;
                    page.EndInit();
                    page.Freeze();

                    int targetX;
                    int targetY;

                    if (page.Height > processHeight)
                    {
                        targetY = processHeight;
                        targetX = (int)page.Height * processHeight / (int)page.Height;
                    }
                    else
                    {
                        targetY = (int)page.Height;
                        targetX = (int)page.Width;
                    }

                    System.Windows.Rect rc = new System.Windows.Rect();
                    rc.Width = targetX;
                    rc.Height = targetY;

                    DrawingVisual dv = new DrawingVisual();
                    using (var dc = dv.RenderOpen())
                    {
                        dc.DrawImage(page, rc);
                    }

                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(targetX, targetY, 96.0d, 96.0d, PixelFormats.Pbgra32);
                    renderBitmap.Render(dv);

                    dv = null;

                    JpegBitmapEncoder enc = new JpegBitmapEncoder();
                    enc.QualityLevel = jpegQuality;

                    BitmapFrame bFrame = BitmapFrame.Create(renderBitmap);
                    enc.Frames.Add(bFrame);



                    string outFileName = newPath + "\\" + Path.GetFileName(createJpgFileName(fileName));

                    using (FileStream fsw = new FileStream(outFileName, FileMode.Create))
                    using (WrappingStream ws = new WrappingStream(fsw))
                    {
                        enc.Save(ws);
                        ws.Close();
                        fsw.Close();
                    }
                    fs.Close();
                    bFrame = null;
                    renderBitmap = null;
                    enc = null;
                    page = null;
                }
            }
        }

        private void UnzipGraphicFile(string fullPath, string wkPath)
        {
            using (var zipReadStream = File.OpenRead(fullPath))
            {
                using (ZipArchive readArchive = new ZipArchive(zipReadStream, ZipArchiveMode.Read))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        foreach (ZipArchiveEntry readEntry in readArchive.Entries)
                        {

                            string Name = readEntry.Name;
                            string useName = string.Empty;

                            if (isGraphicFile(Name))
                            {
                                useName = Name;
                            }
                            if (useName == string.Empty)
                            {
                                continue;
                            }
                            string fileName = wkPath + "\\" + useName;
                            using (FileStream fs = new FileStream(fileName, FileMode.Create))
                            {
                                using (var readEntryStream = readEntry.Open())
                                {
                                    readEntryStream.CopyTo(fs);
                                    fs.Close();
                                }
                            }
                        }
                    }
                }
            }

        }


        string createWriteFileName(string fullPath)
        {
            FileInfo cFileInfo = new FileInfo(fullPath);

            StringBuilder sb = new StringBuilder();

            sb.Append(cFileInfo.DirectoryName);
            sb.Append("\\");

            String Name = cFileInfo.Name;
            String Ext = cFileInfo.Extension;

            int nPos = Name.LastIndexOf(Ext);
            sb.Append(Name.Insert(nPos, "#s"));

            return sb.ToString();
        }

        string createWorkFileDirName(string fullPath)
        {
            string orgDir = Path.GetDirectoryName(fullPath);
            string orgBase = Path.GetFileNameWithoutExtension(fullPath);


            StringBuilder sb = new StringBuilder();

            sb.Append(orgDir);
            sb.Append("\\");
            sb.Append(orgBase);
            sb.Append("#wk");

            return sb.ToString();
        }


        string createWriteFileDirName(string fullPath)
        {
            string orgDir = Path.GetDirectoryName(fullPath);
            string orgBase = Path.GetFileNameWithoutExtension(fullPath);


            StringBuilder sb = new StringBuilder();

            sb.Append(orgDir);
            sb.Append("\\");
            sb.Append(orgBase);
            sb.Append("#s");

            return sb.ToString();
        }

        void createZip(string folder)
        {
            //ZipFile.CreateFromDirectory(folder, createZipFileName(folder));

            using (var zipStream = File.Create(createZipFileName(folder)))
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    foreach (string fileName in Directory.EnumerateFiles(folder))
                    {
                        string name = Path.GetFileName(fileName);
                        var entry = archive.CreateEntry(name);

                        using (FileStream fs = new FileStream(fileName, FileMode.Open))
                        {
                            using (var entryStream = entry.Open())
                            {
                                fs.CopyTo(entryStream);
                            }
                        }
                    }
                }

            }

        }


        string createZipFileName(string folder)
        {
            return folder + ".zip";
        }


        string createJpgFileName(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName) + ".jpg";
        }


        bool isGraphicFile(string fileName)
        {
            bool bRet = false;
            char[] invalidChars = Path.GetInvalidFileNameChars();

            if (fileName != string.Empty)
            {
                if (-1 != fileName.ToLower().IndexOf(".jpg") ||
                    -1 != fileName.ToLower().IndexOf(".jpeg") ||
                    -1 != fileName.ToLower().IndexOf(".bmp") ||
                    -1 != fileName.ToLower().IndexOf(".tif") ||
                    -1 != fileName.ToLower().IndexOf(".png"))
                {
                    if (fileName.IndexOfAny(invalidChars) < 0)
                    {
                        bRet = true;
                    }
                }
            }

            return bRet;
        }

        bool isTiff(string fileName)
        {
            bool bRet = false;

            if (fileName != string.Empty)
            {
                if (-1 != fileName.ToLower().IndexOf(".tif"))
                {
                    bRet = true;
                }
            }

            return bRet;
        }


        //for old stream support not used now!
        int ReadEntireZipStream(Stream inStream, Stream outStream)
        {
            int count = 0, bytesRead;
            byte[] buffer = new byte[1024];

            while ((bytesRead = inStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                count += bytesRead;
                outStream.Write(buffer, 0, bytesRead);
            }

            if (outStream.CanSeek)
            {
                outStream.Seek(0, SeekOrigin.Begin);
            }


            return count;
        }

    }


}