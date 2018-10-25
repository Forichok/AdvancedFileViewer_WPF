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

            Children.CollectionChanged += CollectionChanged;
            PropertyChanged += FileSystemObjectInfo_PropertyChanged;
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertiesChanged("Children");
        }

        public FileSystemObjectInfo(DriveInfo drive)
            : this(drive.RootDirectory)
        {
            Drive = drive;
        }
        #endregion

        #region Properties
        

        public static Users user { get; set; }
        public bool IsSpyOn { get; set; }

        public ObservableCollection<FileSystemObjectInfo> Children { get; set; }

        public FileSystemObjectInfo Parent { get; set; }

        public ImageSource ImageSource { get; set; }


        public bool IsExpanded { get; set; }
        
        public FileSystemInfo FileSystemInfo { get; set; }

        private DriveInfo Drive { get; set;}

        #endregion

        #region Methods

        public void UpdateParentDirectory()
        {

//            var tmpNode = Parent ?? this;



            Parent?.ExploreDirectories();
            Parent?.ExploreFiles();
            RaisePropertyChanged("Children");
            //UpdateChildren(this);

        }

        public void UpdateAll()
        {
            if (Children != null)
                foreach (var child in Children)
                {
                    child.ExploreDirectories();
                    child.ExploreFiles();
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

        private void ExploreDirectories()
        {
            if (!ReferenceEquals(Drive, null))
            {
                if (!Drive.IsReady) return;
            }

            var tmp = new FileSystemObjectInfo[0];
            if (Children != null)
            {
                tmp = new FileSystemObjectInfo[Children.Count];
                Children.CopyTo(tmp, 0);
            }

            var tmpDirectories = tmp.ToList();

            if (FileSystemInfo is DirectoryInfo)
            {
                DirectoryInfo[] directories = new DirectoryInfo[0];
                try
                {
                    directories = ((DirectoryInfo) FileSystemInfo).GetDirectories("*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception e)
                {

                }

                foreach (var directory in directories.OrderBy(d => d.Name))
                {
                    if (!Equals((directory.Attributes & FileAttributes.System), FileAttributes.System) &&
                        !Equals((directory.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                    {
                        var newDirectory = new FileSystemObjectInfo(directory);
                        var directoriesInfo = new List<string>();
                        foreach (var dir in Children)
                        {
                            if (dir.FileSystemInfo == null) continue;
                            var curDirectoryPath = dir.FileSystemInfo.FullName;

                            directoriesInfo.Add(curDirectoryPath);

                            if (!(Directory.Exists(curDirectoryPath) || File.Exists(curDirectoryPath)))
                            {
                                Application.Current.Dispatcher.Invoke(() => tmpDirectories.Remove(dir));
                                directoriesInfo.Remove(dir.FileSystemInfo.FullName);
                            }
                        }

                        if (!directoriesInfo.Contains(newDirectory.FileSystemInfo.FullName))
                        {
                            newDirectory.Parent = this;
                            tmpDirectories.Add(newDirectory);
                        }
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                    Children = new ObservableCollection<FileSystemObjectInfo>(tmpDirectories));
                RaisePropertyChanged("Children");
            }
        }

        private void ExploreFiles()
        {
            if (!ReferenceEquals(Drive, null))
            {
                if (!Drive.IsReady) return;
            }

            if (FileSystemInfo is DirectoryInfo)
            {
                FileInfo[] files =new FileInfo[0];
                try
                {
                    files = ((DirectoryInfo)FileSystemInfo).GetFiles();
                }
                catch (Exception e)
                {
                }
                var tmp = new FileSystemObjectInfo[Children.Count];
                Children.CopyTo(tmp, 0);
                var tmpFiles = tmp.ToList();
                foreach (var file in files.OrderBy(d => d.Name))
                {
                    if (!Equals((file.Attributes & FileAttributes.System), FileAttributes.System) &&
                        !Equals((file.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                    {


                        var newFile = new FileSystemObjectInfo(file);
                        var filesPaths = new List<string>();
                        foreach (var fileObj in Children)
                        {
                            if (fileObj.FileSystemInfo == null) continue;

                            
                            var curDirectoryPath = fileObj.FileSystemInfo.FullName;
                            filesPaths.Add(curDirectoryPath);
                            if (!(Directory.Exists(curDirectoryPath) || File.Exists(curDirectoryPath)))
                            {
                                tmpFiles.Remove(fileObj);
                                filesPaths.Remove(fileObj.FileSystemInfo.FullName);
                            }
                        }

                        if (!filesPaths.Contains(newFile.FileSystemInfo.FullName))
                        {
                            newFile.Parent = this;

                            tmpFiles.Add(newFile);
                        }
                    }
                }

                Application.Current.Dispatcher.Invoke(() => Children = new ObservableCollection<FileSystemObjectInfo>(tmpFiles));
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
                            ExploreDirectories();
                            ExploreFiles();
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
