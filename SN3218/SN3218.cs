using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace WinHill.Devices
{
    // ReSharper disable once InconsistentNaming
    public class SN3218
    {
        private const int I2CAddress = 0x54;
        private const string I2CControllerName = "I2C1";

        private I2cDevice device;

        private enum Command
        {
            EnableOutput = 0x00,
            SetPwmValues = 0x01,
            EnableLeds = 0x13,
            Update = 0x16,
            Reset = 0x17
        }


        public bool Initialize()
        {
            device = Task.Run(async () => { return await InitializeDevice(); }).Result;
            return device != null;
        }

        public void Enable()
        {
            WriteBlockData(Command.EnableOutput, 0x01);
        }

        public void Disable()
        {
            WriteBlockData(Command.EnableOutput, 0x00);
        }

        public void Reset()
        {
            WriteBlockData(Command.Reset, 0xff);
        }

        public void EnableLeds(int bitmask)
        {
            WriteBlockData(Command.EnableLeds, bitmask & 0x3f, (bitmask >> 6) & 0x3f, (bitmask >> 12) & 0X3f);
            WriteBlockData(Command.Update, 0xff);
        }

        public void Output(params int[] levels)
        {
            if (levels.Length > 18)
                Array.Resize(ref levels, 18);

            WriteBlockData(Command.SetPwmValues, levels);
            WriteBlockData(Command.Update, 0xff);
        }

        private void WriteBlockData(Command command, params int[] data)
        {
            if (device == null)
                return;

            var buffer = new byte[data.Length + 1];
            buffer[0] = (byte)command;
            Array.Copy(data.Select(x => (byte)x).ToArray(), 0, buffer, 1, data.Length);

            device.Write(buffer);
        }

        private async Task<I2cDevice> InitializeDevice()
        {
            // initialize I2C communications
            try
            {
                var deviceSelector = I2cDevice.GetDeviceSelector(I2CControllerName);
                var i2CDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);
                var i2CSettings = new I2cConnectionSettings(I2CAddress) {BusSpeed = I2cBusSpeed.FastMode};
                return await I2cDevice.FromIdAsync(i2CDeviceControllers[0].Id, i2CSettings);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception: {0}", e.Message);
                return null;
            }
        }
    }
}
