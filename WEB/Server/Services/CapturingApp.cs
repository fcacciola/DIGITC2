using ENGINE;

namespace DigitC2.Server.Services;

public sealed class CapturingApp : DriverApp
{
    private readonly List<string> _messages = [];
    private readonly List<string> _errors = [];

    public IReadOnlyList<string> Messages => _messages;
    public IReadOnlyList<string> Errors => _errors;

    //public override void AddMessage(string aMsg) => _messages.Add(aMsg);
    public override void AddMessage(string aMsg) {}

    public override void AddErrorMessage(string aMsg) => _errors.Add(aMsg);
}
