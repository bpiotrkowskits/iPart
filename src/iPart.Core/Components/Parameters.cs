namespace iPart.Core.Components;

public sealed class ResistorParameters : IComponentDependent
{
    public Guid ComponentId { get; set; }
    [DecimalSpec(28, 12)] public decimal? ResistanceOhms { get; set; }
    [DecimalSpec(18, 6)] public decimal? TolerancePercent { get; set; }
    [DecimalSpec(18, 9)] public decimal? PowerWatts { get; set; }
    [DecimalSpec(18, 6, AllowNegative = true)] public decimal? TemperatureCoefficientPpmPerC { get; set; }
}
public sealed class CapacitorParameters : IComponentDependent
{
    public Guid ComponentId { get; set; }
    [DecimalSpec(28, 9)] public decimal? CapacitancePf { get; set; }
    [DecimalSpec(18, 6)] public decimal? RatedVoltageV { get; set; }
    [DecimalSpec(18, 6)] public decimal? TolerancePercent { get; set; }
    [DecimalSpec(28, 9)] public decimal? AbsoluteTolerancePf { get; set; }
    public string? Dielectric { get; set; }
    [DecimalSpec(28, 9)] public decimal? EsrMilliOhms { get; set; }
    public bool? LowEsr { get; set; }
    public int? LifetimeHours { get; set; }
}
public sealed class InductorParameters : IComponentDependent
{
    public Guid ComponentId { get; set; }
    [DecimalSpec(28, 9)] public decimal? InductanceMicroHenries { get; set; }
    [DecimalSpec(18, 6)] public decimal? TolerancePercent { get; set; }
    [DecimalSpec(18, 9)] public decimal? RatedCurrentA { get; set; }
    [DecimalSpec(28, 12)] public decimal? DcrOhms { get; set; }
    public string? Core { get; set; }
    public string? Type { get; set; }
}
public sealed class LedParameters : IComponentDependent
{
    public Guid ComponentId { get; set; }
    public string? Color { get; set; }
    [DecimalSpec(28, 9)] public decimal? LuminousIntensityCd { get; set; }
    [DecimalSpec(28, 9)] public decimal? LuminousFluxLm { get; set; }
    [DecimalSpec(18, 9)] public decimal? ForwardCurrentA { get; set; }
    [DecimalSpec(18, 6)] public decimal? ForwardVoltageV { get; set; }
    [DecimalSpec(18, 6)] public decimal? DominantWavelengthNm { get; set; }
}
public sealed class ConnectorParameters : IComponentDependent
{
    public Guid ComponentId { get; set; }
    public string? Type { get; set; }
    public int? PositionCount { get; set; }
    [DecimalSpec(18, 9)] public decimal? PitchMm { get; set; }
    public int? RowCount { get; set; }
    public string? CompatiblePlug { get; set; }
}
public sealed class CrystalParameters : IComponentDependent
{
    public Guid ComponentId { get; set; }
    [DecimalSpec(28, 6)] public decimal? FrequencyHz { get; set; }
    [DecimalSpec(28, 9)] public decimal? LoadCapacitancePf { get; set; }
}
public sealed class ComponentPads : IComponentDependent
{
    public Guid ComponentId { get; set; }
    public int? RC { get; set; }
    public int? TT { get; set; }
    public int? DL { get; set; }
    public int? QL { get; set; }
    public int? BGA { get; set; }
    public int? TP { get; set; }
    public int? THT { get; set; }
    public int? PreTHT { get; set; }
    public int? PressFIT { get; set; }
    public int? MECH { get; set; }
    public bool? BTC { get; set; }
    public bool? NS { get; set; }
}
