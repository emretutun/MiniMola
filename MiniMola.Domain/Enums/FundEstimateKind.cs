namespace MiniMola.Domain.Enums;

public enum FundEstimateKind
{
    // Old rows were overwritten throughout the day: never call them closing estimates.
    Legacy = 0,
    Intraday = 1,
    Closing = 2
}
