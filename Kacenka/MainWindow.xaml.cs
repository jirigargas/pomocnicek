using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Kacenka
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LblPripraveno.Visibility = Visibility.Hidden;
            BtnStart.Visibility = Visibility.Hidden;
            Progres.Visibility = Visibility.Hidden;
        }

        private void VyberCsvSoubor(object sender, RoutedEventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "CSV soubory (*.csv)|*.csv";
                dialog.Multiselect = false;
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TbCsvSoubor.Text = dialog.FileName;
                }
            }

            ZobrazStartPokudJeVseOk();
        }

        private void VyberCilovyAdresar(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TbCilovyAdresar.Text = dialog.SelectedPath;
                }
            }

            ZobrazStartPokudJeVseOk();
        }

        private void ZobrazStartPokudJeVseOk()
        {
            if (string.IsNullOrWhiteSpace(TbCsvSoubor.Text)) return;
            if (string.IsNullOrWhiteSpace(TbCilovyAdresar.Text)) return;

            LblPripraveno.Visibility = Visibility.Visible;
            BtnStart.Visibility = Visibility.Visible;
        }

        private async void Start(object sender, RoutedEventArgs e)
        {
            LblPripraveno.Visibility = Visibility.Hidden;
            BtnStart.Visibility = Visibility.Hidden;
            Progres.Visibility = Visibility.Visible;


            var radky = await PrectiSoubor(TbCsvSoubor.Text);

            var header = radky[0];
            var photoFilenameIndex = header.IndexOf("photo_filename");
            var zdrojovyAdresar = Path.GetDirectoryName(TbCsvSoubor.Text);

            if (photoFilenameIndex == -1)
            {
                NastalProblem("Ten soubor nemá sloupec s názvem \"photo_filename\"!" +
                    Environment.NewLine +
                    "Takhle nevím, kde mám hledat názvy fotek.");
                return;
            }

            var vysledek = await ZpracujData(radky, photoFilenameIndex, zdrojovyAdresar, TbCilovyAdresar.Text);

            if (vysledek.Any())
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Něco ale bylo špatně:");
                foreach(var text in vysledek.Take(10))
                {
                    builder.AppendLine(text);
                }
                if(vysledek.Count() > 10)
                {
                    builder.AppendLine();
                    builder.AppendLine($"A pak tu mám ještě {vysledek.Count() - 10} dalších problémů.");
                    builder.AppendLine($"Ale ty už by se nevešly na obrazovku ...");
                }
                System.Windows.MessageBox.Show(builder.ToString(), "Jsem hotov", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                System.Windows.MessageBox.Show("Vše proběhlo v pořádku ...", "Práce splněna!", MessageBoxButton.OK, MessageBoxImage.Information);
            }


            LblPripraveno.Visibility = Visibility.Visible;
            BtnStart.Visibility = Visibility.Visible;
            Progres.Visibility = Visibility.Hidden;
        }

        private Task<List<string>> ZpracujData(List<List<string>> radky, int photoFilenameIndex, string zdrojovyAdresar, string cilovyAdresar)
        {
            return Task.Run(() =>
            {
                List<string> vysledek = new List<string>();
                int index = 1;
                foreach (var radek in radky.Skip(1))
                {
                    index += 1;
                    try
                    {
                        var fotky = radek[photoFilenameIndex]?.Split(',').Where(x => !string.IsNullOrWhiteSpace(x));
                        foreach (var fotka in fotky)
                        {
                            var jmenoFotky = Path.GetFileName(fotka);
                            var zdroj = Path.Combine(zdrojovyAdresar, fotka);
                            var cil = Path.Combine(cilovyAdresar, jmenoFotky);

                            try
                            {
                                File.Copy(zdroj, cil);
                            }
                            catch (DirectoryNotFoundException e)
                            {
                                vysledek.Add("Řádek " + index + ": nemůžu najít daný adresář ...");
                            }
                            catch (FileNotFoundException e)
                            {
                                vysledek.Add("Řádek " + index + ": nemůžu najít soubor ...");
                            }
                            catch (Exception exception)
                            {
                                Console.Error.WriteLine(exception);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        vysledek.Add("Řádek " + index + " je nějak špatný ...");
                        Console.Error.WriteLine(exception);
                    }
                }

                return vysledek;
            });
        }

        private Task<List<List<string>>> PrectiSoubor(string soubor)
        {
            return Task.Run(() =>
            {
                using (var reader = new StreamReader(new FileStream(soubor, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    var radky = new List<List<string>>();

                    while (!reader.EndOfStream)
                    {
                        var radek = reader.ReadLine();
                        var data = radek.Split(';').ToList();

                        radky.Add(data);
                    }
                    return radky;
                }
            });
        }

        private void NastalProblem(string zprava)
        {
            System.Windows.MessageBox.Show(zprava, "Něco se pokazilo!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
