### Prerequisites
- .NET framwork, or SDK equivalent. (dotnet tool)

## How to use.

Create a self-contained executable.
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

1. Make sure "NAP Locked down browser.exe" is accessible (in PATH or same directory)
3. The program will automatically launch NAP and convert it to windowed mode

## Technical details

Uses Windows API:
- `FindWindow()` - locate window by title
- `SetWindowLong()` - modify window style 
- `ShowWindow()` - display window properly
- `SetWindowPos()` - force window redraw

## Troubleshooting

- "NAP Locked down browser.exe not found": Ensure the executable is in your PATH or current directory
- "Window not found": The window title might be different - check Task Manager for exact title
- Permission issues: Run as administrator if needed

***

## Copy patched DLLs for easy access to built in windows features.

[Additional DLLs](https://github.com/shepfishy/NapWindower/tree/main/Additional%20DLLs)

Run copy.bat with administrator privelages.

**What it do**
- Checks, then kills JanisonReplayService.exe's service 'NAPLDBService'.
- Copies patched DLLs to NAP Locked Down Browser's root directory.
- Initiates a restart to ensure the 'NAPLDBService' restarts properly.
