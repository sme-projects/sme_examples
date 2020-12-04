using System;
using SME;
using SME.Components;

namespace Matmul
{

    public interface MatrixMeta : IBus
    {
        bool valid { get; set; }
        int base_addr { get; set; }
        int height { get; set; }
        int width { get; set; }
        int stride { get; set; }
    }

    public interface Data : IBus
    {
        int data { get; set; }
    }

}