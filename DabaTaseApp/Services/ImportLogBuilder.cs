using System.Text;

namespace DabaTaseApp.Services
{
    public sealed class ImportLogBuilder
    {
        private readonly List<string> _entries = [];

        public int SuccessCount { get; private set; }
        public int WarningCount { get; private set; }
        public int ErrorCount { get; private set; }
        public int FailureCount { get; private set; }
        public bool HasIssues => ErrorCount > 0 || WarningCount > 0 || FailureCount > 0;

        private void Add(string tag, string message)
            => _entries.Add($"[{DateTime.Now:HH:mm:ss}] [{tag}] {message}");

        public void AddSuccess(string message) { Add("УСПІХ", message); SuccessCount++; }
        public void AddWarning(string message) { Add("ПОПЕРЕДЖЕННЯ", message); WarningCount++; }
        public void AddError(string message) { Add("ПОМИЛКА", message); ErrorCount++; }
        public void AddFailure(string message) { Add("ЗБІЙ", message); FailureCount++; }

        public byte[] BuildBytes(string header, bool? savedToDb = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine(header);
            sb.AppendLine();
            foreach (var e in _entries)
                sb.AppendLine(e);
            sb.AppendLine();
            sb.AppendLine("ПІДСУМОК");
            sb.AppendLine($"Успішно збережено рядків: {SuccessCount}");
            sb.AppendLine($"Пропущено рядків через помилки: {ErrorCount}");
            sb.AppendLine($"Попереджень: {WarningCount}");
            sb.AppendLine($"Збоїв: {FailureCount}");
            if (savedToDb.HasValue)
                sb.AppendLine($"Збережено в базу даних: {(savedToDb.Value ? "так" : "ні")}");
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
        }
    }
}
