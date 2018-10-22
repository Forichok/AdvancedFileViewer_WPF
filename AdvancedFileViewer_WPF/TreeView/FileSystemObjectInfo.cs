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
        
        private FileSystemObjectInfo bufferObjectInfo;
        private bool isMovingOn;
        private static Queue<string> _logs=new Queue<string>();
        public static Users user { get; set; }
        public bool IsSpyOn { get; set; }

        public static Queue<string> Logs
        {
            get
            {
                while (_logs.Count > 10)
                {
                    _logs.Dequeue();
                }

                return _logs;
            }
            set => _logs = value;
        }

        public ObservableCollection<FileSystemObjectInfo> Children { get; set; }

        public FileSystemObjectInfo Parent { get; set; }

        public ImageSource ImageSource { get; set; }


        public bool IsExpanded { get; set; }
        
        public FileSystemInfo FileSystemInfo { get; set; }

        private DriveInfo Drive { get; set;}

        #endregion

        #region Methods

        public void UpdateTree()
        {
            var tmpNode = this;
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
            try
            {
                var tmpDirectories = new FileSystemObjectInfo[Children.Count];
                Children.CopyTo(tmpDirectories, 0);
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
                            foreach (var dir in tmpDirectories)
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
            try
            {


                if (!ReferenceEquals(Drive, null))
                {
                    if (!Drive.IsReady) return;
                }


                if (FileSystemInfo is DirectoryInfo)
                {
                    var files = ((DirectoryInfo) FileSystemInfo).GetFiles();
                    var newFiles = new FileSystemObjectInfo[Children.Count];
                    Children.CopyTo(newFiles, 0);
                    foreach (var file in files.OrderBy(d => d.Name))
                    {
                        if (!Equals((file.Attributes & FileAttributes.System), FileAttributes.System) &&
                            !Equals((file.Attributes & FileAttributes.Hidden), FileAttributes.Hidden))
                        {


                            var newFile = new FileSystemObjectInfo(file);
                            var filesPaths = new List<string>();
                            foreach (var fileObj in newFiles)
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
            catch (Exception) { }
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
                        Directory.Delete(obj.FileSystemInfo.FullName, true);
                    }
                    _logs.Enqueue($"{obj.FileSystemInfo.FullName} has been deleted");
                    var parent = obj.Parent;
                    parent.Children.Remove(obj);
                    obj.RemoveDummy();
                    parent.UpdateTree();
                });
            }
        }

        public ICommand CopyCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    _logs.Enqueue($"{obj.FileSystemInfo.FullName} has been copied");
                    bufferObjectInfo = obj;
                });
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
                                _logs.Enqueue($"{bufferObjectInfo.FileSystemInfo.FullName} has been inserted into {obj.FileSystemInfo.FullName}");
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
 
                            obj.UpdateTree();
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
                    _logs.Enqueue($"{bufferObjectInfo.FileSystemInfo.FullName} has been moved to {obj.FileSystemInfo.FullName}");
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
                    var newName = GetStringFromDialog();
                    if(newName.Trim().Length==0) return;
                    
                    var oldPath = obj.FileSystemInfo.FullName;
                    var fileExtention = obj.FileSystemInfo.Extension;
                    newName += newName.EndsWith(fileExtention) || !newName.Contains('.') ? fileExtention : "";
                    var newPath = oldPath.Replace(obj.FileSystemInfo.Name, newName);

                    if (obj.FileSystemInfo.Extension != "")
                    {
                        File.Move(oldPath, newPath);
                        obj.Parent.Children.Add(new FileSystemObjectInfo(new FileInfo(newPath)));
                    }
                    else
                    {
                        CopyDirectory(oldPath, newPath);
                        Directory.Delete(oldPath, true);
                        obj.Parent.Children.Add(new FileSystemObjectInfo(new DirectoryInfo(newPath)));
                    }
                    obj.Parent.Children.Remove(obj);
                    _logs.Enqueue($"{oldPath} has been renamed to {newPath}");
                    obj.UpdateTree();
                });
            }
        }

        public ICommand AtributesCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    MessageBox.Show(obj.FileSystemInfo.Attributes.ToString());
                    _logs.Enqueue($"{bufferObjectInfo.FileSystemInfo.FullName}  atributes has been showed");
                });
            }
        }

        public ICommand TripleDESEncryptCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {
                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.TripleDESEncrypt);
                    }
                });
            }
        }

        public ICommand TripleDESDecryptCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {
                        StartCrypting(obj.FileSystemInfo.FullName,Crypto.TripleDESDecrypt);
                    }
                });
            }
        }

        public ICommand RijndaelEncryptCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {
                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.RijndaelEncrypt);
                    }
                });
            }
        }

        public ICommand RijndaelDecryptCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {
                        
                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.RijndaelDecrypt);
                    }
                });
            }
        }
        public ICommand RC2EncryptCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {
                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.RC2Encrypt);
                    }
                });
            }
        }

        public ICommand RC2DecryptCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {

                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.RC2Decrypt);
                    }
                });
            }
        }
        public ICommand RSAEncryptCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {
                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.RC2Encrypt);
                    }
                });
            }
        }

        public ICommand RSADecryptCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (obj.FileSystemInfo.Extension != "")
                    {

                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.RC2Decrypt);
                    }
                });
            }
        }

        #endregion

        #region Helper methods


        private static void CopyDirectory(string SourcePath, string DestinationPath)
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

            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                SearchOption.AllDirectories))
            {
                var newFilePath = newPath.Replace(SourcePath, DestinationPath);
                File.Copy(newPath, newFilePath);
            }

        }

        private void StartCrypting(string path, Func<byte[], string, byte[]> DecryptFunc)
        {
            byte[] info;
            if (MainViewModel.isKeyRequired)
            {
                var inputDialog = new InputDialog("Please, input key:");
                var result = string.Empty;

                if (inputDialog.ShowDialog() == true)
                {
                    result = inputDialog.Answer;
                    if (result.Trim().Length == 0) return;
                    Crypto.Decryptkey = result;
                }
            }
            using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                info = reader.ReadAllBytes();
            }

            using (var writer =
                new BinaryWriter(new FileStream(path, FileMode.Truncate)))
            {
                
                writer.Write(DecryptFunc.Invoke(info, Crypto.Decryptkey));
            }

            FileSystemObjectInfo_PropertyChanged(this, null);
            _logs.Enqueue($"{path} has beed {DecryptFunc.Method.Name}");
        }

        private static string GetStringFromDialog()
        {
            var inputDialog = new InputDialog("Please, input new filename:");
            var result = string.Empty;

            if (inputDialog.ShowDialog() == true)
            {
                result = inputDialog.Answer;
            }
            return result;
        }

        #endregion

        void FileSystemObjectInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsSpyOn)
            {
                using (var writer = new StreamWriter(new FileStream(Directory.GetCurrentDirectory()+"\\logs.txt", FileMode.Truncate)))
                {
                    writer.Write(string.Join(Environment.NewLine,Logs));
                }
            }
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
