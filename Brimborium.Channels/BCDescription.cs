#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public record class BCDescription(
    string Name
) {
    public BCDescription(
    ) : this(
        Name: string.Empty
    ) {
    }
};