using System;
using System.Threading;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;

namespace Antmicro.Renode.Peripherals.Sensors
{
    public class MRT_LPS22XX : MRT_ST_I2CSensorBase<MRT_LPS22XX.Registers>
    {
        public MRT_LPS22XX()
        {
            Reset();
        }

        protected override void TryIncrementAddress()
        {
            address = (address + 1) % 0x7B;
        }

        protected override void DefineRegisters()
        {
            Registers.WhoAmI.Define(this, 0b10110011);
            Registers.Control1.Define(this)
                .WithEnumField(4, 3, out OutputRate, name: "ODR")
                .WithTaggedFlag("EN_LPFP", 3)
                .WithTaggedFlag("LPFP_CFG", 2)
                .WithTaggedFlag("BDU", 1)
                .WithTaggedFlag("SIM", 0);
            Registers.Control2.Define(this, 0b0001000);
            Registers.Status.Define(this, 0b00000011);
            Registers.PressureLow.Define(this)
                .WithValueField(0, 8, FieldMode.Read, valueProviderCallback: _ => GetScaledPressureValue(Part.Low));
            Registers.PressureMid.Define(this)
                .WithValueField(0, 8, valueProviderCallback: _ => GetScaledPressureValue(Part.Middle));
            Registers.PressureHigh.Define(this)
                .WithValueField(0, 8, valueProviderCallback: _ =>
                GetScaledPressureValue(Part.Upper));
            Registers.TempHigh.Define(this)
                .WithValueField(0, 8, valueProviderCallback: _ => GetScaledValue(Temperature, TemperatureScale, true));
            Registers.TempLow.Define(this)
                .WithValueField(0, 8, valueProviderCallback: _ => GetScaledValue(Temperature, TemperatureScale, false));
        }

        private byte GetScaledPressureValue(Part part)
        {
            var v = (int)Pressure * PressureScale;
            this.NoisyLog("Scaled value is {0}", v);
            switch (part)
            {
                case Part.Low:
                    {
                        return (byte)v;
                    }
                case Part.Middle:
                    {
                        return (byte)(v >> 8);
                    }
                case Part.Upper:
                    {
                        return (byte)(v >> 16);
                    }
                default:
                    {
                        throw new ArgumentException("Unexpected part");
                    }
            }
        }


        private enum Part
        {
            Low,
            Middle,
            Upper,
        }

        private const short PressureScale = 4096;
        private const short TemperatureScale = 100;
        public decimal Pressure { get; set; }
        public decimal Temperature { get; set; }

        IEnumRegisterField<DataRates> OutputRate;
        private enum DataRates
        {
            OneShot = 0x0,
            _1HZ = 0x1,
            _10HZ = 0x2,
            _25HZ = 0x3,
            _50HZ = 0x4,
            _75HZ = 0x5,
            _100HZ = 0x6,
            _200HZ = 0x7,
        }

        public enum Registers
        {
            // 00-0A Reserved
            InterruptConfig = 0x0B,
            PressureThresholdLow = 0x0C,
            PressureThresholdHigh = 0x0D,
            InterfaceControl = 0x0E,
            WhoAmI = 0x0F,
            Control1 = 0x10,
            Control2 = 0x11,
            Control3 = 0x12,
            FifoControl = 0x13,
            FifoWatermark = 0x14,
            PressureReferenceLow = 0x15,
            PressureReferenceHigh = 0x16,
            // 0x17 Reserved
            PressureOffsetLow = 0x18,
            PressureOffsetHigh = 0x19,
            // 1A - 23 Reserved
            Interrupt = 0x24,
            FifoStatus1 = 0x25,
            FifoStatus2 = 0x26,
            Status = 0x27,
            PressureLow = 0x28,
            PressureMid = 0x29,
            PressureHigh = 0x2A,
            TempLow = 0x2B,
            TempHigh = 0x2C,
            // 2D - 77 Reserved
            FifoDataPressureLow = 0x78,
            FifoDataPressureMid = 0x79,
            FifoDataPressureHigh = 0x7A,
        }
    }
}