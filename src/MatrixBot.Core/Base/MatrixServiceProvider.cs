using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace MatrixBot.Core;

/// <summary>
/// 服务容器
/// </summary>
public class MatrixServiceProvider
{
    private readonly ConcurrentDictionary<string, List<MatrixEndpoint>> _serviceEndpoints = [];
    private readonly IServiceProvider _serviceProvider = default!;
    internal MatrixServiceProvider(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(this);
        _serviceProvider = serviceCollection.BuildServiceProvider();
        var serviceDescriptors = serviceCollection.Where(sd => sd.ServiceType != typeof(IServiceProvider)).ToList();
        foreach (var serviceDescriptor in serviceDescriptors)
        {
            var serviceInstance = _serviceProvider.GetRequiredService(serviceDescriptor.ServiceType);
            if (serviceInstance is MatrixService)
            {
                AddMatrixEndpoints(serviceDescriptor.ServiceType, serviceInstance);
            }
            SetServiceDependencies(serviceDescriptor.ServiceType, serviceInstance);
        }
    }

    /// <summary>
    /// 设置服务之间的依赖关系
    /// </summary>
    private void SetServiceDependencies(Type type, object instance)
    {
        foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var fromService = propertyInfo.GetCustomAttribute<FromServiceAttribute>();
            if (fromService != default)
            {
                var propertyInstance = _serviceProvider.GetRequiredService(propertyInfo.PropertyType);
                propertyInfo.SetValue(instance, propertyInstance);
            }
        }
    }

    /// <summary>
    /// 注册Matrix服务终结点
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private void AddMatrixEndpoints(Type type, object instance)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
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
                Log.Warning("[serviceType][methodName]参数列表不正确", type.FullName, method.Name);
                continue;
            }
            if (!parameter.ParameterType.IsAssignableTo(typeof(Context)))
            {
                Log.Warning("[serviceType][methodName]参数列表不正确", type.FullName, method.Name);
                continue;
            }

            var instanceParameter = Expression.Constant(instance);
            var contextParameter = Expression.Parameter(typeof(Context), "ctx");
            var callExpression = Expression.Call(
                Expression.Convert(instanceParameter, type),
                method,
                Expression.Convert(contextParameter, parameter.ParameterType));
            var function = Expression.Lambda<Func<Context?, Task>>(callExpression, contextParameter).Compile();

            foreach (var endpointType in endpointTypes)
            {
                var endpoint = new MatrixEndpoint
                {
                    RuleMatchers = ruleMatchers,
                    Function = function,
                    FunctionName = $"[{type.FullName}][{method.Name}]"
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
    internal T GetRequriedService<T>() where T : class => _serviceProvider.GetRequiredService<T>();

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    internal T? GetService<T>(Type type) where T : class
    {
        if (!type.IsAssignableTo(typeof(T))) return null;
        return _serviceProvider.GetRequiredService(type) as T;
    }

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    internal IEnumerable<T> GetServices<T>() where T : class
    {
        return _serviceProvider.GetServices<T>();
    }
}
