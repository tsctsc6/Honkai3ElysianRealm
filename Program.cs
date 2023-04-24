using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace BH3浅层乐土
{
    internal class Program
    {
        static spireOCRpro ocr = new spireOCRpro();
        static CycMove[] moves = null;
        static IntPtr bh3IntPtr = IntPtr.Zero;

        public static Semaphore rmutex = new Semaphore(1, 1);
        public static Semaphore wmutex = new Semaphore(1, 1);
        public static Semaphore wFirst = new Semaphore(1, 1);//写者优先
        public static int readcount = 0;

        static public bool IsFighting = false;
        static void FigntSat()
        {
            Rectangle bh3pos = new Rectangle();
            while(true)
            {
                WindowCaptureHelper.GetWindowRect(bh3IntPtr, ref bh3pos);
                int normalizedX = 65535 * (bh3pos.X + (int)(1100 / WindowCaptureHelper.zoom)) / Screen.PrimaryScreen.Bounds.Width;
                int normalizedY = 65535 * (bh3pos.Y + (int)(700 / WindowCaptureHelper.zoom)) / Screen.PrimaryScreen.Bounds.Height;
                ocr.Scan(WindowCaptureHelper.GetShotCutImage(bh3IntPtr, 1000, 600, 200, 150));
                while (ocr.Text[0] == "开始战斗")
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_ABSOLUTE | WinAPI.MOUSEEVENTF_MOVE,
                        normalizedX, normalizedY, 0, 0);
                    Thread.Sleep(100);
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTDOWN | WinAPI.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    Thread.Sleep(1000);
                    ocr.Scan(WindowCaptureHelper.GetShotCutImage(bh3IntPtr, 1000, 600, 200, 150));
                }
                Thread.Sleep(1000);
                while (ocr.Text[0] == "加载中")
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_ABSOLUTE | WinAPI.MOUSEEVENTF_MOVE,
                        normalizedX, normalizedY, 0, 0);
                    ocr.Scan(WindowCaptureHelper.GetShotCutImage(bh3IntPtr, 1000, 600, 200, 150));
                    Thread.Sleep(1000);
                }

                wFirst.WaitOne();
                wmutex.WaitOne();
                IsFighting = true;
                wmutex.Release();
                wFirst.Release();

                foreach (var move in moves) move.NewThreadAndStart();
                Thread.Sleep(61000);

                while (ocr.Text[0] != "挑战目标")
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_ABSOLUTE | WinAPI.MOUSEEVENTF_MOVE,
                        normalizedX, normalizedY, 0, 0);
                    ocr.Scan(WindowCaptureHelper.GetShotCutImage(bh3IntPtr, 800, 300, 200, 75));
                    Thread.Sleep(1000);
                }
                Thread.Sleep(3000);

                wFirst.WaitOne();
                wmutex.WaitOne();
                IsFighting = false;
                wmutex.Release();
                wFirst.Release();

                WinAPI.mouse_event(WinAPI.MOUSEEVENTF_ABSOLUTE | WinAPI.MOUSEEVENTF_MOVE,
                        normalizedX, normalizedY, 0, 0);
                Thread.Sleep(100);
                WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTDOWN | WinAPI.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                Thread.Sleep(3000);
            }
        }
        static CycMove[] LoadFightConfig(string path)
        {
            JObject FightFile = JObject.Parse(File.ReadAllText(path));
            JArray tasks = (JArray)FightFile["tasks"];
            CycMove[] moves = new CycMove[tasks.Count];
            for(int j = 0; j < tasks.Count; j++)
            {
                JArray k_array = (JArray)tasks[j]["k_set"];
                Keys[] k_set = new Keys[k_array.Count];
                for(int i = 0; i < k_array.Count; i++)
                {
                    string k_string = k_array[i].ToString();
                    Byte[] k_byte = Encoding.ASCII.GetBytes(k_string);
                    if(k_string.Length == 1) k_set[i] = (Keys)k_byte[0];
                    else if(k_string.Length == 2 & k_string[0] == 'D') k_set[i] = (Keys)k_byte[1];
                }

                JArray m_array = (JArray)tasks[j]["m_set"];
                KeyMode[] m_set = new KeyMode[m_array.Count];
                for (int i = 0; i < m_array.Count; i++)
                {
                    string m_string = m_array[0].ToString();
                    switch(m_string)
                    {
                        case "Click": m_set[i] = KeyMode.Click; break;
                        case "Press": m_set[i] = KeyMode.Press; break;
                    }
                }

                JArray tc_array = (JArray)tasks[j]["tc_set"];
                int[] tc_set = new int[tc_array.Count];
                for (int i = 0; i < tc_array.Count; i++) tc_set[i] = (int)tc_array[i];

                JArray ti_array = (JArray)tasks[j]["ti_set"];
                int[] ti_set = new int[ti_array.Count];
                for (int i = 0; i < ti_array.Count; i++) ti_set[i] = (int)ti_array[i];

                moves[j] = new CycMove(k_set, m_set, tc_set, ti_set, (int)tasks[j]["beginDelay"]);
            }
            return moves;
        }
        static void Main(string[] args)
        {
            //本程序需要管理员权限
            if(args.Length == 0)
            {
                Console.WriteLine("请在命令行参数中输入战斗配置文件");
                Console.ReadKey();
                return;
            }
            
            bh3IntPtr = WinAPI.FindWindow(null, "崩坏3"); //null为类名，可以用Spy++得到，也可以为空
            if (bh3IntPtr == IntPtr.Zero) { return; }
            WinAPI.ShowWindow(bh3IntPtr, WinAPI.SW_RESTORE); //将窗口还原
            WinAPI.SetForegroundWindow(bh3IntPtr); //如果没有ShowWindow，此方法不能设置最小化的窗口
            
            CycMove.log = true;
            try { moves = LoadFightConfig(args[0]); }
            catch (JsonException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
                return;
            }

            Thread.Sleep(1000);
            
            Thread t = new Thread(() => FigntSat());
            t.Start();
        }
    }
}
