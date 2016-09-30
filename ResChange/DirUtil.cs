using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jp.giraffe.DirUtil
{
    public class DirUtil
    {
        /// ----------------------------------------------------------------------------
        /// <summary>
        ///     指定したディレクトリをすべて削除します。</summary>
        /// <param name="stDirPath">
        ///     削除するディレクトリのパス。</param>
        /// ----------------------------------------------------------------------------
        public static void DeleteDirectory(string stDirPath)
        {
            DeleteDirectory(new System.IO.DirectoryInfo(stDirPath));
        }

        /// ----------------------------------------------------------------------------
        /// <summary>
        ///     指定したディレクトリをすべて削除します。</summary>
        /// <param name="hDirectoryInfo">
        ///     削除するディレクトリの DirectoryInfo。</param>
        /// ----------------------------------------------------------------------------
        static void DeleteDirectory(System.IO.DirectoryInfo hDirectoryInfo)
        {
            // すべてのファイルの読み取り専用属性を解除する
            foreach (System.IO.FileInfo cFileInfo in hDirectoryInfo.GetFiles())
            {
                if ((cFileInfo.Attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                {
                    cFileInfo.Attributes = System.IO.FileAttributes.Normal;
                }
            }

            // サブディレクトリ内の読み取り専用属性を解除する (再帰)
            foreach (System.IO.DirectoryInfo hDirInfo in hDirectoryInfo.GetDirectories())
            {
                DeleteDirectory(hDirInfo);
            }

            // このディレクトリの読み取り専用属性を解除する
            if ((hDirectoryInfo.Attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
            {
                hDirectoryInfo.Attributes = System.IO.FileAttributes.Directory;
            }

            // このディレクトリを削除する
            hDirectoryInfo.Delete(true);
        }
    }
}
