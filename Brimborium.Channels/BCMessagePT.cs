#pragma warning disable IDE0047

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Brimborium.Channels;

/// <summary>
/// Represents a channel message with a parameter, carrying a result kind, an optional value, or an optional error.
/// </summary>
/// <typeparam name="P">The type of the parameter associated with the message.</typeparam>
/// <typeparam name="T">The type of the value carried by the message.</typeparam>
/// <param name="Parameter">The parameter associated with this message.</param>
/// <param name="Method">Indicates whether this is an <c>OnNext</c>, <c>OnError</c>, or <c>OnComplete</c> message.</param>
/// <param name="Value">The value carried by the message; only meaningful when <see cref="Method"/> is <see cref="BCResultKind.OnNext"/>.</param>
/// <param name="Error">The error carried by the message; only meaningful when <see cref="Method"/> is <see cref="BCResultKind.OnError"/>.</param>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly record struct BCMessage<P, T>(
    P Parameter,
    BCResultKind Method,
    T Value,
    BCError? Error
) {
    /// <summary>Creates a message that signals the next value.</summary>
    /// <param name="parameter">The parameter to associate with the message.</param>
    /// <param name="value">The value to carry.</param>
    public static BCMessage<P, T> OnNext(P parameter, T value) {
        return new BCMessage<P, T>(parameter, BCResultKind.OnNext, value, default);
    }

    /// <summary>Tries to retrieve the value when this is an <c>OnNext</c> message.</summary>
    /// <param name="value">The value, if the message kind is <see cref="BCResultKind.OnNext"/>.</param>
    /// <returns><c>true</c> if the message is <c>OnNext</c>; otherwise <c>false</c>.</returns>
    public readonly bool TryGetOnNext([MaybeNullWhen(false)] out T value) {
        value = this.Value;
        return (this.Method == BCResultKind.OnNext);
    }

    /// <summary>Creates a message that signals an error.</summary>
    /// <param name="parameter">The parameter to associate with the message.</param>
    /// <param name="error">The error to carry.</param>
    public static BCMessage<P, T> OnError(P parameter, BCError error) {
        return new BCMessage<P, T>(parameter, BCResultKind.OnError, default!, error);
    }

    /// <summary>Tries to retrieve the error when this is an <c>OnError</c> message.</summary>
    /// <param name="error">The error, if the message kind is <see cref="BCResultKind.OnError"/>.</param>
    /// <returns><c>true</c> if the message is <c>OnError</c>; otherwise <c>false</c>.</returns>
    public readonly bool TryGetOnError([MaybeNullWhen(false)] out BCError error) {
        error = this.Error!;
        return (this.Method == BCResultKind.OnError);
    }

    /// <summary>Creates a message that signals completion.</summary>
    /// <param name="parameter">The parameter to associate with the message.</param>
    public static BCMessage<P, T> OnComplete(P parameter) {
        return new BCMessage<P, T>(parameter, BCResultKind.OnComplete, default!, default);
    }

    /// <summary>Returns <c>true</c> if this message signals completion.</summary>
    public readonly bool TryGetOnComplete() {
        return (this.Method == BCResultKind.OnComplete);
    }

    private string GetDebuggerDisplay() {
        return $"{this.Method}";
    }
}
