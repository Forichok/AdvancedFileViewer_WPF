using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedFileViewer_WPF.TreeView
{
    static class FileSystemObjectHandler
    {
        public static void Delete(this FileSystemObjectInfo file)
        {
            if (file.FileSystemInfo.Extension != "")
            {
                File.Delete(file.FileSystemInfo.FullName);
            }
            else
            {
                Directory.Delete(file.FileSystemInfo.FullName, true);
            }
            //_logs.Enqueue($"{file.FileSystemInfo.FullName} has been deleted");
            var parent = file.Parent;
            parent.Children.Remove(file);
            file.RemoveDummy();
            parent.UpdateParentDirectory();

        }
    }
}
