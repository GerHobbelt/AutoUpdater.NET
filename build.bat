set BuildFolder=%1
set Logger=%2
set ZipExtractorProj="%BuildFolder%\ZipExtractor\ZipExtractor.csproj"
set AutoUpdaterProj="%BuildFolder%\AutoUpdater.NET\AutoUpdater.NET.csproj"
set UnitTestsProj="%BuildFolder%\UnitTests\UnitTests.csproj"

set list=
set list=%list%;Release
set list=%list%;Release-NET35
set list=%list%;Release-NET40
set list=%list%;Release-NET452
set list=%list%;Release-NET462

for %%c in (%list%) do (
  msbuild %ZipExtractorProj% /p:Configuration=%%c /verbosity:minimal /logger:%Logger% || EXIT 1
  msbuild %AutoUpdaterProj% /p:Configuration=%%c /verbosity:minimal /logger:%Logger% || EXIT 1
  msbuild %UnitTestsProj% /p:Configuration=%%c /verbosity:minimal /logger:%Logger% || EXIT 1
)