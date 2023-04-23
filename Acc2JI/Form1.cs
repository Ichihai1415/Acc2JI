using MathNet.Numerics.IntegralTransforms;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace Acc2JI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        //データのパス
        public string Directory = "C:\\Users\\proje\\source\\repos\\Arduino_Quake_Intensity_Viewer\\Arduino_Quake_Intensity_Viewer\\bin\\x64\\Debug\\Logs";

        private void Form1_Load(object sender, EventArgs e)
        {
            DateTime StartTime = new DateTime();
            while (true)
            {
                while (true)
                {
                    Console.WriteLine("開始時刻を入力してください。");//2023/04/21 17:16:00
                    try
                    {
                        StartTime = Convert.ToDateTime(Console.ReadLine());
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                DateTime GetTime = StartTime;
                double dataSecond = 0;
                double[] AccX = new double[2048];
                double[] AccY = new double[2048];
                double[] AccZ = new double[2048];
                string newcsv = "\n";

                Console.WriteLine("データ取得中…");
                for (int i = 0; i < 2048;)
                {
                    string[] data_ = File.ReadAllText($"{Directory}\\{GetTime.Year}\\{GetTime.Month}\\{GetTime.Day}\\{GetTime.Hour}\\{GetTime.Minute}\\{GetTime:yyyyMMddHHmmss}.txt").Split('\n');
                    foreach (string data__ in data_)
                    {
                        string[] data = data__.Split(',');
                        if (data.Length == 4)
                        {
                            newcsv += $"\n{data[0]},{data[1]},{data[2]}";
                            AccX[i] = double.Parse(data[0]);
                            AccY[i] = double.Parse(data[1]);
                            AccZ[i] = double.Parse(data[2]);
                            dataSecond += 1.0 / data_.Length;
                            i++;
                            if (i >= 2048)
                                break;
                        }
                    }
                    GetTime += new TimeSpan(0, 0, 1);
                }
                File.WriteAllText("output.csv", newcsv.Replace("\n\n", ""));

                Console.WriteLine("データ変換中…");
                Complex Acc = new Complex(AccX[0], 0);
                Complex[] AccXc = Array.ConvertAll(AccX, x => new Complex(x, 0));
                Complex[] AccYc = Array.ConvertAll(AccY, x => new Complex(x, 0));
                Complex[] AccZc = Array.ConvertAll(AccZ, x => new Complex(x, 0));

                //フーリエ変換
                Console.WriteLine("フーリエ変換中…");
                Fourier.Forward(AccXc);
                Fourier.Forward(AccYc);
                Fourier.Forward(AccZc);

                //フィルター
                Console.WriteLine("フィルター計算中…");
                for (int i = 0; i < 2048; i++)
                {
                    double Hz = (i + 1) / dataSecond;
                    double y = Hz * 0.1;
                    //ローカットフィルター
                    double FL = Math.Pow(1 - Math.Exp(-1 * Math.Pow(Hz / 0.5, 3)), 0.5);
                    //ハイカットフィルター
                    double FH = Math.Pow(1 + 0.694 * Math.Pow(y, 2) + 0.241 * Math.Pow(y, 4) + 0.0557 * Math.Pow(y, 6) + 0.009664 * Math.Pow(y, 8) + 0.00134 * Math.Pow(y, 10) + 0.000155 * Math.Pow(y, 12), -0.5);
                    //周期効果フィルター
                    double FF = Math.Pow(1 / Hz, 0.5);
                    //フィルター合計
                    double FA = FL * FH * FF;
                    AccXc[i] *= FA;
                    AccZc[i] *= FA;
                    AccZc[i] *= FA;
                }

                //逆フーリエ変換
                Console.WriteLine("逆フーリエ変換中…");
                Fourier.Inverse(AccXc);
                Fourier.Inverse(AccYc);
                Fourier.Inverse(AccZc);

                Console.WriteLine("計算中…");
                double[] fdataX = new double[2048];
                double[] fdataY = new double[2048];
                double[] fdataZ = new double[2048];
                for (int i = 0; i < 2048; i++)
                {
                    fdataX[i] = AccXc[i].Magnitude;
                    fdataY[i] = AccYc[i].Magnitude;
                    fdataZ[i] = AccZc[i].Magnitude;
                }
                double[] fdataA = fdataX.Zip(fdataY, (a, b) => Math.Sqrt(a * a + b * b)).Zip(fdataZ, (a, b) => Math.Sqrt(a * a + b * b)).ToArray();
                Array.Sort(fdataA);
                Array.Reverse(fdataA);
                int index = (int)Math.Floor(0.3 / dataSecond * 2048);
                double JI = Math.Round((2 * Math.Log(fdataA[index], 10)) + 0.96, 2, MidpointRounding.AwayFromZero);
                Console.WriteLine("計算終了");
                Console.WriteLine(JI);
                throw new Exception("ok");
            }
        }
    }
}
