@echo off
set NuGet=%~dp0\packages\NuGet.CommandLine.2.8.5\tools\NuGet.exe
set NuSpec=RomajiToHiraganaLibrary.nuspec
set ProjectDir=%~dp0\RomajiToHiraganaLibrary\
set PackingDir=%ProjectDir%bin\Release\
set NuGetOutputDir=%~dp0\Release\

echo NuGet path : %NuGet%
echo nuspec name: %NuSpec%
echo nuspec template dir: %ProjectDir%
echo paking dir : %PackingDir%
echo nuget output dir : %NuGetOutputDir%

pause

copy /y "%ProjectDir%%NuSpec%" "%PackingDir%"
mkdir "%NuGetOutputDir%"
pushd "%NuGetOutputDir%"
"%NuGet%" pack "%PackingDir%%NuSpec%"
popd

echo Please press any key to exit.
pause > nul
