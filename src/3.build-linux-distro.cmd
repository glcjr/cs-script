echo off

rem ensure WSL2 is installed and the distro is named "Ubuntu" not "Ubuntu-20.04"

cd out\ci
..\Windows\cscs.exe build-deb.cs

.\out\Windows\cscs.exe -server:stop

css -server:stop

explorer ..\
