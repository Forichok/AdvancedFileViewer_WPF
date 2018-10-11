using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace AdvancedFileViewer_WPF.TreeView
{
    public static class FolderManager
    {
        public static ImageSource GetImageSource(string directory, ShellManager.ItemState folderType)
        {
            try
            {
                return FolderManager.GetImageSource(directory, new Size(16, 16), folderType);
            }
            catch
            {
                throw;
            }
        }

        public static ImageSource GetImageSource(string directory, Size size, ShellManager.ItemState folderType)
        {
            try
            {
                using (var icon = ShellManager.GetIcon(directory, ShellManager.ItemType.Folder, ShellManager.IconSize.Large, folderType))
                {
                    return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight((int)size.Width, (int)size.Height));
                }
            }
            catch
            {
                throw;
            }
        }
    }

    public static class FileManager
    {
        public static ImageSource GetImageSource(string filename)
        {
            try
            {
                return FileManager.GetImageSource(filename, new Size(16, 16));
            }
            catch
            {
                throw;
            }
        }

        public static ImageSource GetImageSource(string filename, Size size)
        {
            try
            {
                using (var icon = ShellManager.GetIcon(Path.GetExtension(filename), ShellManager.ItemType.File, ShellManager.IconSize.Small, ShellManager.ItemState.Undefined))
                {
                    return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight((int)size.Width, (int)size.Height));
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
