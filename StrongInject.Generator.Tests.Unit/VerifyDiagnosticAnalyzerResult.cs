namespace StrongInject.Generator.Tests.Unit
{
    internal struct VerifyDiagnosticAnalyzerResult
    {
        public bool Success { get; private set; }

        public string ErrorMessage { get; private set; }

        public static VerifyDiagnosticAnalyzerResult Ok()
        {
            return new VerifyDiagnosticAnalyzerResult { Success = true };
        }

        public static VerifyDiagnosticAnalyzerResult Fail(string message)
        {
            return new VerifyDiagnosticAnalyzerResult { Success = false, ErrorMessage = message };
        }
    }
}