﻿namespace TonLibDotNet.Cells
{
    public class CellBuilder
    {
        private readonly bool[] data;
        private readonly List<(Cell?, CellBuilder?)> refs;

        public CellBuilder()
        {
            data  = new bool[Cell.MaxBitsCount + 1]; // +1 to make building Cell easier, see Build()
            refs = new (Cell.MaxRefs);
        }

        public int Length { get; private set; }

        public int RefsCount { get; private set; }

        public bool TryCanStore(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Must be positive");
            }

            return Length + count <= Cell.MaxBitsCount;
        }

        public void EnsureCanStore(int count)
        {
            if (!TryCanStore(count))
            {
                throw new InvalidOperationException("No enough data");
            }
        }

        public void EnsureCanStoreRef()
        {
            if (RefsCount >= Cell.MaxRefs)
            {
                throw new InvalidOperationException("No enough data");
            }
        }

        public CellBuilder StoreBit(bool value)
        {
            EnsureCanStore(1);
            data[Length++] = value;
            return this;
        }

        public CellBuilder StoreBits(ReadOnlySpan<bool> values)
        {
            EnsureCanStore(values.Length);
            values.CopyTo(data.AsSpan(Length));
            Length += values.Length;
            return this;
        }

        public CellBuilder StoreSlice(Slice value)
        {
            EnsureCanStore(value.Length);
            value.LoadBitsTo(data.AsSpan(Length, value.Length));
            Length += value.Length;
            return this;
        }

        public CellBuilder StoreByte(byte value)
        {
            EnsureCanStore(8);
            for (var i = 0; i < 8; i++)
            {
                data[Length++] = ((value << i) & 0b1000_0000) != 0;
            }
            return this;
        }

        public CellBuilder StoreBytes(ReadOnlySpan<byte> values)
        {
            return StoreBytes(values, values.Length * 8);
        }

        public CellBuilder StoreBytes(ReadOnlySpan<byte> values, int bitsToStore)
        {
            EnsureCanStore(bitsToStore);

            var totalBits = values.Length * 8;
            if (bitsToStore > totalBits)
            {
                throw new ArgumentException("No enough data", nameof(values));
            }

            var skipBits = totalBits - bitsToStore;
            for (var i = skipBits; i < totalBits; i++)
            {
                var currentByte = values[i / 8];
                data[Length++] = ((currentByte << (i & 0b111)) & 0b1000_0000) != 0;
            }

            return this;
        }

        public CellBuilder StoreRef(Cell cell) {
            ArgumentNullException.ThrowIfNull(cell);
            EnsureCanStoreRef();
            refs[RefsCount] = (cell, null);
            return this;
        }

        public CellBuilder StoreRef(CellBuilder builder) {
            ArgumentNullException.ThrowIfNull(builder);
            EnsureCanStoreRef();
            refs[RefsCount] = (null, builder);
            return this;
        }

        public Cell Build()
        {
            var byteCount = Length / 8;
            var isAugmented = Length % 8 != 0;
            var augmentedLength = Length;

            if (isAugmented)
            {
                data[Length] = true;
                byteCount++;
                augmentedLength = byteCount * 8;
            }

            var buffer = new byte[byteCount];

            var currentByte = 0;
            var byteIndex = 0;
            for (var i = 0; i < augmentedLength; i++)
            {
                currentByte = (currentByte << 1) | (data[i] ? 1 : 0);

                if ((i & 0b111) == 0b111)
                {
                    buffer[byteIndex++] = (byte)(currentByte & 0xFF);
                    currentByte = 0;
                }
            }

            for (var i = 0; i < RefsCount; i++)
            {
                if (refs[i].Item1 == null)
                {
                    refs[i] = (refs[i].Item2!.Build(), null);
                }
            }

            return new Cell(buffer, isAugmented, refs.Take(RefsCount).Select(x => x.Item1!).ToArray());
        }
    }
}
