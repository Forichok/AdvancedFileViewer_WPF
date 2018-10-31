using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using AdvancedFileViewer_WPF.TreeView;
using DevExpress.Mvvm;
using Interpreter_WPF_3;

namespace AdvancedFileViewer_WPF
{
    class TreeViewModel:ViewModelBase
    {
        public bool IsKeyRequired { get; set; }
        private Users _currentUser;
        private ObservableCollection<string> _logs;
        public ObservableCollection<string> Logs
        {
            get
            {
                while (_logs.Count > CommandBufferSize)
                {
                    var firstLog = _logs.FirstOrDefault();
                    if (firstLog.Contains("has been deleted"))
                    {
                        var fullName = firstLog.Split(new[] { "has been deleted" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                        var fileName = fullName.Substring(fullName.LastIndexOf('\\'));
                        var backUpPath = Directory.GetCurrentDirectory() + '\\' + _currentUser.Name + fileName;
                        if (Directory.Exists(backUpPath))
                        {
                            Directory.Delete(backUpPath);
                        }
                        if (File.Exists(backUpPath))
                        {
                            File.Delete(backUpPath);
                        }

                    }
                    _logs.RemoveAt(0);
                }
                return _logs;
            }

            set => _logs = value;
        }
        private FileSystemObjectInfo _bufferObjectInfo;
        private bool _isMovingOn;
        private readonly Timer _timer;        
        public int CommandBufferSize { get; set; } = 3;
        public ObservableCollection<FileSystemObjectInfo> CurrentDirectories { get; set; }
        private readonly List<FileSystemObjectInfo> _spyingSystemInfos = new List<FileSystemObjectInfo>();


        public TreeViewModel(Users user)
        {
            _currentUser = user;
            CurrentDirectories = new ObservableCollection<FileSystemObjectInfo>(new List<FileSystemObjectInfo>() { new FileSystemObjectInfo(new DirectoryInfo(_currentUser.CurrentDirectory)) });
            _timer = new Timer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = 1000;
            _timer.Start();
            LoadSpying();
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var info in _spyingSystemInfos)
            {
                FileSystemInfo newInfo = null;
                var path = info.FileSystemInfo.FullName;

                if (Directory.Exists(path))
                    newInfo = new DirectoryInfo(path);
                if (File.Exists(path))
                    newInfo = new FileInfo(path);

                if (newInfo == null || info.FileSystemInfo.LastAccessTime != newInfo.LastAccessTime ||
                    info.FileSystemInfo.LastWriteTime != newInfo.LastWriteTime)
                {
                    info.IsModified = true;
                }
            }
        }

        #region FileCommands

        public ICommand TreeDoubleClickCommand
        {
            get
            {
                return new DelegateCommand<object>((obj) =>
                {
                    try
                    {
                        var fileSystemObjectInfo = obj as FileSystemObjectInfo;
                        OpenFile(fileSystemObjectInfo.FileSystemInfo);
                    }
                    catch (Exception)
                    {

                    }
                });
            }
        }

        public ICommand SpyOnCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (_currentUser != null)
                    {
                        if (obj.IsSpyOn)
                        {
                            if (!_spyingSystemInfos.Contains(obj))
                                _spyingSystemInfos.Add(obj);
                        }
                        else
                        {
                            _spyingSystemInfos.Remove(obj);
                        }
                    }
                    _currentUser.SpyingDirectories = string.Join(";", _spyingSystemInfos.Select((i) => i.FileSystemInfo.FullName));
                    DbHandler.AddOrUpdateUserInfo(_currentUser);
                });
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((file) =>
                {
                    var backUpPath = Directory.GetCurrentDirectory() + '\\' + _currentUser.Name;
                    Directory.CreateDirectory(backUpPath);
                    backUpPath += '\\' + file.FileSystemInfo.Name;

                    if (file.FileSystemInfo.Extension != "")
                    {
                        File.Move(file.FileSystemInfo.FullName, backUpPath);
                    }
                    else
                    {
                        Directory.Move(file.FileSystemInfo.FullName, backUpPath);
                    }
                    UpdateLogs($"{file.FileSystemInfo.FullName} has been deleted");
                    var parent = file.Parent;
                    parent.Children.Remove(file);
                    file.RemoveDummy();
                    parent.UpdateParentDirectory();
                });
            }
        }

        public ICommand CopyCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    _bufferObjectInfo = obj;
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
                        if (_bufferObjectInfo != null)
                        {
                            if (obj.FileSystemInfo.Extension == "")
                            {
                                if (_isMovingOn)
                                    UpdateLogs($"{_bufferObjectInfo.FileSystemInfo.FullName} has been moved to {obj.FileSystemInfo.FullName}");
                                else
                                    UpdateLogs($"{_bufferObjectInfo.FileSystemInfo.FullName} has been inserted into {obj.FileSystemInfo.FullName}");

                                var newPath = obj.FileSystemInfo.FullName + "\\" + _bufferObjectInfo.FileSystemInfo.Name;
                                if (_bufferObjectInfo.FileSystemInfo.Extension != "")
                                {
                                    File.Copy(_bufferObjectInfo.FileSystemInfo.FullName, newPath);
                                    if (_isMovingOn)
                                    {
                                        File.Delete(_bufferObjectInfo.FileSystemInfo.FullName);
                                        _bufferObjectInfo.Parent.Children.Remove(_bufferObjectInfo);
                                        _bufferObjectInfo.UpdateParentDirectory();
                                        _bufferObjectInfo = null;
                                        _isMovingOn = false;
                                    }
                                }
                                else
                                {
                                    CopyDirectory(_bufferObjectInfo.FileSystemInfo.FullName, newPath);
                                    if (_isMovingOn)
                                    {
                                        Directory.Delete(_bufferObjectInfo.FileSystemInfo.FullName);
                                        _bufferObjectInfo.Parent.Children.Remove(_bufferObjectInfo);
                                        _bufferObjectInfo.UpdateParentDirectory();
                                        _bufferObjectInfo = null;
                                        _isMovingOn = false;
                                    }
                                }
                            }
                            obj.UpdateParentDirectory();
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
                    _isMovingOn = true;
                    _bufferObjectInfo = obj;
                });
            }
        }

        public ICommand RenameCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    var newName = GetStringFromDialog($"Please, input new filename for {obj.FileSystemInfo.Name}:");
                    if (newName.Trim().Length == 0) return;

                    var oldPath = obj.FileSystemInfo.FullName;
                    var fileExtention = obj.FileSystemInfo.Extension;
                    newName += newName.EndsWith(fileExtention) || !newName.Contains('.') ? fileExtention : "";
                    var newPath = oldPath.Replace(obj.FileSystemInfo.Name, newName);
                    try
                    {
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

                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(@"File is already in use. \n Try again later.");
                    }
                    obj.Parent.Children.Remove(obj);
                    UpdateLogs($"{oldPath} has been renamed to {newPath}");
                    obj.UpdateParentDirectory();
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
                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.TripleDESDecrypt);
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
                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.RSAEncrypt);
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

                        StartCrypting(obj.FileSystemInfo.FullName, Crypto.RSADecrypt);
                    }
                });
            }
        }

        #endregion


        #region Methods

        private void LoadSpying()
        {
            var paths = _currentUser.SpyingDirectories.Split(';');
            foreach (var path in paths)
            {
                var info = FileSystemObjectInfo.UpdateSpying(CurrentDirectories.FirstOrDefault(), path);

                if (info != null)
                {
                    _spyingSystemInfos.Add(info);
                }
            }}

        private void OpenFile(FileSystemInfo file)
        {
            if (file.Extension == "") return;
            var pi = new ProcessStartInfo(file.FullName)
            {
                Arguments = Path.GetFileName(file.FullName),
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(file.FullName),
                Verb = "OPEN"
            };
            Process.Start(pi);
        }

        private static void CopyDirectory(string sourcePath, string destinationPath)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*",
                SearchOption.AllDirectories))
            {
                var newPath = dirPath.Replace(sourcePath, destinationPath);
                Directory.CreateDirectory(newPath);
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",
                SearchOption.AllDirectories))
            {
                var newFilePath = newPath.Replace(sourcePath, destinationPath);
                File.Copy(newPath, newFilePath);
            }
        }

        private void StartCrypting(string path, Func<byte[], string, byte[]> decryptFunc)
        {
            byte[] info;
            if (IsKeyRequired)
            {
                var inputDialog = new InputDialog("Please, input key:");

                if (inputDialog.ShowDialog() == true)
                {
                    var result = inputDialog.Answer;
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

                writer.Write(decryptFunc.Invoke(info, Crypto.Decryptkey));
            }

            UpdateLogs($"{path} has beed {decryptFunc.Method.Name}");
        }

        private void UpdateLogs(string log)
        {
            log = log.Trim();
            if (log.Length == 0) return;
            _logs.Add(log);
            _currentUser.Logs = String.Join("; ", Logs);
            DbHandler.AddOrUpdateUserInfo(_currentUser);
            RaisePropertyChanged("Logs");
        }

        private static string GetStringFromDialog(string str)
        {
            var inputDialog = new InputDialog(str);
            var result = string.Empty;

            if (inputDialog.ShowDialog() == true)
            {
                result = inputDialog.Answer;
            }
            return result;
        }


        #endregion
    }
}
