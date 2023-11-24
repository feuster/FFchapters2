//////////////////////////////////////////////////////////////////////
// Cake build script for FFchapters2
//////////////////////////////////////////////////////////////////////

#addin nuget:?package=Cake.7zip&version=3.0.0
#addin nuget:?package=Cake.FileHelpers&version=6.1.3

var artifacts = Argument("artifacts", "artifacts");
var release = Argument("release", "Release");
var configuration = Argument("configuration", "Release");
var framework = Argument("framework", "net8.0");
var runtime = Argument("runtime", "win-x64");
var target = Argument("target", "ZipRelease");
IEnumerable<string> IGitVersion;
string SGitVersion;
string sAssemblyVersion;

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("AssemblyVersion")
    .Does(() =>
    {
        if (FileExists("./FFchapters2/Program.cs"))
        {
            Console.WriteLine(Environment.NewLine + "Reading assembly version from FFchapters2.csproj");
            sAssemblyVersion = FindRegexMatchInFile("./FFchapters2/FFchapters2.csproj", 
                                "<AssemblyVersion>(.*?)</AssemblyVersion>",
                                System.Text.RegularExpressions.RegexOptions.None
                                );
            sAssemblyVersion = sAssemblyVersion.Replace("<AssemblyVersion>","").Replace("</AssemblyVersion>","").Replace("0.0.","");
            Console.WriteLine(Environment.NewLine + $"AssemblyVersion version: {sAssemblyVersion}");
        }
    });

Task("GitVersion")
    .Does(() =>
    {
    var exitCodeWithArgument = StartProcess
        (
            "git",
            new ProcessSettings{
                Arguments = "rev-parse --short HEAD",
                RedirectStandardOutput = true
            },
            out IGitVersion
        );
    SGitVersion = string.Join("", IGitVersion).Trim();
    if (SGitVersion == "") SGitVersion = "0";
    SGitVersion = "git-" + SGitVersion;
    Console.WriteLine(Environment.NewLine + $"Git version: {SGitVersion}");
    });

Task("CleanAll")
        .IsDependentOn("Clean")
        .IsDependentOn("CleanArtifacts")
    ;

Task("Clean")
    .Does(() =>
    {
        Console.WriteLine(Environment.NewLine + $"Cleaning folder: ./FFchapters2/bin/{configuration}");
        CleanDirectory($"./FFchapters2/bin/{configuration}");
    });

Task("CleanArtifacts")
    .Does(() =>
    {
        Console.WriteLine(Environment.NewLine + $"Cleaning folder: ./{artifacts}");
        CleanDirectory($"./{artifacts}");
    });

Task("CleanRelease")
    .Does(() =>
    {
        Console.WriteLine(Environment.NewLine + $"Cleaning folder: ./Release");
        CleanDirectory($"./{release}");
    });

Task("RegexGitVersion")
    .IsDependentOn("GitVersion")
    .Does(() =>
    {
        if (FileExists("./FFchapters2/Program.cs"))
        {
            Console.WriteLine(Environment.NewLine + "Patching git revision in Program.cs");
            ReplaceRegexInFiles("./FFchapters2/Program.cs", 
                                "const string GitVersion = \"(.*?)\"", 
                                $"const string GitVersion = \"{SGitVersion}\"");
        }
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("CleanRelease")
    .IsDependentOn("RegexGitVersion")
    .Does(() =>
    {
        DotNetBuild("./FFchapters2/FFchapters2.csproj", new DotNetBuildSettings
        {
            Configuration = configuration,
            Framework = framework,
            Runtime = runtime,
            OutputDirectory = Context.Environment.WorkingDirectory + $"/{release}/"
        });
    });

Task("Publish")
    .IsDependentOn("CleanAll")
    .IsDependentOn("RegexGitVersion")
    .Does(() =>
    {
        Console.WriteLine();
        DotNetPublish("./FFchapters2/FFchapters2.csproj", new DotNetPublishSettings
        {
            Configuration = configuration,
            EnableCompressionInSingleFile = true,
            Framework = framework,
            OutputDirectory = $"./{artifacts}/",
            PublishSingleFile = true,
            PublishReadyToRun = true,
            PublishTrimmed = false,
            PublishReadyToRunShowWarnings = true,
            Runtime = runtime,
            SelfContained = true           
        });
    });


Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetTest("./FFchapters2.sln", new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true
        });
    });

Task("ZipRelease")
    .IsDependentOn("AssemblyVersion")
    .IsDependentOn("Publish")
    .Does(() =>
    {
        var WorkDir = Context.Environment.WorkingDirectory;
        if (FileExists($"{WorkDir}/{release}/FFchapters2_V{sAssemblyVersion}_{runtime}_{SGitVersion}.zip"))
        {
            Console.Write(Environment.NewLine + $"Deleting existing FFchapters2_V{sAssemblyVersion}_{runtime}_{SGitVersion}.zip");
            DeleteFile($"{WorkDir}/{release}/FFchapters2_V{sAssemblyVersion}_{runtime}_{SGitVersion}.zip");
        }
        Context.Environment.WorkingDirectory +=  $"/{artifacts}/";
        Console.Write(Environment.NewLine + "Start Zipping...");
        FilePathCollection files;
        if (runtime.Contains("linux"))
        {
            files = new FilePathCollection(new[]
                        {
                            new FilePath($"./FFchapters2"),
                            new FilePath($"{WorkDir}/LICENSE"),
                            new FilePath($"{WorkDir}/README.md"),
                            new FilePath($"{WorkDir}/MANUAL.txt")
                        });
            if (FileExists($"{WorkDir}/ffmpeg") && FileExists($"{WorkDir}/LICENSE"))
            {
                Console.Write("Found ffmpeg binary and license in root folder. Adding to Zip...");
                files.Add(new FilePath($"{WorkDir}/ffmpeg"));
            }
            else
                Console.WriteLine("Found no ffmpeg binary in root folder or missing license file. Zip will contain only FFchapters2");
        }
        else
        {
            files = new FilePathCollection(new[]
                        {
                            new FilePath($"./FFchapters2.exe"),
                            new FilePath($"{WorkDir}/LICENSE"),
                            new FilePath($"{WorkDir}/README.md"),
                            new FilePath($"{WorkDir}/AddChaptersToMovieFile.cmd"),
                            new FilePath($"{WorkDir}/MANUAL.txt")
                        });
            if (FileExists($"{WorkDir}/ffmpeg.exe") && FileExists($"{WorkDir}/LICENSE"))
            {
                Console.Write("Found ffmpeg.exe and license in root folder. Adding to Zip...");
                files.Add(new FilePath($"{WorkDir}/ffmpeg.exe"));
            }
            else
                Console.WriteLine("Found no ffmpeg.exe in root folder or missing license file. Zip will contain only FFchapters2");
        }

        SwitchCompressionMethod method = new SwitchCompressionMethod();
        method.Level = 9;
        method.Method = "Bzip2";
        SevenZip(new SevenZipSettings
        {
            Command = new AddCommand
            {
                Files = files,
                CompressionMethod = method,
                Archive = new FilePath($"{WorkDir}/{release}/FFchapters2_V{sAssemblyVersion}_{runtime}_{SGitVersion}.zip"),
            }
        });
        Context.Environment.WorkingDirectory =  WorkDir;
        Console.WriteLine("finished!" + Environment.NewLine);
        if (FileExists($"{WorkDir}/{release}/FFchapters2_V{sAssemblyVersion}_{runtime}_{SGitVersion}.zip"))
            Console.WriteLine($"FFchapters2_V{sAssemblyVersion}_{runtime}_{SGitVersion}.zip successfully created!");
        else
            Console.WriteLine($"FFchapters2_V{sAssemblyVersion}_{runtime}_{SGitVersion}.zip creation failed!");
    });

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);