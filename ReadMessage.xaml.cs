using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Practise_
{
    /// <summary>
    /// Interaction logic for ReadMessage.xaml
    /// </summary>
    public partial class ReadMessage : Window
    {
        public ReadMessage()
        {
            InitializeComponent();
        }
        public void SetMessageContent(string content)
        {
            messageContentTextBlock.Text = content;
        }
    }
}
