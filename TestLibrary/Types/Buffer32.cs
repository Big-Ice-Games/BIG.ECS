using System;
using System.Runtime.InteropServices;

namespace TestLibrary.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Buffer32
    {
        private fixed int _data[32];
        public int this[int index]
        {
            get => (uint)index >= 32 ? throw new IndexOutOfRangeException() : _data[index];
            set
            {
                if ((uint)index >= 32)
                    throw new IndexOutOfRangeException();

                _data[index] = value;
            }
        }
    }
}
