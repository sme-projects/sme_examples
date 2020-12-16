using System;
using SME;
using SME.Components;
using SME.VHDL;

namespace HistogramBinning
{

    public class Tester : SimulationProcess
    {
        [InputBus]
        public TrueDualPortMemory<uint>.IReadResultB bram_result;
        [InputBus]
        public Idle idle;

        [OutputBus]
        public TrueDualPortMemory<uint>.IControlB bram_ctrl = Scope.CreateBus<TrueDualPortMemory<uint>.IControlB>();
        [OutputBus]
        public Detector output = Scope.CreateBus<Detector>();

        int mem_size;
        Random rand = new Random();
        bool short_test;

        public Tester(bool short_test, int mem_size)
        {
            this.short_test = short_test;
            this.mem_size = mem_size;
        }

        private async System.Threading.Tasks.Task MemRead(int addr)
        {
            bram_ctrl.Enabled = true;
            bram_ctrl.Address = addr;
            bram_ctrl.IsWriting = false;
            bram_ctrl.Data = 0;
            await ClockAsync();
            // Ensure no latching
            bram_ctrl.Enabled = false;
            bram_ctrl.Address = 0;
            bram_ctrl.IsWriting = false;
            bram_ctrl.Data = 0;
        }

        private async System.Threading.Tasks.Task MemWrite(int addr, uint data)
        {
            bram_ctrl.Enabled = true;
            bram_ctrl.Address = addr;
            bram_ctrl.IsWriting = true;
            bram_ctrl.Data = data;
            await ClockAsync();
            // Ensure no latching
            bram_ctrl.Enabled = false;
            bram_ctrl.Address = 0;
            bram_ctrl.IsWriting = false;
            bram_ctrl.Data = 0;
        }

        private async System.Threading.Tasks.Task Test(bool reset, int[] input_idxs, uint[] input_data, uint[] output_data)
        {
            // Ensure that memory is initialised as 0
            if (reset)
                for (int i = 0; i < mem_size; i++)
                    await MemWrite(i, 0);

            // Transfer inputdata
            for (uint i = 0; i < input_idxs.Length; i++)
                await ToBinning(input_idxs[i], input_data[i]);

            // Wait until binning is idle
            while (!idle.flg)
                await ClockAsync();

            // Verify that the result matches the expected output
            await MemRead(0);
            for (int i = 1; i <= output_data.Length; i++)
            {
                await MemRead(i % mem_size);
                System.Diagnostics.Debug.Assert(
                    bram_result.Data == output_data[i-1],
                    $"Error on index {i-1}: Expected {output_data[i-1]}, got {bram_result.Data}");
            }
        }

        private async System.Threading.Tasks.Task ToBinning(int idx, uint data)
        {
            output.valid = true;
            output.idx = idx;
            output.data = data;
            await ClockAsync();
            // Ensure no latching
            output.valid = false;
            output.idx = 0;
            output.data = 0;
        }

        public async override System.Threading.Tasks.Task Run()
        {
            // Ensure that the network is waiting for input
            await ClockAsync();

            /*****
             *
             * Hard coded test
             *
             *****/
            int[] input_idxs  = { 0, 1, 1, 0, 2, 2 };
            uint[] input_data  = { 3, 4, 1, 6, 7, 8 };
            uint[] output_data = { 9, 5, 15 };
            await Test(true, input_idxs, input_data, output_data);

            return;
            /*****
             *
             * Continueous test
             * Tests whether on not multiple inputs into same bins without resetting will work.
             *
             *****/
            input_idxs = new  int[] {  0,  2, 1, 0, 2 };
            input_data = new uint[] { 12, 15, 3, 5, 1 };
            for (int i = 0; i < input_data.Length; i++)
                output_data[input_idxs[i]] += input_data[i];
            await Test(false, input_idxs, input_data, output_data);

            /*****
             *
             * Test to capture error, which occurs when there is a gap between two
             * of the same indices
             *
             */
            input_idxs = new  int[] { 0, 1, 0, 1 };
            input_data = new uint[] { 1, 2, 3, 4 };
            output_data = new uint[] { 4, 6 };
            await Test(true, input_idxs, input_data, output_data);
            return;

            /*****
             *
             * Generated test - short
             *
             *****/
            int short_test_length = 1000;
            input_idxs  = new int[short_test_length];
            input_data  = new uint[short_test_length];
            output_data = new uint[mem_size];
            for (int i = 0; i < short_test_length; i++)
            {
                input_idxs[i] =       rand.Next(mem_size);
                input_data[i] = (uint)rand.Next(10);
                output_data[input_idxs[i]] += input_data[i];
            }
            await Test(true, input_idxs, input_data, output_data);

            if (short_test)
                return;
            /*****
             *
             * Generated test - long
             *
             */
            int long_test_length = 100 * mem_size;
            input_idxs  = new  int[long_test_length];
            input_data  = new uint[long_test_length];
            output_data = new uint[mem_size];
            for (int i = 0; i < long_test_length; i++)
            {
                input_idxs[i] =       rand.Next(mem_size);
                input_data[i] = (uint)rand.Next();
                output_data[input_idxs[i]] += input_data[i];
            }
            await Test(true, input_idxs, input_data, output_data);
        }
    }

}
