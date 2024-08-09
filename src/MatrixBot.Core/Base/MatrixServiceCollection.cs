using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace MatrixBot.Core;

public class MatrixServiceCollection
{
    private readonly IServiceCollection _serviceCollection = new ServiceCollection();

    internal MatrixServiceCollection(List<object> singletonServices)
    {
        foreach (var service in singletonServices)
        {
            _serviceCollection.AddSingleton(service.GetType(), service);
        }
    }
    /// <summary>
    /// 注册服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public MatrixServiceCollection AddService<T>() where T : class
    {
        _serviceCollection.AddSingleton<T>();
        return this;
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="type"></param>
    public MatrixServiceCollection AddService(Type type)
    {
        var instance = Activator.CreateInstance(type)!;
        _serviceCollection.AddSingleton(type, instance);
        if (type.IsAssignableTo(typeof(MatrixService)))
        {
            _serviceCollection.AddSingleton(typeof(MatrixService), instance);
        }
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

        foreach (var serviceType in serviceTypes)
        {
            AddService(serviceType);
        }
        return this;
    }

    /// <summary>
    /// 构造服务容器
    /// </summary>
    /// <returns></returns>
    public MatrixServiceProvider Build()
    {
        return new MatrixServiceProvider(_serviceCollection);
    }
}
