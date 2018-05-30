# GDUT-Timetable
A timetable export tool for GDUT HEMC (Higher Education Mega Center) campus.

## Build
Install .Net Core 2.1 SDK, then run these commands in the command line.
```
$ cd Program
$ dotnet restore
$ dotnet build
```

## Run
```
$ dotnet run
```

Caution:
- The connection to the GDUT server is not encrypted, but this program does not record your password.
- You'll need to type in the captcha manually and the captcha image is downloaded to Program/captcha.png.
- 