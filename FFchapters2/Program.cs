﻿#region using
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
List<string> ScenesRaw = new List<string>();
List<string> ScenesAll = new List<string>();
List<string> ScenesSelected = new List<string>();
List<string> ChaptersRaw = new List<string>();
List<string> MetaChaptersRaw = new List<string>();
List<string> Chapters = new List<string>();
List<string> MetaChapters = new List<string>();
int ShortStringMaxLength = 80;
int Length = 0;
string ChapterTitle = "";
string ChapterFile = "";
string InputFile = "";
string FFmpeg = "";
string TempFile = "";
string MetaFile = "";
bool Close = false;
bool IgnoreExistingChapters = false;
bool ChapterStyle1 = false;
bool ChapterStyle2 = false;
#endregion

#region Title
//Title
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
AnsiConsole.Profile.Out.SetEncoding(Encoding.UTF8);
AnsiConsole.Clear();
AnsiConsole.Write(new FigletText(FigletFont.Default, (Assembly.GetEntryAssembly().GetName().Name.Length > 0 ? Assembly.GetEntryAssembly().GetName().Name : "FFChapters2")).Centered().Color(Color.Blue));
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine($"[green]Version: \"{"V" + Assembly.GetEntryAssembly().GetName().Version.Major.ToString() + "." + Assembly.GetEntryAssembly().GetName().Version.MinorRevision.ToString()}\"[/]");
AnsiConsole.WriteLine();
#endregion

#region Commandline Argument Parser
AnsiConsole.Write(new Rule("[blue]Commandline arguments[/]"));
AnsiConsole.WriteLine("");

Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.FFmpeg != "")
                       {
                           if (File.Exists(o.FFmpeg))
                           {
                               FFmpeg = o.FFmpeg;
                               AnsiConsole.MarkupLine($"[green]FFmpeg path: \"{ShortString(FFmpeg, ShortStringMaxLength)}\"[/]");
                           }
                           else
                           {
                               buffer = Environment.GetEnvironmentVariable("PATH").Split(';').Where(s => File.Exists(Path.Combine(s, "ffmpeg.exe"))).FirstOrDefault();
                               if (buffer == null)
                               {
                                   AnsiConsole.MarkupLine($"[yellow]FFmpeg path: \"{ShortString(o.FFmpeg, ShortStringMaxLength)}\" not found[/]");
                                   FFmpeg = "ffmpeg.exe";
                                   AnsiConsole.MarkupLine($"[green]FFmpeg path: \"{ShortString(FFmpeg, ShortStringMaxLength)}\" (Default Fallback)[/]");
                               }
                               else
                               {
                                   FFmpeg = buffer + "\\ffmpeg.exe";
                                   AnsiConsole.MarkupLine($"[green]FFmpeg path: \"{ShortString(FFmpeg, ShortStringMaxLength)}\" (in Environment Path)[/]");
                               }
                           }
                       }

                       if (o.InputFile != "")
                       {
                           if (File.Exists(o.InputFile))
                           {
                               if (o.InputFile.Substring(0, 2) == ".\\")
                                   InputFile = AppDomain.CurrentDomain.BaseDirectory + o.InputFile.Replace(".\\", "");
                               else
                                   InputFile = o.InputFile;
                               AnsiConsole.MarkupLine($"[green]Inputfile path: \"{ShortString(InputFile, ShortStringMaxLength)}\"[/]");
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
                                       InputFile = AppDomain.CurrentDomain.BaseDirectory + args[0].Replace(".\\", "");
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
                               ChapterFile = Path.ChangeExtension(o.InputFile, ".txt");
                               AnsiConsole.MarkupLine($"[green]Chapterfile path: \"{ShortString(ChapterFile, ShortStringMaxLength)}\" (Auto)[/]");
                           }
                           else if (!ChapterStyle1 && ChapterStyle2)
                           {
                               AnsiConsole.MarkupLine($"[yellow]Metafile path missing! Autocreating path![/]");
                               MetaFile = Path.ChangeExtension(o.InputFile, ".meta");
                               AnsiConsole.MarkupLine($"[green]Metafile path: \"{ShortString(MetaFile, ShortStringMaxLength)}\" (Auto)[/]");
                           }
                           else if (ChapterStyle1 && ChapterStyle2)
                           {
                               AnsiConsole.MarkupLine($"[yellow]Chapterfile/Metafile paths missing! Autocreating path![/]");
                               ChapterFile = Path.ChangeExtension(o.InputFile, ".txt");
                               MetaFile = Path.ChangeExtension(o.InputFile, ".meta");
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
                           AnsiConsole.MarkupLine($"[green]Chapter length: {Length.ToString()} (Minutes)[/]");
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
        prompt.AddChoice($"{i.ToString()} minutes");
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
AnsiConsole.Status()
    .Spinner(Spinner.Known.Material)
    .SpinnerStyle(Style.Parse("green bold"))
    .AutoRefresh(false)
    .Start("[green]FFmpeg running[/]", ctx =>
    {
        TempFile = Path.ChangeExtension(ChapterFile, ".tmp");
        try
        {
            if (File.Exists(TempFile))
                File.Delete(TempFile);

            Process RunProcess = new Process();
            RunProcess.StartInfo.FileName = "cmd.exe";
            RunProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(FFmpeg);
            RunProcess.StartInfo.Arguments = $"/c {@FFmpeg} -hide_banner -i \"{@InputFile}\" -vf blackdetect=d=1.0:pic_th=0.90:pix_th=0.00,blackframe=98:32,\"select='gt(scene,0.10)',showinfo\" -an -f null - 2>&1"; // > \"{@TempFile}\"";
            RunProcess.StartInfo.UseShellExecute = false;
            RunProcess.StartInfo.RedirectStandardOutput = true;
            RunProcess.StartInfo.Verb = "";
            RunProcess.EnableRaisingEvents = true;
            RunProcess.StartInfo.CreateNoWindow = true;
            RunProcess.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    ScenesRaw.Add(e.Data);
                }
            });
            var Start = DateTime.Now;
            AnsiConsole.MarkupLine("[Green]Start: " + Start.ToLongTimeString() + "[/]");
            RunProcess.Start();
            RunProcess.BeginOutputReadLine();
            while (!RunProcess.HasExited)
            {
                ctx.Refresh();
            }
            var Stop = DateTime.Now;
            AnsiConsole.MarkupLine("[Green]Stop:  " + Stop.ToLongTimeString() + "[/]");
            AnsiConsole.MarkupLine("[Green]Time:  " + (Stop - Start).ToString(@"hh\:mm\:ss") + "[/]");
            ctx.Refresh();
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
                    buffer = ScenesRaw[i];
                    buffer = Regex.Match(buffer, "(?<=pts_time:)(.*)(?=pos:)").Value.Trim();
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

//remove trailing Spaces which were needed for correct sorting
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
void AnyKey()
{
    if (!Close)
    {
        Console.WriteLine("");
        AnsiConsole.Write(new Rule("[blue][red]<Press any Key>[/][/]"));
        Console.ReadKey();
    }
}

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

public class Options
{
    public Options() { }

    [Value(0)]
    public IEnumerable<string> Props { get; set; }

    [Option('f', "ffmpeg", Default = "ffmpeg.exe", Required = false, HelpText = "Set path to FFmpeg.exe")]
    public string FFmpeg { get; set; } = "ffmpeg.exe";

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