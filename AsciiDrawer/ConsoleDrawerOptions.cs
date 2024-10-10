namespace AsciiDrawer;

public struct ConsoleDrawerOptions
{
    public string readFrom;
    public string consoleWindowTitle;
    public char[] charMap;
    public (int x, int y) videoRatio;
    public bool runInExperimental;
    public bool drawWithoutColor;
    public double playbackSpeed;
    public bool runInDebug;
    public bool playWithSound;

    public static ConsoleDrawerOptions Default()
    {
        ConsoleDrawerOptions options = new()
        {
            readFrom = "0",
            consoleWindowTitle = "AsciiDrawer",
            charMap = ['.', '*', ':', '#', '@'],
            videoRatio = (1, 1),
            runInExperimental = false,
            drawWithoutColor = false,
            playbackSpeed = 1.0D,
            runInDebug = false,
            playWithSound = false
        };

        return options;
    }
}
