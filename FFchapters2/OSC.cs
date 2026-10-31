using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Terminal;

/// <summary>
/// Operating System Command (OSC)
/// </summary>
internal partial class Osc
{
    #region Cursor
    /// <summary>
    /// Cursor styles compatible with Windows Terminal
    /// </summary>
    internal enum CursorStyle
    {
        Default,
        BoxBlinking,
        BoxStatic,
        UnderscoreBlinking,
        UnderscoreStatic,
        BarBlinking,
        BarStatic
    }

    /// <summary>
    /// Set the Terminal Cursor style (generic function)
    /// </summary>
    /// <param name="Style">new Cursor style</param>
    internal static void SetCursorStyle(CursorStyle Style)
    {
        Console.Write($"\x1b[{(int)Style} q");
    }

    /// <summary>
    /// (Re)Set the default Cursor style
    /// </summary>
    public static void SetCursorStyleDefault()
    {
        SetCursorStyle(CursorStyle.Default);
    }

    /// <summary>
    /// Set the Cursor style to box & blinking
    /// </summary>
    public static void SetCursorStyleBoxBlinking()
    {
        SetCursorStyle(CursorStyle.BoxBlinking);
    }

    /// <summary>
    /// Set the Cursor style to box & static
    /// </summary>
    public static void SetCursorStyleBoxStatic()
    {
        SetCursorStyle(CursorStyle.BoxStatic);
    }

    /// <summary>
    /// Set the Cursor style to underscore & blinking
    /// </summary>
    public static void SetCursorStyleUnderscoreBlinking()
    {
        SetCursorStyle(CursorStyle.UnderscoreBlinking);
    }

    /// <summary>
    /// Set the Cursor style to underscore & static
    /// </summary>
    public static void SetCursorStylUnderscoreStatic()
    {
        SetCursorStyle(CursorStyle.UnderscoreStatic);
    }

    /// <summary>
    /// Set the Cursor style to bar & blinking
    /// </summary>
    public static void SetCursorStyleBarBlinking()
    {
        SetCursorStyle(CursorStyle.BarBlinking);
    }

    /// <summary>
    /// Set the Cursor style to bar & static
    /// </summary>
    public static void SetCursorStyleBarStatic()
    {
        SetCursorStyle(CursorStyle.BarStatic);
    }
    #endregion

    #region Terminal Settings

    //Backup for Terminal title for optional Title reset
    private static string TerminalTitleBackup = string.Empty;

    [GeneratedRegex(@"^#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")]
    private static partial Regex HexColorString();

    /// <summary>
    /// Set the Terminal title
    /// </summary>
    /// <param name="Title">new title text</param>
    public static void SetTerminalTitle(string Title)
    {
        if (string.IsNullOrEmpty(Title))
            return;

        //Backup console title if running on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            TerminalTitleBackup = Console.Title;
        Console.Write($"\x1b]0;{Title}\x07");
    }

    /// <summary>
    /// Set Terminal foreground color
    /// </summary>
    /// <param name="ForegroundColor">color value</param>
    public static void SetTerminalForegroundColor(Color ForegroundColor)
    {
        Console.Write($"\x1b]10;#{ForegroundColor}\x07");
    }

    /// <summary>
    /// Set Terminal foreground color
    /// </summary>
    /// <param name="ForegroundColor">color value in text format #RRGGBB</param>
    public static void SetTerminalForegroundColor(String ForegroundColor)
    {
        if (string.IsNullOrEmpty(ForegroundColor) || !HexColorString().IsMatch(ForegroundColor))
            return;
        Color Foreground = ColorTranslator.FromHtml(ForegroundColor);
        Console.Write($"\x1b]10;#{Foreground.R:X2}{Foreground.G:X2}{Foreground.B:X2}\x07");
    }

    /// <summary>
    /// Set Terminal background color
    /// </summary>
    /// <param name="BackgroundColor">color value</param>
    public static void SetTerminalBackgroundColor(Color BackgroundColor)
    {
        Console.Write($"\x1b]11;#{BackgroundColor}\x07");
    }

    /// <summary>
    /// Set Terminal background color
    /// </summary>
    /// <param name="BackgroundColor">color value in text format #RRGGBB</param>
    public static void SetTerminalBackgroundColor(String BackgroundColor)
    {
        if (string.IsNullOrEmpty(BackgroundColor) || !HexColorString().IsMatch(BackgroundColor))
            return;
        Color Background = ColorTranslator.FromHtml(BackgroundColor);
        Console.Write($"\x1b]11;#{Background.R:X2}{Background.G:X2}{Background.B:X2}\x07");
    }

    /// <summary>
    /// Set Terminal cursor color
    /// </summary>
    /// <param name="CursorColor">color value</param>
    public static void SetTerminalCursorColor(Color CursorColor)
    {
        Console.Write($"\x1b]12;#{CursorColor}\x07");
    }

    /// <summary>
    /// Set Terminal cursor color
    /// </summary>
    /// <param name="CursorColor">color value in text format #RRGGBB</param>
    public static void SetTerminalCursorColor(String CursorColor)
    {
        if (string.IsNullOrEmpty(CursorColor) || !HexColorString().IsMatch(CursorColor))
            return;
        Color Cursor = ColorTranslator.FromHtml(CursorColor);
        Console.Write($"\x1b]12;#{Cursor.R:X2}{Cursor.G:X2}{Cursor.B:X2}\x07");
    }
    #endregion

    #region Terminal Reset
    /// <summary>
    /// Reset all Terminal settings to default
    /// </summary>
    public static void ResetTerminal()
    {
        ResetTerminalTitle();
        ResetTerminalForegroundColor();
        ResetTerminalBackgroundColor();
        ResetTerminalCursorColor();
        SetCursorStyleDefault();
    }

    /// <summary>
    /// Reset Terminal title to previous title (only on Windows available)
    /// </summary>
    public static void ResetTerminalTitle()
    {
        if (string.IsNullOrEmpty(TerminalTitleBackup))
            return;
        Console.Write($"\x1b]0;{TerminalTitleBackup}\x07");
    }

    /// <summary>
    /// Reset Terminal foreground color
    /// </summary>
    public static void ResetTerminalForegroundColor()
    {
        Console.Write("\x1b]110\x07");
    }

    /// <summary>
    /// Reset Terminal background color
    /// </summary>
    public static void ResetTerminalBackgroundColor()
    {
        Console.Write("\x1b]111\x07");
    }

    /// <summary>
    /// Reset Terminal cursor color
    /// </summary>
    public static void ResetTerminalCursorColor()
    {
        Console.Write("\x1b]112\x07");
    }
    #endregion
}

/// <summary>
/// Sixel
/// </summary>
internal partial class Sixel
{
    #region Sixel support check
    private static bool SixelSupportChecked = false;
    private static bool SixelAllowed = false;

    /// <summary>
    /// Check Windows Terminal for version 1.22 or newer which support OSC and Sixel
    /// </summary>
    /// <returns>true = Windows Terminal version is 1.22 or higher, false = outdated version</returns>
    public static bool IsWindowsTerminal122OrHigher()
    {
        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"(Get-AppxPackage -Name Microsoft.WindowsTerminal).Version\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                return false;

            if (!Version.TryParse(output, out Version? installedVersion))
                return false;

            return installedVersion >= new Version(1, 22);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check terminal for Sixel graphics support
    /// </summary>
    /// <param name="timeoutMs">timeout for this check in ms</param>
    /// <returns>true if Sixel graphics are support, false for missing Sixel support</returns>
    public static bool TerminalSupportsSixel(int timeoutMs = 250)
    {
        //Sixel support need only to be checked once
        if (SixelSupportChecked)
            return SixelAllowed;
        else
            SixelSupportChecked = true;

        //test if terminal reacts on „Device Attributes“ (DA) request
        Console.Write("\x1B[c");
        Console.Out.Flush();

        var sb = new StringBuilder();
        long end = Environment.TickCount64 + timeoutMs;

        while (Environment.TickCount64 < end)
        {
            while (Console.KeyAvailable)
                sb.Append(Console.ReadKey(true).KeyChar);

            if (sb.ToString().EndsWith('c'))
                break;

            Thread.Sleep(10);
        }

        //when the terminal has not responded to the DA request clear the DA request
        var m = Regex.Match(sb.ToString(), @"\x1B\[\?([0-9;]+)c");
        if (!m.Success)
        {
            Console.Write("\b\b\b   \b\b\b");
            Console.Out.Flush();
            SixelAllowed = false;
            return SixelAllowed;
        }
        string[] attrs = m.Groups[1].Value.Split(';');

        //when Sixel graphics are not supported in the attributes clear the DA request
        SixelAllowed = Array.IndexOf(attrs, "4") >= 0;
        if (!SixelAllowed)
        {
            Console.Write("\b\b\b   \b\b\b");
            Console.Out.Flush();
        }

        //result
        return SixelAllowed;
    }
    #endregion
}