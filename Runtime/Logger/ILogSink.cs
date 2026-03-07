namespace Basic.Logger
{
    public interface ILogSink
    {
        void Emit(in LogEntry entry);
    }
}
