using AsciiDrawer;

internal class Program
{
    private async static Task Main(string[] args)
    {
        if(args.Length == 0)
        {
            PrintUsage();
            return;
        }

        ConsoleDrawerOptions options = ConsoleDrawerOptions.Default();

        for(int i = 0; i < args.Length; i++)
        {
            switch(args[i])
            {
                case "-d":
                {
                    options.runInDebug = true;
                    break;
                }

                case "-nc":
                {
                    options.drawWithoutColor = true;
                    break;
                }

                case "-c":
                {
                    if(i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("No value passed to parameter [-c].");
                        PrintUsage();
                        return;
                    }

                    string charsString = args[i + 1];
                    options.charMap = charsString.ToCharArray();
                    break;
                }

                case "-i":
                {
                    if(i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("No value passed to parameter [-i].");
                        PrintUsage();
                        return;
                    }

                    string readType = args[i + 1];

                    if(int.TryParse(readType, out _) == false)
                    {
                        if(Path.IsPathRooted(readType) == false)
                        {
                            readType = Path.Combine(Directory.GetCurrentDirectory(), readType);
                        }

                        if(File.Exists(readType) == false)
                        {
                            Console.Error.WriteLine($"File '{readType}', doesn't exist.");
                            PrintUsage();
                            return;
                        }
                    }

                    // TODO
                    options.readFrom = readType;
                    i++;
                    break;
                }

                case "-s":
                {
                    if(i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("No value passed to parameter [-s].");
                        PrintUsage();
                        return;
                    }

                    if(double.TryParse(args[i + 1], out double speed) == false)
                    {
                        Console.Error.WriteLine($"Value '{args[i + 1]}' passed to parameter [-s] is not a number.");
                        PrintUsage();
                        return;
                    }

                    if(speed <= 0)
                    {
                        Console.Error.WriteLine($"Value '{args[i + 1]}' passed to parameter [-s] can't be 0 or negative.");
                        PrintUsage();
                        return;
                    }

                    options.playbackSpeed = speed;
                    i++;
                    break;
                }

                case "-r":
                {
                    if(i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("No value passed to parameter [-r].");
                        PrintUsage();
                        return;
                    }

                    string given = args[i + 1];
                    string[] ratioArr = given.Split(':');

                    if(ratioArr.Length != 2)
                    {
                        Console.Error.WriteLine("Invalid value passed to parameter [-r]. (ex. 16:9).");
                        PrintUsage();
                        return;
                    }

                    if(int.TryParse(ratioArr[0], out int r1) == false)
                    {
                        Console.Error.WriteLine("Invalid value passed to parameter [-r]. (ex. 16:9).");
                        PrintUsage();
                        return;
                    }

                    if(int.TryParse(ratioArr[1], out int r2) == false)
                    {
                        Console.Error.WriteLine("Invalid value passed to parameter [-r]. (ex. 16:9).");
                        PrintUsage();
                        return;
                    }

                    _ = (r1, r2);
                    i++;
                    break;
                }
            }
        }

        using ConsoleDrawer drawer = new(options);

        ConsoleKeyInfo key = Console.ReadKey(true);

        while(key.Key != ConsoleKey.C)
        {
            using(Task displayedTask = drawer.DisplayAsync())
            {
                await displayedTask;
            }

            key = Console.ReadKey(true);
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("""
        Usage (in any order):
        [-i value] [-c value] [-s value] [-r value] [-d] [-x] [-nc]

        -i :    full/path/to/file or a number (id of webcam)
        -c :    string of characters used to render the gray-scaled pixel value (from lightest to darkest) (default:".-:#$@").
        -nc :   draw without color.
        -s :    speed for video playback (double).
        -r :    (not implemented) ratio of the output video (int : int, ex. 16:9).
        -d :    run in debug (logs errors).
        -x :    (not implemented) run in experimental mode (can improve the performance at the cost of possible crashes.).
        """);
        return;
    }
}
