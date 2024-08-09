using Serilog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace MatrixBot.Core;

/// <summary>
/// 服务提供器
/// </summary>
public class MatrixServiceProvider
{
    private readonly ConcurrentDictionary<string, List<MatrixEndpoint>> _serviceEndpoints = [];
    private readonly ConcurrentDictionary<Type, object> _serviceInstances = [];
    internal MatrixServiceProvider(List<Type> serviceTypes, List<object> defaultServices)
    {
        _serviceInstances.TryAdd(typeof(MatrixServiceProvider), this);
        foreach (var defaultService in defaultServices)
        {
            _serviceInstances.TryAdd(defaultService.GetType(), defaultService);
        }
        foreach (var serviceType in serviceTypes)
        {
            var serviceInstance = _serviceInstances.GetOrAdd(serviceType, Activator.CreateInstance(serviceType)!);
            if (serviceType.IsAssignableTo(typeof(MatrixService)))
            {
                AddMatrixEndpoints(serviceType, serviceInstance);
            }
        }
        SetServiceDependencies();
    }

    /// <summary>
    /// 设置服务之间的依赖关系
    /// </summary>
    protected void SetServiceDependencies()
    {
        foreach (var serviceInstance in _serviceInstances)
        {
            foreach (var propertyInfo in serviceInstance.Key.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var fromService = propertyInfo.GetCustomAttribute<FromServiceAttribute>();
                if (fromService != default)
                {
                    if (_serviceInstances.TryGetValue(propertyInfo.PropertyType, out var propertyInstance))
                    {
                        propertyInfo.SetValue(serviceInstance.Value, propertyInstance);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 注册Matrix服务终结点
    /// </summary>
    /// <param name="serviceType"></param>
    /// <returns></returns>
    protected void AddMatrixEndpoints(Type serviceType, object serviceInstance)
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

            var instanceParameter = Expression.Constant(serviceInstance);
            var contextParameter = Expression.Parameter(typeof(Context), "ctx");
            var callExpression = Expression.Call(
                Expression.Convert(instanceParameter, serviceType),
                method,
                Expression.Convert(contextParameter, parameter.ParameterType));
            var function = Expression.Lambda<Func<Context?, Task>>(callExpression, contextParameter).Compile();

            foreach (var endpointType in endpointTypes)
            {
                var endpoint = new MatrixEndpoint
                {
                    RuleMatchers = ruleMatchers,
                    Function = function,
                    FunctionName = $"[{serviceType.FullName}][{method.Name}]"
                };
                _serviceEndpoints.AddOrUpdate(endpointType, [endpoint], (k, v) =>
                {
                    v.Add(endpoint);
                    return v;
                });
            }
        }
    }

    /// <summary>
    /// 获取Matrix服务终结点
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    internal bool TryGetEndpoints(string eventType, [NotNullWhen(true)] out List<MatrixEndpoint>? endpoints)
    {
        return _serviceEndpoints.TryGetValue(eventType, out endpoints) && endpoints != null;
    }

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    internal MatrixService? GetMatrixService(Type serviceType)
    {
        if (!serviceType.IsAssignableTo(typeof(MatrixService))) return null;
        return _serviceInstances.GetValueOrDefault(serviceType) as MatrixService;
    }

    /// <summary>
    /// 获取Matrix服务列表
    /// </summary>
    /// <returns></returns>
    internal List<MatrixService> GetMatrixServices()
    {
        return _serviceInstances.Values.Where(x => x is MatrixService).Cast<MatrixService>().ToList();
    }

}
