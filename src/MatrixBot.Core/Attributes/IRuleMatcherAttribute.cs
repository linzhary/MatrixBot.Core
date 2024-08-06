using System;
using System.Text.RegularExpressions;
namespace MatrixBot.Core;

public interface IRuleMatcherAttribute : IMatrixAttribute
{
    bool IsMatch(object? args);
}


