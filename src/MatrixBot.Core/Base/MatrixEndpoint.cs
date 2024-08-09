namespace MatrixBot.Core;

internal class MatrixEndpoint
{
    public string FunctionName { get; set; } = default!;
    public Type ServiceType { get; set; } = default!;
    public Func<MatrixService, Context?, Task> Function { get; set; } = default!;
    public List<IRuleMatcher> RuleMatchers { get; set; } = [];

    public async Task InvokeAsync(Context? context)
    {
        var service = MatrixServiceProvider.Instance.GetService(ServiceType);
        if (service is not null)
        {
            await Function.Invoke(service, context);
        }
    }
}
