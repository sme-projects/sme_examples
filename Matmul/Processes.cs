using System;
using SME;
using SME.Components;

namespace Matmul
{

    [ClockedProcess]
    public class AccessGenerator : SimpleProcess
    {
        [InputBus] public MatrixMeta matrix_meta_A;
        [InputBus] public MatrixMeta matrix_meta_B;

        [OutputBus] public MatrixMeta matrix_meta_C = Scope.CreateBus<MatrixMeta>();
        [OutputBus] public SME.Components.TrueDualPortMemory<int>.IControlB matrix_ctrl_A;
        [OutputBus] public SME.Components.TrueDualPortMemory<int>.IControlB matrix_ctrl_B;
        [OutputBus] public Data addr_C = Scope.CreateBus<Data>();

        public AccessGenerator(SME.Components.TrueDualPortMemory<int> matrix_A, SME.Components.TrueDualPortMemory<int> matrix_B)
        {
            matrix_ctrl_A = matrix_A.ControlB;
            matrix_ctrl_B = matrix_B.ControlB;
        }

        int i, j, k;
        int base_A, base_B;
        int stride_A, stride_B;
        // TODO danger nameclashing!
        bool was_valid;
        int M, K, N;
        bool running;

        protected override void OnTick()
        {
            if (!running && matrix_meta_A.valid && matrix_meta_B.valid)
            {
                i = 0;
                j = 0;
                k = 0;
                base_A = matrix_meta_A.base_addr;
                base_B = matrix_meta_B.base_addr;
                M = matrix_meta_A.height;
                K = matrix_meta_A.width;
                N = matrix_meta_B.width;
                running = true;
                was_valid = true;
            }

            if (running)
            {
                int addr_A = base_A + ((i * K) + k) * stride_A;
                int addr_B = base_B + ((k * N) + j) * stride_B;

                matrix_ctrl_A.Enabled = true;
                matrix_ctrl_A.Address = addr_A;
                matrix_ctrl_A.IsWriting = false;
                matrix_ctrl_A.Data = 0;

                matrix_ctrl_B.Enabled = true;
                matrix_ctrl_B.Address = addr_B;
                matrix_ctrl_B.IsWriting = false;
                matrix_ctrl_B.Data = 0;

                addr_C.data = (i * N) + j;

                k++;
                if (k == N)
                {
                    k = 0;
                    j++;
                    if (j == K)
                    {
                        j = 0;
                        i++;
                        if (i == M)
                        {
                            running = false;
                        }
                    }
                }
            }
            else
            {
                matrix_ctrl_A.Enabled = false;
                matrix_ctrl_A.Address = 0;
                matrix_ctrl_A.IsWriting = false;
                matrix_ctrl_A.Data = 0;

                matrix_ctrl_B.Enabled = false;
                matrix_ctrl_B.Address = 0;
                matrix_ctrl_B.IsWriting = false;
                matrix_ctrl_B.Data = 0;

                matrix_meta_C.valid = was_valid;
                matrix_meta_C.base_addr = 0;
                matrix_meta_C.height = M;
                matrix_meta_C.width = N;
                matrix_meta_C.stride = 1;
            }
        }
    }

    [ClockedProcess]
    public class Accumelator : SimpleProcess
    {

    }

    public class AddressPacker : SimpleProcess
    {
        [InputBus] public SME.Components.TrueDualPortMemory<int>.IControlB input_ctrl;
        [InputBus] public Data input_data;

        [OutputBus] public SME.Components.TrueDualPortMemory<int>.IControlB output;

        public AddressPacker(SME.Components.TrueDualPortMemory<int>.IControlB input_ctrl, Data input_data)
        {
            this.input_ctrl = input_ctrl;
            this.input_data = input_data;
        }

        protected override void OnTick()
        {
            output.Enabled = input_ctrl.Enabled;
            output.Address = input_ctrl.Address;
            output.IsWriting = input_ctrl.IsWriting;
            output.Data = input_data.data;
        }
    }

    [ClockedProcess]
    public class CtrlReg : SimpleProcess
    {
        [InputBus] public SME.Components.TrueDualPortMemory<int>.IControlB input;

        [OutputBus] public SME.Components.TrueDualPortMemory<int>.IControlB output;

        public CtrlReg(SME.Components.TrueDualPortMemory<int>.IControlB input, SME.Components.TrueDualPortMemory<int>.IControlB output)
        {
            this.input = input;
            this.output = output;
        }

        protected override void OnTick()
        {
            output.Enabled = input.Enabled;
            output.Address = input.Address;
            output.IsWriting = input.IsWriting;
            output.Data = input.Data;
        }
    }

    [ClockedProcess]
    public class DataReg : SimpleProcess
    {
        [InputBus] public Data input;

        [OutputBus] public Data output = Scope.CreateBus<Data>();

        public DataReg(Data input)
        {
            this.input = input;
        }

        protected override void OnTick()
        {
            output.data = input.data;
        }
    }

    [ClockedProcess]
    public class Multiplier : SimpleProcess
    {
        [InputBus] public Data input_a;
        [InputBus] public Data input_b;

        [OutputBus] public Data output = Scope.CreateBus<Data>();

        public Multiplier(Data input_a, Data input_b)
        {
            this.input_a = input_a;
            this.input_b = input_b;
        }

        protected override void OnTick()
        {
            output.data = input_a.data * input_b.data;
        }
    }

}