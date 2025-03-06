using System.Diagnostics;
using Sharpie;
using Sharpie.Backend;

bool CheckForDotnet(out string errorOutput)
{
    ProcessStartInfo startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "--version",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using (Process process = new Process())
    {
        process.StartInfo = startInfo;
        process.Start();

        errorOutput = process.StandardError.ReadToEnd();
        process?.WaitForExit();
        return process?.ExitCode == 0 ? true : false;
    }
}

try
{
    string test = string.Is
    string errorOutput = string.Empty;
    if (!CheckForDotnet(out errorOutput))
    {
        Console.WriteLine("dotnet command was not found on PATH.");
        Console.WriteLine($"Error: {errorOutput}");
        return; 
    }

    var backend = CursesBackend.Load();
    var options = new TerminalOptions(UseStandardKeySequenceResolvers: true, AllocateFooter: true);
    using var terminal = new Terminal(backend, options);

    // Set the main screen attributes for text and drawings.
    // terminal.Screen.ColorMixture = terminal.Colors.MixColors(StandardColor.Green, StandardColor.Blue);

    // Create a child window within the terminal to operate within.
    // The other cells contain the border so we don't want to overwrite those.
    var subWindow = terminal.Screen.Window(new(1, 1, terminal.Screen.Size.Width / 2, terminal.Screen.Size.Height - 2));

    // Force a refresh so that all drawings will be actually pushed to teh screen.
    using (terminal.AtomicRefresh())
    {
        terminal.Screen.Refresh();
        subWindow.Refresh();
    }

    subWindow.WriteText($"Using {terminal.CursesVersion} on {terminal.Name}\n");

    // Process all events coming from the terminal.
    foreach (var @event in terminal.Events.Listen(subWindow))
    {
        // Write the  event that occured.
        subWindow.WriteText($"{@event}\n");

        // If the event is a resize, change the size of the child window
        // to allow for the screen to maintain its border.
        // And then redraw the border of the main screen.
        if (@event is TerminalResizeEvent re)
        {
            subWindow.Size = new(re.Size.Width - 2, re.Size.Height - 2);
            terminal.Screen.DrawBorder();

            using (terminal.AtomicRefresh())
            {
                terminal.Screen.MarkDirty();
                terminal.Screen.Refresh();

                subWindow.MarkDirty();
                subWindow.Refresh();
            }
        }

        // If the user pressed CTRL+C, break the loop.
        if (@event is KeyEvent { Key: Key.Character, Char.IsAscii: true, Char.Value: 'C', Modifiers: ModifierKey.Ctrl })
        {
            break;
        }
    }
}
catch (CursesInitializationException)
{
    Console.WriteLine("Sorry, no compatible Curses backend found.");
}

