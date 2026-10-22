namespace Qorel;

/// <summary>
/// What an operator takes as its value.
/// </summary>
internal enum Operand
{
    /// <summary>No value, e.g. <c>IsNull</c>.</summary>
    None,

    /// <summary>One value, e.g. <c>Equals</c>.</summary>
    Single,

    /// <summary>A list of values, e.g. <c>IsOneOf</c>.</summary>
    List,
}
