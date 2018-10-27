using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using AdvancedFileViewer_WPF.ViewModels;
using DevExpress.Mvvm;
using Interpreter_WPF_3;
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
        public bool IsSpyOn { get; set; }
        public ObservableCollection<FileSystemObjectInfo> Children { get; set; }

        public FileSystemObjectInfo Parent { get; set; }

        public ImageSource ImageSource { get; set; }

        public bool IsExpanded { get; set; }
        
        public FileSystemInfo FileSystemInfo { get; set; }        

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
            foreach (var child in copyChildren)
            {
                if (child.FileSystemInfo == null) continue;
                if (child.FileSystemInfo.FullName == path.Trim())
                {
                    child.Parent.Explore();
                    child.Explore();
                }
                UpdateDirectory(child, path);
            }
        }

        public void UpdateAll()
        {
            if (Children != null)
                foreach (var child in Children)
                {                    
                    child.Explore();
                    RaisePropertyChanged("Children");
                    Task.Factory.StartNew(()=>child.UpdateAll());
                }
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

        void FileSystemObjectInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
