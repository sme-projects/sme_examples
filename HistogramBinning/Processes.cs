using System;
using SME;
using SME.Components;
using SME.VHDL;

namespace SME_Binning
{

    [ClockedProcess]
    public class Adder : SimpleProcess
    {
        [InputBus]
        public Stored stored;
        [InputBus]
        public Detector input;

        [OutputBus]
        public AdderResult output = Scope.CreateBus<AdderResult>();

        protected override void OnTick()
        {
            output.val = input.data + stored.val;
        }
    }

    public class AdderMux : SimpleProcess
    {
        [InputBus]
        public Detector input_pipe;
        [InputBus]
        public TrueDualPortMemory<uint>.IReadResultA brama;
        [InputBus]
        public AdderResult adder;
        [InputBus]
        public Forward forward;
        [InputBus]
        public TrueDualPortMemory<uint>.IReadResultA last;

        [OutputBus]
        public Stored output = Scope.CreateBus<Stored>();

        protected override void OnTick()
        {
            if (input_pipe.valid)
                switch (forward.option)
                {
                    default:
                    case ForwardOptions.dont: output.val = brama.Data; break;
                    case ForwardOptions.last: output.val = last.Data; break;
                    case ForwardOptions.intermediate: output.val = adder.val; break;
                }
            else
                output.val = 0;
        }
    }

    public class BRAMPortAPacker : SimpleProcess
    {
        [InputBus]
        public Detector input;

        [OutputBus]
        public TrueDualPortMemory<uint>.IControlA output;

        protected override void OnTick()
        {
            output.Enabled = input.valid;
            output.Address = input.idx;
            output.IsWriting = false;
            output.Data = 0;
        }
    }

    public class BRAMPortBPacker : SimpleProcess
    {
        [InputBus]
        public Detector dtct;
        [InputBus]
        public AdderResult adderout;
        [InputBus]
        public TrueDualPortMemory<uint>.IControlB external;

        [OutputBus]
        public TrueDualPortMemory<uint>.IControlB output;

        protected override void OnTick()
        {
            if (dtct.valid)
            {
                output.Enabled = true;
                output.Address = dtct.idx;
                output.IsWriting = true;
                output.Data = adderout.val;
            }
            else
            {
                output.Enabled = external.Enabled;
                output.Address = external.Address;
                output.IsWriting = external.IsWriting;
                output.Data = external.Data;
            }
        }
    }

    public class Forwarder : SimpleProcess
    {
        [InputBus]
        public Detector input;
        [InputBus]
        public Detector intermediate;
        [InputBus]
        public AdderResult adder;

        [OutputBus]
        public Forward forward = Scope.CreateBus<Forward>();
        [OutputBus]
        public TrueDualPortMemory<uint>.IReadResultA last = Scope.CreateBus<TrueDualPortMemory<uint>.IReadResultA>();

        bool last_valid = false;
        int last_idx = 0;
        uint last_data = 0;

        protected override void OnTick()
        {
            last.Data = last_data;

            if (last_valid && input.idx == last_idx)
                forward.option = ForwardOptions.last;
            else if (intermediate.valid && input.idx == intermediate.idx)
                forward.option = ForwardOptions.intermediate;
            else
                forward.option = ForwardOptions.dont;
            
            last_valid = intermediate.valid;
            last_idx = intermediate.idx;
            last_data = adder.val;
        }
    }

    public class IdleChecker : SimpleProcess
    {
        [InputBus]
        public Detector input;

        [OutputBus]
        public Idle output = Scope.CreateBus<Idle>();

        protected override void OnTick() 
        {
            output.flg = !input.valid;
        }
    }

    [ClockedProcess]
    public class Pipe : SimpleProcess
    {
        [InputBus]
        public Detector input;

        [OutputBus]
        public Detector output = Scope.CreateBus<Detector>();

        protected override void OnTick()
        {
            output.valid = input.valid;
            output.idx   = input.idx;
            output.data  = input.data;
        }
    }

}
