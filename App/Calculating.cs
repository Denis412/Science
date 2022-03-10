using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Text;
using System.Threading.Tasks;

namespace App
{
    internal class Calculating : MainWindow
    {
        static int CountLayers = 0, prevLayers = 0;
        int[] MasHMin = new int[CountLayers];
        int[] MasHMax = new int[CountLayers];

        Double deltaTS = 1;
        Double N;
        Double[] MasM;
        public Calculating()
        {
            N = Convert.ToInt32(TextBoxTime.Text) / deltaTS;
        }
        public void InitializingValues()
        {
            if (TextBoxTime.Text == "" || TextBoxTempLeft.Text == "" || TextBoxTempRight.Text == "")
            {
                MessageBox.Show("Недостаточно данных! Пожалуйста, уточните введенную информацию и повторите снова!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                int Time = Convert.ToInt32(TextBoxTime.Text);

                Double deltaTM = 0.001, deltaTS = 1, deltaX = 0.001, deltaT = 1;
                Double Tx = 25 + 273.15;

                Double[] Temperatures;

                Double N = Time / deltaTS;
                Double TempLeft = Convert.ToDouble(TextBoxTempLeft.Text) + 273.15;
                Double TempRight = Convert.ToDouble(TextBoxTempRight.Text) + 273.15;

                int SumH = 0;

                Double[] MasL = new Double[CountLayers];
                Double[] MasC = new Double[CountLayers];

                Double[] Mas_H = new Double[CountLayers];
                int[] MasH = new int[CountLayers];
                Double[] MasH_Min = new Double[CountLayers];
                Double[] MasH_Max = new Double[CountLayers];

                MasM = new double[CountLayers];

                Double[] MasK = new Double[CountLayers];

                MasH_Min[0] = 0.0;
                MasHMin[0] = 1;

                for (int i = 1; i <= CountLayers; i++)
                {
                    MasL[i] = Convert.ToDouble(((TextBox)((StackPanel)LambdaPanel.Children[i]).Children[1]).Text);

                    MasC[i] = Convert.ToDouble(
                        ((TextBox)((StackPanel)HeatCapacityPanel.Children[i]).Children[1])
                         .Text.Split(new char[] { '+' })
                         .Select(x => Convert.ToDouble(x))
                         .Sum() * Tx
                        );

                    Mas_H[i] = Convert.ToDouble(((TextBox)((StackPanel)ThicknessPanel.Children[i]).Children[1]).Text);
                    MasH_Max[i] = MasH_Min[i] + Mas_H[i];
                    if (i != 0)
                        MasH_Min[i] = MasH_Max[i - 1];

                    MasK[i] = MasL[i] / MasC[i];

                    MasH[i] = Convert.ToInt32(Mas_H[i] / deltaX);
                    SumH += MasH[i];

                    MasHMax[i] = MasHMin[i] + MasH[i] - 1;
                    if (i != 0)
                        MasHMin[i] = MasHMax[i - 1] + 1;

                    if ((Math.Pow(deltaX, 2) / (2 * MasK[i])) != 0 || (Math.Pow(deltaX, 2) / (2 * MasK[i])) > deltaT)
                        throw new Exception("Не выполнено условие устойчивости решения!");

                    MasM[i] = ((MasC[i] * Math.Pow(deltaX, 2)) / (deltaT * MasL[i]));
                }

                Temperatures = new double[SumH + 1];
                for (int i = 1; i <= SumH; i++)
                {
                    Temperatures[i] = 25 + 273.15;
                }

                Temperatures[MasHMin[0] - 1] = TempLeft;
                Temperatures[MasHMax[MasHMax.Length - 1] + 1] = TempRight;

                List<List<Double>> Tg = CalculateTemperature(Temperatures);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void method(List<List<Double>> tg)
        {
            using (StreamWriter writer = new StreamWriter("Test.txt", false))
            {
                for(int i = 0; i < tg.Count; i++)
                {
                    for(int j = 0; j < tg[i].Count; j++)
                    {
                        await writer.WriteAsync(Convert.ToString(tg[i][j]));
                    }
                    await writer.WriteLineAsync("");
                }
            }
        }

        public void AddLayers()
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

        public List<List<Double>> CalculateTemperature(Double[] temperatures)
        {
            List<List<Double>> TempNew = new List<List<Double>>(CountLayers);
            List<List<Double>> TempOld = new List<List<Double>>(CountLayers);

            List<List<Double>> Tg = new List<List<Double>>(CountLayers);

            int index = -1;

            for (int i = 0; i < CountLayers; i++) 
            {
                TempOld[i] = temperatures.ToList<Double>();
                TempNew[i] = temperatures.ToList<Double>();
            }

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < CountLayers; j++)
                {
                    for (int g = MasHMin[j]; g < MasHMax[j]; g++)
                    {
                        TempNew[i][g] = TempOld[i][g] + 1 / MasM[i] * (TempOld[i][g - 1] - 2 * TempOld[i][g] + TempOld[i][g + 1]);
                    }
                }
                TempOld = TempNew;
                index = Convert.ToInt32(deltaTS * i / 1);

                for(int j = 0; j < MasHMax[MasHMax.Length - 1] + 1; j++)
                {
                    Tg[index][j] = TempNew[i][j];
                }
            }

            return Tg;
        }
    }
}
