using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using AdvancedFileViewer_WPF.TreeView;
using DevExpress.Mvvm;
using Interpreter_WPF_3;

namespace AdvancedFileViewer_WPF.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        private int commandBufferSize = 10;
        public ObservableCollection<FileSystemObjectInfo> CurrentDirectories { get; set; }
        private FileSystemObjectInfo _bufferObjectInfo;
        private bool _isMovingOn;
        private ObservableCollection<string> _logs;


        public ObservableCollection<string> Logs
        {
            get
            {
                while (_logs.Count>10)
                {
                    _logs.RemoveAt(0);
                }
                return _logs;
            }

            set => _logs = value;
        }

        public string UserName { get; set; } = "admin";
        public string Test { get; set; } = "Test";
        public string Password { get; set; } = "";
        public Users CurrentUser { get; set; }
        public bool IsKeyRequired { get; set; }
        

        public MainViewModel()
        {
            
            CurrentUser = DbHandler.GetUserInfo(UserName, Password);
            _logs = new ObservableCollection<string>(CurrentUser.Logs.Split(';'));
            
            CurrentDirectories = new ObservableCollection<FileSystemObjectInfo>(new List<FileSystemObjectInfo>(){new FileSystemObjectInfo(new DirectoryInfo(CurrentUser.CurrentDirectory))});
        }


        #region Commands
        public ICommand SelectRootCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    var fbd = new FolderBrowserDialog();
                    var result = fbd.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        CurrentDirectories.Clear();
                        CurrentDirectories.Add(new FileSystemObjectInfo(new DirectoryInfo(fbd.SelectedPath)));
                        CurrentUser.CurrentDirectory = fbd.SelectedPath;
                        DbHandler.AddOrUpdateUserInfo(CurrentUser);
                        RaisePropertyChanged("Commands");
                    }
                });
            }
        }
        public ICommand ChangeKeyCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    var inputDialog = new InputDialog("Input key:");
                    var result = string.Empty;

                    if (inputDialog.ShowDialog() == true)
                    {
                        result = inputDialog.Answer;
                        try
                        {
                            Crypto.Decryptkey = result;
                        }
                        catch (Exception e)
                        {
                        }
                    }

                });
            }
        }
        public ICommand LoginCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    var User = DbHandler.GetUserInfo(UserName, Password);
                    if (User==null) return;
                    CurrentUser = User;
                    _logs = new ObservableCollection<string>(User.Logs.Split(';'));
                    var curDir =  new FileSystemObjectInfo(new DirectoryInfo(User.CurrentDirectory));
                    CurrentDirectories.Clear();
                    CurrentDirectories.Add(curDir);
                    RaisePropertyChanged("Logs");
                });
            }
        }
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
        public ICommand ExitCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    System.Windows.Application.Current.Shutdown();
                });
            }
        }

        public ICommand ResetCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    var commandLog = Logs.LastOrDefault();

                    if (commandLog == null) return;

                    if (commandLog.Contains("has been renamed to"))
                    {
                        var FullNames = commandLog.Split(new []{ "has been renamed to" },StringSplitOptions.RemoveEmptyEntries);

                        var oldPath = FullNames.LastOrDefault();

                        var newPath = FullNames.FirstOrDefault();

                        if (File.Exists(oldPath))
                        {
                            File.Move(oldPath, newPath);
                        }
                        else if(Directory.Exists(oldPath))
                        {
                            CopyDirectory(oldPath, newPath);
                            Directory.Delete(oldPath, true);
                        }
                        
                        CurrentDirectories.First().UpdateAll();
                    }
                });
            }
        }

        #region FileCommands

        public ICommand SpyOnCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((obj) =>
                {
                    if (CurrentUser != null)
                    {
                        if (!obj.IsSpyOn)
                        {
                            if(!CurrentUser.SpyingDirectories.Contains(obj.FileSystemInfo.FullName))
                            CurrentUser.SpyingDirectories += " " + obj.FileSystemInfo.FullName;
                        }
                        else
                        {
                            CurrentUser.SpyingDirectories =
                                CurrentUser.SpyingDirectories.Replace(" " + obj.FileSystemInfo.FullName, "");
                        }
                    }

                    DbHandler.AddOrUpdateUserInfo(CurrentUser);
                    obj.IsSpyOn = !obj.IsSpyOn; 
                    
                });
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                return new DelegateCommand<FileSystemObjectInfo>((file) =>
                {
                    if (file.FileSystemInfo.Extension != "")
                    {
                        File.Delete(file.FileSystemInfo.FullName);
                    }
                    else
                    {
                        Directory.Delete(file.FileSystemInfo.FullName, true);
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
                                if(_isMovingOn)
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
                    var newName = GetStringFromDialog();
                    if (newName.Trim().Length == 0) return;

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

        #endregion

        #region Methods

        private void OpenFile(FileSystemInfo file)
        {
            if(file.Extension=="") return;
            var pi = new ProcessStartInfo(file.FullName)
            {
                Arguments = Path.GetFileName(file.FullName),
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(file.FullName),
                Verb = "OPEN"
            };
            Process.Start(pi);
        }
        #endregion

        #region Helper Methods
        
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
            if (IsKeyRequired)
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

            UpdateLogs($"{path} has beed {DecryptFunc.Method.Name}");
        }

        private void UpdateLogs(string log)
        {
            _logs.Add(log);
            CurrentUser.Logs=String.Join("; ",Logs);
            DbHandler.AddOrUpdateUserInfo(CurrentUser);
            RaisePropertyChanged("Logs");
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


        

    }
}

