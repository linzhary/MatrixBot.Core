using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixBot.Core;

public class MatrixBotException : Exception
{
    public bool Success { get; set; } = false;
}
