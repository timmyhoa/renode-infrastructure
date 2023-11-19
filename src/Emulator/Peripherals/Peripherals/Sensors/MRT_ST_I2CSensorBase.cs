using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.Miscellaneous;
using Antmicro.Renode.Utilities;
using Antmicro.Renode.Utilities.Collections;

namespace Antmicro.Renode.Peripherals.Sensors
{
    public abstract class MRT_ST_I2CSensorBase<T> : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection> where T : IConvertible
    {
        public MRT_ST_I2CSensorBase()
        {
            cache = new SimpleCache();
            RegistersCollection = new ByteRegisterCollection(this);

            DefineRegisters();
            Reset();
        }

        protected byte GetScaledValue(decimal value, short sensitivity, bool upperByte)
        {
            var scaled = (short)(value * sensitivity);
            return upperByte
                ? (byte)(scaled >> 8)
                : (byte)scaled;
        }

        protected short CalculateScale(int minVal, int maxVal, int width)
        {
            var range = maxVal - minVal;
            return (short)(((1 << width) / range) - 1);
        }

        public byte[] Read(int count = 1)
        {
            var r = RegistersCollection.Read(address);
            this.NoisyLog(
                "Reading register {0} (0x{1:X}) from device: 0x{2:X}", cache.Get(address, x => Enum.GetName(typeof(T), x)), address,
                r);
            TryIncrementAddress();
            return new byte[] { r };
        }

        public void Write(byte[] data)
        {
            foreach (var b in data)
            {
                WriteByte(b);
            }
        }

        public void WriteByte(byte b)
        {
            switch (state)
            {
                case State.Idle:
                    {
                        address = BitHelper.GetValue(b, offset: 0, size: 7);
                        this.NoisyLog("Setting register address to {0}, (0x{0:X})",
                                cache.Get(address, x => Enum.GetName(typeof(T), x)),
                                address
                        );
                        state = State.Processing;
                        break;
                    }
                case State.Processing:
                    {
                        this.NoisyLog("Writing value 0x{0:X} to register {1} (0x{2:X})",
                                b,
                                cache.Get(address, x => Enum.GetName(typeof(T), x)),
                                address
                        );
                        RegistersCollection.Write(address, b);
                        TryIncrementAddress();
                        break;
                    }
                default:
                    throw new ArgumentException($"Unexpected state: {state}");
            }
        }

        public virtual void FinishTransmission()
        {
            this.NoisyLog("Finishing transmission, going to the Idle state");
            state = State.Idle;
        }

        protected abstract void TryIncrementAddress();

        protected abstract void DefineRegisters();

        public virtual void Reset()
        {
            state = State.Idle;
            address = 0;
        }


        public ByteRegisterCollection RegistersCollection
        { get; private set; }
        private State state;
        protected uint address;
        private readonly SimpleCache cache;

        private enum State
        {
            Idle,
            Processing
        }

    }
}