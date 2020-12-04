using System;
using SME;

namespace Counter
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                var interval = 5;
                var counter = new HardwareCounter(interval);
                var tester = new Tester(interval);

                counter.ctrl = tester.ctrl;
                tester.leds = counter.output;

                sim
                    .AddTopLevelInputs(counter.ctrl)
                    .AddTopLevelOutputs(counter.output)
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }
}
