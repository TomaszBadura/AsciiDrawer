using FastConsole;
using System.Diagnostics;
using OpenCvSharp;

namespace AsciiDrawer;

public class ConsoleDrawer : IDisposable
{
    private readonly ConsoleDrawerOptions options;
    private readonly Mat image = new();
    private readonly VideoCapture? vc;

    private bool isDisposed = false;
    private readonly bool isImg = false;
    public ConsoleDrawer(ConsoleDrawerOptions options)
    {
        this.options = options;

        // TODO: deal better with given file input

        try
        {
            if(int.TryParse(options.readFrom, out int deviceId))
            {
                List<int> connected = GetAllConnectedCameras();
                if(connected.Contains(deviceId) == false)
                {
                    Console.Error.WriteLine($"No camera device with id {deviceId}");
                    Console.WriteLine("Available camera ids:");
                    Console.WriteLine(string.Join(", ", connected));
                    Console.WriteLine("if you don't see your device try increasing the DeviceLookupCount");
                }

                vc = new(deviceId);
                return;
            }

            switch(Path.GetExtension(options.readFrom).ToLower())
            {
                // tested :)
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".webp":
                // uncharted territory :(
                case ".svg":
                case ".jfif":
                case ".heif":
                case ".tiff":
                case ".tif":
                case ".jxl":
                {
                    isImg = true;
                    vc = new VideoCapture(options.readFrom, VideoCaptureAPIs.IMAGES);
                    break;
                }

                default:
                {
                    isImg = false;
                    vc = new VideoCapture(options.readFrom);
                    break;
                }
            }
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("Something went wrong...");
            if(options.runInDebug)
            {
                Console.Error.WriteLine(e.Message);
            }

            Dispose();
        }
    }

    public async Task DisplayAsync()
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentNullException.ThrowIfNull(vc);

        FConsole.Initialize("AsciiDrawer", FConsole.DEFAULT_FOREGROUND_COLOR, FConsole.DEFAULT_BACKGROUND_COLOR); // TODO give a way to change this especially when drawWithoutColor is true

        double fps = vc.Get(VideoCaptureProperties.Fps);

        double targetDelay = 1000 / (fps * options.playbackSpeed);

        // TODO Play from video on the internet (youtube / twitch)
        // TODO Play sound
        // TODO experimental mode (resize the whole video to console window size and terminate when window size changed (give a cli like experience for choosing options))
        // TODO not assume the image is size of terminal buffer, aka implement the ratio
        // TODO add a way to stop the video

        if(options.playWithSound)
        {
        }

        Stopwatch sw = new();
        Console.WriteLine("Press any key to play");
        Console.ReadKey(true);
        bool isOpened = true;
        while(isOpened)
        {
            if(isDisposed)
            {
                break;
            }

            sw.Restart();
            isOpened = vc.IsOpened();

            if(vc.Read(image) == false)
            {
                break; // If no more frames
            }

            Cv2.Resize(image, image, new Size(Console.BufferWidth, Console.BufferHeight));
            FConsole.CharInfo[] buffer = await GetBufferFromImageAsync(options.charMap, image, options.drawWithoutColor);
            FConsole.SetBuffer(buffer, ((short) image.Width, (short) image.Height), true);
            sw.Stop();
            double delay = Math.Max(0, targetDelay - sw.ElapsedMilliseconds);
            Debug.WriteLineIf(delay == 0, "drop!");
            if(isImg == true)
            {
                vc.PosFrames--;
                continue;
            }

            await Task.Delay((int) delay);
        }
    }

    // TODO a way to plug what colors to use
    private static async Task<FConsole.CharInfo[]> GetBufferFromImageAsync(char[] chars, Mat image, bool useColor = false)
    {
        FConsole.CharInfo[] buffer = new FConsole.CharInfo[image.Width * image.Height];
        int width = image.Width;
        int height = image.Height;

        void ProcessRow(int j)
        {
            int address = j * width;
            for(short i = 0; i < width; i++)
            {
                Vec3b pixel = image.At<Vec3b>(j, i);

                int grayValue = (pixel.Item2 + pixel.Item1 + pixel.Item0) / 3;
                char asciiChar = chars[grayValue * chars.Length / 256];
                short colorset;

                if(!useColor)
                {
                    colorset = FConsole.Colorset(GetConsoleColor(pixel, grayValue), FConsole.DEFAULT_BACKGROUND_COLOR);
                }
                else
                {
                    colorset = 15; // Colorset(GetConsoleColor(pixel, grayValue), FConsole.DEFAULT_BACKGROUND_COLOR);
                }

                buffer[address] = new FConsole.CharInfo { Char = asciiChar, Attributes = colorset };
                address++;
            }
        }

        Task[] tasks = new Task[height];
        for(short j = 0; j < height; j++)
        {
            int row = j;
            tasks[row] = Task.Run(() => ProcessRow(row));
        }

        await Task.WhenAll(tasks);

        return buffer;
    }

    private static ConsoleColor GetConsoleColor(Vec3b pixel, int grayValue = -1)
    {
        // TODO: fix the colors being all janky

        byte r = pixel.Item2;
        byte g = pixel.Item1;
        byte b = pixel.Item0;

        if(r > b && g > b && g > 128 && r > 128)
        {
            return r + g > 256 + 128 ? ConsoleColor.Yellow : ConsoleColor.DarkYellow;
        }
        else if(r > b && b > g && b > 128 && r > 128)
        {
            return r + b > 256 + 128 ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta;
        }
        else if(g > r && b > g && b > 128 && g > 128)
        {
            return g + b > 256 + 128 ? ConsoleColor.Cyan : ConsoleColor.DarkCyan;
        }
        else if(g > r && g > b)
        {
            return g > 220 ? ConsoleColor.Green : ConsoleColor.DarkGreen;
        }
        else if(b > r && b > g)
        {
            return b > 220 ? ConsoleColor.Blue : ConsoleColor.DarkBlue;
        }
        else if(r > g && r > b)
        {
            return r > 220 ? ConsoleColor.Red : ConsoleColor.DarkRed;
        }
        else
        {
            if(grayValue < 0)
            {
                grayValue = (byte) ((r + g + b) / 3);
            }

            return grayValue < 32 ? ConsoleColor.Black
                : grayValue < 64 ? ConsoleColor.DarkGray
                : grayValue < 128 ? ConsoleColor.Gray
                : ConsoleColor.White;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        image.Release();
        vc?.Release();
        isDisposed = true;
        Console.Clear();
        Console.WriteLine("disposed");
    }

    private static List<int> GetAllConnectedCameras()
    {
        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.Black;
        List<int> cameraIds = [];
        for(int i = 0; i < 10; i++)
        {
            VideoCapture x = VideoCapture.FromCamera(i);
            if(x.IsOpened() == false)
            {
                x.Release();
                continue;
            }

            cameraIds.Add(i);
            x.Release();
        }
        Console.ResetColor();
        return cameraIds;
    }
}
