using System.Collections.Generic;
using Neo.FileStorage.API.Acl;

namespace Neo.FileStorage.Storage.Services.Object.Acl
{
    public static class BasicAclHelper
    {
        public const int ReservedBitNumber = 2;
        public const int StickyBitPos = ReservedBitNumber;
        public const int FinalBitPos = StickyBitPos + 1;
        public const int OpOffset = FinalBitPos + 1;
        public const int BitsPerOp = 4;
        public const int OpNumber = 7;
        public const int LeftACLBitPos = OpOffset + BitsPerOp * OpNumber - 1;

        public const byte BitUser = 0;
        public const byte BitSystem = 1;
        public const byte BitOthers = 2;
        public const byte BitBearer = 3;

        public static Dictionary<Operation, byte> Order = new()
        {
            { Operation.Getrangehash, 0 },
            { Operation.Getrange, 1 },
            { Operation.Search, 2 },
            { Operation.Delete, 3 },
            { Operation.Put, 4 },
            { Operation.Head, 5 },
            { Operation.Get, 6 },
        };

        public static bool IsLeftBitSet(this uint value, byte n)
        {
            var bitMask = (uint)(1 << (LeftACLBitPos - n));
            return bitMask != 0 && (value & bitMask) == bitMask;
        }

        public static void SetLeftBit(this ref uint value, byte n)
        {
            value |= (uint)(1 << (LeftACLBitPos - n));
        }

        public static void ResetLeftBit(this ref uint value, byte n)
        {
            value &= ~(uint)(1 << (LeftACLBitPos - n));
        }

        public static bool Final(this uint value)
        {
            return IsLeftBitSet(value, FinalBitPos);
        }

        public static void SetFinal(this ref uint value)
        {
            value.SetLeftBit(FinalBitPos);
        }

        public static void ResetFinal(this ref uint value)
        {
            value.ResetLeftBit(FinalBitPos);
        }

        public static bool Sticky(this ref uint value)
        {
            return value.IsLeftBitSet(StickyBitPos);
        }

        public static void SetSticky(this ref uint value)
        {
            value.SetLeftBit(StickyBitPos);
        }

        public static bool UserAllowed(this uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                return value.IsLeftBitSet((byte)(OpOffset + n * BitsPerOp + BitUser));
            }
            return false;
        }

        public static void AllowUser(this ref uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                value.SetLeftBit((byte)(OpOffset + n * BitsPerOp + BitUser));
            }
        }

        public static void ForbidUser(this ref uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                value.ResetLeftBit((byte)(OpOffset + n * BitsPerOp + BitUser));
            }
        }

        public static bool SystemAllowed(this uint value, Operation op)
        {
            if (op != Operation.Delete && op != Operation.Getrange) return true;
            if (Order.TryGetValue(op, out byte n))
            {
                return value.IsLeftBitSet((byte)(OpOffset + n * BitsPerOp + BitSystem));
            }
            return false;
        }

        public static void AllowSystem(this ref uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                value.SetLeftBit((byte)(OpOffset + n * BitsPerOp + BitSystem));
            }
        }

        public static void ForbidSystem(this ref uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                value.ResetLeftBit((byte)(OpOffset + n * BitsPerOp + BitSystem));
            }
        }

        public static bool InnerRingAllowed(this uint value, Operation op)
        {
            if (op == Operation.Search || op == Operation.Getrangehash || op == Operation.Head) return true;
            if (Order.TryGetValue(op, out byte n))
            {
                return value.IsLeftBitSet((byte)(OpOffset + n * BitsPerOp + BitSystem));
            }
            return false;
        }

        public static bool OthersAllowed(this uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                return value.IsLeftBitSet((byte)(OpOffset + n * BitsPerOp + BitOthers));
            }
            return false;
        }

        public static void AllowOthers(this ref uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                value.SetLeftBit((byte)(OpOffset + n * BitsPerOp + BitOthers));
            }
        }

        public static void ForbidOthers(this ref uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                value.ResetLeftBit((byte)(OpOffset + n * BitsPerOp + BitOthers));
            }
        }

        public static bool BearsAllowed(this uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                return value.IsLeftBitSet((byte)(OpOffset + n * BitsPerOp + BitBearer));
            }
            return false;
        }

        public static void AllowBears(this ref uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                value.SetLeftBit((byte)(OpOffset + n * BitsPerOp + BitBearer));
            }
        }

        public static void ForbidBears(this ref uint value, Operation op)
        {
            if (Order.TryGetValue(op, out byte n))
            {
                value.ResetLeftBit((byte)(OpOffset + n * BitsPerOp + BitBearer));
            }
        }
    }
}
