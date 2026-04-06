#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Immutable record that holds a human-readable <see cref="Name"/> identifying a pipeline part
/// in logs and diagnostics.
/// </summary>
public record class BCDescription(
    string Name
) {
    public BCDescription(
    ) : this(
        Name: string.Empty
    ) {
    }

    public static implicit operator BCDescription(string Name)
        => new(Name);

};