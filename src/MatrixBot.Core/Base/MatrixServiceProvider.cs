using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace MatrixBot.Core;

public class MatrixServiceProvider
{
    public static readonly MatrixServiceProvider Instance = new();

    private List<Type> _serviceTypes = default!;
    private readonly ConcurrentDictionary<string, List<MatrixEndpoint>> _eventEndpoints = new();
    
    private MatrixServiceProvider() { }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="assembly"></param>
    public void AddServices(Assembly assembly)
    {
        _serviceTypes = assembly.GetExportedTypes()
           .Where(x => x.IsAssignableTo(typeof(MatrixService)))
           .ToList();

        foreach (var serviceType in _serviceTypes)
        {
            AddServiceEndpoints(serviceType);
        }
    }

    /// <summary>
    /// 注册服务终结点
    /// </summary>
    /// <param name="serviceType"></param>
    /// <returns></returns>
    public void AddServiceEndpoints(Type serviceType)
    {
        var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes()
                   .Where(x => x is IMatrixAction)
                   .ToList();

            if (attributes is { Count: 0 }) continue;

            var endpointTypes = attributes
                .Where(x => x is ITypeMatcher)
                .Cast<ITypeMatcher>()
                .Select(x => x.EventType)
                .Distinct()
                .ToList();
            var ruleMatchers = attributes
                .Where(x => x is IRuleMatcher)
                .Cast<IRuleMatcher>()
                .ToList();
            var parameter = method.GetParameters().FirstOrDefault();

            if (parameter is null)
            {
                Log.Warning("[serviceType][methodName]参数列表不正确", serviceType.FullName, method.Name);
                continue;
            }
            if (!parameter.ParameterType.IsAssignableTo(typeof(Context)))
            {
                Log.Warning("[serviceType][methodName]参数列表不正确", serviceType.FullName, method.Name);
                continue;
            }

            var instanceParameter = Expression.Parameter(typeof(MatrixService), "instance");
            var contextParameter = Expression.Parameter(typeof(Context), "ctx");
            var callExpression = Expression.Call(
                Expression.Convert(instanceParameter, serviceType),
                method,
                Expression.Convert(contextParameter, parameter.ParameterType));
            var function = Expression.Lambda<Func<MatrixService, Context?, Task>>(callExpression, instanceParameter, contextParameter).Compile();

            foreach (var endpointType in endpointTypes)
            {
                var endpoint = new MatrixEndpoint
                {
                    ServiceType = serviceType,
                    RuleMatchers = ruleMatchers,
                    Function = function,
                    FunctionName = $"[{serviceType.FullName}][{method.Name}]"
                };
                _eventEndpoints.AddOrUpdate(endpointType, [endpoint], (k, v) =>
                {
                    v.Add(endpoint);
                    return v;
                });
            }
        }
    }

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    public MatrixService? GetService(Type serviceType)
    {
        return Activator.CreateInstance(serviceType) as MatrixService;
    }

    /// <summary>
    /// 初始化应用
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    internal async Task OnReadyAsync(MatrixBotClient client, CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            foreach (var serviceType in _serviceTypes)
            {
                if (cancellationToken.IsCancellationRequested) return;
                try
                {
                    var service = Activator.CreateInstance(serviceType) as MatrixService;
                    if (service is null) continue;
                    await service.OnReadyAsync(client).ConfigureAwait(false);
                    Log.Information("初始化[{service}]成功", serviceType.FullName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "初始化[{service}]失败", serviceType.FullName);
                }
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 获取事件终结点
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    internal bool TryGetEndpoints(string eventType, [NotNullWhen(true)] out List<MatrixEndpoint>? endpoints)
    {
        return _eventEndpoints.TryGetValue(eventType, out endpoints) && endpoints != null;
    }


}
