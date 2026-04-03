namespace Brimborium.Channels;

/// <summary>
/// Discriminates the three possible kinds of a <see cref="BCMessage{T}"/> or <see cref="BCMessage{P,T}"/>:
/// a normal value (<c>OnNext</c>), an error (<c>OnError</c>), or end-of-stream (<c>OnComplete</c>).
/// </summary>
public enum BCResultKind { OnNext, OnError, OnComplete }
