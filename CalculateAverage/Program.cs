
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace CalculateAverage;

internal static class Program
{
    private const char Separator = ';';
    
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: CalculateAverage <path to measurement.txt file>");
            Environment.Exit(1);
        }

        var file = args[0];
        if (!File.Exists(file))
            throw new FileNotFoundException($"File {file} does not exit");
        
        Stopwatch watch = new Stopwatch();
        watch.Start();

        var output = Process(file);
        
        watch.Stop();
        
        Console.WriteLine(watch.ElapsedMilliseconds);
        Console.WriteLine(output);
    }

    private static string Process(string file)
    {
        ConcurrentDictionary<string, MeasurementAggregator> measurementsDic = new();
        
        var processLines = Task.Factory.StartNew(() =>
        {
            Parallel.ForEach(File.ReadLines(file), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount},line =>
            {
                var lineSpan = line.AsSpan();
                var indexSeparator = lineSpan.IndexOf(Separator);
                var stationName = lineSpan.Slice(0, indexSeparator).ToString();
                var value = double.Parse(lineSpan.Slice(indexSeparator + 1, lineSpan.Length - (indexSeparator + 1)));

                if (measurementsDic.TryAdd(stationName, new MeasurementAggregator(value))) return;
                while(measurementsDic.TryGetValue(stationName, out MeasurementAggregator curValue))
                {
                    if(measurementsDic.TryUpdate(stationName, curValue.Combine(new MeasurementAggregator(value)), curValue))
                        break;
                }
                //Console.WriteLine($"Station: {stationName.ToString()}, measurement: {value.ToString(CultureInfo.InvariantCulture)}");
            }); 
        });

        processLines.Wait();

        var sortedEntries = measurementsDic.OrderBy(x => x.Key);
        
        var sb = new StringBuilder();
        sb.Append("{");
        foreach (var sortedEntry in sortedEntries)
        {
            sb.Append(sortedEntry.Key);
            sb.Append("=");
            sb.Append(sortedEntry.Value);
            sb.Append(", ");
        }
        sb.Append("}");
        return sb.ToString();
    }
}