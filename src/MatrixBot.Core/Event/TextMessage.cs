using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixBot.Core;

public class TextMessage : Message
{
    public TextMessage(string text,string? formatted_body = null)
    {
        MsgType = "m.text";
        Body = text;
        if (!string.IsNullOrEmpty(formatted_body))
        {
            Format = "org.matrix.custom.html";
            FormattedBody = formatted_body.Trim();
        }
    }
}
