using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FastConsole;

internal static class FConsole
{
    public static ConsoleColor ForegroundColor
    {
        get => _ForegroundColor;
        set
        {
            _ForegroundColor = value;
            Console.ForegroundColor = value;
        }
    }
    public static ConsoleColor BackgroundColor
    {
        get => _BackgroundColor;
        set
        {
            _BackgroundColor = value;
            Console.BackgroundColor = value;
        }
    }
    public static int BufferHeight => height;
    public static int BufferWidth => width;
    public const ConsoleColor DEFAULT_BACKGROUND_COLOR = ConsoleColor.Black;
    public const ConsoleColor DEFAULT_FOREGROUND_COLOR = ConsoleColor.White;

    private static ConsoleColor _ForegroundColor, _BackgroundColor;
    private static short Bottom => (short) (top + height - 1);
    private static short Right => (short) (left + width - 1);
    private static SafeFileHandle outputHandle = new();
    private static short width, height, left, top;
    private const short overlineBit = 0x0400;
    private static CharInfo[] buffer = [];

    /// <exception cref="ApplicationException">Invalid output handle</exception>
    [STAThread]
    public static void Initialize(string title, ConsoleColor foreground = DEFAULT_FOREGROUND_COLOR, ConsoleColor background = DEFAULT_BACKGROUND_COLOR)
    {
        Console.OutputEncoding = System.Text.Encoding.Unicode;
        Console.Title = title;

        Console.CursorVisible = false;

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);

        outputHandle = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

        if(outputHandle.IsInvalid)
        {
            throw new ApplicationException("CreateFile returned an invalid output handle.");
        }

        ResizeBuffer((short) Console.WindowWidth, (short) Console.WindowHeight);

        ForegroundColor = foreground;
        BackgroundColor = background;
        FillBuffer(' ', ForegroundColor, BackgroundColor);
    }

    public static void ResizeBuffer(short x, short y)
    {
        width = x;
        height = y;
        left = 0;
        top = 0;
        buffer = new CharInfo[width * height];
    }

    public static void SetBuffer(CharInfo[] _buffer, (short width, short height) bufferSize, bool draw = false)
    {
        buffer = _buffer;

        width = bufferSize.width;
        height = bufferSize.height;

        if(draw)
        {
            DrawBuffer();
        }
    }

    /// <exception cref="ArgumentException">x or y out of bounds of the buffer.</exception>
    public static void SetChar(short x, short y, char c, ConsoleColor foreground, ConsoleColor background)
    {
        int address = (width * y) + x;
        short colorset = Colorset(foreground, background);

        if(address < 0 || address >= buffer.Length)
        {
            throw new ArgumentException("Can't write to address (" + address + ") at (" + x + "," + y + ").");
        }

        buffer[address].Char = c;
        buffer[address].Attributes = (short) (colorset + Gridset(false, false, false));
    }

    public static void DrawBuffer(Coord drawCoords = default)
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleOutputW(
          SafeFileHandle hConsoleOutput,
          CharInfo[] lpBuffer,
          Coord dwBufferSize,
          Coord dwBufferCoord,
          ref Rectangle lpWriteRegion);

        Rectangle rect = new(left, top, Right, Bottom);
        _ = WriteConsoleOutputW(outputHandle, buffer, new Coord(width, height), drawCoords, ref rect);
    }

    public static void FillBuffer(char c, ConsoleColor foreground = DEFAULT_FOREGROUND_COLOR, ConsoleColor background = DEFAULT_BACKGROUND_COLOR)
    {
        for(int i = 0; i < buffer.Length; i++)
        {
            buffer[i].Attributes = Colorset(foreground, background);
            buffer[i].Char = c;
        }
    }
    
    public static PixelValue ReadChar(Coord coords)
    {
        short address = (short) ((width * coords.y) + coords.x);
        char character = (char) buffer[address].Char;
        short attributes = buffer[address].Attributes;
        attributes &= 0x0FF;
        ConsoleColor foreground = (ConsoleColor) (attributes & 0x0F);
        ConsoleColor background = (ConsoleColor) (attributes >> 4);
        return new(foreground, background, character);
    }

    public static short Colorset(ConsoleColor foreground, ConsoleColor background)
        => (short) (foreground + ((short) background << 4));

    public static short Gridset(bool overline, bool leftline, bool rightline)
    {
        const short leftlineBit = 0x0800;
        const short rightlineBit = 0x1000;

        return (short) (
            (overline ? overlineBit : 0) + (leftline ? leftlineBit : 0) + (rightline ? rightlineBit : 0)
        );
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Coord
    {
        public short x, y;
        public Coord(short x, short y)
        {
            this.x = x;
            this.y = y;
        }
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CharInfo
    {
        [FieldOffset(0)] public ushort Char;
        [FieldOffset(2)] public short Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rectangle
    {
        public short left, top, right, bottom;
        public Rectangle(short left, short top, short right, short bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
    }
}

internal readonly record struct PixelValue
{
    public readonly ConsoleColor foreground, background;
    public readonly char character;

    public override string ToString() => foreground.ToString() + character + background.ToString();

    public enum Density { background, sparse, medium, dense }

    private static char DensityChar(Density d)
        => d == Density.background ? ' ' :
        d == Density.sparse ? '░' :
        d == Density.medium ? '▒' :
        d == Density.dense ? '▓' :
        '?';
    public PixelValue(ConsoleColor foreground, ConsoleColor background, char character)
    {
        this.foreground = foreground;
        this.background = background;
        this.character = character;
    }
    public PixelValue(ConsoleColor foreground, ConsoleColor background, Density density)
        : this(foreground, background, DensityChar(density)) { }
    public PixelValue(ConsoleColor color) : this(color, color, DensityChar(Density.background)) { }
}