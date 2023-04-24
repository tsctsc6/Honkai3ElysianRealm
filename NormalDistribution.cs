using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH3浅层乐土
{
    //在这个类中，均值固定为0，标准差固定为10
    internal class NormalDistribution
    {
        static Random r = new Random();
        readonly static double[] StdNorTable = new double[]{
            0.5000, 0.5398, 0.5793, 0.6179, 0.6554, 0.6915, 0.7257, 0.7580, 0.7881, 0.8159,
            0.8413, 0.8643, 0.8849, 0.9032, 0.9192, 0.9332, 0.9452, 0.9554, 0.9641, 0.9713,
            0.9772, 0.9821, 0.9861, 0.9893, 0.9918, 0.9938, 0.9953, 0,9965, 0.9974, 0.9981, 0.9987
        };
        public static int GetNum()
        {
            double d = r.NextDouble() * 0.5 + 0.5;
            //绝对值
            int n = StdNorTable.Length;
            for(int i = 0; i < StdNorTable.Length; i++)
            {
                if (StdNorTable[i] < d)
                {
                    n = i;
                    break;
                }
            }
            //正负
            if (r.Next(0, 2) == 0) n = -1 * n;
            return n;
        }

    }
}
