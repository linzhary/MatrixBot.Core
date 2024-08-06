using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixBot.Core;

public class MatrixAppService
{
    public string ServiceName { get; set; } = default!;
    public Delegate Delegate { get; set; } = default!;
    public List<IRuleMatcherAttribute> RuleMatchers { get; set; } = [];

}
public class MatrixAppContainer
{
    private readonly ConcurrentDictionary<Type, MatrixApplication> _appInstances = new();
    private readonly ConcurrentDictionary<Type, Func<MatrixBotClient, Task>> _onReadyFunctions = [];
    private readonly ConcurrentDictionary<string, List<MatrixAppService>> _appServices = new();

    /// <summary>
    /// 注册引用
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public MatrixApplication Register(Type type)
    {
        var instance = _appInstances.GetOrAdd(type, (MatrixApplication)Activator.CreateInstance(type)!);
        _onReadyFunctions.TryAdd(type, instance.OnReadyAsync);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes()
                   .Where(x => x is IMatrixAttribute)
                   .ToList();

            if (attributes is { Count: 0 }) continue;
            var msgTypes = attributes
                .Where(x => x is ITypeMatcherAttribute)
                .Cast<ITypeMatcherAttribute>()
                .Select(x=>x.MsgType)
                .Distinct()
                .ToList();
            var ruleMatchers = attributes
                .Where(x => x is IRuleMatcherAttribute)
                .Cast<IRuleMatcherAttribute>()
                .ToList();

            var parameterInfos = method.GetParameters();
            var parameterExprs = new List<ParameterExpression>();
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                parameterExprs.Add(Expression.Parameter(parameterInfos[i].ParameterType, $"arg_{i}"));
            }
            var instanceExpression = Expression.Constant(instance);
            // 创建方法调用表达式 instance.method(ctxParameter)
            var callExpression = Expression.Call(instanceExpression, method, parameterExprs);
            // 编译表达式树 ()=>instance.method(ctxParameter)
            var @delegate = Expression.Lambda(callExpression, parameterExprs).Compile()!;

            foreach (var msgType in msgTypes)
            {
                var appService = new MatrixAppService
                {
                    ServiceName = $"[{type.FullName}][{method.Name}]",
                    RuleMatchers = ruleMatchers,
                    Delegate = @delegate
                };
                _appServices.AddOrUpdate(msgType, [appService], (k, v) =>
                {
                    v.Add(appService);
                    return v;
                });
            }
        }
        return instance;
    }

    /// <summary>
    /// 初始化应用
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task InitAsync(MatrixBotClient client,CancellationToken cancellationToken)
    {
        var onReadyHandlerEnumerator = _onReadyFunctions.GetEnumerator();
        while (!cancellationToken.IsCancellationRequested && onReadyHandlerEnumerator.MoveNext())
        {
            Log.Information("初始化应用[{AppName}]", onReadyHandlerEnumerator.Current.Key);
            await onReadyHandlerEnumerator.Current.Value.Invoke(client);
        }
    }

    public bool TryGetService(string eventType, [NotNullWhen(true)] out List<MatrixAppService>? appService)
    {
        return _appServices.TryGetValue(eventType, out appService) && appService != null;
    }
}
