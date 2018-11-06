using System;
using System.Windows;

namespace AdvancedFileViewer_WPF
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        public MessageWindow(string question="")
        {
            InitializeComponent();
            lblQuestion.Content = question;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

    }
}
