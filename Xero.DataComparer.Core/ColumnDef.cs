namespace Xero.DataComparer.Core;

/// <summary>
/// Describes a single column in a dynamic row schema.
/// Used to generate a CLR type at runtime via <see cref="DynamicTypeBuilder"/>.
/// </summary>
/// <param name="Name">Property name — must be a valid C# identifier.</param>
/// <param name="Type">
/// CLR type hint: <c>"string"</c> (default), <c>"decimal"</c>, <c>"int"</c>, <c>"bool"</c>.
/// Any unrecognised value falls back to <c>string</c>.
/// </param>
public sealed record ColumnDef(string Name, string Type = "string");
