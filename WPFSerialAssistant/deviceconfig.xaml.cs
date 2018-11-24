﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFSerialAssistant
{
    /// <summary>
    /// deviceconfig.xaml 的交互逻辑
    /// </summary>
    public partial class deviceconfig : Window
    {
        MainWindow parent;
        public deviceconfig(MainWindow parent)
        {
            InitializeComponent();
            this.parent = parent;
        }

    

        //目前参数个数的60
        public const int ParaCnt = 60;

        public string[] Para   = new string[ParaCnt];          //存放688B参数，用于XML保存
        public byte[] ReadPara = new byte[ParaCnt * 2];          //保存收到的参数,用于作于对比的基准
        public byte[] SendPara = new byte[ParaCnt * 2];          //保存发送的参数，用于判断哪个参数修改过
        public byte[] JsonPara = new byte[ParaCnt * 2];           //加载从本地xml文件转换后的参数


        public bool m_resetfact = false;

        public string m_showinfo = "This is a test!";

        //这几个索引号的参数是使用二进制显示
        public int[] ParaIndex = { 2, 6, 9, 10, 28, 54};

        //这几个参数是会改变的，比较的时候需要跳过
        public int[] ChangeIndex = {22,41,42};

        //这几个参数是版本号的索引号
        public int[] VsersionIndex = { 33, 34, 120, 121};


        public string[] HexTab = { "0000", "0001", "0010", "0011",
                                   "0100", "0101", "0110", "0111",
                                   "1000", "1001", "1010", "1011",
                                   "1100", "1101", "1110", "1111",};







        public void FillBox(string[] Para)
        {
            for (int i = 0; i < Para.Count(); ++i)
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    ((TextBox)FindName("textBox" + i.ToString())).Text = Para[i];
                }));
            }

            //先将数值转换成16进制然后查表转换成2进制显示
            this.Dispatcher.Invoke(new Action(delegate
            {
                for (int i = 0; i < ParaIndex.Count(); ++i)
                {
                    try
                    {
                            string Gstring = "";
                            foreach (char c in Convert.ToInt16(((TextBox)FindName("textBox" + ParaIndex[i].ToString())).Text).ToString("X4"))
                            {
                                if (c <= '9')
                                {
                                    Gstring += HexTab[c - '0'];
                                }
                                else
                                {
                                    Gstring += HexTab[c - 'A' + 10];
                                }
                                Gstring += " ";
                            }
                        ((TextBox)FindName("textBox" + ParaIndex[i].ToString())).Text = Gstring;
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show("请先恢复原厂参数再获取参数");
                        Clear();
                        return;
                    }

                }
            }));
        }

        private void GetConfig_Button_Click(object sender, RoutedEventArgs e)
        {
            if ((string)(this.parent.openClosePortButton.Content) == "关闭")
            {
                this.parent.SendData("01 03 00 00 00 3C 45 DB");
            }
            else
            {
                MessageBox.Show("请先打开串口，再获取参数");
            }
        }



        //清空显示
       public void Clear()
        {
            for (int i = 0; i < ParaCnt; ++i)
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    ((TextBox)FindName("textBox" + i.ToString())).Text = "0";
                }));
            }

            this.Dispatcher.Invoke(new Action(delegate
            {
                for (int i = 0; i < ParaIndex.Count(); ++i)
                {
                    string Gstring = "0000 0000 0000 0000";
                    ((TextBox)FindName("textBox" + ParaIndex[i].ToString())).Text = Gstring;
                }
            }));

            this.Dispatcher.Invoke(new Action(delegate
            {
                string Gstring = "0x0000";
                ((TextBox)FindName("textBox41")).Text = Gstring;
                ((TextBox)FindName("textBox42")).Text = Gstring;
            }));
        }


        //CRC校验函数
        public static byte[] CRC16(byte[] data, int length)
        {
            int len = length;
            if (len > 0)
            {
                ushort crc = 0xFFFF;

                for (int i = 0; i < len; i++)
                {
                    crc = (ushort)(crc ^ (data[i]));
                    for (int j = 0; j < 8; j++)
                    {
                        crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                    }
                }
                byte hi = (byte)((crc & 0xFF00) >> 8);
                byte lo = (byte)(crc & 0x00FF);

                return new byte[] { lo, hi };//不知道为什么顺序是反的？？？？
            }
            return new byte[] { 0, 0 };
        }



        private void ClearShow_Button_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }


        private void SavePara(string[] para)
        {
            //使用json格式保存参数
            if (para.Length == 0)
            {
                MessageBox.Show("请先获取参数再尝试保存");
            }
            Configuration Para_config = new Configuration();

            for (int i = 0; i < para.Length; ++i)
            {
                Para_config.Add("BOX" + i.ToString(), para[i]);
            }


            string localFilePath = "";
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            //设置文件类型 
            sfd.Filter = "JSON文件|*.json";

            //设置默认文件类型显示顺序 
            sfd.FilterIndex = 1;

            //保存对话框是否记忆上次打开的目录 
            sfd.RestoreDirectory = true;

            //点了保存按钮进入 
            if (sfd.ShowDialog() == true)
            {
                localFilePath = sfd.FileName.ToString(); //获得文件路径 
                Configuration.Save(Para_config, localFilePath);
                MessageBox.Show("保存配置文件成功");
            }
        }


        private void SetConfig_Button_Click(object sender, RoutedEventArgs e)
        {

            if (((TextBox)FindName("textBox0")).Text == "0")
            {
                MessageBox.Show("请先获取参数");
                return;
            }


            byte[] SendData = new byte[11];
            SendData[0] = 0x01;             //addr code
            SendData[1] = 0x10;             //func code
            SendData[2] = 0x00;             //start addr high
            SendData[3] = 0x00;             //start addr low
            SendData[4] = 0x00;             //reg num high
            SendData[5] = 0x01;             //reg num low
            SendData[6] = 0x02;             //byte mount


            UInt16 I;
            byte[] Buf = new byte[2];

            for (int i = 0; i < ParaCnt; i++)
            {
                if (ParaIndex.Contains(i))
                {
                    //使用16进制的数据是否需要特别处理
                    //先清除字符串的空格键
                    string temp = ((TextBox)FindName("textBox" + i.ToString())).Text.Replace(" ", "");
                    I = Convert.ToUInt16(temp, 2);//2进制转int
                }
                else
                {
                    I = Convert.ToUInt16(((TextBox)FindName("textBox" + i.ToString())).Text);
                }
                Buf = BitConverter.GetBytes(I);

                SendPara[2 * i]     = Buf[1];               //将参数保存在发送数组中，然后对比
                SendPara[2 * i + 1] = Buf[0];
            }




            //将修改后的数据和修改前的数据对比，然后基于差异得出修改的参数
            byte j = 0;
            bool SetFlag = false;
            if (SendPara.Length == ReadPara.Length)
            {
                for (j = 0; j < SendPara.Length; ++j)
                {
                    if (SendPara[j] != ReadPara[j])
                    {
                       
                        MessageBox.Show("你将修改" + ((Label)FindName("label" + (j / 2).ToString())).Content
                            + ":" + ReadPara[j].ToString() + "-->" + SendPara[j]);
                        //计算crc
                        SetFlag = true;

                        SendData[2] = 0x00;     //start addr high
                        SendData[3] = Convert.ToByte(j >> 1);     //start addr low
                        SendData[7] = SendPara[Convert.ToByte((j >> 1) * 2)];     //reg num low
                        SendData[8] = SendPara[Convert.ToByte((j >> 1) * 2) + 1];     //byte mount

                        byte[] CrcSend = CRC16(SendData, 9);
                        SendData[9] = CrcSend[0];
                        SendData[10] = CrcSend[1];
                        this.parent.SendData(SendData);
                        System.Threading.Thread.Sleep(2000);
                    }
                }

                if (SetFlag == false)
                {
                    MessageBox.Show("没有参数修改过:)");
                }
                else
                {
                    SetFlag = false;
                    //更新显示
                    this.parent.SendData("01 03 00 00 00 3C 45 DB");
                }
            }
            else
            {
                MessageBox.Show("设置参数出错:(");
            }
        }


        private void SaveConfigFile_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (((TextBox)FindName("textBox0")).Text == "0")
                {
                    MessageBox.Show("请先获取参数！:(");
                    return;
                }

                //保存参数实际调用的函数
                SavePara(Para);
                
            }
            catch (Exception err)
            {
                //显示错误信息    
                Console.WriteLine(err.Message);
            }
        }



        private void LoadConfigFile_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "JSON文件|*.json";      //设置要选择的文件的类型
            if (fileDialog.ShowDialog() == true)
            {
                string JsonLocalFile = fileDialog.FileName;
                ParaBox.Text = "参数文件" + JsonLocalFile;

                this.Dispatcher.Invoke(new Action(delegate
                {
                    this.parent.statusInfoTextBlock.Text = "加载配置文件成功";
                }));
                Configuration config = Configuration.Read(JsonLocalFile);
                if (config == null)
                {
                    MessageBox.Show("加载文件为空，请选择正确的文件");
                    return;
                }
                byte[] Buf = new byte[2];
                UInt16 L;
                for (int i = 0; i < ParaCnt; ++i)
                {
                    L = Convert.ToUInt16(config.GetString("BOX" + i.ToString()));
                    Buf = BitConverter.GetBytes(L);
                    JsonPara[2 * i] = Buf[1];
                    JsonPara[2 * i + 1] = Buf[0];
                }
            }
        }






        private void CheckConfig_Button_Click(object sender, RoutedEventArgs e)
        {


            if (!ParaBox.Text.EndsWith(".json"))
            {
                MessageBox.Show("校验前请先加载配置文件");
                return;
            }

            bool checkflag = false;

            if (((TextBox)FindName("textBox0")).Text == "0")
            {
                MessageBox.Show("请先获取参数!");
                return;
            }

   

            //如果用户选择自动校验版本号，就需要首先检测用户输入
            this.Dispatcher.Invoke(new Action(delegate
            {

                if (((CheckBox)FindName("checkBox0")).IsChecked == true)
                {
                    if (((TextBox)FindName("DSP_vserion")).Text != this.parent.GetString(JsonPara, 33 * 2))
                    {
                        checkflag = true;
                        MessageBox.Show("DSP版本号校验不通过");
                        return;
                    }
                }

                if (((CheckBox)FindName("checkBox1")).IsChecked == true)
                {
                    if (((TextBox)FindName("DPS_boot_version")).Text != this.parent.GetString(JsonPara, 49 *2 ))
                    {
                        checkflag = true;
                        MessageBox.Show("DSP boot版本号检验不通过");
                        return;
                    }
                }

                if (((CheckBox)FindName("checkBox2")).IsChecked == true)
                {
                    if (((TextBox)FindName("MCU_version")).Text != this.parent.GetString(JsonPara, 34 * 2))
                    {
                        checkflag = true;
                        MessageBox.Show("单片机版本号检验不通过");
                        return;
                    }
                }

                if (((CheckBox)FindName("checkBox3")).IsChecked == true)
                {
                     if (((TextBox)FindName("MCU_boot_version")).Text != this.parent.GetString(JsonPara, 50 / 2))
                    {
                        checkflag = true;
                        MessageBox.Show("单片机boot版本号检验不通过");
                        return;
                    }
                }

            }));

            if (checkflag)
            {
                return;
            }


            byte[] SendData = new byte[11];
            SendData[0] = 0x01;             //addr code
            SendData[1] = 0x10;             //func code
            SendData[2] = 0x00;             //start addr high
            SendData[3] = 0x00;             //start addr low
            SendData[4] = 0x00;             //reg num high
            SendData[5] = 0x01;             //reg num low
            SendData[6] = 0x02;             //byte mount

            //将修改后的数据和修改前的数据对比，然后基于差异得出修改的参数
            byte j = 0;
            bool SetFlag = false;


            //如果版本号程序版本号不一样，不需要比对，直接退出

            if((((this.parent.GetString(JsonPara, 33 * 2)) != this.parent.GetString(ReadPara, 33 * 2))) ||
               (((this.parent.GetString(JsonPara, 34 * 2)) != this.parent.GetString(ReadPara, 34 * 2))) ||
               (((this.parent.GetString(JsonPara, 49 * 2)) != this.parent.GetString(ReadPara, 49 * 2))) ||
               (((this.parent.GetString(JsonPara, 50 * 2)) != this.parent.GetString(ReadPara, 50 * 2))))
            {

                string show = "配置文件中的版本号与当前设备的软件版本不一致，退出本次自动校验\r\n" +
                               "配置文件DSP:" + (this.parent.GetString(JsonPara, 33 * 2)) + "<-->" +
                               "当前DSP:" + (this.parent.GetString(ReadPara, 33 * 2)) + "\r\n" +
                               "配置文件DSP boot:" + (this.parent.GetString(JsonPara, 49 * 2)) + "<-->" +
                               "当前DSP boot:" + (this.parent.GetString(ReadPara, 49 * 2)) + "\r\n" +
                               "配置文件MCU:" + (this.parent.GetString(JsonPara, 34 * 2)) + "<-->" +
                               "当前MCU:" + (this.parent.GetString(ReadPara, 34 * 2)) + "\r\n" +
                               "配置文件MCU boot:" + (this.parent.GetString(JsonPara, 50 * 2)) + "<-->" +
                               "当前MCU boot:" + (this.parent.GetString(ReadPara, 50 * 2)) + "\r\n";

                MessageBox.Show(show);
                return;
            }

            this.Dispatcher.Invoke(new Action(delegate
            {
                ((Button)FindName("CheckConfig_Button")).Content = "校验中";
            }));


            if (JsonPara.Length == ReadPara.Length)
            {
                for (j = 0; j < JsonPara.Length; ++j)
                {
                    if (JsonPara[j] != ReadPara[j])
                    {
                        //如果是GPS相关的信息，不需要比对
                        if (ChangeIndex.Contains(j))
                            continue;

                        //this.Dispatcher.Invoke(new Action(delegate
                        //{
                        //    this.parent.statusInfoTextBlock.Text = "你将修改" + ((Label)FindName("label" + (j / 2).ToString())).Content
                        //    + ":" + ReadPara[j].ToString() + "-->" + JsonPara[j];
                        //}));

                        //MessageBox.Show("你将修改" + ((Label)FindName("label" + (j / 2).ToString())).Content
                        //    + ":" + ReadPara[j].ToString() + "-->" + JsonPara[j]);

                        //计算crc
                        SetFlag = true;

                        SendData[2] = 0x00;     //start addr high
                        SendData[3] = Convert.ToByte(j >> 1);     //start addr low
                        SendData[7] = JsonPara[Convert.ToByte((j >> 1) * 2)];     //reg num low
                        SendData[8] = JsonPara[Convert.ToByte((j >> 1) * 2) + 1];     //byte mount

                        byte[] CrcSend = CRC16(SendData, 9);
                        SendData[9] = CrcSend[0];
                        SendData[10] = CrcSend[1];
                        this.parent.SendData(SendData);
                        this.Dispatcher.Invoke(new Action(delegate
                        {
                            this.parent.statusInfoTextBlock.Text = "参数自动校验中...";
                        }));
                        System.Threading.Thread.Sleep(2000);
                    }
                }
            }

            if (SetFlag == false)
            {
                MessageBox.Show("校验通过:)");

            }
            else
            {
                SetFlag = false;
                //更新显示
                MessageBox.Show("自动修改完成,，自动获取新的参数");
                this.parent.SendData("01 03 00 00 00 3C 45 DB");
            }


            this.Dispatcher.Invoke(new Action(delegate
            {
                ((Button)FindName("CheckConfig_Button")).Content = "校验参数";
            }));

        }

        private void ResetFact_Button_Click(object sender, RoutedEventArgs e)
        {
            m_resetfact = true;
            this.parent.SendData("01 10 00 03 00 01 02 00 01 67 A3");
        }
    }
}
