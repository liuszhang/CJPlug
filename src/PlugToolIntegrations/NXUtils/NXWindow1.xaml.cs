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
using System.Windows.Threading;

namespace NXUtils
{
    /// <summary>
    /// NXWindow1.xaml 的交互逻辑
    /// </summary>
    public partial class NXWindow1 : Window
    {
        public NXWindow1()
        {
            InitializeComponent();
            Console.WriteLine("666");
            //NXUtils.OpenFile();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("777");
            var thread = new Thread(() =>
            {
                Console.WriteLine("888");
                //NXUtils.NXUtils.OpenFile();                
                NXUtils.OpenFile();
            });

            thread.SetApartmentState(ApartmentState.STA); // 设置线程为STA模式
            thread.Start();
            thread.Join(); // 等待线程完成（如果需要）
            
        }
    }
}
