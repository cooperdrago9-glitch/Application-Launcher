using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Spotlight;

public partial class MainWindow : Window
{
    private List<AppResult> apps = new();
    private List<AppResult> pinnedApps = new();
    private int selectedIndex = -1;

    private IntPtr _hwnd;
    private const int HOTKEY_ID = 9000;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            LoadApps();          // load everything first
            ShowPinnedApps();    // IMPORTANT: show UI immediately

            SearchBox.Focus();
            PlayOpenAnimation();
            Hide();
        };
    }

    // =========================
    // HOTKEY (CTRL + SPACE)
    // =========================
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;

        RegisterHotKey(_hwnd, HOTKEY_ID, 0x0002, 0x20);

        HwndSource source = HwndSource.FromHwnd(_hwnd);
        source.AddHook(HwndHook);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;

        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            ToggleWindow();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void ToggleWindow()
    {
        if (Visibility == Visibility.Visible)
            Hide();
        else
        {
            Show();
            Activate();
            SearchBox.Focus();
            SearchBox.SelectAll();
        }
    }

    // =========================
    // ANIMATION
    // =========================
    private void PlayOpenAnimation()
    {
        Opacity = 0;

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
        BeginAnimation(Window.OpacityProperty, fade);

        var scale = new ScaleTransform(0.96, 0.96);
        Glass.RenderTransformOrigin = new Point(0.5, 0.5);
        Glass.RenderTransform = scale;

        var anim = new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    // =========================
    // LOAD APPS
    // =========================
    private void LoadApps()
    {
        apps.Clear();
        pinnedApps.Clear();

        string vsCode = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Programs\Microsoft VS Code\Code.exe");

        pinnedApps.Add(new AppResult { Name = "Terminal", Path = "wt.exe" });

        if (File.Exists(vsCode))
            pinnedApps.Add(new AppResult { Name = "Visual Studio Code", Path = vsCode });

        pinnedApps.Add(new AppResult { Name = "File Explorer", Path = "explorer.exe" });

        if (File.Exists(@"C:\Program Files\Mozilla Firefox\firefox.exe"))
            pinnedApps.Add(new AppResult { Name = "Firefox", Path = @"C:\Program Files\Mozilla Firefox\firefox.exe" });

        if (File.Exists(@"C:\Program Files\Oracle\VirtualBox\VirtualBox.exe"))
            pinnedApps.Add(new AppResult { Name = "VirtualBox", Path = @"C:\Program Files\Oracle\VirtualBox\VirtualBox.exe" });

        string[] folders =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
        };

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
                continue;

            foreach (var file in Directory.GetFiles(folder, "*.lnk", SearchOption.AllDirectories))
            {
                apps.Add(new AppResult
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Path = file
                });
            }
        }
    }

    // =========================
    // SHOW PINNED APPS (FIX)
    // =========================
    private void ShowPinnedApps()
    {
        ResultsList.Items.Clear();

        foreach (var app in pinnedApps)
            ResultsList.Items.Add(app);

        selectedIndex = 0;

        if (ResultsList.Items.Count > 0)
            ResultsList.SelectedIndex = 0;
    }

    // =========================
    // SEARCH
    // =========================
    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        string query = SearchBox.Text.Trim().ToLower();

        ResultsList.Items.Clear();
        selectedIndex = -1;

        if (string.IsNullOrWhiteSpace(query))
        {
            ShowPinnedApps();
            return;
        }

        var results = apps
            .Where(a => a.Name.ToLower().Contains(query))
            .Take(8)
            .ToList();

        if (results.Count == 0)
        {
            ResultsList.Items.Add(new AppResult
            {
                Name = $"Search web for \"{SearchBox.Text}\"",
                Path = "WEB_SEARCH"
            });

            selectedIndex = 0;
            ResultsList.SelectedIndex = 0;
            return;
        }

        foreach (var r in results)
            ResultsList.Items.Add(r);

        selectedIndex = 0;
        ResultsList.SelectedIndex = 0;
    }

    // =========================
    // INPUT
    // =========================
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (ResultsList.Items.Count == 0)
        {
            if (e.Key == Key.Escape)
                Hide();

            return;
        }

        if (e.Key == Key.Down)
        {
            selectedIndex = Math.Min(selectedIndex + 1, ResultsList.Items.Count - 1);
            ResultsList.SelectedIndex = selectedIndex;
            ResultsList.ScrollIntoView(ResultsList.SelectedItem);
        }
        else if (e.Key == Key.Up)
        {
            selectedIndex = Math.Max(selectedIndex - 1, 0);
            ResultsList.SelectedIndex = selectedIndex;
            ResultsList.ScrollIntoView(ResultsList.SelectedItem);
        }
        else if (e.Key == Key.Enter)
        {
            LaunchSelected();
        }
        else if (e.Key == Key.Escape)
        {
            Hide();
        }
    }

    // =========================
    // LAUNCH
    // =========================
    private void ResultsList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        LaunchSelected();
    }

    private void LaunchSelected()
    {
        if (ResultsList.SelectedItem is not AppResult item)
            return;

        if (item.Path == "WEB_SEARCH")
        {
            string query = Uri.EscapeDataString(SearchBox.Text);

            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://www.google.com/search?q={query}",
                UseShellExecute = true
            });

            Hide();
            return;
        }

        Process.Start(new ProcessStartInfo(item.Path)
        {
            UseShellExecute = true
        });

        Hide();
    }

    // =========================
    // HOTKEY
    // =========================
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
}

// =========================
// MODEL
// =========================
public class AppResult
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}