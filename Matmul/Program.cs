using System;
using SME;
using SME.Components;

namespace Matmul
{
    public class Program
    {
        static void Main(string[] args)
        {
            var tests = new []
            {
                (3, 3, 3, 2, false),
                (3, 3, 1, 2, false),
                (3, 1, 1, 2, false),
                (3, 1, 3, 2, false),
                (1, 3, 3, 2, false),
                (1, 3, 1, 2, false),
                (10, 10, 10, 3, true)
            };
            foreach ((var n, var k, var m, var repeats, var transpile) in tests)
            {
                using (var sim = new Simulation())
                {
                    var tester = new Tester(n, m, k, repeats);
                    var matmul = new MatrixMultiplication(n, m, k);
                    tester.matrix_A = matmul.matrix_A.ControlA;
                    tester.matrix_B = matmul.matrix_B.ControlA;
                    tester.matrix_C = matmul.matrix_C.ControlA;
                    tester.matrix_C_read = matmul.matrix_C.ReadResultA;
                    tester.matrix_A_meta = matmul.matrix_A_meta;
                    tester.matrix_B_meta = matmul.matrix_B_meta;
                    tester.matrix_C_meta = matmul.matrix_C_meta;

                    if (transpile)
                    {
                        sim
                            .AddTopLevelInputs(
                                matmul.matrix_A.ControlA,
                                matmul.matrix_B.ControlA,
                                matmul.matrix_C.ControlA,
                                matmul.matrix_A_meta,
                                matmul.matrix_B_meta
                            )
                            .AddTopLevelOutputs(
                                matmul.matrix_A.ReadResultA,
                                matmul.matrix_B.ReadResultA,
                                matmul.matrix_C.ReadResultA,
                                matmul.matrix_C_meta
                            )
                            .BuildCSVFile()
                            .BuildVHDL();
                    }

                    sim.Run();
                }
            }
        }
    }

    public class MatrixMultiplication
    {
        public TrueDualPortMemory<int> matrix_A;
        public TrueDualPortMemory<int> matrix_B;
        public TrueDualPortMemory<int> matrix_C;
        public MatrixMeta matrix_A_meta;
        public MatrixMeta matrix_B_meta;
        public MatrixMeta matrix_C_meta;

        public MatrixMultiplication(int N, int K, int M)
        {
            // Make the processes
            matrix_A = new TrueDualPortMemory<int>(N*K);
            matrix_B = new TrueDualPortMemory<int>(K*M);
            matrix_C = new TrueDualPortMemory<int>(N*M);
            var access = new AccessGenerator();
            var data_reg_a = new DataReg();
            var data_reg_b = new DataReg();
            var multiplier = new Multiplier();
            var ctrl_reg_0 = new CtrlReg();
            var ctrl_reg_1 = new CtrlReg();
            var ctrl_reg_2 = new CtrlReg();
            var accumulator = new Accumulator();
            var meta_reg_0 = new MetaReg();
            var meta_reg_1 = new MetaReg();
            var meta_reg_2 = new MetaReg();
            var meta_reg_3 = new MetaReg();

            // Connect the processes
            access.matrix_ctrl_A = matrix_A.ControlB;
            access.matrix_ctrl_B = matrix_B.ControlB;
            access.matrix_ctrl_C = ctrl_reg_0.input;
            data_reg_a.input = matrix_A.ReadResultB;
            data_reg_b.input = matrix_B.ReadResultB;
            ctrl_reg_0.output = ctrl_reg_1.input;
            multiplier.input_a = data_reg_a.output;
            multiplier.input_b = data_reg_b.output;
            ctrl_reg_1.output = ctrl_reg_2.input;
            accumulator.input = multiplier.output;
            //ctrl_reg_1.output = accumulator.addr;
            ctrl_reg_2.output = accumulator.addr;
            //accumulator.addr = ctrl_reg_1.output;
            accumulator.output = matrix_C.ControlB;
            meta_reg_0.input = access.matrix_meta_C;
            meta_reg_1.input = meta_reg_0.output;
            meta_reg_2.input = meta_reg_1.output;
            meta_reg_3.input = meta_reg_2.output;

            // Connect the top-level busses
            matrix_A_meta = access.matrix_meta_A;
            matrix_B_meta = access.matrix_meta_B;
            matrix_C_meta = meta_reg_3.output;
        }
    }
}