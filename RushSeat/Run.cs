﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace RushSeat
{
    class Run
    {
        
        public static string startTime = "";  //单位是分钟，12点是720
        public static string endTime = "";    //13点
        public static string buildingID = "1";
        public static string roomID = "4";  //6是三楼西，4是一楼某个区域
        public static string date = "2018-04-23";  //yyyy-mm-dd
        public static bool only_window = false;
        public static bool only_computer = false;

        //获取空座列表后的等待延迟
        public static int rankSuccessGetFreeSeat = 1500;
        public static int repeatSearchInterval = 3000;

        public static int preventCount = 0; 
        
        public static int waitsecond;
       

        private static Thread thread;

        private static bool success = false;

         public static void Start()
        {

            thread = new Thread(run);
            thread.IsBackground = true;
            thread.Start();
        }

        public static void run()
         {
             Config.config.button2.Enabled = false;
             int count = 0;
             int wrong_count = 0;
             
             while (true)
             {
                 bool get_list = false;  //逻辑BUG 每次循环应重置
                 if (RushSeat.stop_rush == true)
                 {
                     Config.config.textBox1.AppendText("用户取消抢座\n");
                     Config.config.textBox1.AppendText("-------------------------------------------\n");
                     Config.config.button1.Text = "开始抢座";
                     RushSeat.stop_rush = false;
                     return;
                 }
                 Config.config.textBox1.AppendText("即将开始第 " + (++count).ToString() + " 次检索...\n");
                 //移除之前的空座列表
                 RushSeat.freeSeats.Clear();
                 if (RushSeat.SearchFreeSeat(buildingID, roomID, date, startTime, endTime) == "Success")
                 {
                     Config.config.textBox1.AppendText("检索到符合条件空座列表，开始尝试预约...\n");
                     get_list = true;
                 }


                 //如果检索到空座
                 if (get_list == true)
                 {

                     //阶级等待
                     if (Config.rank != 'A')
                        Thread.Sleep(rankSuccessGetFreeSeat);

                     //先释放当前座位
                     string resInfo = RushSeat.CheckHistoryInf(false);
                     if (resInfo == "RESERVE")
                     {
                         if(RushSeat.CancelReservation(RushSeat.resID) != true)
                         {
                             Config.config.textBox1.AppendText("请手动重试...");
                             return;
                         }
                     }
                     if (resInfo == "CHECK_IN" || resInfo == "AWAY")
                     {
                         if (RushSeat.StopUsing() != true)
                         {
                             Config.config.textBox1.AppendText("请手动重试...");
                             return;
                         }
                     }

                     foreach (string seatID in RushSeat.freeSeats)
                     {
                         if (RushSeat.BookSeat(seatID, date, startTime, endTime) == "Success")
                         {
                             success = true;
                             break;
                         }
                         Thread.Sleep(500);
                         Config.config.textBox1.AppendText("座位ID " + seatID.ToString() + " 预约失败,尝试预约下一个座位\n");
                     }
                     //成功抢座后自动关机
                     if (success == true)
                     {
                         //静默检查预约信息，激活释放按钮
                         RushSeat.CheckHistoryInf(false);

                         //窗口弹出
                         if (Config.config.Visible != true)
                            Config.config.Visible = true;
                         Config.config.WindowState = FormWindowState.Normal;

                         //发短信
                         if (Config.config.checkBox4.Checked)
                         {
                             Config.config.textBox1.AppendText("短信已发送，返回值：\n" + RushSeat.SendMessage() + "\n");
                             Config.config.textBox1.AppendText("若返回值小于0为发送失败，请联系开发者\n");
                             Config.config.textBox1.AppendText("------------------------------------------\n");
                         } 

                         if (Config.config.checkBox3.Checked)
                         {
                             Config.config.textBox1.AppendText("2min后自动关机\n");
                             Config.config.textBox1.AppendText("如果想取消自动关机请在桌面用快捷键win + R启动控制台, 在控制台自行输入 shutdown -a\n");
                             Config.config.textBox1.AppendText("-----------------------------------------------------\n");
                             Process.Start("shutdown.exe", "-s -t " + "120");
                         }
                         else
                         {
                             //Config.config.textBox1.AppendText("订座成功");
                         }

                         Config.config.button1.Text = "开始抢座";
                         break;
                     }
                     else
                     {
                         //有空座但是抢座失败(别人手快或者碰到更高级的了)
                         wrong_count++;
                         Config.config.textBox1.AppendText("*****预约失败，"+(((double)repeatSearchInterval)/1000).ToString() +"s后重新开始检索...*****\n");
                         get_list = false;
                         if(wrong_count == 5)
                         {
                             Config.config.textBox1.AppendText("多次抢座失败，为防止封号中止抢座\n");
                             Config.config.textBox1.AppendText("请联系开发者\n");
                             return;
                         }
                     }
                 }
                 Thread.Sleep(repeatSearchInterval);
                 preventCount++;
                 //if (preventCount == 30)
                 //{
                 //    Config.config.textBox1.AppendText("防止被封，睡眠10s..........\n");
                 //    Thread.Sleep(10000);
                 //    preventCount = 0;
                 //}
             }
         }
    }
}
