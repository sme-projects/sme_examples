using System;
using SME;
using SME.VHDL;

namespace Counter
{

    [ClockedProcess]
    public class HardwareCounter : SimpleProcess
    {
        [InputBus] public Control ctrl;

        [OutputBus] public LEDs output = Scope.CreateBus<LEDs>();

        public HardwareCounter(int interval)
        {
            this.interval = interval;
        }

        int count = 0;
        int interval;
        UInt4 value = 0;
        const int uint4_max = 16;

        protected override void OnTick()
        {
            if (ctrl.active)
            {
                count = (count + 1) % interval;
                if (count == 0)
                    value = (UInt4)((value + 1) % uint4_max);
            }
            output.value = value;
        }
    }

    public class Tester : SimulationProcess
    {
        [InputBus] public LEDs leds;

        [OutputBus] public Control ctrl = Scope.CreateBus<Control>();

        public Tester(int interval)
        {
            this.interval = interval;
        }

        int interval;
        Random rng = new Random();
        const int uint4_max = 16;

        public override async System.Threading.Tasks.Task Run()
        {
            await ClockAsync();

            ctrl.active = true;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < interval; j++)
                    await ClockAsync();
                System.Diagnostics.Debug.Assert(leds.value == i, $"Expected {i}, got {leds.value}");
            }

            ctrl.active = false;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < interval; j++)
                    await ClockAsync();
                System.Diagnostics.Debug.Assert(leds.value == 10, $"Expected {i}, got {leds.value}");
            }

            ctrl.active = true;
            int total_ticks = (interval * 10) - 1;
            for (int i = 0; i < 10; i++)
            {
                int waits = rng.Next() % 30;
                for (int j = 0; j < waits; j++)
                {
                    await ClockAsync();
                    total_ticks++;
                }
                System.Diagnostics.Debug.Assert(leds.value == (total_ticks / interval) % uint4_max, $"Expected {(total_ticks / interval) % uint4_max}, got {leds.value}");
            }
        }
    }

}