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
        public ObservableCollection<FileSystemObjectInfo> CurrentDirectories { get; set; }
        private FileSystemObjectInfo bufferObjectInfo;
        private bool isMovingOn;
        private static Queue<string> _logs = new Queue<string>();
        public string UserName { get; set; } = "Username";
        public string Test { get; set; } = "Test";
        public string Password { get; set; }
        public Users CurrentUser { get; set; }

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

        public static bool isKeyRequired;
        public bool IsKeyInputRequired { get =>isKeyRequired; set => isKeyRequired=value;
        }

        public MainViewModel()
        {
            var curDir = Environment.GetLogicalDrives().Select(i => new FileSystemObjectInfo(new DirectoryInfo(i)));
            CurrentDirectories = new ObservableCollection<FileSystemObjectInfo>(curDir);
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
                        DbHandler.UpdateUserInfo(CurrentUser);
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
                    FileSystemObjectInfo.user = User;
                    if (User==null) return;
                    CurrentUser = User;
                    var curDir =  new FileSystemObjectInfo(new DirectoryInfo(User.CurrentDirectory));
                    CurrentDirectories.Clear();
                    CurrentDirectories.Add(curDir);
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

        #region FileCommands

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
                                if(isMovingOn)
                                    _logs.Enqueue($"{bufferObjectInfo.FileSystemInfo.FullName} has been moved to {obj.FileSystemInfo.FullName}");
                                else
                                    _logs.Enqueue($"{bufferObjectInfo.FileSystemInfo.FullName} has been inserted into {obj.FileSystemInfo.FullName}");

                                var newPath = obj.FileSystemInfo.FullName + "\\" + bufferObjectInfo.FileSystemInfo.Name;
                                if (bufferObjectInfo.FileSystemInfo.Extension != "")
                                {
                                    File.Copy(bufferObjectInfo.FileSystemInfo.FullName, newPath);
                                    if (isMovingOn)
                                    {
                                        File.Delete(bufferObjectInfo.FileSystemInfo.FullName);
                                        bufferObjectInfo.Parent.Children.Remove(bufferObjectInfo);
                                        bufferObjectInfo.UpdateTree();
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
                                        bufferObjectInfo.Parent.Children.Remove(bufferObjectInfo);
                                        bufferObjectInfo.UpdateTree();
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
                    _logs.Enqueue($"{obj.FileSystemInfo.FullName}  atributes has been showed");
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


        #endregion

    }
}

