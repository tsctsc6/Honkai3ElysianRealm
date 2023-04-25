using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace BH3浅层乐土
{
    public enum KeyMode
    {
        Click,//连续点击
        Press//长按
    }
    internal class CycMove
    {
        //连续点击的间隔，单位ms
        const int PressNT = 50;
        const int ms_I = 100;
        //按下按键时，在控制台输出对应的字符
        public static bool log = false;
        //直接对属性赋值，只是复制了指针
        /// <summary>
        /// key: 按键列表
        /// mode: 模式
        /// tc: 单个按键模式的持续时间
        /// ti: 时间间隔，单位ms
        /// ti[0]表示key[0]和key[1]之间的间隔
        /// tc + ti才是真正的周期
        /// beginDelay: 一开始暂停毫秒数
        /// </summary>
        public Keys[] key { get; set; } = null;
        public KeyMode[] mode { get; set; } = null;
        public int[] tc { get; set; } = null;
        public int[] ti { get; set; } = null;
        public int beginDelay = 0;
        public Thread thread = null;
        public CycMove() { }
        public CycMove(Keys[] key, KeyMode[] mode, int[] tc, int[] ti, int beginDelay)
        {
            this.key = (Keys[])key.Clone();
            this.mode = (KeyMode[])mode.Clone();
            this.tc = (int[])tc.Clone();
            this.ti = (int[])ti.Clone();
            this.beginDelay = beginDelay;
        }
        public void NewThreadAndStart()
        {
            thread = new Thread(() => Move());
            thread.Start();
        }
        public void Move()
        {
            bool IsFighting_ = false;
            if (SleepPro(beginDelay, Program.IsFighting, false, ms_I)) return;
            for (int i =  0; i < key.Length; i = ( i + 1 ) % key.Length)
            {
                Program.wFirst.WaitOne();
                Program.wFirst.Release();
                Program.rmutex.WaitOne();
                if (Program.readcount == 0) Program.wmutex.WaitOne();
                Program.readcount++;
                Program.rmutex.Release();

                IsFighting_ = Program.IsFighting;

                Program.rmutex.WaitOne();
                Program.readcount--;
                if (Program.readcount == 0) Program.wmutex.Release();
                Program.rmutex.Release();

                if (!IsFighting_) return;

                switch (mode[i])
                {
                    case KeyMode.Click:
                    {
                        for (int j = 0; j < (int)(tc[i] / PressNT / 2); j++)
                        {
                            if (log) Console.Write(key[i] + " ");
                            WinAPI.keybd_event((byte)key[i], (byte)WinAPI.MapVirtualKeyA((int)key[i], 0), 1 | 0, 0);
                            //Thread.Sleep(PressNT + NormalDistribution.GetNum());
                            if (SleepPro(PressNT + NormalDistribution.GetNum(), Program.IsFighting, false, ms_I)) return;
                            WinAPI.keybd_event((byte)key[i], (byte)WinAPI.MapVirtualKeyA((int)key[i], 0), 1 | 2, 0);
                            //Thread.Sleep(PressNT + NormalDistribution.GetNum());
                            if (SleepPro(PressNT + NormalDistribution.GetNum(), Program.IsFighting, false, ms_I)) return;
                        }
                        if (log) Console.WriteLine();
                        break;
                    }
                    case KeyMode.Press:
                    {
                        if (log) Console.WriteLine(key[i] + "----");
                        WinAPI.keybd_event((byte)key[i], (byte)WinAPI.MapVirtualKeyA((int)key[i], 0), 1 | 0, 0);
                        //Thread.Sleep(tc[i] + NormalDistribution.GetNum());
                        if (SleepPro(tc[i] + NormalDistribution.GetNum(), Program.IsFighting, false, ms_I)) return;
                        WinAPI.keybd_event((byte)key[i], (byte)WinAPI.MapVirtualKeyA((int)key[i], 0), 1 | 2, 0);
                        break;
                    }
                }
                //Thread.Sleep(ti[i] + NormalDistribution.GetNum());
                if (SleepPro(ti[i] + NormalDistribution.GetNum(), Program.IsFighting, false, ms_I)) return;
            }
        }
        /// <summary>
        /// 使当前线程暂停msTimeout时间
        /// 如果中途，foc的值为b，则马上返回true
        /// msInterval是检测间隔
        /// </summary>
        static bool SleepPro(int msTimeout, in bool foc, bool b, int msInterval = 0)
        {
            bool IsFighting_;
            if (msInterval > 0) Thread.Sleep(msTimeout % msInterval);
            for(int i = 0; i < msTimeout / msInterval; i++)
            {
                Program.wFirst.WaitOne();
                Program.wFirst.Release();
                Program.rmutex.WaitOne();
                if (Program.readcount == 0) Program.wmutex.WaitOne();
                Program.readcount++;
                Program.rmutex.Release();

                IsFighting_ = Program.IsFighting;

                Program.rmutex.WaitOne();
                Program.readcount--;
                if (Program.readcount == 0) Program.wmutex.Release();
                Program.rmutex.Release();

                if (IsFighting_ == b) return true;
                Thread.Sleep(msInterval);
            }
            return false;
        }
    }
}
