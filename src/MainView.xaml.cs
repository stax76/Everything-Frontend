﻿
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using Shell;

namespace EverythingNET
{
    public partial class View : Window
    {
        public RoutedCommand SettingsCommand { get; } = new RoutedCommand();
        public RoutedCommand ShowMenuCommand { get; } = new RoutedCommand();

        MainViewModel ViewModel;

        public View()
        {
            DarkMode.BeforeWindowCreation();
            InitializeComponent();
            DarkMode.AfterWindowCreation(new WindowInteropHelper(this).Handle);
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }

        void DataGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while ((dep != null) & !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);

            DataGridRow row = dep as DataGridRow;
          
            if (row != null)
                row.IsSelected = true;
        }

        void DataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowMenu(PointToScreen(Mouse.GetPosition(this)));
        }

        void ShowMenu(Point screenPos)
        {
            if (MainDataGrid.SelectedItem != null)
            {
                Item item = MainDataGrid.SelectedItem as Item;
                string file = Path.Combine(item.Directory, item.Name);

                if (File.Exists(file))
                {
                    ShellContextMenu menu = new ShellContextMenu();
                    FileInfo[] files = { new FileInfo(file) };
                    IntPtr handle = new WindowInteropHelper(this).Handle;
                    System.Drawing.Point screenPos2 = new System.Drawing.Point((int)screenPos.X, (int)screenPos.Y);
                    menu.ShowContextMenu(handle, files, screenPos2);
                    Task.Run(() => {
                        Thread.Sleep(2000);
                        ViewModel.Update();
                    });
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                if (SearchTextBox.Text != "")
                    SearchTextBox.Text = "";
                else
                    Close();
            }

            if (e.Key == Key.F1)
            {
                using var proc = Process.GetCurrentProcess();

                string txt = "Everything.NET\n\nCopyright (C) 2020-2021 Frank Skare (stax76)\n\nVersion " +
                    FileVersionInfo.GetVersionInfo(proc.MainModule.FileName).FileVersion.ToString() +
                    "\n\n" + "MIT License";

                MessageBox.Show(txt);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!string.IsNullOrEmpty(ViewModel.SearchText))
                RegistryHelp.SetValue(RegistryHelp.ApplicationKey, "LastText", ViewModel.SearchText);
        }

        void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up && SearchTextBox.Text == "")
            {
                string last = RegistryHelp.GetString(RegistryHelp.ApplicationKey, "LastText");

                if (!string.IsNullOrEmpty(last))
                {
                    SearchTextBox.Text = last;
                    SearchTextBox.CaretIndex = 1000;
                }
            }

            if (MainDataGrid.Items.Count > 0)
            {
                if (e.Key == Key.Up)
                {
                    int index = MainDataGrid.SelectedIndex;
                    index--;

                    if (index < 0)
                        index = 0;

                    MainDataGrid.SelectedIndex = index;
                }

                if (e.Key == Key.Down)
                {
                    int index = MainDataGrid.SelectedIndex;
                    index++;

                    if (index > MainDataGrid.Items.Count - 1)
                        index = MainDataGrid.Items.Count - 1;

                    MainDataGrid.SelectedIndex = index;
                }
            }

            if (e.Key == Key.Apps)
            {
                Application.Current.Dispatcher.InvokeAsync(() => {
                    ShowMenu(PointToScreen(new Point(0d, 0d)));
                });
            }
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // WM_SYSKEYDOWN
            if (msg == 0x104 && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                Application.Current.Dispatcher.InvokeAsync(() => {
                    ShowMenu(PointToScreen(new Point(0d, 0d)));
                });
            }

            return IntPtr.Zero;
        }

        void Window_Activated(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ViewModel.SearchText))
                ViewModel.Update();
        }

        void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            NameColumn.Width = ActualWidth * 0.25;
            DirectoryColumn.Width = ActualWidth * 0.49;
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        void SearchTextBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            SearchTextBox.Text = Clipboard.GetText();
            SearchTextBox.SelectionStart = SearchTextBox.Text.Length;
        }

        void SearchTextBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
