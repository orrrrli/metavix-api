using NpgsqlTypes;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace API.Logging;

internal static class LogTableColumns
{
    internal static IDictionary<string, ColumnWriterBase> Default => new Dictionary<string, ColumnWriterBase>
    {
        { "message",          new RenderedMessageColumnWriter() },
        { "message_template", new MessageTemplateColumnWriter() },
        { "level",            new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
        { "raise_date",       new TimestampColumnWriter() },
        { "exception",        new ExceptionColumnWriter() },
        { "properties",       new LogEventSerializedColumnWriter() },
        { "http_method",      new SinglePropertyColumnWriter("RequestMethod", PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) },
        { "endpoint",         new SinglePropertyColumnWriter("RequestPath",   PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) },
        { "correlation_id",   new SinglePropertyColumnWriter("CorrelationId", PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) },
        { "user_id",          new SinglePropertyColumnWriter("UserId",        PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) },
        { "role",             new SinglePropertyColumnWriter("Role",          PropertyWriteMethod.Raw, NpgsqlDbType.Varchar) },
    };
}
