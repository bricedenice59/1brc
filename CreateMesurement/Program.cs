namespace CreateMeasurement;

internal static class Program
{
    private static readonly string Filename = "measurements.txt";

    private static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: create_measurements.sh <number of records to create> <dir>");
            Environment.Exit(1);
        }

        var size = 0;
        try
        {
            size = int.Parse(args[0]);
        }
        catch (FormatException)
        {
            Console.WriteLine("Invalid value for <number of records to create>");
            Console.WriteLine("Usage: CreateMeasurements <number of records to create>");
            Environment.Exit(1);
        }
        catch (OverflowException)
        {
            Console.WriteLine($"Value must be lower or equal than {int.MaxValue} for <number of records to create>");
            Environment.Exit(1);
        }
        
        if(!Directory.Exists(args[1]))
        {
            Console.WriteLine($"Directory {args[1]} does not exist!");
            Environment.Exit(1);
        }
        
        Console.WriteLine($"Creating measurement file {Filename} in {args[1]}");
        CreateMeasurement(size, args[1]);
    }

    private static void CreateMeasurement(int size, string filePathname)
    {
        var stations = Stations.GetAll();
        using var fos = File.Create(Path.Combine(filePathname, Filename));
        using var bw = new StreamWriter(fos);
        for (var i = 0; i < size; i++)
        {
            var station = stations[new Random().Next(stations.Count)];
            bw.Write(station.id);
            bw.Write(";" + station.Measurement());
            bw.WriteLine();
        }

        bw.Flush();

        Console.WriteLine("Created file with {0:N0} measurements ", size);
    }
}

public record WeatherStation(string id, double meanTemperature)
{
    public double Measurement()
    {
        var m = new Random().NextDouble() * 20 - 10 + meanTemperature;
        return Math.Round(m * 10.0) / 10.0;
    }
}