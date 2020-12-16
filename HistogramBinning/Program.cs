using System;
using SME;
using SME.Components;
using SME.VHDL;

namespace SME_Binning
{

    class MainClass
    {
        public static void Main(string[] args)
        {
            using(var sim = new Simulation())
            {
                int mem_size = 4;

                var adder = new Adder();
                var bram = new TrueDualPortMemory<uint>(mem_size);
                var bram_porta = new BRAMPortAPacker();
                var bram_portb = new BRAMPortBPacker();
                var forward = new Forwarder();
                var mux = new AdderMux();
                var idle = new IdleChecker();
                var input_pipe = new Pipe();
                var intermediate_pipe = new Pipe();
                var tester = new Tester(false, mem_size);

                adder.stored = mux.output;
                adder.input = input_pipe.output;
                bram_porta.input = tester.output;
                bram_porta.output = bram.ControlA;
                bram_portb.adderout = adder.output;
                bram_portb.dtct = intermediate_pipe.output;
                bram_portb.external = tester.bram_ctrl;
                bram_portb.output = bram.ControlB;
                forward.adder = adder.output;
                forward.input = input_pipe.output;
                forward.intermediate = intermediate_pipe.output;
                mux.brama = bram.ReadResultA;
                mux.adder = adder.output;
                mux.forward = forward.forward;
                mux.input_pipe = input_pipe.output;
                mux.last = forward.last;
                idle.input = intermediate_pipe.output;
                input_pipe.input = tester.output;
                intermediate_pipe.input = input_pipe.output;
                tester.bram_result = bram.ReadResultB;
                tester.idle = idle.output;

                sim
                    .AddTopLevelInputs(input_pipe.input, bram.ControlB)
                    .AddTopLevelOutputs(bram.ReadResultB, idle.output)
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }

}
