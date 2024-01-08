using System.Diagnostics;
using System.Text;

namespace CalculateAverage;

internal static class Program
{
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

        var mApp = new MeasurementApp(file);
        var result = mApp.Process();
        watch.Stop();
        
        mApp.Dispose();
        
        var sortedEntries = result.OrderBy(x => x.Key);
        
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
        
        Console.WriteLine(sb.ToString());
        
        Console.WriteLine(watch.ElapsedMilliseconds);
    }
}