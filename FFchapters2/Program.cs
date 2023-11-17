#region using
using CommandLine;
using Spectre.Console;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
#endregion

#region Declarations
//Declarations
string buffer;
List<string> ScenesRaw = new();
List<string> ScenesAll = new();
List<string> ScenesSelected = new();
List<string> ChaptersRaw = new();
List<string> MetaChaptersRaw = new();
List<string> Chapters = new();
List<string> MetaChapters = new();
int ShortStringMaxLength = 80;
int Length = 0;
int Duration = 0;
int Progress = 0;
string ChapterTitle = "";
string ChapterFile = "";
string InputFile = "";
string FFmpeg = "";
string FFmpegVersion = "";
string MetaFile = "";
string AppVersion = $"[green]Version: {"V" + Assembly.GetEntryAssembly().GetName().Version.Major.ToString() + "." + Assembly.GetEntryAssembly().GetName().Version.MinorRevision.ToString()}[/]";
string? AppName = Assembly.GetEntryAssembly().GetName().Name;
bool Close = false;
bool IgnoreExistingChapters = false;
bool ChapterStyle1 = false;
bool ChapterStyle2 = false;
bool OSLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
//GitVersion will be only be actualized/overwritten when using Cake build!
const string GitVersion = "git-7f23622";
#endregion

#region Title
//Title
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
FigletText fText = new FigletText(FigletFont.Default, AppName ?? "FFChapters2").Centered().Color(Color.Blue);
#if DEBUG
AppVersion += $" [red]Debug[/]";
#else
AppVersion += $" [green]Release[/]";
#endif
#if NET6_0
AppVersion += $" [green].NET6[/]";
#elif NET7_0
AppVersion += $" [green].NET7[/]";
#elif NET8_0
AppVersion += $" [green].NET8[/]";
#endif
if (GitVersion != "") AppVersion += $" [green]{GitVersion}[/]";
AnsiConsole.Profile.Out.SetEncoding(Encoding.UTF8);
AnsiConsole.Clear();
AnsiConsole.Write(fText);
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine(AppVersion);
AnsiConsole.WriteLine();
#endregion

#region Commandline Argument Parser
AnsiConsole.Write(new Rule("[blue]Commandline arguments[/]"));
AnsiConsole.WriteLine("");

Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       //FFmpeg options
                       if (o.FFmpeg != "")
                       {
                           if (File.Exists(o.FFmpeg))
                           {
                               if (Path.GetDirectoryName(o.FFmpeg) != string.Empty)
                               {
                                   FFmpeg = o.FFmpeg;
                                   AnsiConsole.MarkupLine($"[green]FFmpeg path: \"{ShortString(FFmpeg, ShortStringMaxLength)}\"[/]");
                               }
                               else
                               {
                                   //Try to get full ffmpeg path
                                   buffer = Environment.GetEnvironmentVariable("PATH").Split(';').Where(s => File.Exists(Path.Combine(s, o.FFmpeg))).FirstOrDefault();

                                   //Fallback to old value in case path is not found or autogenerate full path
                                   if (buffer == null)
                                   {
                                       FFmpeg = o.FFmpeg;
                                       AnsiConsole.MarkupLine($"[green]FFmpeg path: \"{ShortString(FFmpeg, ShortStringMaxLength)}\"[/]");
                                   }
                                   else
                                   {
                                       FFmpeg = buffer + "\\" + o.FFmpeg;
                                       AnsiConsole.MarkupLine($"[green]FFmpeg path: \"{ShortString(FFmpeg, ShortStringMaxLength)}\" (in Environment Path)[/]");
                                   }
                               }
                               
                           }
                           else
                           {
                               if (OSLinux)
                                   buffer = Environment.GetEnvironmentVariable("PATH").Split(';').Where(s => File.Exists(Path.Combine(s, "ffmpeg"))).FirstOrDefault();
                               else
                                   buffer = Environment.GetEnvironmentVariable("PATH").Split(';').Where(s => File.Exists(Path.Combine(s, "ffmpeg.exe"))).FirstOrDefault();
                               if (buffer == null)
                               {
                                   AnsiConsole.MarkupLine($"[yellow]FFmpeg path: \"{ShortString(o.FFmpeg, ShortStringMaxLength)}\" not found[/]");
                                   if (OSLinux)
                                       FFmpeg = "/usr/bin/local/ffmpeg";
                                   else
                                       FFmpeg = "ffmpeg.exe";
                                   AnsiConsole.MarkupLine($"[green]FFmpeg path: \"{ShortString(FFmpeg, ShortStringMaxLength)}\" (Default Fallback)[/]");
                               }
                               else
                               {
                                   if (OSLinux)
                                       FFmpeg = buffer + "\\ffmpeg.exe";
                                   else
                                       FFmpeg = buffer + "\\ffmpeg.exe";
                                   AnsiConsole.MarkupLine($"[green]FFmpeg path: \"{ShortString(FFmpeg, ShortStringMaxLength)}\" (in Environment Path)[/]");
                               }
                           }
                       }

                       //FFmpeg not found abort
                       if (!File.Exists(FFmpeg))
                       {
                           AnsiConsole.MarkupLine($"[red]FFmpeg not found! Abort chapter creation![/]");
                           AnyKey();
                           Environment.Exit(-1);
                       }

                       //FFmpeg version
                       FFmpegVersion = GetFFmpegVersion();
                       if (FFmpegVersion != string.Empty)
                           AnsiConsole.MarkupLine($"[green]FFmpeg version: {FFmpegVersion}[/]");
                       else
                           AnsiConsole.MarkupLine($"[yellow]FFmpeg version: unknown[/]");

                       //Input file option
                       if (o.InputFile != "")
                       {
                           if (File.Exists(o.InputFile))
                           {
                               if (o.InputFile.Substring(0, 2) == ".\\")
                               {
                                   InputFile = Environment.CurrentDirectory + o.InputFile.Replace(".\\", "\\");
                                   AnsiConsole.MarkupLine($"[green]Inputfile path: \"{ShortString(InputFile, ShortStringMaxLength)}\" (Auto)[/]");
                               }
                               else
                               {
                                   InputFile = o.InputFile;
                                   AnsiConsole.MarkupLine($"[green]Inputfile path: \"{ShortString(InputFile, ShortStringMaxLength)}\"[/]");
                               }
                           }
                           else
                           {
                               AnsiConsole.MarkupLine($"[yellow]Inputfile path: \"{ShortString(o.InputFile, ShortStringMaxLength)}\" not found[/]");
                               AnsiConsole.MarkupLine($"[red]Inputfile missing! Abort chapter creation![/]");
                               AnyKey();
                               Environment.Exit(-1);
                           }
                       }
                       else
                       {
                           if (args.Length < 1 || args.Length > 1)
                           {
                               AnsiConsole.MarkupLine($"[red]Inputfile missing! Abort chapter creation![/]");
                               AnyKey();
                               Environment.Exit(-1);
                           }
                           else
                           {
                               if (File.Exists(args[0]))
                               {
                                   if (args[0].Substring(0, 2) == ".\\")
                                       InputFile = Environment.CurrentDirectory + args[0].Replace(".\\", "\\");
                                   else
                                       InputFile = args[0];
                                   AnsiConsole.MarkupLine($"[green]Inputfile path: \"{ShortString(InputFile, ShortStringMaxLength)}\"[/]");
                                   o.ChapterFile = Path.ChangeExtension(InputFile, ".txt");
                                   o.ChapterLength = 5;
                                   o.IgnoreExistingChapters = true;
                               }
                               else
                               {
                                   AnsiConsole.MarkupLine($"[red]Inputfile missing! Abort chapter creation![/]");
                                   AnyKey();
                                   Environment.Exit(-1);
                               }
                           }
                       }

                       //Chapter options
                       if (o.IgnoreExistingChapters)
                       {
                           IgnoreExistingChapters = o.IgnoreExistingChapters;
                           AnsiConsole.MarkupLine($"[green]Inputfile: ignore existing chapters[/]");
                       }

                       if (o.ChapterStyle.ToLowerInvariant() == "chapters")
                       {
                           ChapterStyle1 = true;
                           ChapterStyle2 = false;
                           AnsiConsole.MarkupLine($"[green]Chapterfile style: \"chapters\"[/]");
                       }
                       else if (o.ChapterStyle.ToLowerInvariant() == "meta")
                       {
                           ChapterStyle1 = false;
                           ChapterStyle2 = true;
                           AnsiConsole.MarkupLine($"[green]Chapterfile style: \"meta\"[/]");
                       }
                       else
                       {
                           ChapterStyle1 = true;
                           ChapterStyle2 = true;
                           AnsiConsole.MarkupLine($"[green]Chapterfile style: \"chapters, meta\" (all)[/]");
                       }

                       if (o.ChapterFile != "" && o.ChapterFile != InputFile)
                       {
                           ChapterFile = o.ChapterFile;
                           if (ChapterStyle1 && !ChapterStyle2)
                               AnsiConsole.MarkupLine($"[green]Chapterfile path: \"{ShortString(ChapterFile, ShortStringMaxLength)}\"[/]");
                           else if (!ChapterStyle1 && ChapterStyle2)
                           {
                               MetaFile = ChapterFile;
                               AnsiConsole.MarkupLine($"[green]Metafile path: \"{ShortString(MetaFile, ShortStringMaxLength)}\"[/]");
                           }
                           else if (ChapterStyle1 && ChapterStyle2)
                           {
                               //MetaFile = ChapterFile + ".meta";
                               MetaFile = Path.ChangeExtension(ChapterFile, ".meta");
                               AnsiConsole.MarkupLine($"[green]Chapterfile path: \"{ShortString(ChapterFile, ShortStringMaxLength)}\"[/]");
                               AnsiConsole.MarkupLine($"[green]Metafile path: \"{ShortString(MetaFile, ShortStringMaxLength)}\" (Auto)[/]");
                           }
                       }
                       else if (o.ChapterFile == InputFile)
                       {
                           if (ChapterStyle1 && !ChapterStyle2)
                           {
                               ChapterFile = InputFile + ".txt";
                               AnsiConsole.MarkupLine($"[yellow]Chapterfile and Inputfile have the same path! Autocorrecting path![/]");
                               AnsiConsole.MarkupLine($"[green]Chapterfile path: \"{ShortString(ChapterFile, ShortStringMaxLength)}\" (Auto)[/]");
                           }
                           else if (!ChapterStyle1 && ChapterStyle2)
                           {
                               MetaFile = InputFile + ".meta";
                               AnsiConsole.MarkupLine($"[yellow]Metafile and Inputfile have the same path! Autocorrecting path![/]");
                               AnsiConsole.MarkupLine($"[green]Metafile path: \"{ShortString(ChapterFile, ShortStringMaxLength)}\" (Auto)[/]");
                           }
                           else if (ChapterStyle1 && ChapterStyle2)
                           {
                               ChapterFile = InputFile + ".txt";
                               MetaFile = InputFile + ".meta";
                               AnsiConsole.MarkupLine($"[yellow]Chapterfile/Metafile and Inputfile have the same path! Autocorrecting path![/]");
                               AnsiConsole.MarkupLine($"[green]Chapterfile path: \"{ShortString(ChapterFile, ShortStringMaxLength)}\" (Auto)[/]");
                               AnsiConsole.MarkupLine($"[green]Metafile path: \"{ShortString(ChapterFile, ShortStringMaxLength)}\" (Auto)[/]");
                           }
                       }
                       else
                       {
                           if (ChapterStyle1 && !ChapterStyle2)
                           {
                               AnsiConsole.MarkupLine($"[yellow]Chapterfile path missing! Autocreating path![/]");
                               ChapterFile = Path.ChangeExtension(InputFile, ".txt");
                               AnsiConsole.MarkupLine($"[green]Chapterfile path: \"{ShortString(ChapterFile, ShortStringMaxLength)}\" (Auto)[/]");
                           }
                           else if (!ChapterStyle1 && ChapterStyle2)
                           {
                               AnsiConsole.MarkupLine($"[yellow]Metafile path missing! Autocreating path![/]");
                               MetaFile = Path.ChangeExtension(InputFile, ".meta");
                               AnsiConsole.MarkupLine($"[green]Metafile path: \"{ShortString(MetaFile, ShortStringMaxLength)}\" (Auto)[/]");
                           }
                           else if (ChapterStyle1 && ChapterStyle2)
                           {
                               AnsiConsole.MarkupLine($"[yellow]Chapterfile/Metafile paths missing! Autocreating path![/]");
                               ChapterFile = Path.ChangeExtension(InputFile, ".txt");
                               MetaFile = Path.ChangeExtension(InputFile, ".meta");
                               AnsiConsole.MarkupLine($"[green]Chapterfile path: \"{ShortString(ChapterFile, ShortStringMaxLength)}\" (Auto)[/]");
                               AnsiConsole.MarkupLine($"[green]Metafile path: \"{ShortString(MetaFile, ShortStringMaxLength)}\" (Auto)[/]");
                           }
                           else
                           {
                               AnsiConsole.MarkupLine($"[red]Chapterfile/Metafile paths missing! Autocreating path not possible![/]");
                               AnyKey();
                               Environment.Exit(-1);
                           }
                       }

                       if (o.ChapterLength > 0 && o.ChapterLength < 16)
                       {
                           Length = (int)o.ChapterLength;
                           AnsiConsole.MarkupLine($"[green]Chapter length: {Length} (Minutes)[/]");
                       }

                       if (o.Close)
                       {
                           Close = o.Close;
                           AnsiConsole.MarkupLine($"[green]Auto close: after chapter creation and on errors[/]");
                       }

                       if (o.ChapterTitle != "")
                       {
                           ChapterTitle = o.ChapterTitle;
                           AnsiConsole.MarkupLine($"[green]Chaptertitle: \"{ChapterTitle}\"[/]");
                       }
                       else
                       {
                           buffer = CultureInfo.CurrentCulture.ThreeLetterISOLanguageName.ToLowerInvariant();
                           if (buffer == "deu")
                               ChapterTitle = "Kapitel";
                           else if (buffer == "fra")
                               ChapterTitle = "Chapitre";
                           else if (buffer == "ita")
                               ChapterTitle = "Capitolo";
                           else if (buffer == "spa")
                               ChapterTitle = "Capitulo";
                           else
                               ChapterTitle = "Chapter";
                           AnsiConsole.MarkupLine($"[green]Chaptertitle: \"{ChapterTitle}\" (System Standard \"{buffer}\")[/]");
                       }
                   })
                   .WithNotParsed<Options>(o =>
                   {
                       AnsiConsole.MarkupLine($"[red]Commandline arguments missing! Abort chapter creation![/]");
                       AnyKey();
                       Environment.Exit(-1);
                   });
#endregion

#region Menu
//Menu
if (Length == 0)
{
    AnsiConsole.Write(new Rule("[blue]Chapter length[/]"));
    AnsiConsole.WriteLine("");
    var prompt = new SelectionPrompt<string>
    {
        PageSize = 5,
        Title = "[green]Chapter average length selection[/]",
        Mode = SelectionMode.Leaf,
        MoreChoicesText = "[grey](Move up and down to reveal more options)[/]"
    };
    prompt.AddChoice("1 minute");
    for (int i = 2; i < 16; i++)
    {
        prompt.AddChoice($"{i} minutes");
    }
    string selection = AnsiConsole.Prompt(prompt);
    AnsiConsole.Markup($"[green]{selection}[/]");
    AnsiConsole.WriteLine("");
    int.TryParse(selection.Replace(" minutes", "").Replace(" minute", ""), out Length);
}
Length *= 60;
AnsiConsole.WriteLine("");
#endregion

#region FFmpeg execution
AnsiConsole.Write(new Rule("[blue]FFmpeg extract scene changes[/]"));
AnsiConsole.WriteLine("");
AnsiConsole
    //.Status()
    //.Spinner(Spinner.Known.Material)
    //.SpinnerStyle(Style.Parse("green bold"))
    //.AutoRefresh(false)
    .Progress()
    .AutoRefresh(true)
    .HideCompleted(true)
    .Columns(new ProgressColumn[]
    {
        new TaskDescriptionColumn(),    // Task description
        new ProgressBarColumn(),        // Progress bar
        new PercentageColumn(),         // Percentage
        new RemainingTimeColumn(),      // Remaining time
        new SpinnerColumn(),            // Spinner
    })
    .Start(ctx =>
    {
        var task = ctx.AddTask("[green]Extracting scene changes[/]");
        try
        {
            //Prepare FFmpeg process for chapter extraction
            Process RunProcess = new();
            if (OSLinux)
            {
                RunProcess.StartInfo.FileName = $"{@FFmpeg}";               
                RunProcess.StartInfo.Arguments = $"-hide_banner -i \"{@InputFile}\" -vf blackdetect=d=1.0:pic_th=0.90:pix_th=0.00,blackframe=98:32,\"select='gt(scene,0.10)',showinfo\" -an -f null -";
            }
            else
            {
                RunProcess.StartInfo.FileName = "cmd.exe";
                RunProcess.StartInfo.Arguments = $"/c {@FFmpeg} -hide_banner -i \"{@InputFile}\" -vf blackdetect=d=1.0:pic_th=0.90:pix_th=0.00,blackframe=98:32,\"select='gt(scene,0.10)',showinfo\" -an -f null - 2>&1";
            }
            RunProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(@InputFile);
            RunProcess.StartInfo.UseShellExecute = false;
            RunProcess.StartInfo.RedirectStandardOutput = true;
            RunProcess.StartInfo.RedirectStandardError = true;
            RunProcess.StartInfo.RedirectStandardInput = false;
            RunProcess.StartInfo.Verb = "";
            RunProcess.EnableRaisingEvents = true;
            RunProcess.StartInfo.CreateNoWindow = true;
            //Optional preparation in case environment variables might be used in the future. Uncomment next line if needed.
            //RunProcess.StartInfo.EnvironmentVariables["FFREPORT"] = $"file='{TempFile}':level=32";

            void DataReadHandler(object sender, DataReceivedEventArgs e)
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    //Add aoutput to raw list
                    ScenesRaw.Add(e.Data);

                    //Extract inputfile duration and actual progress
                    if (Duration == 0)
                    {
                        buffer = Regex.Match(string.Join("", ScenesRaw), @"Duration: \d\d:\d\d:\d\d.\d\d").Value.Trim().Replace("Duration: ", "", StringComparison.InvariantCultureIgnoreCase);
                        if (!string.IsNullOrEmpty(buffer))
                        {
                            Duration = Convert.ToInt32(buffer.Substring(0, 2)) * 3600 + Convert.ToInt32(buffer.Substring(3, 2)) * 60 + Convert.ToInt32(buffer.Substring(6, 2));
                            task.MaxValue = Duration;
                        }
                    }
                    else
                    {
                        buffer = Regex.Match(ScenesRaw.Last(), @" t:\d*\.\d* ").Value.Trim().Replace("t:", "", StringComparison.InvariantCultureIgnoreCase);
                        if (!string.IsNullOrEmpty(buffer))
                            Progress = Convert.ToInt32(buffer.Substring(0,buffer.IndexOf(".")));
                        else
                        {
                            buffer = Regex.Match(ScenesRaw.Last(), @" pts_time:\d*\.\d* ").Value.Trim().Replace("pts_time:", "", StringComparison.InvariantCultureIgnoreCase);
                            if (!string.IsNullOrEmpty(buffer))
                                Progress = Convert.ToInt32(buffer.Substring(0, buffer.IndexOf(".")));
                        }
                    }
                }

            }
            RunProcess.ErrorDataReceived += DataReadHandler;
            RunProcess.OutputDataReceived += DataReadHandler;

            //Start FFmpeg chapter extraction
            var Start = DateTime.Now;
            AnsiConsole.MarkupLine("[Green]Start: " + Start.ToLongTimeString() + "[/]");
            RunProcess.Start();
            RunProcess.BeginOutputReadLine();
            RunProcess.BeginErrorReadLine();

            //Show progressbar with time estimation
            while (!RunProcess.HasExited)
            {
                if (task.Value < Progress && Duration != 0)
                    task.Value = Progress;
            }
            task.Value = task.MaxValue;

            var Stop = DateTime.Now;
            AnsiConsole.MarkupLine("[Green]Stop:  " + Stop.ToLongTimeString() + "[/]");
            AnsiConsole.MarkupLine("[Green]Time:  " + (Stop - Start).ToString(@"hh\:mm\:ss") + "[/]");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[White on Red]Exception: " + e.Message + "[/]");
            AnyKey();
            Environment.Exit(-1);
        }
    });

if (ScenesRaw.Count == 0)
{
    AnsiConsole.MarkupLine($"[red]FFmpeg could not create raw scenes![/]");
    AnyKey();
    Environment.Exit(-1);
}
AnsiConsole.MarkupLine("[Green]ScenesRaw count:  " + ScenesRaw.Count.ToString() + "[/]");
AnsiConsole.WriteLine("");
#endregion

#region Read all scene changes
AnsiConsole.Write(new Rule("[blue]Read all scene changes[/]"));
AnsiConsole.Progress().Start(ctx =>
    {
        ScenesAll.Clear();

        //Define console output task
        var task1 = ctx.AddTask("[green]Extracting scene changes[/]");
        task1.MaxValue = ScenesRaw.Count;
        while (!ctx.IsFinished)
        {
            //Extract timecodes and preformat them for sorting
            for (int i = 0; i < ScenesRaw.Count; i++)
            {
                if (ScenesRaw[i].ToLowerInvariant().Contains("chapter #") && !IgnoreExistingChapters)
                {
                    buffer = ScenesRaw[i];
                    buffer = buffer.Substring(buffer.LastIndexOf(" ") + 1);
                    ScenesAll.Add(float.Parse(buffer, CultureInfo.InvariantCulture.NumberFormat).ToString("F3").PadLeft(20));
                }

                if (ScenesRaw[i].ToLowerInvariant().Contains("parsed_showinfo") && ScenesRaw[i].ToLowerInvariant().Contains("pts_time"))
                {
                    buffer = "";
                    //Support pts_time format for ffmpeg versions <=5.1.2 and >=6.0
                    buffer = Regex.Match(ScenesRaw[i], "(?<=pts_time:)(.*)(?=\\sduration:)").Value.Trim().Replace(" ", "");   //ffmpeg version >=6.0
                    if (buffer.Length == 0 || buffer == null)
                        buffer = Regex.Match(ScenesRaw[i], "(?<=pts_time:)(.*)(?=pos:)").Value.Trim().Replace(" ", "");       //ffmpeg version <=5.1.2                  
                    if (buffer.Length > 0 || buffer != null)
                        ScenesAll.Add(float.Parse(buffer, CultureInfo.InvariantCulture.NumberFormat).ToString("F3").PadLeft(20));
                }

                if (ScenesRaw[i].ToLowerInvariant().Contains("blackdetect") && ScenesRaw[i].ToLowerInvariant().Contains("black_start"))
                {
                    buffer = ScenesRaw[i];
                    buffer = Regex.Match(buffer, "(?<=black_start:)(.*)(?=black_end:)").Value.Trim();
                    ScenesAll.Add(float.Parse(buffer, CultureInfo.InvariantCulture.NumberFormat).ToString("F3").PadLeft(20));
                }

                if (ScenesRaw[i].ToLowerInvariant().Contains("Parsed_blackframe") && ScenesRaw[i].ToLowerInvariant().Contains(" t:"))
                {
                    buffer = ScenesRaw[i];
                    buffer = Regex.Match(buffer, "(?<= t:)(.*)(?=type:)").Value.Trim();
                    ScenesAll.Add(float.Parse(buffer, CultureInfo.InvariantCulture.NumberFormat).ToString("F3").PadLeft(20));
                }

                task1.Increment(1);
                ctx.Refresh();
            }
        }
    });
if (ScenesAll.Count == 0)
{
    AnsiConsole.MarkupLine($"[red]No scenes found![/]");
    AnyKey();
    Environment.Exit(-1);
}

//Sort timecodes
ScenesAll = ScenesAll.Distinct().ToList();
ScenesAll.Sort();

//Remove trailing Spaces which were needed for correct sorting
for (int i = 0; i < ScenesAll.Count; i++)
{
    ScenesAll[i] = ScenesAll[i].Trim();
}
AnsiConsole.MarkupLine("[Green]ScenesAll count:  " + ScenesAll.Count.ToString() + "[/]");
AnsiConsole.WriteLine("");
#endregion

#region Keep only scenes within chapter length
AnsiConsole.Write(new Rule("[blue]Keep only scenes within chapter length[/]"));
AnsiConsole.Progress().Start(ctx =>
{
    //Define console output task
    var task1 = ctx.AddTask("[green]Keep only scenes within chapter length[/]");
    int Chapter = 1;
    decimal MinimumNextChapter = (decimal)(Chapter * Length);
    ScenesSelected.Clear();
    var TimestampMax = Convert.ToDecimal(ScenesAll[ScenesAll.Count - 1]);

    task1.MaxValue = ScenesAll.Count;
    while (!ctx.IsFinished)
    {
        for (int i = 0; i < ScenesAll.Count; i++)
        {
            var Timestamp = Convert.ToDecimal(ScenesAll[i]);
            if ((decimal)((Chapter + 1) * Length) < Timestamp)
            {
                Chapter++;
                MinimumNextChapter = (decimal)(Chapter * Length);
            }
            else
            {
                if (Timestamp >= MinimumNextChapter && MinimumNextChapter <= TimestampMax)
                {
                    ScenesSelected.Add(ScenesAll[i]);
                    Chapter++;
                    MinimumNextChapter = (decimal)(Chapter * Length);
                }
            }
            task1.Increment(1);
            ctx.Refresh();
        }
    }
});

//Sort selected scenes
ScenesSelected = ScenesSelected.Distinct().ToList();
ScenesSelected.Sort();

//Fallback in case no scenes are found
if (ScenesSelected.Count == 0)
    ScenesSelected.Add("0");
AnsiConsole.MarkupLine("[Green]ScenesSelected count:  " + ScenesSelected.Count.ToString() + "[/]");
AnsiConsole.WriteLine("");
#endregion

#region Convert scene timecodes to hh:mm:ss.mmm time chapter format
if (ChapterStyle1)
{
    AnsiConsole.Write(new Rule("[blue]Convert scene timecodes to hh:mm:ss.mmm time chapter format[/]"));
    AnsiConsole.Progress().Start(ctx =>
    {
        ChaptersRaw.Clear();

        //Define console output task
        var task1 = ctx.AddTask("[green]Convert scene timecodes to chapters[/]");

        task1.MaxValue = ScenesSelected.Count;
        while (!ctx.IsFinished)
        {
            for (int i = 0; i < ScenesSelected.Count; i++)
            {
                var TIMECODE_Time = float.Parse(ScenesSelected[i]);
                var TIMECODE_Sec = Math.Truncate(TIMECODE_Time);
                var TIMECODE_Sec_Frac = TIMECODE_Time - Math.Truncate(TIMECODE_Time);

                //convert timecode values parts to hh:mm:ss.ms time values
                int Hours = (int)Math.Floor(Math.Truncate(TIMECODE_Sec / 3600));
                TIMECODE_Sec = TIMECODE_Sec - (Hours * 3600);
                int Minutes = (int)Math.Round(Math.Truncate(TIMECODE_Sec / 60));
                TIMECODE_Sec = TIMECODE_Sec - (Minutes * 60);
                int Seconds = (int)Math.Round(Math.Truncate(TIMECODE_Sec));
                int MilliSeconds = (int)Math.Round(TIMECODE_Sec_Frac * 1000);

                //add chapter time to temporary chapter list
                ChaptersRaw.Add(Hours.ToString("D2") + ":" + Minutes.ToString("D2") + ":" + Seconds.ToString("D2") + "." + MilliSeconds.ToString("D3"));
                task1.Increment(1);
                ctx.Refresh();
            }
        }
    });

    //Sort chapters
    ChaptersRaw = ChaptersRaw.Distinct().ToList();
    ChaptersRaw.Sort();

    //Fallback in case no chapters are found
    if (ChaptersRaw[0] != "00:00:00.000")
        ChaptersRaw.Insert(0, "00:00:00.000");

    //Sort chapters
    ChaptersRaw = ChaptersRaw.Distinct().ToList();
    ChaptersRaw.Sort();
}
#endregion

#region Convert scene timecodes to 1 / 1000000000 timebase chapter format
if (ChapterStyle2)
{
    AnsiConsole.Write(new Rule("[blue]Convert scene timecodes to 1 / 1000000000 timebase chapter format[/]"));
    AnsiConsole.Progress().Start(ctx =>
    {
        MetaChaptersRaw.Clear();

        //Define console output task
        var task1 = ctx.AddTask("[green]Convert scene timecodes to meta chapters[/]");

        var TIMECODE_Time = float.Parse(ScenesAll[ScenesAll.Count - 1]);
        var TIMECODE_Sec = Math.Truncate(TIMECODE_Time);
        var TIMECODE_Sec_Frac = TIMECODE_Time - Math.Truncate(TIMECODE_Time);

        //add last chapter time to temporary chapter list
        MetaChaptersRaw.Add(TIMECODE_Sec.ToString() + ((int)Math.Round(TIMECODE_Sec_Frac * 1000)).ToString("D3") + "000000");

        task1.MaxValue = ScenesSelected.Count;
        while (!ctx.IsFinished)
        {
            for (int i = 0; i < ScenesSelected.Count; i++)
            {
                TIMECODE_Time = float.Parse(ScenesSelected[i]);
                TIMECODE_Sec = Math.Truncate(TIMECODE_Time);
                TIMECODE_Sec_Frac = TIMECODE_Time - Math.Truncate(TIMECODE_Time);

                //add chapter time to temporary chapter list
                MetaChaptersRaw.Add(TIMECODE_Sec.ToString() + ((int)Math.Round(TIMECODE_Sec_Frac * 1000)).ToString("D3") + "000000");
                task1.Increment(1);
                ctx.Refresh();
            }
        }
    });

    //Sort chapters
    MetaChaptersRaw = MetaChaptersRaw.Distinct().ToList();
    MetaChaptersRaw = MetaChaptersRaw.OrderBy(c => Convert.ToInt64(c)).ToList();

    //Fallback in case no chapters are found
    if (MetaChaptersRaw[0] != "0")
        MetaChaptersRaw.Insert(0, "0");

    //Sort chapters
    MetaChaptersRaw = MetaChaptersRaw.Distinct().ToList();
    MetaChaptersRaw = MetaChaptersRaw.OrderBy(c => Convert.ToInt64(c)).ToList();
}
#endregion

#region Create chapter file
if (ChapterStyle1)
{
    AnsiConsole.Write(new Rule("[blue]Create chapter entries[/]"));
    AnsiConsole.Progress().Start(ctx =>
    {
        Chapters.Clear();

        //Define console output task
        var task1 = ctx.AddTask("[green]Convert chapters to chapter entries[/]");

        task1.MaxValue = ChaptersRaw.Count;
        while (!ctx.IsFinished)
        {
            for (int i = 1; i <= ChaptersRaw.Count; i++)
            {
                Chapters.Add("CHAPTER" + i.ToString("D2") + "=" + ChaptersRaw[i - 1]);
                Chapters.Add("CHAPTER" + i.ToString("D2") + "NAME=" + ChapterTitle + " " + i.ToString());
                task1.Increment(1);
                ctx.Refresh();
            }
        }
    });

    //Save chapter file
    AnsiConsole.Write(new Rule("[blue]Create chapter file[/]"));
    AnsiConsole.WriteLine("");
    try
    {
        File.Delete(ChapterFile);
        File.WriteAllLines(ChapterFile, Chapters);
    }
    catch (Exception e)
    {
        AnsiConsole.MarkupLine("[White on Red]Exception: " + e.Message + "[/]");
    }
    if (File.Exists(ChapterFile))
        AnsiConsole.MarkupLine($"[Bold White on Green]Chapter file \"{ShortString(ChapterFile, ShortStringMaxLength)}\" created[/]");
    else
        AnsiConsole.MarkupLine($"[Bold White on Red]Chapter file \"{ShortString(ChapterFile, ShortStringMaxLength)}\" not created[/]");
    Console.WriteLine("");
}
#endregion

#region Create meta file
if (ChapterStyle2)
{
    AnsiConsole.Write(new Rule("[blue]Create meta entries[/]"));
    AnsiConsole.Progress().Start(ctx =>
    {
        MetaChapters.Clear();
        MetaChapters.Add(";FFMETADATA1");

        //Define console output task
        var task1 = ctx.AddTask("[green]Convert chapters to meta entries[/]");

        task1.MaxValue = MetaChaptersRaw.Count - 1;
        int j = 0;
        while (!ctx.IsFinished)
        {
            for (int i = 1; i <= MetaChaptersRaw.Count - 1; i++)
            {
                j++;
                MetaChapters.Add("[CHAPTER]");
                MetaChapters.Add("TIMEBASE=1/1000000000");
                MetaChapters.Add("START=" + MetaChaptersRaw[i - 1]);
                MetaChapters.Add("END=" + MetaChaptersRaw[i]);
                MetaChapters.Add("title=" + ChapterTitle + " " + j.ToString());
                task1.Increment(1);
                ctx.Refresh();
            }
        }
    });

    //Save chapter file
    AnsiConsole.Write(new Rule("[blue]Create meta chapter file[/]"));
    AnsiConsole.WriteLine("");
    try
    {
        File.Delete(MetaFile);
        File.WriteAllLines(MetaFile, MetaChapters);
    }
    catch (Exception e)
    {
        AnsiConsole.MarkupLine("[White on Red]Exception: " + e.Message + "[/]");
    }
    if (File.Exists(MetaFile))
        AnsiConsole.MarkupLine($"[Bold White on Green]Chapter file \"{ShortString(MetaFile, ShortStringMaxLength)}\" created[/]");
    else
        AnsiConsole.MarkupLine($"[Bold White on Red]Chapter file \"{ShortString(MetaFile, ShortStringMaxLength)}\" not created[/]");
    Console.WriteLine("");
}
#endregion

#region End
AnsiConsole.Write(new Rule("[blue]Finished![/]"));
Console.WriteLine("");
AnyKey();
#endregion

#region Functions & Classes

#region AnyKey
void AnyKey()
{
    if (!Close)
    {
        Console.WriteLine("");
        AnsiConsole.Write(new Rule("[blue][red]<Press any Key>[/][/]"));
        Console.ReadKey();
    }
}
#endregion

#region ShortString
string ShortString(string Text, int MaxLength)
{
    if (Text.Length < 6 || MaxLength > Text.Length)
        return Text;
    string Part1 = String.Empty;
    string Part2 = String.Empty;
    int PartLength = (MaxLength - 3) / 2;
    if ((MaxLength - 3) % 2 == 0)
        Part1 = Text.Substring(0, PartLength);
    else
        Part1 = Text.Substring(0, PartLength + 1);
    Part2 = Text.Substring(Text.Length - PartLength);
    return Part1 + "..." + Part2;
}
#endregion

#region Read ffmpeg version
string GetFFmpegVersion()
{
    try
    {
        buffer = string.Empty;
        Process RunProcess = new();
        if (OSLinux)
        {
            RunProcess.StartInfo.FileName = $"{@FFmpeg}";
            RunProcess.StartInfo.Arguments = $"-version";
        }
        else
        {
            RunProcess.StartInfo.FileName = "cmd.exe";
            RunProcess.StartInfo.Arguments = $"/c {@FFmpeg} -version 2>&1";
        }
        RunProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(@FFmpeg);
        RunProcess.StartInfo.UseShellExecute = false;
        RunProcess.StartInfo.RedirectStandardOutput = true;
        RunProcess.StartInfo.RedirectStandardError = true;
        RunProcess.StartInfo.RedirectStandardInput = false;
        RunProcess.StartInfo.Verb = "";
        RunProcess.EnableRaisingEvents = true;
        RunProcess.StartInfo.CreateNoWindow = true;

        void DataReadHandler(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                buffer += e.Data;
            }
        }
        RunProcess.ErrorDataReceived += DataReadHandler;
        RunProcess.OutputDataReceived += DataReadHandler;

        RunProcess.Start();
        RunProcess.BeginOutputReadLine();
        RunProcess.BeginErrorReadLine();
        RunProcess.WaitForExit();
        return Regex.Match(buffer, @"[\d][\.][\d\.]*(?=-)").Value.Trim();
    }
    catch (Exception e)
    {
        AnsiConsole.MarkupLine("[White on Red]Exception: " + e.Message + "[/]");
        return string.Empty;
    }
}
#endregion

#region Options
public class Options
{
    public Options() { }

    [Value(0)]
    public IEnumerable<string> Props { get; set; } = new List<string>();

#if LINUX
    [Option('f', "ffmpeg", Default = "/usr/bin/ffmpeg", Required = false, HelpText = "Set path to FFmpeg")]
    public string FFmpeg { get; set; } = "/usr/bin/ffmpeg";
#else
    [Option('f', "ffmpeg", Default = "ffmpeg.exe", Required = false, HelpText = "Set path to FFmpeg.exe")]
    public string FFmpeg { get; set; } = "ffmpeg.exe";
#endif

    [Option('i', "input", Default = "", Required = false, HelpText = "Set path to input video")]
    public string InputFile { get; set; } = "";

    [Option('o', "output", Default = "", Required = false, HelpText = "Set path to output chapter file")]
    public string ChapterFile { get; set; } = "";

    [Option('l', "length", Default = 5, Required = false, HelpText = "Set chapter length in minutes (1-15)")]
    public int ChapterLength { get; set; }

    [Option('c', "close", Default = false, Required = false, HelpText = "Close application automatically after chapter creation and on errors")]
    public bool Close { get; set; }

    [Option('n', "nochapters", Default = false, Required = false, HelpText = "Ignore existing chapters in video file")]
    public bool IgnoreExistingChapters { get; set; }

    [Option('t', "title", Default = "", Required = false, HelpText = "Set chapter title (is used for all chapters)")]
    public string ChapterTitle { get; set; } = "";

    [Option('s', "style", Default = "all", Required = false, HelpText = "Set chapter style (chapters, meta, all)\n[chapters = simple chapter format Matroska compatible, meta = METAINFO ffmpeg compatible]")]
    public string ChapterStyle { get; set; } = "";
}
#endregion

#endregion