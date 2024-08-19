using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604, CS8602

namespace QuestionnaireMultiagent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MultiAgent? multiAgent;
        public MainWindow()
        {
            InitializeComponent();
            multiAgent = new MultiAgent(this);
            this.DataContext = multiAgent;
        }

        private async void AskButton_Click(object sender, RoutedEventArgs e)
        {
            await multiAgent.AskQuestion();
        }
        private void QuestionBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AskButton_Click(sender, e);
            }
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            //Open a file picker dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".xlsx";
            dlg.Filter = "Excel Files (*.xlsx)|*.xlsx";

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true) {
                string filename = dlg.FileName;
                multiAgent?.AnswerInExcelFile(filename);
            }
        }
    }
}