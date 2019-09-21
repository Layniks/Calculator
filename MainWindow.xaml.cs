using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using System.Globalization;
using System.Windows.Threading;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace Calculator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            var path = assemblyName.Name + ".dll";

            if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
            {
                path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
            }

            using (Stream stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    return null;
                }

                var assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }

        DoubleAnimation da = new DoubleAnimation();
        DoubleAnimation sizeC = new DoubleAnimation();
        DoubleAnimation sizeAgree = new DoubleAnimation();
        DispatcherTimer timer = new DispatcherTimer();
        RegistryKey lastsettingsopen;
        RegistryKey lastsettingscreate;

        Rect rect;
        bool reset = false, znak = false, maximazed = false;
        string action = "", strokah = "";
        object result, lastnum;
        Button[] numbtn;
        Brush borderbrush;
        NCalc2.Expression ex;
        double mwidth, mheight;

        private void load(object sender, EventArgs e)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows 10"))
            {
                borderbrush = SystemParameters.WindowGlassBrush;
                leftscroll.Foreground = SystemParameters.WindowGlassBrush;
                rightscroll.Foreground = SystemParameters.WindowGlassBrush;
                fbtn.Foreground = SystemParameters.WindowGlassBrush;
            }
            else
            {
                borderbrush = new SolidColorBrush(Colors.White);
                leftscroll.Foreground = new SolidColorBrush(Colors.White);
                rightscroll.Foreground = new SolidColorBrush(Colors.White);
                fbtn.Foreground = new SolidColorBrush(Colors.White);
            }

            rect1.Fill = Brushes.Transparent;
            tb.Text = "0";
            history.Text = "0";
            history.Tag = 0;
            leftscroll.Tag = 0;
            rightscroll.Tag = 0;
            sizeAgree.FillBehavior = FillBehavior.Stop;
            sizeC.FillBehavior = FillBehavior.Stop;
            textbox1.Visibility = Visibility.Hidden;

            sizeC.Completed += new EventHandler(sizeC_Completed);
            sizeAgree.Completed += new EventHandler(sizeAgree_Completed);

            lastsettingsopen = Registry.CurrentUser.OpenSubKey("CalculatorLastSettings");

            if (lastsettingsopen != null)
            {
                if (lastsettingsopen.GetValue("FirstStart") == null)
                {
                    fgrid.Visibility = Visibility.Visible;
                    overlay.Visibility = Visibility.Visible;
                }
                else
                {
                    fgrid.Visibility = Visibility.Hidden;
                    overlay.Visibility = Visibility.Hidden;
                }

                window.Width = int.Parse(lastsettingsopen.GetValue("Width").ToString());
                window.Height = int.Parse(lastsettingsopen.GetValue("Height").ToString());
                window.Left = int.Parse(lastsettingsopen.GetValue("Left").ToString());
                window.Top = int.Parse(lastsettingsopen.GetValue("Top").ToString());
            }

            da.From = 0.0;
            da.To = 1.0;
            da.Duration = new TimeSpan(0, 0, 0, 0, 300);
            BeginAnimation(MainWindow.OpacityProperty, da);

            numbtn = new Button[10] { b24, b21, b22, b23, b18, b19, b20, b15, b16, b17 };

            leftscroll.Visibility = Visibility.Hidden;
            rightscroll.Visibility = Visibility.Hidden;

            timer.Tick += new EventHandler(timerTick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);

            active(sender, e);
        }// Load  

        private void sizeC_Completed(object sender, EventArgs e)
        {
            textbox1.FontSize = 36;
            textbox1.Height = 48;
            history.FontSize = 60;
            history.Height = 68;
        }

        private void sizeAgree_Completed(object sender, EventArgs e)
        {
            textbox1.FontSize = 58;
            textbox1.Height = 65;
            history.FontSize = 24;
            history.Height = 50;

            calc(sender, e);
        }

        private void firststart(object sender, EventArgs e)
        {
            fgrid.Visibility = Visibility.Hidden;
            overlay.Visibility = Visibility.Hidden;

            lastsettingscreate = Registry.CurrentUser.CreateSubKey("CalculatorLastSettings");
            lastsettingscreate.SetValue("FirstStart", "No", RegistryValueKind.String);
        }// First start

        private void closingAnimation(object sender, EventArgs e)
        {
            lastsettingsopen = Registry.CurrentUser.OpenSubKey("CalculatorLastSettings");

            if (lastsettingsopen != null)
            {
                if (lastsettingsopen.GetValue("FirstStart").ToString() == "No")
                {
                    lastsettingscreate = Registry.CurrentUser.CreateSubKey("CalculatorLastSettings");

                    if (maximazed == true)
                    {
                        lastsettingscreate.SetValue("Width", mwidth, RegistryValueKind.DWord);
                        lastsettingscreate.SetValue("Height", mheight, RegistryValueKind.DWord);
                        lastsettingscreate.SetValue("Left", window.Left, RegistryValueKind.DWord);
                        lastsettingscreate.SetValue("Top", window.Top, RegistryValueKind.DWord);
                    }
                    else
                    {
                        lastsettingscreate.SetValue("Width", window.Width, RegistryValueKind.DWord);
                        lastsettingscreate.SetValue("Height", window.Height, RegistryValueKind.DWord);
                        lastsettingscreate.SetValue("Left", window.Left, RegistryValueKind.DWord);
                        lastsettingscreate.SetValue("Top", window.Top, RegistryValueKind.DWord);
                    }
                }
            }
            Close();
        }// Close

        private void rect1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                this.DragMove();
            }
            else if (e.ClickCount == 2)
            {
                maximaze(sender, e);
            }
            else { }

        }// Move window

        private void timerTick(object sender, EventArgs e)
        {
            long tick = 0;
            tick = Int64.Parse(history.Tag.ToString());

            if (tick < 35)
            {
                if (rightscroll.Tag.ToString() == "1")
                {
                    if (reset == false)
                    {
                        history.ScrollToHorizontalOffset(history.HorizontalOffset + 7.5);
                    }
                    else
                    {
                        history.ScrollToHorizontalOffset(history.HorizontalOffset + 3.5);
                    }
                }

                if (leftscroll.Tag.ToString() == "1")
                {
                    if (reset == false)
                    {
                        history.ScrollToHorizontalOffset(history.HorizontalOffset - 7.5);
                    }
                    else
                    {
                        history.ScrollToHorizontalOffset(history.HorizontalOffset - 2.5);
                    }
                }

                history.Tag = tick + 1;
            }
            else
            {
                timer.Stop();
                history.Tag = 0;
                rightscroll.Tag = 0;
                leftscroll.Tag = 0;
            }
        }

        private void maximaze(object sender, EventArgs e)
        {
            mwidth = window.Width;
            mheight = window.Height;

            if (window.WindowState == WindowState.Normal)
            {
                maximazed = true;
                tomax.Content = "❐";
                window.WindowState = WindowState.Maximized;
                grid.Margin = new Thickness(5);

                b1.Style = (Style)FindResource("defaultmathactionsMAX"); b2.Style = (Style)FindResource("defaultmathactionsMAX");
                b3.Style = (Style)FindResource("defaultmathactionsMAX"); b4.Style = (Style)FindResource("defaultmathactionsMAX");
                b5.Style = (Style)FindResource("defaultmathactionsMAX"); b6.Style = (Style)FindResource("actionsMAX");

                b7.Style = (Style)FindResource("actionsMAX"); b9.Style = (Style)FindResource("actionsMAX");
                b8.Style = (Style)FindResource("actionsMAX"); b10.Style = (Style)FindResource("actionsMAX");
                b11.Style = (Style)FindResource("actionsMAX"); b12.Style = (Style)FindResource("actionsMAX");
                b13.Style = (Style)FindResource("actionsMAX"); b14.Style = (Style)FindResource("actionsMAX");

                b15.Style = (Style)FindResource("numbersMAX"); b16.Style = (Style)FindResource("numbersMAX");
                b17.Style = (Style)FindResource("numbersMAX"); b18.Style = (Style)FindResource("numbersMAX");
                b19.Style = (Style)FindResource("numbersMAX"); b20.Style = (Style)FindResource("numbersMAX");
                b21.Style = (Style)FindResource("numbersMAX"); b22.Style = (Style)FindResource("numbersMAX");
                b23.Style = (Style)FindResource("numbersMAX"); b24.Style = (Style)FindResource("numbersMAX");
            }
            else
            {
                maximazed = false;
                tomax.Content = "◻";
                window.WindowState = WindowState.Normal;
                grid.Margin = new Thickness(0);
                fontchange(sender, e);
            }

            da.To = 1;
            da.Duration = new TimeSpan(0, 0, 0, 0, 100);
            BeginAnimation(MainWindow.OpacityProperty, da);
        }// Maximaze

        private void minimaze(object sender, EventArgs e)
        {
            if (window.WindowState == WindowState.Normal)
            {
                window.WindowState = WindowState.Minimized;
            }
            else if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Minimized;
            }

            da.To = 1;
            BeginAnimation(MainWindow.OpacityProperty, da);

        }// Minimaze

        private void active(object sender, EventArgs e)
        {
            window.BorderBrush = borderbrush;
            window.BorderThickness = new Thickness(1);
        }// Focus on

        private void inactive(object sender, EventArgs e)
        {
            window.BorderBrush = new SolidColorBrush(Colors.Transparent);
        }// Focus off

        private void CopyClick(object sender, EventArgs e)
        {
            Clipboard.SetText(result.ToString());
            b14.Focus();
        }// Copy   

        private void CopylnClick(object sender, EventArgs e)
        {
            if (tb.Text.LastIndexOf(" ") > 0)
            {
                Clipboard.SetText(tb.Text.Substring(tb.Text.LastIndexOf(" ") + 1));
            }
            else
            {
                Clipboard.SetText(tb.Text);
            }

            b14.Focus();
        }// Copy last number    

        private void PasteClick(object sender, EventArgs e)
        {
            string clip = Clipboard.GetText();

            if (Regex.IsMatch(clip, "^[0-9+\\-\\.*/ ∞]+$"))
            {
                tb.Text = clip;
                history.Text = clip;
                b14.Focus();
            }
            else
            {
                da.To = 37.0;
                da.Duration = new TimeSpan(0);
                history.BeginAnimation(TextBox.FontSizeProperty, da);

                tb.Text = "";
                textbox1.Text = "";
                history.Text = "Invalid input";
                b14.Focus();
            }

            calc(sender, e);
        }// Paste 

        private void PasteAfterClick(object sender, EventArgs e)
        {
            string clip = Clipboard.GetText();

            if (Regex.IsMatch(clip, "^[0-9+\\-\\.*/ ∞]+$"))
            {
                tb.Text += clip;
                history.Text += clip;
                b14.Focus();
            }

            calc(sender, e);
        }// Paste

        private void clear(object sender, EventArgs e)
        {
            tb.Text = "0";
            history.Text = "0";
            textbox1.Text = "";
            lastnum = "0";
            result = "0";
            reset = false;
            znak = false;
            anim();

        } // CLAER (C)

        private void anim()
        {
            sizeC.From = textbox1.FontSize;
            sizeC.To = 36.0;
            sizeC.Duration = new TimeSpan(0, 0, 0, 0, 1);
            textbox1.BeginAnimation(FontSizeProperty, sizeC);

            sizeC.From = textbox1.Height;
            sizeC.To = 48.0;
            sizeC.Duration = new TimeSpan(0, 0, 0, 0, 1);
            textbox1.BeginAnimation(HeightProperty, sizeC);

            sizeC.From = history.FontSize;
            sizeC.To = 60.0;
            sizeC.Duration = new TimeSpan(0, 0, 0, 0, 1);
            history.BeginAnimation(FontSizeProperty, sizeC);

            sizeC.From = history.Height;
            sizeC.To = 68.0;
            sizeC.Duration = new TimeSpan(0, 0, 0, 0, 1);
            history.BeginAnimation(HeightProperty, sizeC);

            textbox1.Margin = new Thickness(15, 145, 15, 0);
        }// Animation

        private void clearend(object sender, RoutedEventArgs e)
        {
            if (history.Text == "Can't divide by 0")
            {
                history.Text = strokah;

                da.To = 60;
                da.Duration = new TimeSpan(0);
                history.BeginAnimation(TextBox.FontSizeProperty, da);
            }

            if (tb.Text.LastIndexOf(' ') > 0)
            {
                tb.Text = tb.Text.Substring(0, tb.Text.LastIndexOf(' ') + 1); ;
                history.Text = history.Text.Substring(0, history.Text.LastIndexOf(' ') + 1);
            }
            else
            {
                history.Text = "0";
                tb.Text = "0";
            }
        }

        private void btnsenable()
        {
            b1.IsEnabled = true; b2.IsEnabled = true; b3.IsEnabled = true;
            b4.IsEnabled = true; b5.IsEnabled = true; b6.IsEnabled = true;
            b7.IsEnabled = true; b8.IsEnabled = true; b9.IsEnabled = true;
            b10.IsEnabled = true; b11.IsEnabled = true;
        }// Enable buttons

        private void btnsdisable()
        {
            b1.IsEnabled = false; b2.IsEnabled = false; b3.IsEnabled = false;
            b4.IsEnabled = false; b5.IsEnabled = false; b6.IsEnabled = false;
            b7.IsEnabled = false; b8.IsEnabled = false; b9.IsEnabled = false;
            b10.IsEnabled = false; b11.IsEnabled = false;
        }// Disable buttons

        private void backspace(object sender, EventArgs e) // Backspace
        {
            if (history.Text == "Can't divide by 0")
            {
                history.Text = strokah;

                da.To = 60.0;
                da.Duration = new TimeSpan(0);
                history.BeginAnimation(TextBox.FontSizeProperty, da);
            }

            if (history.Text.Substring(history.Text.Length - 1) == ")" || history.Text.Substring(history.Text.Length - 1) == "²")
            {
                tb.Text = tb.Text.Substring(0, tb.Text.LastIndexOf(" ") + 2);
                int sd = 0;
                tb.Text.Substring(0, sd);
                history.Text = history.Text.Substring(0, history.Text.LastIndexOf(" ") + 2);
            }

            if (history.Text == "Invalid input")
            {
                tb.Text = "0";
                history.Text = "0";
                textbox1.Text = "";

                da.To = 60.0;
                da.Duration = new TimeSpan(0);
                history.BeginAnimation(TextBox.FontSizeProperty, da);
            }

            if (tb.Text.Length > 2)
            {
                if (Regex.IsMatch(tb.Text.Substring(tb.Text.Length - 2), ".\\.0$"))
                {
                    tb.Text = tb.Text.Substring(0, tb.Text.Length - 2);
                }
            }

            if (tb.Text.Substring(tb.Text.Length - 1) != " ")
            {
                tb.Text = tb.Text.Substring(0, tb.Text.Length - 1);
                history.Text = history.Text.Substring(0, history.Text.Length - 1);
            }
            else if (tb.Text.Substring(tb.Text.Length - 5, 2) == ".0" && tb.Text.Substring(tb.Text.Length - 1) == " ")
            {
                tb.Text = tb.Text.Substring(0, tb.Text.Length - 5);
                history.Text = history.Text.Substring(0, history.Text.Length - 3);
            }
            else
            {
                tb.Text = tb.Text.Substring(0, tb.Text.Length - 3);
                history.Text = history.Text.Substring(0, history.Text.Length - 3);
            }

            if (tb.Text != "" && history.Text != "")
            {
                calc(sender, e);
            }
            else if (history.Text == "")
            {
                tb.Text = "0";
                history.Text = "0";
                textbox1.Text = "";
            }
        }

        private void setdot(object sender, EventArgs e)
        {
            string dot = "";

            if (tb.Text.LastIndexOf(' ') > 0)
            {
                dot = tb.Text.Substring(tb.Text.LastIndexOf(' ') + 1);
            }
            else
            {
                dot = tb.Text;
            }

            if (!dot.Contains('.') && !dot.Contains('√') && !dot.Contains(")"))
            {
                if (tb.Text.Substring(tb.Text.Length - 1) != " ")
                {
                    tb.Text += ".";
                    history.Text += ".";
                }
                else
                {
                    tb.Text += "0.";
                    history.Text += "0.";
                }
            }
        } // Decimal

        private void button_Click(object sender, EventArgs e)
        {
            Button s = (Button)sender;
            addInput(s.Content.ToString());
        }

        private void addInput(string i)
        {
            if (history.Text.Substring(history.Text.Length - 1) == ")" && reset == false)
            {
            }
            else
            {
                if (tb.Text == "0" || tb.Text == "Invalid input" || reset == true || history.Text == "Can't divide by 0")
                {
                    tb.Text = i;
                    history.Text = i;
                    textbox1.Text = "";

                    if (tb.Text == "Invalid input" || reset == true)
                    {
                        anim();
                    }
                    reset = false;
                }
                else
                {
                    try
                    {
                        if (i == "0" && tb.Text.Substring(tb.Text.LastIndexOf(" ") + 1, 1) == "0")
                        {
                            try
                            {
                                if (tb.Text.Substring(tb.Text.LastIndexOf(" ") + 1, 2) == "0.")
                                {
                                    tb.Text += i;
                                    history.Text += i;
                                }
                            }
                            catch
                            {
                            }
                        }
                        else
                        {
                            tb.Text += i;
                            history.Text += i;
                        }
                    }
                    catch
                    {
                        tb.Text += i;
                        history.Text += i;
                    }
                }
            }
        }

        private void TextChanged(object sender, EventArgs e)
        {
            double dlinatb = history.ActualWidth;
            double text_widthtb = MeasureString(history.Text, history).Width;

            history.CaretIndex = history.Text.Length;
            rect = history.GetRectFromCharacterIndex(history.CaretIndex);

            history.ScrollToHorizontalOffset(history.HorizontalOffset + rect.Right);

            if (history.Text != "Invalid input" && history.Text != "Can't divide by 0")
            {
                if (b1.IsEnabled == false && b2.IsEnabled == false && b3.IsEnabled == false &&
                    b4.IsEnabled == false && b5.IsEnabled == false && b6.IsEnabled == false &&
                    b7.IsEnabled == false && b8.IsEnabled == false && b9.IsEnabled == false &&
                    b11.IsEnabled == false)
                {
                    btnsenable();
                }

                if (tb.Text.Length > 1 && znak == true)
                {
                    if (double.TryParse(tb.Text.Substring(tb.Text.Length - 1, 1), out double result1))
                    {
                        calc(sender, e);
                    }
                }

                if (tb.Text.Length == 0)
                {
                    tb.Text = "0";
                }

                if (text_widthtb > dlinatb)
                {
                    leftscroll.Visibility = Visibility.Visible;
                    rightscroll.Visibility = Visibility.Visible;
                }
                else
                {
                    leftscroll.Visibility = Visibility.Hidden;
                    rightscroll.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                btnsdisable();
            }
        } // Text Changed

        private void window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Convert.ToUInt64(e.Key) >= 34 && Convert.ToUInt64(e.Key) <= 43) ||
                (Convert.ToUInt64(e.Key) >= 74 && Convert.ToUInt64(e.Key) <= 83) ||
                (e.Key == Key.Back) || (e.Key == Key.Decimal) || (e.Key == Key.OemPeriod) ||
                (e.Key == Key.Add) || (e.Key == Key.Subtract) || (e.Key == Key.Multiply) || (e.Key == Key.Divide) ||
                (e.Key == Key.Return) || (e.Key == Key.OemMinus) || (e.Key == Key.OemPlus) ||
                (e.Key == Key.Escape) || (e.Key == Key.Delete))
            {
                if (Convert.ToUInt64(e.Key) >= 74 && Convert.ToUInt64(e.Key) <= 83)
                {
                    addInput((Convert.ToUInt64(e.Key) - 74).ToString());

                    btnpress(numbtn[Convert.ToUInt64(e.Key) - 74], "numbers", "down");
                }

                if (Convert.ToUInt64(e.Key) >= 34 && Convert.ToUInt64(e.Key) <= 43)
                {
                    addInput((Convert.ToUInt64(e.Key) - 34).ToString());

                    btnpress(numbtn[Convert.ToUInt64(e.Key) - 34], "numbers", "down");
                }

                if (e.Key == Key.Add)
                {
                    Doact(b4, e);
                    btnpress(b4, "defact", "down");
                }

                if (e.Key == Key.Subtract)
                {
                    Doact(b3, e);
                    btnpress(b3, "defact", "down");
                }

                if (e.Key == Key.Multiply || (e.Key == Key.D8 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift))
                {
                    Doact(b2, e);
                    btnpress(b2, "defact", "down");
                }

                if (e.Key == Key.Divide)
                {
                    Doact(b1, e);
                    btnpress(b1, "defact", "down");
                }

                if (e.Key == Key.OemMinus)
                {
                    Doact(b3, e);
                    btnpress(b3, "defact", "down");
                }

                if (e.Key == Key.OemPlus)
                {
                    Doact(b4, e);
                    btnpress(b4, "defact", "down");
                }

                if (e.Key == Key.OemPeriod)
                {
                    setdot(sender, e);
                    btnpress(b6, "actions", "down");
                }

                if (e.Key == Key.Decimal)
                {
                    setdot(sender, e);
                    btnpress(b6, "actions", "down");
                }

                if (e.Key == Key.Back)
                {
                    backspace(sender, e);
                    btnpress(b14, "actions", "down");
                }

                if (e.Key == Key.Return)
                {
                    agree(sender, e);
                    btnpress(b5, "defact", "down");
                }

                if (e.Key == Key.Escape)
                {
                    clear(sender, e);
                    btnpress(b13, "actions", "down");
                }

                if (e.Key == Key.Delete)
                {
                    clearend(sender, e);
                    btnpress(b12, "actions", "down");
                }
            }
        } // Key Down

        private void window_KeyUp(object sender, KeyEventArgs e)
        {
            if (Convert.ToUInt64(e.Key) >= 74 && Convert.ToUInt64(e.Key) <= 83)
            {
                btnpress(numbtn[Convert.ToUInt64(e.Key) - 74], "numbers", "up");
            }

            if (Convert.ToUInt64(e.Key) >= 34 && Convert.ToUInt64(e.Key) <= 43)
            {
                btnpress(numbtn[Convert.ToUInt64(e.Key) - 34], "numbers", "up");
            }

            if (e.Key == Key.Add)
            {
                btnpress(b4, "defact", "up");
            }

            if (e.Key == Key.Subtract)
            {
                btnpress(b3, "defact", "up");
            }

            if (e.Key == Key.Multiply)
            {
                btnpress(b2, "defact", "up");
            }

            if (e.Key == Key.Divide)
            {
                btnpress(b1, "defact", "up");
            }

            if (e.Key == Key.OemMinus)
            {
                btnpress(b3, "defact", "up");
            }

            if (e.Key == Key.OemPlus)
            {
                btnpress(b4, "defact", "up");
            }

            if (e.Key == Key.OemPeriod)
            {
                btnpress(b6, "actions", "up");
            }

            if (e.Key == Key.Decimal)
            {
                btnpress(b6, "actions", "up");
            }

            if (e.Key == Key.Back)
            {
                btnpress(b14, "actions", "up");
            }

            if (e.Key == Key.Return)
            {
                btnpress(b5, "defact", "up");
            }

            if (e.Key == Key.Escape)
            {
                btnpress(b13, "actions", "up");
            }

            if (e.Key == Key.Delete)
            {
                btnpress(b12, "actions", "up");
            }
        }// Key Up

        private Size MeasureString(string candidate, TextBox txtb)
        {
            var formattedText = new FormattedText(
                candidate,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(txtb.FontFamily, txtb.FontStyle, txtb.FontWeight, txtb.FontStretch),
                txtb.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                TextFormattingMode.Display);

            return new Size(formattedText.Width, formattedText.Height);
        }// Measure 

        private void calc(object sender, EventArgs e)
        {
            string hnum = "", tbnum = "", stroka = "", strokatb = "";
            double numtb;

            ex = new NCalc2.Expression(tb.Text);

            switch (action)
            {
                case "sqrt":

                    action = "";
                    try
                    {
                        if (tb.Text.LastIndexOf(' ') > 0)
                        {
                            hnum = history.Text.Substring(history.Text.LastIndexOf(' ') + 1);
                            tbnum = tb.Text.Substring(tb.Text.LastIndexOf(' ') + 1);

                            stroka = history.Text.Substring(0, history.Text.LastIndexOf(' ') + 1);
                            strokatb = tb.Text.Substring(0, tb.Text.LastIndexOf(' ') + 1);

                            tb.Text = strokatb + Math.Sqrt(double.Parse(tbnum)).ToString();
                            history.Text = stroka + "√" + "(" + hnum + ")";
                        }
                        else
                        {
                            tbnum = tb.Text;
                            hnum = history.Text;

                            tb.Text = Math.Sqrt(double.Parse(tbnum)).ToString();
                            history.Text = stroka + "√" + "(" + hnum + ")";
                            calc(sender, e);
                            reset = true;
                        }
                        if (!tb.Text.Contains(' '))
                        {
                            reset = true;
                        }
                    }

                    catch
                    {
                    }

                    break;

                case "reverse":

                    action = "";
                    int count = 0;
                    count = count++;

                    if (tb.Text.LastIndexOf(' ') > 0)
                    {
                        stroka = history.Text.Substring(0, history.Text.LastIndexOf(' ') + 1);
                        strokatb = tb.Text.Substring(0, tb.Text.LastIndexOf(' ') + 1);

                        hnum = history.Text.Substring(history.Text.LastIndexOf(' ') + 1);

                        if (tb.Text.Substring(tb.Text.LastIndexOf(' ') + 1, 1) == "(")
                        {

                            numtb = double.Parse(tb.Text.Substring(tb.Text.LastIndexOf(" ") + 3, tb.Text.Substring(tb.Text.LastIndexOf(" ") + 3).Length - 1));

                            tb.Text = strokatb + "(" + (-1 / numtb).ToString() + ")";

                            if (hnum.Substring(0, 1) == "(")
                            {
                                history.Text = stroka + "1/" + hnum;
                            }
                            else
                            {
                                history.Text = stroka + "1/(" + hnum + ")";
                            }
                        }
                        else
                        {
                            numtb = double.Parse(tb.Text.Substring(tb.Text.LastIndexOf(" ") + 1));

                            tb.Text = strokatb + (1 / numtb).ToString();
                            history.Text = stroka + "1/(" + hnum + ")";
                        }
                    }
                    else
                    {
                        hnum = history.Text;
                        numtb = double.Parse(tb.Text);
                        tb.Text = (1 / numtb).ToString();
                        history.Text = "1/(" + hnum + ")";
                    }

                    calc(sender, e);

                    if (!tb.Text.Contains(' '))
                    {
                        reset = true;
                    }
                    break;

                case "sqr":

                    action = "";

                    if (tb.Text.LastIndexOf(' ') > 0)
                    {
                        hnum = history.Text.Substring(history.Text.LastIndexOf(' ') + 1);
                        numtb = double.Parse(tb.Text.Substring(tb.Text.LastIndexOf(' ') + 1));

                        stroka = history.Text.Substring(0, history.Text.LastIndexOf(' ') + 1);
                        strokatb = tb.Text.Substring(0, tb.Text.LastIndexOf(' ') + 1);

                        tb.Text = strokatb + Math.Pow(numtb, 2);
                        history.Text = stroka + "(" + hnum + ")²";
                    }
                    else
                    {
                        numtb = double.Parse(tb.Text);
                        hnum = history.Text;

                        tb.Text = Math.Pow(numtb, 2).ToString();
                        history.Text = stroka + "(" + hnum + ")²";
                        calc(sender, e);
                        reset = true;
                    }
                    if (!history.Text.Contains(' '))
                    {
                        reset = true;
                    }
                    break;

                default:

                    if (tb.Text != "Invalid input")
                    {
                        if (!tb.Text.Substring(tb.Text.Length - 1).Contains(" "))
                        {
                            if (!ex.HasErrors())
                            {

                                if (history.Text.Contains("∞") || tb.Text.Contains("∞"))
                                {
                                    result = double.PositiveInfinity;
                                }
                                else result = ex.Evaluate();
                            }
                            else
                            {
                                result = double.PositiveInfinity;
                            }
                        }

                        if (!result.ToString().Contains('.') || (reset != true) || (result.ToString().Contains('.') && window.ActualWidth > 600) ||
                            (result.ToString().Contains('.') && MeasureString(textbox1.Text, textbox1).Width <= textbox1.ActualWidth))
                        {
                            textbox1.Text = "= " + result;
                        }
                        else
                        {
                            textbox1.Text = "≈ " + String.Format("{0:0.00000}", result);
                            while (textbox1.Text.Substring(textbox1.Text.Length - 1) == "0")
                            {
                                textbox1.Text = textbox1.Text.Substring(0, textbox1.Text.Length - 1);
                            }
                        }
                    }

                    lastnum = result;

                    break;
            }
        } // Calculate

        private void sqrt(object sender, RoutedEventArgs e)
        {
            if (tb.Text.Substring(tb.Text.Length - 1, 1) != " ")
            {
                action = "sqrt";
                calc(sender, e);
            }
        }// Square root

        private void reverse(object sender, RoutedEventArgs e)
        {
            if (tb.Text.Substring(tb.Text.Length - 1, 1) != " ")
            {
                action = "reverse";
                calc(sender, e);
            }
        }// 1/

        private void sqr(object sender, RoutedEventArgs e)
        {
            if (tb.Text.Substring(tb.Text.Length - 1, 1) != " ")
            {
                action = "sqr";
                calc(sender, e);
            }
        }// Square

        private void plusminus(object sender, RoutedEventArgs e)
        {
            string strokatb = tb.Text.Substring(0, tb.Text.LastIndexOf(" ") + 1),
                   strokah = history.Text.Substring(0, history.Text.LastIndexOf(" ") + 1),
                   hnum = "", numtb = "";

            if (tb.Text.LastIndexOf(" ") > 0 && tb.Text.Substring(tb.Text.LastIndexOf(" ") + 1) != "0" && tb.Text.Substring(tb.Text.Length - 1) != " ")
            {
                if (tb.Text.Substring(tb.Text.LastIndexOf(" ") + 1).Contains("(-") == true)
                {
                    numtb = tb.Text.Substring(tb.Text.LastIndexOf(" ") + 3);
                    hnum = history.Text.Substring(history.Text.LastIndexOf(" ") + 3);

                    tb.Text = strokatb + numtb.ToString().Substring(0, numtb.ToString().Length - 1);
                    history.Text = strokah + hnum.Substring(0, hnum.Length - 1);
                }
                else
                {
                    numtb = tb.Text.Substring(tb.Text.LastIndexOf(" ") + 1);
                    hnum = history.Text.Substring(history.Text.LastIndexOf(" ") + 1);

                    tb.Text = strokatb + "(-" + numtb + ")";
                    history.Text = strokah + "(-" + hnum + ")";
                }
            }
            else if (tb.Text != "0" && tb.Text != "0." && tb.Text.Substring(tb.Text.Length - 1) != " ")
            {
                if (tb.Text.Contains("-") == false)
                {
                    tb.Text = "-" + tb.Text;
                    history.Text = "-" + history.Text;
                }
                else
                {
                    tb.Text = tb.Text.Substring(1);
                    history.Text = history.Text.Substring(1);
                }
            }

            if (tb.Text != "0" && tb.Text != "0.")
            {
                calc(sender, e);
            }
        }// Plus/Minus

        private void fontchange(object sender, EventArgs e)
        {
            if (textbox1.Text.Length > 0 && reset == true)
            {
                calc(sender, e);
                sizeAgree_Completed(sender, e);
            }
            Textbox1_TextChanged(null, null);

            if (window.ActualWidth > 450 || window.ActualHeight > 600)
            {
                b1.Style = (Style)FindResource("defaultmathactionsBIG"); b2.Style = (Style)FindResource("defaultmathactionsBIG");
                b3.Style = (Style)FindResource("defaultmathactionsBIG"); b4.Style = (Style)FindResource("defaultmathactionsBIG");
                b5.Style = (Style)FindResource("defaultmathactionsBIG"); b6.Style = (Style)FindResource("actionsBIG");

                b7.Style = (Style)FindResource("actionsBIG"); b9.Style = (Style)FindResource("actionsBIG");
                b8.Style = (Style)FindResource("actionsBIG"); b10.Style = (Style)FindResource("actionsBIG");
                b11.Style = (Style)FindResource("actionsBIG"); b12.Style = (Style)FindResource("actionsBIG");
                b13.Style = (Style)FindResource("actionsBIG"); b14.Style = (Style)FindResource("actionsBIG");

                b15.Style = (Style)FindResource("numbersBIG"); b16.Style = (Style)FindResource("numbersBIG");
                b17.Style = (Style)FindResource("numbersBIG"); b18.Style = (Style)FindResource("numbersBIG");
                b19.Style = (Style)FindResource("numbersBIG"); b20.Style = (Style)FindResource("numbersBIG");
                b21.Style = (Style)FindResource("numbersBIG"); b22.Style = (Style)FindResource("numbersBIG");
                b23.Style = (Style)FindResource("numbersBIG"); b24.Style = (Style)FindResource("numbersBIG");
            }
            else
            {
                b1.Style = (Style)FindResource("defaultmathactions"); b2.Style = (Style)FindResource("defaultmathactions");
                b3.Style = (Style)FindResource("defaultmathactions"); b4.Style = (Style)FindResource("defaultmathactions");
                b5.Style = (Style)FindResource("defaultmathactions"); b6.Style = (Style)FindResource("actions");

                b7.Style = (Style)FindResource("actions"); b9.Style = (Style)FindResource("actions");
                b8.Style = (Style)FindResource("actions"); b10.Style = (Style)FindResource("actions");
                b11.Style = (Style)FindResource("actions"); b12.Style = (Style)FindResource("actions");
                b13.Style = (Style)FindResource("actions"); b14.Style = (Style)FindResource("actions");

                b15.Style = (Style)FindResource("numbers"); b16.Style = (Style)FindResource("numbers");
                b17.Style = (Style)FindResource("numbers"); b18.Style = (Style)FindResource("numbers");
                b19.Style = (Style)FindResource("numbers"); b20.Style = (Style)FindResource("numbers");
                b21.Style = (Style)FindResource("numbers"); b22.Style = (Style)FindResource("numbers");
                b23.Style = (Style)FindResource("numbers"); b24.Style = (Style)FindResource("numbers");
            }

            if (window.ActualWidth > 800 && window.ActualHeight > 800)
            {
                b1.Style = (Style)FindResource("defaultmathactionsMAX"); b2.Style = (Style)FindResource("defaultmathactionsMAX");
                b3.Style = (Style)FindResource("defaultmathactionsMAX"); b4.Style = (Style)FindResource("defaultmathactionsMAX");
                b5.Style = (Style)FindResource("defaultmathactionsMAX"); b6.Style = (Style)FindResource("actionsMAX");

                b7.Style = (Style)FindResource("actionsMAX"); b9.Style = (Style)FindResource("actionsMAX");
                b8.Style = (Style)FindResource("actionsMAX"); b10.Style = (Style)FindResource("actionsMAX");
                b11.Style = (Style)FindResource("actionsMAX"); b12.Style = (Style)FindResource("actionsMAX");
                b13.Style = (Style)FindResource("actionsMAX"); b14.Style = (Style)FindResource("actionsMAX");

                b15.Style = (Style)FindResource("numbersMAX"); b16.Style = (Style)FindResource("numbersMAX");
                b17.Style = (Style)FindResource("numbersMAX"); b18.Style = (Style)FindResource("numbersMAX");
                b19.Style = (Style)FindResource("numbersMAX"); b20.Style = (Style)FindResource("numbersMAX");
                b21.Style = (Style)FindResource("numbersMAX"); b22.Style = (Style)FindResource("numbersMAX");
                b23.Style = (Style)FindResource("numbersMAX"); b24.Style = (Style)FindResource("numbersMAX");
            }
        }// Font change

        private void leftscroll_Click(object sender, RoutedEventArgs e)
        {
            leftscroll.Tag = 1;
            timer.Start();
        }// Scroll to left

        private void Textbox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            double fontsize = textbox1.FontSize;
            double dlina = textbox1.ActualWidth;
            double text_width = MeasureString(textbox1.Text, textbox1).Width;

            if (text_width >= dlina && textbox1.Text.Length > 0)
            {
                while (text_width >= dlina)
                {
                    textbox1.FontSize -= 0.1;
                    text_width = MeasureString(textbox1.Text, textbox1).Width;
                    dlina = textbox1.ActualWidth;
                }
            }
            else if (textbox1.Text.Length > 0)
            {
                while (text_width <= dlina && fontsize <= 42)
                {
                    textbox1.FontSize += 0.1;
                    text_width = MeasureString(textbox1.Text, textbox1).Width;
                    dlina = textbox1.ActualWidth;
                    fontsize = textbox1.FontSize;
                }
            }

            if (textbox1.Text == "")
            {
                textbox1.Visibility = Visibility.Hidden;
            }
            else
            {
                textbox1.Visibility = Visibility.Visible;
            }
        }// Textbox1 visibility

        private void rightscroll_Click(object sender, RoutedEventArgs e)
        {
            rightscroll.Tag = 1;
            timer.Start();
        }// Scroll to right

        private void percent(object sender, RoutedEventArgs e)
        {
            string pnum = "",
                   pnumtb = "",
                   tbnum = "",
                   stroka = "",
                   strokatb = "";

            if (tb.Text.LastIndexOf(' ') > 0)
            {
                if (!tb.Text.Substring(tb.Text.LastIndexOf(" ") + 1).Contains("(-"))
                {
                    tbnum = tb.Text.Substring(tb.Text.LastIndexOf(' ') + 1);

                    stroka = history.Text.Substring(0, history.Text.LastIndexOf(' ') + 1);
                    strokatb = tb.Text.Substring(0, tb.Text.LastIndexOf(' ') + 1);

                    pnumtb = tb.Text.Substring(0, tb.Text.LastIndexOf(' ') - 2);
                    pnum = pnumtb.Substring(pnumtb.LastIndexOf(' ') + 1);

                    tb.Text = strokatb + ((double.Parse(tbnum) / 100) * double.Parse(pnum));
                    history.Text = stroka + ((double.Parse(tbnum) / 100) * double.Parse(pnum));
                }
                else
                {
                    tbnum = tb.Text.Substring(tb.Text.LastIndexOf(' ') + 3, tb.Text.Substring(tb.Text.LastIndexOf(" ") + 3).Length - 1);

                    stroka = history.Text.Substring(0, history.Text.LastIndexOf(' ') + 1);
                    strokatb = tb.Text.Substring(0, tb.Text.LastIndexOf(' ') + 1);

                    pnumtb = tb.Text.Substring(0, tb.Text.LastIndexOf(' ') - 2);
                    pnum = pnumtb.Substring(pnumtb.LastIndexOf(' ') + 1);

                    tb.Text = strokatb + "(" + (-(double.Parse(tbnum) / 100) * double.Parse(pnum)) + ")";
                    history.Text = stroka + "(" + (-(double.Parse(tbnum) / 100) * double.Parse(pnum)) + ")";
                }
            }

            calc(sender, e);
        }// Percent

        private void Doact(object sender, EventArgs e)
        {
            string str = "", strh = "";
            Button s = (Button)sender;

            znak = true;

            if (tb.Text.Substring(tb.Text.Length - 1) != "∞")
            {
                if (tb.Text.Substring(tb.Text.Length - 1) == ".")
                {
                    tb.Text += "0";
                    history.Text += "0";
                }

                if (tb.Text.LastIndexOf(' ') > 0 && !tb.Text.Substring(tb.Text.LastIndexOf(' ') + 1).Contains(".")
                    && tb.Text.Substring(tb.Text.LastIndexOf(' ') + 1).Length != 0 && tb.Text.Substring(tb.Text.Length - 1) != ")")
                {
                    tb.Text += ".0";
                }
                else if (!tb.Text.Contains(".") && tb.Text.LastIndexOf(' ') < 0)
                {
                    tb.Text += ".0";
                }
            }

            str = tb.Text;
            strh = history.Text;

            if (tb.Text.Length > 3)
            {
                if (str.Substring(str.Length - 3).Contains(" ") && str.Substring(str.Length - 1).Contains(" "))
                {
                    tb.Text = str.Substring(0, str.Length - 3) + " " + s.Tag.ToString() + " ";
                    history.Text = strh.Substring(0, strh.Length - 3) + " " + s.Tag.ToString() + " ";
                }
                else
                {
                    tb.Text += " " + s.Tag.ToString() + " ";
                    history.Text += " " + s.Tag.ToString() + " ";
                }
            }
            else
            {
                tb.Text += " " + s.Tag.ToString() + " ";
                history.Text += " " + s.Tag.ToString() + " ";
            }

            if (reset == true)
            {
                if (!lastnum.ToString().Contains("."))
                {
                    tb.Text = lastnum.ToString() + ".0" + " " + s.Tag.ToString() + " ";
                    history.Text = lastnum.ToString() + " " + s.Tag.ToString() + " ";
                }
                else
                {
                    tb.Text = lastnum.ToString() + " " + s.Tag.ToString() + " ";
                    history.Text = lastnum.ToString() + " " + s.Tag.ToString() + " ";
                }

                reset = false;
                anim();
            }
        }

        private void agree(object sender, EventArgs e)
        {
            if (tb.Text != "0" && textbox1.Text != "")
            {
                reset = true;

                if (tb.Text.Substring(tb.Text.Length - 1).Contains(" "))
                {
                    tb.Text = tb.Text.Substring(0, tb.Text.Length - 3);
                    history.Text = history.Text.Substring(0, history.Text.Length - 3);
                }

                sizeAgree.From = textbox1.FontSize;
                sizeAgree.To = 58.0;
                sizeAgree.Duration = new TimeSpan(0, 0, 0, 0, 300);
                textbox1.BeginAnimation(FontSizeProperty, sizeAgree);

                sizeAgree.From = textbox1.Height;
                sizeAgree.To = 65.0;
                sizeAgree.Duration = new TimeSpan(0, 0, 0, 0, 300);
                textbox1.BeginAnimation(HeightProperty, sizeAgree);

                sizeAgree.From = history.FontSize;
                sizeAgree.To = 24.0;
                sizeAgree.Duration = new TimeSpan(0, 0, 0, 0, 300);
                history.BeginAnimation(FontSizeProperty, sizeAgree);

                sizeAgree.From = history.Height;
                sizeAgree.To = 50.0;
                sizeAgree.Duration = new TimeSpan(0, 0, 0, 0, 300);
                history.BeginAnimation(HeightProperty, sizeAgree);

                textbox1.Margin = new Thickness(15, 128, 15, 0);

                if (history.Text != "0" && history.Text != "Invalid input" && history.Text != "Can't divide by 0")
                {
                    historyfield.Text += history.Text + " " + textbox1.Text;
                    historyfield.AppendText(Environment.NewLine);

                    for (int i = 0; i <= (history.Text.Length + textbox1.Text.Length); i++)
                    {
                        historyfield.AppendText("—");
                    };

                    historyfield.AppendText(Environment.NewLine);
                }
            }
        }

        private void btnpress(Button btn, string str, string act)
        {
            if (act == "down")
            {
                if (str == "numbers")
                {
                    if (window.ActualWidth > 800 && window.ActualHeight > 800 || window.WindowState == WindowState.Maximized)
                    {
                        btn.Style = (Style)FindResource("numbersMAXpressed");
                    }
                    else if (window.ActualWidth > 450 || window.ActualHeight > 600)
                    {
                        btn.Style = (Style)FindResource("numbersBIGpressed");
                    }
                    else
                    {
                        btn.Style = (Style)FindResource("numberspressed");
                    }
                }

                if (str == "actions")
                {
                    if (window.ActualWidth > 800 && window.ActualHeight > 800 || window.WindowState == WindowState.Maximized)
                    {
                        btn.Style = (Style)FindResource("actionsMAXpressed");
                    }
                    else if (window.ActualWidth > 450 || window.ActualHeight > 600)
                    {
                        btn.Style = (Style)FindResource("actionsBIGpressed");
                    }
                    else
                    {
                        btn.Style = (Style)FindResource("actionspressed");
                    }
                }

                if (str == "defact")
                {
                    if (!System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows 10"))
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(149, 149, 149));
                    }

                    if (window.ActualWidth > 800 && window.ActualHeight > 800 || window.WindowState == WindowState.Maximized)
                    {
                        btn.Style = (Style)FindResource("defaultmathactionsMAXpressed");
                    }
                    else if (window.ActualWidth > 450 || window.ActualHeight > 600)
                    {
                        btn.Style = (Style)FindResource("defaultmathactionsBIGpressed");
                    }
                    else
                    {
                        btn.Style = (Style)FindResource("defaultmathactionspressed");
                    }
                }
            }

            if (act == "up")
            {
                if (str == "numbers")
                {
                    if (window.ActualWidth > 800 && window.ActualHeight > 800 || window.WindowState == WindowState.Maximized)
                    {
                        btn.Style = (Style)FindResource("numbersMAX");
                    }
                    else if (window.ActualWidth > 450 || window.ActualHeight > 600)
                    {
                        btn.Style = (Style)FindResource("numbersBIG");
                    }
                    else
                    {
                        btn.Style = (Style)FindResource("numbers");
                    }
                }

                if (str == "actions")
                {
                    if (window.ActualWidth > 800 && window.ActualHeight > 800 || window.WindowState == WindowState.Maximized)
                    {
                        btn.Style = (Style)FindResource("actionsMAX");
                    }
                    else if (window.ActualWidth > 450 || window.ActualHeight > 600)
                    {
                        btn.Style = (Style)FindResource("actionsBIG");
                    }
                    else
                    {
                        btn.Style = (Style)FindResource("actions");
                    }
                }

                if (str == "defact")
                {
                    if (!System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows 10"))
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                    }

                    if (window.ActualWidth > 800 && window.ActualHeight > 800 || window.WindowState == WindowState.Maximized)
                    {
                        btn.Style = (Style)FindResource("defaultmathactionsMAX");
                    }
                    else if (window.ActualWidth > 450 || window.ActualHeight > 600)
                    {
                        btn.Style = (Style)FindResource("defaultmathactionsBIG");
                    }
                    else
                    {
                        btn.Style = (Style)FindResource("defaultmathactions");
                    }
                }
            }
        }// Button press

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                window_KeyDown(sender, e);
            }
        }

        private void History_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void Historybtn_Click(object sender, RoutedEventArgs e)
        {
            if (historyoverlay.Visibility == Visibility.Hidden)
            {
                historyoverlay.Visibility = Visibility.Visible;
                overlay.Visibility = Visibility.Visible;
            }
            else
            {
                historyoverlay.Visibility = Visibility.Hidden;
                overlay.Visibility = Visibility.Hidden;
            }
        }

        private void History_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        // !Windows 10 //

        private void enter(object sender, MouseEventArgs e)
        {
            Button btn = (Button)sender;

            if (!System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows 10"))
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(93, 93, 93));
            }
        }

        private void leave(object sender, MouseEventArgs e)
        {
            Button btn = (Button)sender;

            if (!System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows 10"))
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
            }
        }

        private void down(object sender, MouseButtonEventArgs e)
        {
            Button btn = (Button)sender;

            if (!System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows 10"))
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(149, 149, 149));
                btn.Opacity = 0.92;
            }
        }

        private void up(object sender, MouseButtonEventArgs e)
        {
            Button btn = (Button)sender;

            if (!System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows 10"))
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                btn.Opacity = 0.92;
                enter(sender, e);
            }
        }
    }
}