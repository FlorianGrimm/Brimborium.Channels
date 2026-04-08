namespace Brimborium.Channels;

public class BCMonitorToLogItem : BCMonitorBase {
    private readonly List<BCLogItem> _ListLogItem = new();

    public BCMonitorToLogItem() {
    }

    public List<BCLogItem> GetListLogItem() {
        lock (this._ListLogItem) {
            return new List<BCLogItem>(this._ListLogItem);
        }
    }

    public override void Write(in BCLogItem logItem) {
        lock (this._ListLogItem) {
            this._ListLogItem.Add(logItem);
        }
    }
}
