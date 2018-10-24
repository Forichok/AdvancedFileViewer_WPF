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

        public string UserName { get; set; } = "Username";
        public string Test { get; set; } = "Test";
        public string Password { get; set; }
        public Users CurrentUser { get; set; }
    
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
        public ICommand DeleteCommand
        {
            get
            {
                return new DelegateCommand<object>((obje) =>
                {
                    var obj = obje as FileSystemObjectInfo;
                    if (obj.FileSystemInfo.Extension != "")
                    {
                        File.Delete(obj.FileSystemInfo.FullName);
                    }
                    else
                    {
                        Directory.Delete(obj.FileSystemInfo.FullName, true);
                    }
//                    _logs.Enqueue($"{obj.FileSystemInfo.FullName} has been deleted");
                    var parent = obj.Parent;
                    parent.Children.Remove(obj);
                    obj.RemoveDummy();
                    parent.UpdateTree();
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

      
    }
}

