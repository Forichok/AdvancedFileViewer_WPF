using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AdvancedFileViewer_WPF.TreeView;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Mvvm.UI;
using Interpreter_WPF_3;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.Synthesis;


namespace AdvancedFileViewer_WPF.TreeView
{    public class FileSystemObjectInfo : ViewModelBase
    {
        #region Static Fields

        private static FileSystemObjectInfo bufferObjectInfo;
        private static bool isMovingOn;

        #endregion

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

        public bool IsSpyOn { get; set; }

        public ObservableCollection<FileSystemObjectInfo> Children { get; set; }

        public FileSystemObjectInfo Parent { get; set; }

        public ImageSource ImageSource { get; set; }


        public bool IsExpanded { get; set; }
        
        public FileSystemInfo FileSystemInfo { get; set; }

        private DriveInfo Drive { get; set;}

        #endregion

        #region Methods

        public void RaisePropertiesChanged()
        {
            UpdateTree(this);
        }

        private void UpdateTree(FileSystemObjectInfo fileSystemObjectInfo)
        {
            var tmpNode = fileSystemObjectInfo;
            while (tmpNode.Parent != null)
            {
                
                tmpNode = tmpNode.Parent;
            }
           // tmpNode.Children.Clear();
            tmpNode.ExploreFiles();
            tmpNode.ExploreDirectories();
                        
            UpdateChildren(tmpNode);

        }

        private void UpdateChildren(FileSystemObjectInfo fileSystemObjectInfo)
        {
            if (fileSystemObjectInfo.Children != null)
                foreach (var child in fileSystemObjectInfo.Children)
                {
                    
                    //child.Children.Clear();
                    child.ExploreDirectories();
                    child.ExploreFiles();
                    RaisePropertiesChanged("Children");
                    Task.Factory.StartNew(()=>UpdateChildren(child));
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

        private void RemoveDummy()
        {
            Children.Remove(GetDummy());
        }

        private void ExploreDirectories()
        {
            if (!ReferenceEquals(Drive, null))
            {
                if (!Drive.IsReady) return;
            }
            try
            {
                if (FileSystemInfo is DirectoryInfo)
                {
                    var directories = ((DirectoryInfo)FileSystemInfo).GetDirectories();
                    foreach (var directory in directories.OrderBy(d => d.Name))
                    {
                        if (!Equals((directory.Attributes & FileAttributes.System), FileAttributes.System) &&
                            !Equals((directory.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                        {
                            var newDirectory = new FileSystemObjectInfo(directory);
                            var directoriesInfo = new List<string>();
                            foreach (var dir in Children)
                            {
                                var curDirectoryPath = dir.FileSystemInfo.FullName;
                                directoriesInfo.Add(curDirectoryPath);
                                if (!Directory.Exists(curDirectoryPath))
                                {
                                    dir.Parent.Children.Remove(dir);
                                }
                            }
                            
                            if (!directoriesInfo.Contains(newDirectory.FileSystemInfo.FullName))
                            {
                                newDirectory.Parent = this;
                                Children.Add(newDirectory);
                            }
                        }
                    }
                }
            }
            catch
            {
                /*throw;*/
            }
        }

        private void ExploreFiles()
        {
            if (!ReferenceEquals(Drive, null))
            {
                if (!Drive.IsReady) return;
            }

            try
            {
                if (FileSystemInfo is DirectoryInfo)
                {
                    var files = ((DirectoryInfo) FileSystemInfo).GetFiles();
                    foreach (var file in files.OrderBy(d => d.Name))
                    {
                        if (!Equals((file.Attributes & FileAttributes.System), FileAttributes.System) &&
                            !Equals((file.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                        {


                            var newFile = new FileSystemObjectInfo(file);
                            var filesPaths = new List<string>();
                            foreach (var fileObj in Children)
                            {
                                filesPaths.Add(fileObj.FileSystemInfo.FullName);
                                var curDirectoryPath = fileObj.FileSystemInfo.FullName;
                                filesPaths.Add(curDirectoryPath);
                                if (!Directory.Exists(curDirectoryPath))
                                {
                                    fileObj.Parent.Children.Remove(fileObj);
                                }
                            }

                            if (!filesPaths.Contains(newFile.FileSystemInfo.FullName))
                            {
                                newFile.Parent = this;
                                Children.Add(newFile);
                            }
                        }
                    }
                }
            }
            catch
            {
                /*throw;*/
            }
        }

        #endregion

        #region Commands

        public ICommand DeleteCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {
                        File.Delete(obj.FileSystemInfo.FullName);
                    }
                    else
                    {
                        Directory.Delete(obj.FileSystemInfo.FullName);
                    }
                    
                    var parent = obj.Parent;
                    parent.Children.Remove(obj);
                    obj.RemoveDummy();
                    UpdateTree(parent);
                });
            }
        }

        public ICommand CopyCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) => { bufferObjectInfo = obj; });
            }
        }

        public ICommand PasteCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    try
                    {
                        if (bufferObjectInfo != null)
                        {
                            if (obj.FileSystemInfo.Extension == "")
                            {
                                var newPath = obj.FileSystemInfo.FullName + "\\" + bufferObjectInfo.FileSystemInfo.Name;
                                if (bufferObjectInfo.FileSystemInfo.Extension != "")
                                {
                                    File.Copy(bufferObjectInfo.FileSystemInfo.FullName, newPath);
                                    if (isMovingOn)
                                    {
                                        File.Delete(bufferObjectInfo.FileSystemInfo.FullName);
                                        bufferObjectInfo.Parent.Children.Clear();
                                        bufferObjectInfo = null;
                                        isMovingOn = false;
                                    }
                                }
                                else
                                {
                                    CopyDirectory(bufferObjectInfo.FileSystemInfo.FullName, newPath);
                                    if (isMovingOn)
                                    {
                                        File.Delete(bufferObjectInfo.FileSystemInfo.FullName);
                                        bufferObjectInfo.Parent.Children.Clear();
                                        bufferObjectInfo = null;
                                        isMovingOn = false;
                                    }
                                }
                            }

                            UpdateTree(obj);
                        }
                    }
                    catch (Exception e)
                    {

                    }
                });
            }
        }

        public ICommand MoveCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    isMovingOn = true;
                    bufferObjectInfo = obj;
                });
            }
        }

        public ICommand RenameCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {
                        var newName = GetStringFromDialog();
                        var fileExtention = obj.FileSystemInfo.Extension;
                        newName += newName.EndsWith(fileExtention) || !newName.Contains('.') ? fileExtention : "";
                        var oldPath = obj.FileSystemInfo.FullName;
                        var newPath = oldPath.Replace(obj.FileSystemInfo.Name, newName);

                        File.Move(oldPath, newPath);
                        //obj = new FileSystemObjectInfo(new FileInfo(newPath));
                        obj.Parent.Children.Remove(obj);
                        obj.Parent.Children.Add(new FileSystemObjectInfo(new FileInfo(newPath)));

                        UpdateTree(obj.Parent);
                    }
                    else
                    {

                    }

                });
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

        private void CopyDirectory(string SourcePath, string DestinationPath)
        {
            if (!Directory.Exists(DestinationPath))
            {
                Directory.CreateDirectory(DestinationPath);
            }
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",
                SearchOption.AllDirectories))
            {
                var newPath = dirPath.Replace(SourcePath, DestinationPath);
                Directory.CreateDirectory(newPath);
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                SearchOption.AllDirectories))
            {
                var newFilePath = newPath.Replace(SourcePath, DestinationPath);
                File.Copy(newPath, newFilePath);
            }
                
        }

        private string GetStringFromDialog()
        {
            var inputDialog = new InputDialog("Please, input new filename:");
            var result = string.Empty;

            if (inputDialog.ShowDialog() == true)
            {
                result = inputDialog.Answer;
            }
            return result;
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
