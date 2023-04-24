@echo off
cls
if [%1]==[] goto help
title Add chapters to movie file %~n1
FFchapters2.exe -c -i %1 -s meta -o "%~n1.meta"
ffmpeg.exe -y -i %1 -i "%~n1.meta" -map_metadata 1 -c:v copy -c:a copy -c:s copy "%~d1%~p1%~n1_with_Chapters.mkv"
if exist "%~n1.meta" del "%~n1.meta"
echo.
if exist "%~d1%~p1%~n1_with_Chapters.mkv" (
	echo "%~d1%~p1%~n1_with_Chapters.mkv" created!
) else (
	echo Error: could not create new MKV "%~d1%~p1%~n1_with_Chapters.mkv"!
)
echo.
pause
exit

:help
title Script help
echo Drag and drop your movie file on this script to create a copy of your movie file with generated chapters included,
echo or call this script via commandline with the path to your movie file:
echo.
echo     %0 "MovieFilePath"
echo.
pause
exit
