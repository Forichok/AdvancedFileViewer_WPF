using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using DevExpress.Mvvm;
using Application = System.Windows.Application;


namespace AdvancedFileViewer_WPF.TreeView
{    public class FileSystemObjectInfo : ViewModelBase
    {
        #region Constructors

        public FileSystemObjectInfo(FileSystemInfo info)
        {
            if (this is DummyFileSystemObjectInfo) return;
            Children = new ObservableCollection<FileSystemObjectInfo>();
            FileSystemInfo = info;
            if (info is DirectoryInfo)
            {
                ImageSource = FolderManager.GetImageSource(info.FullName, ShellManager.ItemState.Close);
                AddDummy();
            }
            else if (info is FileInfo)
            {
                ImageSource = FileManager.GetImageSource(info.FullName);
            }            
            PropertyChanged += FileSystemObjectInfo_PropertyChanged;
        }
        #endregion

        #region Properties

        public bool IsModified { get; set; }
        public bool IsSpyOn { get; set; }
        public bool IsExpanded { get; set; }
        
        public ImageSource ImageSource { get; set; }
        public FileSystemObjectInfo Parent { get; set; }
        public FileSystemInfo FileSystemInfo { get; set; }
        public ObservableCollection<FileSystemObjectInfo> Children { get; set; }
        
        #endregion

        #region Methods

        public void UpdateParentDirectory()
        {            
            Explore();
            RaisePropertyChanged("Children");
        }

        public static void UpdateDirectory(FileSystemObjectInfo root,string path)
        {
            if(root.Children==null) return;
            var copyChildren = new List<FileSystemObjectInfo>(root.Children);
            root.Explore();
            foreach (var child in copyChildren)
            {
                if (child.FileSystemInfo == null) continue;
                if (child.FileSystemInfo.FullName == path.Trim())
                {
                    child.Parent.Explore();
                    child.Explore();
                }
                
                //Task.Factory.StartNew(()=>UpdateDirectory(child, path));
                Application.Current.Dispatcher.Invoke(() => UpdateDirectory(child, path));
            }
        }

        public static FileSystemObjectInfo UpdateSpying(FileSystemObjectInfo root, string path)
        {
            if (root.Children == null) return null;
            
            root.Explore();
            foreach (var child in root.Children)
            {
                if (child.FileSystemInfo == null) continue;
                if (child.FileSystemInfo.FullName == path.Trim())
                {
                    child.IsSpyOn = true;
                    return child;
                }
                
                //Task.Factory.StartNew(() => UpdateSpying(child, path));

                Application.Current.Dispatcher.Invoke(() => UpdateDirectory(child, path));
            }
            return null;
        }

        private void AddDummy()
        {
            Children.Add(new DummyFileSystemObjectInfo());
        }

        private bool HasDummy()
        {
            return !ReferenceEquals(GetDummy(), null);
        }

        private DummyFileSystemObjectInfo GetDummy()
        {
            var list = Children.OfType<DummyFileSystemObjectInfo>().ToList();
            if (list.Count > 0) return list.First();
            return null;
        }

        public void RemoveDummy()
        {
            Children.Remove(GetDummy());
        }

        private void Explore()
        {
            if (FileSystemInfo is DirectoryInfo)
            {
                var systemObjects = new List<FileSystemInfo>();
                try
                {
                    systemObjects.AddRange(((DirectoryInfo)FileSystemInfo).GetDirectories().OrderBy(d => d.Name));
                    systemObjects.AddRange((FileSystemInfo as DirectoryInfo).GetFiles().OrderBy(d => d.Name));

                }
                catch (Exception e)
                {
                    // ignored
                }

                var filesPaths = Children.Select((i) =>i.FileSystemInfo?.FullName).ToList();

                foreach (var systemInfo in systemObjects)
                {
                    if (!Equals((systemInfo.Attributes & FileAttributes.System), FileAttributes.System) &&
                        !Equals((systemInfo.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                    {
                        if (!filesPaths.Contains(systemInfo.FullName))
                        {
                            filesPaths.Add(systemInfo.FullName);
                            var newFile = new FileSystemObjectInfo(systemInfo);
                            newFile.Parent = this;
                            Application.Current.Dispatcher.Invoke(()=>Children.Add(newFile));
                        }
                    }
                }
                var childrenCopy = Children.Select((i) => i).ToList();
                foreach (var info in childrenCopy)
                {
                    if (info.FileSystemInfo == null) continue;
                    if (!(File.Exists(info.FileSystemInfo.FullName)||Directory.Exists(info.FileSystemInfo.FullName)))
                        Application.Current.Dispatcher.Invoke(()=>Children.Remove(info));
                }
                RaisePropertyChanged("Children");
            }
        }

        #endregion

        private void FileSystemObjectInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (FileSystemInfo is DirectoryInfo)
            {
                if (string.Equals(e.PropertyName, "IsExpanded", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (IsExpanded)
                    {
                        ImageSource = FolderManager.GetImageSource(FileSystemInfo.FullName, ShellManager.ItemState.Open);
                        if (HasDummy())
                        {
                            RemoveDummy();
                            Explore();
                        }
                    }
                    else
                    {
                        ImageSource = FolderManager.GetImageSource(FileSystemInfo.FullName, ShellManager.ItemState.Close);
                    }
                }
            }
        }

        private class DummyFileSystemObjectInfo : FileSystemObjectInfo
        {
            public DummyFileSystemObjectInfo()
                : base(new DirectoryInfo("DummyFileSystemObjectInfo"))
            {
            }
        }
    }
}
