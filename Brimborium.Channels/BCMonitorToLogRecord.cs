namespace Brimborium.Channels;

public class BCMonitorToLogRecord : BCMonitorBase {
    private List<BCLogRecord> _ListLogRecord = new(1024);

    public BCMonitorToLogRecord() {
    }

    public List<BCLogRecord> GetListLogItem() {
        lock (this._ListLogRecord) {
            return new List<BCLogRecord>(this._ListLogRecord);
        }
    }

    public List<BCLogRecord> GetAndClearListLogItem() {
        lock (this._ListLogRecord) {
            var result = this._ListLogRecord;
            this._ListLogRecord = new(1024);
            return result;
        }
    }

    public override void Write(in BCLogItem logItem) {
        if (logItem.Part is IBCMonitored monitored) {
            var nodeId = monitored.GetNodeId();
            if (nodeId is { Length: > 0 }) {
                lock (this._ListLogRecord) {
                    this._ListLogRecord.Add(
                        new(logItem.Timestamp, nodeId, logItem.Name, logItem.Kind));
                }
            }
        }
    }
}