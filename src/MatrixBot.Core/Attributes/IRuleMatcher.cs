using System;
using System.Text.RegularExpressions;
namespace MatrixBot.Core;

public interface IRuleMatcher : IMatrixAction
{
    bool IsMatch(object? args);
}


