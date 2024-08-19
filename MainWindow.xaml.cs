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
            await multiAgent.askQuestion();
        }
        private void QuestionBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AskButton_Click(sender, e);
            }
        }
    }
}