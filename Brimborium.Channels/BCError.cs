#pragma warning disable IDE1006 // Naming Styles

using System.Runtime.ExceptionServices;

namespace Brimborium.Channels;

public sealed record class BCError(
    Exception Error,
    ExceptionDispatchInfo DispatchInfo,
    object? Value) {
    public BCError(
        Exception Error,
        object? Value = default
        )
        : this(
              Error,
              ExceptionDispatchInfo.Capture(Error),
              Value) { }
    private bool _IsHandled;
    public bool IsHandled => this._IsHandled;
    public void SetIsHandled() { this._IsHandled = true; }

    private bool _IsLogged;
    public bool IsLogged => this._IsLogged;
    public void SetLogged() { this._IsLogged = true; }

    public void ThrowIfNotHandled() {
        if (this._IsHandled) {

        } else {
            this.DispatchInfo.Throw();
        }
    }
};
