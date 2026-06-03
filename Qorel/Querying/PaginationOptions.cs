namespace Qorel.Querying;

internal static class PaginationDefaults
{
    public const int Default = 20;
    public const int Maximum = 100;
}

internal sealed class PaginationOptions
{
    public int Default { get; set; } = PaginationDefaults.Default;
    public int Maximum { get; set; } = PaginationDefaults.Maximum;
}
