# FFchapters2

## Introduction
FFchapters2 is a FFmpeg based video chapter generation tool.

FFchapters2 uses FFmpeg for frame accurate movie scene change detection and creates a chapter file, suitable for e.g. for Matroska
and/or a metafile for use directly with ffmpeg (add ```-i "<PATH>\<FILENAME>.meta" -map_metadata 1``` to your ffmpeg commandline).

As alternative you can use the [AddChaptersToMovieFile.cmd](./AddChaptersToMovieFile.cmd) Windows script by drag & drop your movie file on it to create a copy with generated markers.

There is also the optional raw mode creates an additional timestamp raw file for independent use.

## FFmpeg Installation
The required FFmpeg is not be bundled with the FFchapters2 Linux release.
In that case or if you intend to use another ffmpeg binary for the Windows version you can download a FFmpeg binary
at the [FFmpeg homepage](https://ffmpeg.org/download.html).

The FFmpeg binary can be utilized by FFchapter2 via the following options:
- the FFmpeg binary is situated in the same folder as the FFchapters2 binary
- the FFmpeg binary can be globally accessed in the OS for e.g. when situated in a $PATH environment variable defined folder
- the path to the FFmpeg binary can be in FFchapters2 configured with the commandline switch -f or --ffmpeg, for e.g. ```-f "<PATH>\ffmpeg.exe"```
- as default ffmpeg binary is "ffmpeg.exe" searched in Windows and "ffmpeg" (without extension) in Linux, you can override this also with ```-f "<PATH>\<FFMPEGFILENAME><.OPTIONALEXTENSION>"```

## License / Copyright
FFchapters2 is licensed under [GPL-2.0-only](./LICENSE).

© Alexander Feuster 2023-2024

## Running Demo
![Running Demo](./Running.gif)
