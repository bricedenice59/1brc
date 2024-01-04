namespace CalculateAverage;

public sealed record MeasurementAggregator(double Min, double Max, double Sum, int Count)
{
    public MeasurementAggregator(double initValue) :
        this(initValue, initValue, initValue, 1)
    {

    }
    
    public MeasurementAggregator Combine(MeasurementAggregator measurement) 
    {
        return new MeasurementAggregator(
            Math.Min(Min, measurement.Min),
            Math.Max(Max, measurement.Max),
            Sum + measurement.Sum,
            Count + measurement.Count
        );
    }

    public override string ToString()
    {
        return Round(Min) + "/" + Round((Sum) / Count) + "/" + Round(Max);
    }

    private double Round(double value) => Math.Round(value * 10.0) / 10.0;
}