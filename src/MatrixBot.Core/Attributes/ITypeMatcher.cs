using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixBot.Core;

public interface ITypeMatcher: IMatrixAction
{
    abstract string EventType { get; }
}
