namespace Brimborium.Channels {
    [Serializable]
    public class BCInvalidStateException : Exception {
        public BCInvalidStateException() {
        }

        public BCInvalidStateException(string? message) : base(message) {
        }

        public BCInvalidStateException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
}