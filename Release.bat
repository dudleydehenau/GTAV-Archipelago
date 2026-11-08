@echo off
setlocal EnableExtensions
cd /d "%~dp0"

git fetch --tags -q 2>nul
for /f "delims=" %%t in ('git tag --list "v*" --sort=-v:refname 2^>nul ^| findstr /v "_mods"') do (
  set "LAST=%%t"
  goto found
)
:found
if defined LAST echo Last release: %LAST%

set "VERSION=%~1"
if "%VERSION%"=="" set /p "VERSION=New release version (for example 1.2.4): "
if "%VERSION%"=="" goto fail
if /i "%VERSION:~0,1%"=="v" set "VERSION=%VERSION:~1%"
set "MESSAGE=%~2"
if "%MESSAGE%"=="" set /p "MESSAGE=Commit message: "
if "%MESSAGE%"=="" set "MESSAGE=Release v%VERSION%"
set "MSGFILE=%TEMP%\gtav-archipelago-commit.txt"
>"%MSGFILE%" echo(%MESSAGE%
if not "%~2"=="" goto details_done
echo Details, one line at a time (press Enter on an empty line to finish):
set "FIRST=1"
:details
set "LINE="
set /p "LINE=> "
if not defined LINE goto details_done
if not defined FIRST goto add_line
>>"%MSGFILE%" echo.
set "FIRST="
:add_line
setlocal EnableDelayedExpansion
>>"%MSGFILE%" echo(!LINE!
endlocal
goto details
:details_done

git rev-parse -q --verify "refs/tags/v%VERSION%" >nul 2>&1 && (
  echo The tag v%VERSION% already exists. Pick another version.
  goto fail
)

echo.
echo [1/3] Commit
git add -A || goto fail
git diff --cached --quiet && echo Nothing new to commit.
git diff --cached --quiet || git commit -q -F "%MSGFILE%" || goto fail

echo [2/3] Push
git push || goto fail

echo [3/3] Tag v%VERSION%
git tag "v%VERSION%" || goto fail
git push origin "v%VERSION%" || goto fail

echo.
echo Done. GitHub builds and publishes v%VERSION% in a few minutes:
echo https://github.com/dudleydehenau/GTAV-Archipelago/actions
pause
exit /b 0

:fail
echo.
echo Stopped. Nothing after this step was done.
pause
exit /b 1
