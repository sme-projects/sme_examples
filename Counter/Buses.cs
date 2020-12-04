using System;
using SME;
using SME.VHDL;

namespace Counter
{

    [InitializedBus]
    public interface Control : IBus
    {
        bool active { get; set; }
    }

    [InitializedBus]
    public interface LEDs : IBus
    {
        UInt4 value { get; set; }
    }

}