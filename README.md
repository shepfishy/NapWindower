# DISCLAIMER: This is for EDUCATIONAL PURPOSES ONLY. Do not use this to violate academic integrity.
___

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