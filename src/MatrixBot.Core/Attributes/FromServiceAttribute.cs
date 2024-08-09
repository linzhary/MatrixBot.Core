using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixBot.Core;

/// <summary>
/// 从服务容器中解析服务
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FromServiceAttribute : Attribute
{
}
