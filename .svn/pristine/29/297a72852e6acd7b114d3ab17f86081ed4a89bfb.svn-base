set PRJNAME=checkmod
set /p Version=<version
set Version=%Version:,=&rem %

set NCAT=%~dp0
For /D %%a In ("%NCAT:~0,-1%.txt") Do Set NCAT=%%~na

rmdir /Q /S ..\Archive
mkdir ..\Archive
xcopy /E ..\%NCAT% ..\Archive\

cd ..\Archive
call clr.bat
cd ..\%NCAT%

if not exist obj\Debug\%PRJNAME%.exe goto ex1

winrar a -m5 -EP1 %PRJNAME%_v%Version%.zip obj\Debug\%PRJNAME%.exe %PRJNAME%.txt

:ex1

winrar a -m5 -R -EP1 %PRJNAME%_dev%Version%.zip ..\Archive\

rmdir /Q /S ..\Archive\

set NCAT=
set PRJNAME=
set Version=