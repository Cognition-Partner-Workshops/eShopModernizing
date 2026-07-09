namespace Catalog.Service.Protos;

/// <summary>
/// Conversions between the protobuf <see cref="DecimalValue"/> message and the
/// CLR <see cref="decimal"/> type, following the Microsoft-recommended
/// units/nanos representation so money round-trips without precision loss.
/// </summary>
public partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000;

    public DecimalValue(long units, int nanos)
    {
        Units = units;
        Nanos = nanos;
    }

    public static implicit operator decimal(DecimalValue value) =>
        value.Units + value.Nanos / NanoFactor;

    public static implicit operator DecimalValue(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NanoFactor);
        return new DecimalValue(units, nanos);
    }
}
