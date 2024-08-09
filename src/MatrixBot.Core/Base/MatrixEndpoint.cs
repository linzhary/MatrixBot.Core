namespace MatrixBot.Core;

internal class MatrixEndpoint
{
    public string FunctionName { get; set; } = default!;
    public Type ServiceType { get; set; } = default!;
    public Func<Context?, Task> Function { get; set; } = default!;
    public List<IRuleMatcher> RuleMatchers { get; set; } = [];

    public async Task InvokeAsync(Context? context)
    {
        await Function.Invoke(context);
    }
}
