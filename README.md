# TScript programming language extension
***The*** definitive TScript Programming Language VSCode Extension.

## Prerequisites
You have to have dotnet 8.0 installed in order to run the language serverr.

## Usage
To get completions for variables with a FIXED type you have to specify it's type in a special way.
Here is a short example for having a variable have the `canvas.Bitmap` type:
```tscript
#type=canvas.Bitmap
var bitmap = canvas.Bitmap(".......");

bitmap. # <-- completions would appear after you type the dot (".")
```

## Features
- Syntax highlighting
- Full completion support

## WIP Features
- Cross file completions (aka having a function in one file and still recognizing it's existence in another one)
- Standard library integration
- `use namespace <name>` actually working lol
- Derived classes contributing to the completions
- Respecting public and private methods

## Extension Settings
- **Enable Debug Log**: Enables logging debugging information useful for debugging the language server (disabled by default)
- **Log file location**: Set a custom path for the log file (default location is right next to the server binary)

## Known Issues
Dont do too long lines it breaks the entire tokenization system qwq

## Release Notes
### 1.0.0
- I released this abomination lol

## Most importantly...
**H A V E   F U N**
