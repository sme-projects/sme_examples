using System;
using SME;
using SME.Components;

namespace Matmul
{

    [ClockedProcess]
    public class AccessGenerator : SimpleProcess
    {
        [InputBus] public MatrixMeta matrix_meta_A = Scope.CreateBus<MatrixMeta>();
        [InputBus] public MatrixMeta matrix_meta_B = Scope.CreateBus<MatrixMeta>();

        [OutputBus] public MatrixMeta matrix_meta_C = Scope.CreateBus<MatrixMeta>();
        [OutputBus] public TrueDualPortMemory<int>.IControlB matrix_ctrl_A;
        [OutputBus] public TrueDualPortMemory<int>.IControlB matrix_ctrl_B;
        [OutputBus] public TrueDualPortMemory<int>.IControlB matrix_ctrl_C;

        int i, j, k;
        // Base and stride of C will always be 0 and 1
        int base_A = 0, base_B = 0;
        int stride_A = 1, stride_B = 1;
        bool was_valid;
        int A_height, A_width, B_width;
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
                stride_A = matrix_meta_A.stride;
                stride_B = matrix_meta_B.stride;
                A_height = matrix_meta_A.height;
                A_width = matrix_meta_A.width;
                B_width = matrix_meta_B.width;
                running = true;
                was_valid = true;
            }

            if (running)
            {
                int addr_A = base_A + ((i * A_width) + k) * stride_A;
                int addr_B = base_B + ((k * B_width) + j) * stride_B;
                int addr_C = (i * B_width) + j;

                //Console.WriteLine($"A {addr_A}");
                //Console.WriteLine($"B {addr_B}");
                //Console.WriteLine($"C {addr_C}");

                matrix_ctrl_A.Enabled = true;
                matrix_ctrl_A.Address = addr_A;
                matrix_ctrl_A.IsWriting = false;
                matrix_ctrl_A.Data = 0;

                matrix_ctrl_B.Enabled = true;
                matrix_ctrl_B.Address = addr_B;
                matrix_ctrl_B.IsWriting = false;
                matrix_ctrl_B.Data = 0;

                matrix_ctrl_C.Enabled = true;
                matrix_ctrl_C.Address = addr_C;
                matrix_ctrl_C.IsWriting = true;
                matrix_ctrl_C.Data = 0; // Will be written by the accumulator

            /*for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < b.GetLength(1); j++)
                    for (int k = 0; k < b.GetLength(0); k++)
                        c[i,j] += a[i,k] * b[k, j];*/
                k++;
                if (k == A_width)
                {
                    k = 0;
                    j++;
                    if (j == B_width)
                    {
                        j = 0;
                        i++;
                        if (i == A_height)
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

                matrix_ctrl_C.Enabled = false;
                matrix_ctrl_C.Address = 0;
                matrix_ctrl_C.IsWriting = false;
                matrix_ctrl_C.Data = 0;

                matrix_meta_C.valid = was_valid;
                matrix_meta_C.base_addr = 0;
                matrix_meta_C.height = A_height;
                matrix_meta_C.width = B_width;
                matrix_meta_C.stride = 1;
                was_valid = false;
            }
        }
    }

    [ClockedProcess]
    public class Accumulator : SimpleProcess
    {
        [InputBus] public Data input;
        [InputBus] public TrueDualPortMemory<int>.IControlB addr = Scope.CreateBus<TrueDualPortMemory<int>.IControlB>();

        [OutputBus] public TrueDualPortMemory<int>.IControlB output;

        int current_addr = -1;
        int accum = 0;

        protected override void OnTick()
        {
            if (addr.Enabled)
            {
                if (addr.Address == current_addr)
                {
                    output.Enabled = false;
                    output.Address = -1;
                    output.IsWriting = false;
                    output.Data = 0;

                    accum += input.data;
                }
                else
                {
                    output.Enabled = current_addr > -1;
                    output.Address = current_addr;
                    output.IsWriting = true;
                    output.Data = accum;

                    accum = input.data;
                    current_addr = addr.Address;
                }
            }
            else
            {
                // Assumes that not reading triggers a reset.
                // Flush the last value, if present.
                if (current_addr > -1)
                {
                    output.Enabled = true;
                    output.Address = current_addr;
                    output.IsWriting = true;
                    output.Data = accum;
                }
                else
                {
                    output.Enabled = false;
                    output.Address = -1;
                    output.IsWriting = false;
                    output.Data = 0;
                }

                accum = 0;
                current_addr = -1;
            }
        }
    }

    public class AddressPacker : SimpleProcess
    {
        [InputBus] public SME.Components.TrueDualPortMemory<int>.IControlB input_ctrl;
        [InputBus] public Data input_data;

        [OutputBus] public SME.Components.TrueDualPortMemory<int>.IControlB output;

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
        [InputBus] public TrueDualPortMemory<int>.IControlB input = Scope.CreateBus<TrueDualPortMemory<int>.IControlB>();

        [OutputBus] public TrueDualPortMemory<int>.IControlB output;

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
        [InputBus] public TrueDualPortMemory<int>.IReadResultB input;

        [OutputBus] public Data output = Scope.CreateBus<Data>();

        protected override void OnTick()
        {
            output.data = input.Data;
        }
    }

    [ClockedProcess]
    public class MetaReg : SimpleProcess
    {
        [InputBus] public MatrixMeta input;

        [OutputBus] public MatrixMeta output = Scope.CreateBus<MatrixMeta>();

        protected override void OnTick()
        {
            output.valid = input.valid;
            output.base_addr = input.base_addr;
            output.height = input.height;
            output.width = input.width;
            output.stride = input.stride;
        }
    }

    [ClockedProcess]
    public class Multiplier : SimpleProcess
    {
        [InputBus] public Data input_a;
        [InputBus] public Data input_b;

        [OutputBus] public Data output = Scope.CreateBus<Data>();

        protected override void OnTick()
        {
            output.data = input_a.data * input_b.data;
        }
    }

    public class Tester : SimulationProcess
    {
        [InputBus] public MatrixMeta matrix_C_meta;
        [InputBus] public SME.Components.TrueDualPortMemory<int>.IReadResultA matrix_C_read;

        [OutputBus] public MatrixMeta matrix_A_meta;
        [OutputBus] public MatrixMeta matrix_B_meta;
        [OutputBus] public SME.Components.TrueDualPortMemory<int>.IControlA matrix_C;
        [OutputBus] public SME.Components.TrueDualPortMemory<int>.IControlA matrix_A;
        [OutputBus] public SME.Components.TrueDualPortMemory<int>.IControlA matrix_B;

        public Tester(int N, int K, int M, int repeats)
        {
            this.N = N;
            this.K = K;
            this.M = M;

            A = new int[N, K];
            B = new int[K, M];
            C = new int[N, M];

            rng = new Random();
            this.repeats = repeats;
        }

        public void init(int[,] arr)
        {
            for (int i = 0; i < arr.GetLength(0); i++)
                for (int j = 0; j < arr.GetLength(1); j++)
                    arr[i,j] = rng.Next() % 100;
        }

        public void expected(int[,] a, int[,] b, int[,] c)
        {
            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < b.GetLength(1); j++)
                    for (int k = 0; k < b.GetLength(0); k++)
                        c[i,j] += a[i,k] * b[k, j];

        }

        public void reset(int[,] arr)
        {
            for (int i = 0; i < arr.GetLength(0); i++)
                for (int j = 0; j < arr.GetLength(1); j++)
                    arr[i,j] = 0;
        }

        Random rng;
        int repeats;
        int N, K, M;
        int[,] A, B, C;

        public override async System.Threading.Tasks.Task Run()
        {
            await ClockAsync();

            for (int iters = 0; iters < repeats; iters++)
            {
                // Init and compute result
                init(A);
                init(B);
                reset(C);
                expected(A, B, C);

                // Ensure the controls are disabled
                matrix_A_meta.valid = false;
                matrix_B_meta.valid = false;
                await ClockAsync();

                // Fill the input memories
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < K; j++)
                    {
                        matrix_A.Enabled = true;
                        matrix_A.Address = i*K + j;
                        matrix_A.IsWriting = true;
                        matrix_A.Data = A[i,j];
                        await ClockAsync();
                    }
                matrix_A.Enabled = false;
                await ClockAsync();

                for (int i = 0; i < K; i++)
                    for (int j = 0; j < M; j++)
                    {
                        matrix_B.Enabled = true;
                        matrix_B.Address = i*M + j;
                        matrix_B.IsWriting = true;
                        matrix_B.Data = B[i,j];
                        await ClockAsync();
                    }
                matrix_B.Enabled = false;
                await ClockAsync();

                // Init the controls
                matrix_A_meta.valid = true;
                matrix_A_meta.base_addr = 0;
                matrix_A_meta.height = N;
                matrix_A_meta.width = K;
                matrix_A_meta.stride = 1;

                matrix_B_meta.valid = true;
                matrix_B_meta.base_addr = 0;
                matrix_B_meta.height = K;
                matrix_B_meta.width = M;
                matrix_B_meta.stride = 1;
                await ClockAsync();

                // Disable controls and wait for result
                matrix_A_meta.valid = false;
                matrix_B_meta.valid = false;
                while (!matrix_C_meta.valid)
                    await ClockAsync();

                // Verify output
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                    {
                        var flat_addr = i*M + j;
                        matrix_C.Enabled = flat_addr < N*M;
                        matrix_C.Address = flat_addr;
                        matrix_C.IsWriting = false;
                        matrix_C.Data = 0;
                        await ClockAsync();
                        await ClockAsync();
                        System.Diagnostics.Debug.Assert(matrix_C_read.Data == C[i,j], $"Error, C[{i},{j}]: Expected {C[i,j]}, got {matrix_C_read.Data}");
                    }

                // Reset reading
                matrix_C.Enabled = false;
            }
        }
    }

}