# Terminal
A simple Unity console replacement with a command line.
![term_pic_1](https://github.com/user-attachments/assets/1ebccf07-ac77-4f92-8e0b-8fe479ed697d)

## Console Replacement
The console can output anything sent to `Application.logMessageReceived`. So regular Unity logs appear if you enable them.
- Compilation errors are preserved like the Console and can be double clicked.
- Stacktraces are supported with links (package errors untested)

## Commands
A command can be created by adding the `[Command]` attribute to any static method with a single `string[]` parameter.
```csharp
[Command("help")]
public static void Help(string[] args)
{
    Log.Print("------------------------");
    Log.Print("Welcome to the terminal!");
    Log.Print("------------------------");
    Log.Print("");
    Log.Print("You can trigger commands defined in c# with a [Command] attribute.");
    Log.Print("Commands must be static and have a string[] parameter.");
    Log.Print("");
    Log.Print("To view a list of all commands, enter 'commands'");
}
```

The `Log.Print` function is included to help style your command output nicely instead of using `Debug.Log`

## Settings
The terminal settings can be accessed by right clicking anywhere inside the window.

---

### TODO
- [ ] Light mode unsupported
- [ ] Package stacktraces untested
