using System.Threading;

namespace XeroMath_Test
{
    internal class Program
    {
        public static float ready = 0;
        public static XeroNumber a;


        static void Main(string[] args)
        {
            Thread thread = new Thread(thread1);
            thread.Start();
            a = XeroNumber.ArrangementWithRepeat(999, 20000, ref ready);
            using(FileStream fs = new FileStream("number1.txt", FileMode.OpenOrCreate))
                using(StreamWriter sw = new StreamWriter(fs))
                    sw.WriteLine(a.ToString());
        }

        static void thread1()
        {
            while (ready < 1)
            {
                Thread.Sleep(100);
                Console.Clear();
                Console.WriteLine(ready);
            }
            Console.WriteLine(a);
        }
    }
}
