using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixBot.Core;

public class MatrixMediaInfo
{
    public string FileName { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public long FileSize { get; set; } = 0;
    public Stream FileStream { get; set; } = new MemoryStream();
    public string MatrixUrl { get; set; } = string.Empty;
}
