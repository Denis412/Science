using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace App
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //delegate void Del(int x);
        //Calculating calc;
        static int CountLayers = 0, prevLayers = 0;
        int[] MasHMin;
        int[] MasHMax;

        Double deltaTS = 1;
        int N; // кол-во секунд
        Double[] MasM;
        public MainWindow()
        {
            InitializeComponent();

            //calc = new Calculating();

            MinHeight = 480;
            MinWidth = 854;
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxTime.Text == "" || TextBoxTempLeft.Text == "" || TextBoxTempRight.Text == "")
            {
                MessageBox.Show("Недостаточно данных! Пожалуйста, уточните введенную информацию и повторите снова!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                int Time = Convert.ToInt32(TextBoxTime.Text);

                N = Convert.ToInt32(Convert.ToInt32(TextBoxTime.Text) / deltaTS);

                Progress.Value = 0;
                Progress.Maximum = N;

                Double deltaTM = 0.001, deltaX = 0.001, deltaT = 1;
                Double Tx = 25 + 273.15;

                Double[] Temperatures;

                //Double N = Time / deltaTS;
                Double TempLeft = Convert.ToDouble(TextBoxTempLeft.Text) + 273.15;
                Double TempRight = Convert.ToDouble(TextBoxTempRight.Text) + 273.15;

                int SumH = 0;

                Double[] MasL = new Double[CountLayers + 1];
                Double[] MasC = new Double[CountLayers + 1];

                MasHMin = new int[CountLayers + 1];
                MasHMax = new int[CountLayers + 1];

                Double[] Mas_H = new Double[CountLayers + 1];
                int[] MasH = new int[CountLayers + 1];
                Double[] MasH_Min = new Double[CountLayers + 1];
                Double[] MasH_Max = new Double[CountLayers + 1];

                MasM = new double[CountLayers + 1];

                Double[] MasK = new Double[CountLayers + 1];

                MasH_Min[0] = 0.0;
                MasHMin[0] = 1;

                for (int i = 1; i < CountLayers + 1; i++)
                {
                    MasL[i] = Convert.ToDouble(((TextBox)((StackPanel)LambdaPanel.Children[i]).Children[1]).Text);

                    MasC[i] = Convert.ToDouble(
                        ((TextBox)((StackPanel)HeatCapacityPanel.Children[i]).Children[1])
                         .Text.Split(new char[] { '+' })
                         .Select((x, y) => Convert.ToDouble(x) * (y % 2 == 1 ? Tx : 1))
                         .Sum()
                        );

                    //MessageBox.Show($"Размер: {MasC[i]}\nTx: {Tx}");

                    Mas_H[i] = Convert.ToDouble(((TextBox)((StackPanel)ThicknessPanel.Children[i]).Children[1]).Text);

                    //TestBlock.Text = ($"Размер: {Mas_H[i]}");

                    MasH_Max[i] = MasH_Min[i] + Mas_H[i];
                    if (i != 0)
                        MasH_Min[i] = MasH_Max[i - 1];

                    MasK[i] = MasL[i] / MasC[i];

                    MasH[i] = Convert.ToInt32(Mas_H[i] / deltaX);
                    SumH += MasH[i];

                    MasHMax[i] = MasHMin[i] + MasH[i] - 1;
                    if (i != 0)
                        MasHMin[i] = MasHMax[i - 1] + 1;

                    //if ((Math.Pow(deltaX, 2) / (2 * MasK[i])) != 0 || (Math.Pow(deltaX, 2) / (2 * MasK[i])) > deltaT)
                        //throw new Exception("Не выполнено условие устойчивости решения!");

                    MasM[i] = ((MasC[i] * Math.Pow(deltaX, 2)) / (deltaT * MasL[i]));
                }

                //MessageBox.Show($"Размер: {SumH}");

                Temperatures = new Double[SumH];
                for (int i = 0; i < SumH; i++)
                {
                    Temperatures[i] = 25 + 273.15;
                }

                //MessageBox.Show($"Массив TG: {MasHMin[0] - 1}\nМассив MasHMax: {MasHMax[MasHMax.Length - 1] + 1}");

                //MessageBox.Show($"Temperatures: {Temperatures.Length}\nМассив TG: {MasHMin[0] - 1}\nМассив MasHMax: {MasHMax[MasHMax.Length - 1] + 1}");

                Temperatures[MasHMin[0] - 1] = TempLeft;
                Temperatures[MasHMax[MasHMax.Length - 1]] = TempRight;


                Progress.IsIndeterminate = true;
                List<Double[]> Tg = await CalculateTemperature(Temperatures);
                Progress.IsIndeterminate = false;

                method(Tg);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private async void method(List<Double[]> tg)
        {
            using (StreamWriter writer = new StreamWriter("Test.txt", false))
            {
                for (int i = 0; i < tg.Count; i++)
                {
                    for (int j = 0; j < tg[i].Length; j++)
                    {
                        await writer.WriteAsync(Convert.ToString(tg[i][j]));
                    }
                    await writer.WriteLineAsync("\n\n");
                }
            }
        }

        public async ValueTask<List<Double[]>> CalculateTemperature(Double[] temperatures)
        {
            Double[] TempNew = temperatures;
            Double[] TempOld = temperatures;

            List<Double[]> Tg = new List<Double[]>();

            //MessageBox.Show($"Размер: {TempNew.Length}");

            for (int i = 0; i < N; i++)
            {
                Tg.Add(new Double[temperatures.Length]);
            }
            //MessageBox.Show($"Массив TG: {Tg[0].Length}\nМассив MasHMax: {MasHMax[MasHMax.Length - 1] + 1}");
            int index = -1;

            await Task.Run(() =>
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 1; j < CountLayers + 1; j++)
                    {
                        for (int g = MasHMin[j]; g < MasHMax[j]; g++)
                        {
                            TempNew[g] = TempOld[g] + (1 / MasM[j]) * (TempOld[g - 1] - 2 * TempOld[g] + TempOld[g + 1]);
                        }
                    }
                    TempOld = TempNew;
                    index = Convert.ToInt32(deltaTS * i / 1);

                    for (int j = 0; j < MasHMax[MasHMax.Length - 1] + 1; j++)
                    {
                        Tg[index][j] = TempNew[j];
                    }
                }
            });

            return Tg;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            TestBlock.Text = Convert.ToString(Convert.ToDouble(((TextBox)((StackPanel)ThicknessPanel.Children[1]).Children[1]).Text)); ;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (TextBoxNumberOfLayers.Text == "")
                return;

            CountLayers += Convert.ToInt32(TextBoxNumberOfLayers.Text);

            TextBlockCurrentLayers.Text = Convert.ToString(CountLayers);

            StackPanel[] stackPanels = new StackPanel[3];
            TextBox[] textBoxes = new TextBox[3];
            TextBlock[] textBlocks = new TextBlock[3];

            StackPanel[] standartPanels = {
                    LambdaPanel,
                    HeatCapacityPanel,
                    ThicknessPanel
            };

            try
            {
                for (int i = prevLayers; i < CountLayers; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        stackPanels[j] = new StackPanel();
                        stackPanels[j].Orientation = Orientation.Horizontal;

                        textBlocks[j] = new TextBlock();
                        textBlocks[j].Text = (j == 0 ? "L" + (i + 1) : j == 1 ? "C" + (i + 1) : "H" + (i + 1)) + " ";
                        textBlocks[j].Name = j == 0 ? "TextBlockL" + (i + 1) : j == 1 ? "TextBlockC" + (i + 1) : "TextBlockH" + (i + 1);

                        stackPanels[j].Children.Add(textBlocks[j]);

                        textBoxes[j] = new TextBox();
                        textBoxes[j].Name = j == 0 ? "TextBoxL" + (i + 1) : j == 1 ? "TextBoxC" + (i + 1) : "TextBoxH" + (i + 1);
                        textBoxes[j].Width = 70;

                        stackPanels[j].Children.Add(textBoxes[j]);

                        standartPanels[j].Children.Add(stackPanels[j]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Введены некорректные данные! Проверьте правильность введенных данных!", "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            prevLayers += Convert.ToInt32(TextBoxNumberOfLayers.Text);
        }
    }
}
