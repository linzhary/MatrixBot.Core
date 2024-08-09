using Serilog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace MatrixBot.Core;

public class MatrixServiceCollection
{
    private readonly List<Type> _serviceTypes = [];
    private readonly List<object> _globalService;
    internal MatrixServiceCollection(List<object> globalServices)
    {
        _globalService = globalServices;
        _globalService.Add(this);
    }
    /// <summary>
    /// 注册服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public MatrixServiceCollection AddServices<T>()
    {
        _serviceTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="type"></param>
    public MatrixServiceCollection AddServices(Type type)
    {
        _serviceTypes.Add(type);
        return this;
    }

    /// <summary>
    /// 注册程序集中标记了<see cref="ManagedServiceAttribute"/>的服务
    /// </summary>
    /// <param name="assembly"></param>
    public MatrixServiceCollection RegisterAssembly(Assembly assembly)
    {
        var serviceTypes = assembly.GetExportedTypes()
            .Where(x => x.IsClass)
            .Where(x => !x.IsAbstract)
            .Where(x => x.GetCustomAttribute<ManagedServiceAttribute>(true) is not null)
            .ToList();

        _serviceTypes.AddRange(serviceTypes);
        return this;
    }

    /// <summary>
    /// 构造服务容器
    /// </summary>
    /// <returns></returns>
    public MatrixServiceProvider Build()
    {
        return new MatrixServiceProvider(_serviceTypes, _globalService);
    }

    /// <summary>
    /// 构造服务容器(作用域)
    /// </summary>
    /// <returns></returns>
    internal MatrixScopedServiceProvider BuildScoped()
    {
        return new MatrixScopedServiceProvider(_serviceTypes, _globalService);
    }
}
