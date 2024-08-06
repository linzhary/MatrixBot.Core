using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixBot.Core;

public interface ITypeMatcherAttribute: IMatrixAttribute
{
    abstract string MsgType { get; }
}
