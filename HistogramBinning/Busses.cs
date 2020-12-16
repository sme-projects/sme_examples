using System;
using SME;
using SME.Components;
using SME.VHDL;

namespace SME_Binning
{

    [InitializedBus]
    public interface AdderResult : IBus
    {
        uint val { get; set; }
    }

    [InitializedBus]
    public interface Detector : IBus
    {
        bool valid { get; set; }
        int idx { get; set; }
        uint data { get; set; }
    }

    [InitializedBus]
    public interface Forward : IBus
    {
        ForwardOptions option { get; set; }
    }

    public interface Idle : IBus
    {
        [InitialValue(true)]
        bool flg { get; set; }
    }

    [InitializedBus]
    public interface Stored : IBus
    {
        uint val { get; set; }
    }

}
