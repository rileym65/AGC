using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Agc
{
    class CPU
    {
        public const int REG_A = 0;
        public const int REG_L = 1;
        public const int REG_Q = 2;
        public const int REG_EB = 3;
        public const int REG_FB = 4;
        public const int REG_Z = 5;
        public const int REG_BB = 6;
        public const int REG_ARUPT = 8;
        public const int REG_LRUPT = 9;
        public const int REG_QRUPT = 10;
        public const int REG_ZRUPT = 13;
        public const int REG_BBRUPT = 14;
        public const int REG_BRUPT = 15;
        public const int REG_CYR = 16;
        public const int REG_SR = 17;
        public const int REG_CYL = 18;
        public const int REG_EDOP = 19;
        public const int REG_TIME2 = 20;
        public const int REG_TIME1 = 21;
        public const int REG_TIME3 = 22;
        public const int REG_TIME4 = 23;
        public const int REG_TIME5 = 24;
        public const int REG_TIME6 = 25;


        public const int INT_T6RUPT = 1 << 1;
        public const int INT_T5RUPT = 1 << 2;
        public const int INT_T3RUPT = 1 << 3;
        public const int INT_T4RUPT = 1 << 4;
        public const int INT_DSKY = 1 << 5;

        public const int UP_QUEUE_SIZE = 32;

        public const int IO_FEB = 7;

        public const int PINC_1 = 1;
        public const int PINC_3 = 3;
        public const int PINC_4 = 4;
        public const int PINC_5 = 5;
        public const int PINC_6 = 6;
        public const int DINC_6 = 7;

        private int[] ram;
        private int[] rom;
        private int[] port;
        private Boolean extend;
        private Boolean debugMode;
        private int mct;
        private Boolean ie;
        private Boolean inhint;
        private int sentPort;
        private Boolean execFromBrupt;
        private int index;
        private String debug;
        private int interrupts;
        private int[] upQueue;
        private int upStart;
        private int upEnd;
        private int nextPinc1;
        private int nextPinc3;
        private int nextPinc4;
        private int nextPinc5;
        private int nextDinc6;
        

        public CPU()
        {
            ram = new int[0x800];
            rom = new int[0x9000];
            port = new int[0x1ff];
            upQueue = new int[UP_QUEUE_SIZE];
            debugMode = false;
            debug = "";
            reset();
        }

        public void reset()
        {
            extend = false;
            ram[REG_Z] = 0x800;
            ram[REG_A] = 0x000;
            ram[REG_L] = 0x000;
            ram[REG_Q] = 0x000;
            ram[REG_EB] = 0x000;
            ram[REG_FB] = 0x000;
            ram[REG_BB] = 0x000;
            mct = 0;
            ie = true;
            inhint = false;
            sentPort = -1;
            execFromBrupt = false;
            interrupts = 0;
            index = 0;
            upStart = 0;
            nextPinc3 = 850;
            nextPinc4 = 425;
            nextPinc1 = 638;
            nextPinc5 = 212;
            nextDinc6 = 53;
            upEnd = 0;
        }

        public int getMct()
        {
            return mct;
        }

        public String getDebug()
        {
            String ret;
            ret = debug;
            debug = "";
            return ret;
        }

        public Boolean getIE()
        {
            return ie;
        }

        public Boolean getInhint()
        {
            return inhint;
        }

        public int getSentPort()
        {
            return sentPort;
        }

        public void setDebugMode(Boolean b)
        {
            debugMode = b;
            debug = "";
        }

        public int[] getRam()
        {
            return ram;
        }

        public int[] getRom()
        {
            return rom;
        }

        public int[] getPorts()
        {
            return port;
        }

        private String toHex(int i)
        {
            int t;
            String ret;
            ret = "";
            t = i / 4096;
            i -= t * 4096;
            if (t < 10) ret += t.ToString(); else ret += Convert.ToChar(t - 10 + 'A').ToString();
            t = i / 256;
            i -= t * 256;
            if (t < 10) ret += t.ToString(); else ret += Convert.ToChar(t - 10 + 'A').ToString();
            t = i / 16;
            i -= t * 16;
            if (t < 10) ret += t.ToString(); else ret += Convert.ToChar(t - 10 + 'A').ToString();
            if (i < 10) ret += i.ToString(); else ret += Convert.ToChar(i - 10 + 'A').ToString();
            return ret;
        }

        private int readMem(int address)
        {
            int bank;
            int value;
            value = 0;
            if (address == 7) return 0;
            if (address <= 0x3ff)
            {
                if (address >= 0x300 && address <= 0x3ff)
                {
                    address = (address & 0xff) | (ram[REG_EB] & 0x700);
                }
                value = ram[address];
            }
            if (address >= 0x400 && address <= 0x7ff)
            {
                bank = (ram[REG_FB] >> 10) & 0x1f;
                if (bank > 0x18 && (port[IO_FEB] & 0x80) == 0x80) bank += 8;
                address = address | (bank << 10);
                value = rom[address];
            }
            if (address >= 0x800 && address <= 0xfff)
            {
                value = rom[address];
            }
            if (address > 2) value |= ((value & 0x4000) << 1);
            return value;
        }

        private void writeMem(int address, int value)
        {
            int tmp;
            if (address > 2) value = overflowCorrect(value);
            if (address == 7) return;
            if (address <= 0x3ff)
            {
                if (address >= 0x300 && address <= 0x3ff)
                {
                    address = (address & 0xff) | (ram[REG_EB] & 0x700);
                }
                if (address == REG_CYR)
                {
                    tmp = (value & 1) << 14;
                    value = ((value >> 1) & 0x3fff) | tmp;
                }
                if (address == REG_SR)
                {
                    tmp = value & 0x4000;
                    value = ((value >> 1) & 0x3fff) | tmp;
                }
                if (address == REG_CYL)
                {
                    tmp = (value >> 14) & 1;
                    tmp = ((value << 1) & 0x7ffe) | tmp;
                    value = tmp;
                }
                if (address == REG_EDOP)
                {
                    value = ((value >> 7) & 0x7f);
                }
                if (address == REG_EB)
                {
                    ram[REG_BB] = (ram[REG_BB] & 0x7c00) | ((value >> 8) & 0x7);
                }
                if (address == REG_FB)
                {
                    ram[REG_BB] = (ram[REG_BB] & 0x3ff) | (value & 0x7c00);
                }
                if (address == REG_BB)
                {
                    ram[REG_EB] = (value << 8) & 0x7ff;
                    ram[REG_FB] = (value & 0x7c00);
                }
                ram[address] = value;
                if (address == 1 || address == 2) port[address] = value;
            }
        }

        private int inPort(int p)
        {
            int value;
            value = port[p];
            if (p > 2) value |= ((value & 0x4000) << 1);
            return value;
        }

        private void outPort(int p, int value)
        {
            if (p > 2) value = overflowCorrect(value);
            port[p] = value;
            if (p == 1 || p == 2) ram[p] = value;
            sentPort = p;
        }

        private int overflowCorrect(int value)
        {
            if ((value & 0x8000) == (value & 0x4000)) return (value & 0x7fff);
            if ((value & 0x8000) == 0)
            {
                value &= 0x3fff;
                return value;
            }
            value &= 0x3fff;
            value |= 0x4000;
            return value;
        }

        private int convertDP(int value1, int value2)
        {
            int ret;
            value1 = ((value1 & 0x4000) == 0x4000) ? -(((value1 ^ 0x7fff) & 0x3fff) << 14) : (value1 & 0x3fff) << 14;
            value2 = ((value2 & 0x4000) == 0x4000) ? -((value2 ^ 0x7fff) & 0x3fff) : value2 & 0x3fff;
            ret = value1 + value2;
            return ret;
        }

        private int add(int value1, int value2, int carry)
        {
            int sign;
            Boolean overflow;
            overflow = (value1 & 0x4000) == (value2 & 0x4000);
            sign = value1 & 0x8000;
            value1 = (value1 & 0x7fff) + (value2 & 0x7fff) + carry;
            if ((value1 & 0x8000) == 0x8000) value1++;
            value1 = (value1 & 0x7fff) | sign;
            if (!overflow)
            {
                value1 &= 0x7fff;
                value1 |= ((value1 & 0x4000) << 1);
            }
            return value1;
        }

        private void do_ad(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0xfff;
            value1 = ram[REG_A];
            value2 = readMem(addr);
            value1 = add(value1, value2, 0);
            writeMem(addr, value2);
            writeMem(REG_A, value1);
            if (debugMode) debug += "AD " + toHex(addr) + ", A = " + toHex(ram[REG_A]);
            if (debugMode && addr < 2048) debug += ", [" + toHex(addr) + "] = " + toHex(ram[addr]);
            mct = 2;
   }

        private void do_ads(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x3ff;
            value1 = ram[REG_A];
            value2 = readMem(addr);
            value1 = add(value1, value2, 0);
            writeMem(REG_A, value1);
            writeMem(addr, value1);
            if (debugMode) debug += "ADS " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", ["+toHex(addr)+"] = " + toHex(ram[addr]);
        }

        private void do_aug(int inst)
        {
            int value;
            int addr;
            addr = inst & 0x3ff;
            value = readMem(addr);
            value += ((value &0x4000) == 0x4000) ? -1 : 1;
            writeMem(addr, value);
            if (debugMode) debug += "AUG " + toHex(addr) + ", [" + toHex(addr) + "] = " + toHex(ram[addr]);
            mct = 2;
        }

        private void do_bzmf(int inst)
        {
            int addr;
            addr = inst & 0xfff;
            if (debugMode) debug += "BZMF " + toHex(addr);
            if (((ram[REG_A] & 0x8000) == 0x8000) || ((ram[REG_A] & 0xffff) == 0x0000))
            {
                ram[REG_Z] = addr;
                if (debugMode) debug += ", Z = " + toHex(ram[REG_Z]);
                mct = 1;
            }
            else
            {
                if (debugMode) debug += ", Z = " + toHex(ram[REG_Z]);
                mct = 2;
            }
        }

        private void do_bzf(int inst)
        {
            int addr;
            addr = inst & 0xfff;
            if (debugMode) debug += "BZF " + toHex(addr);
            if (((ram[REG_A] & 0xffff) == 0xffff) || ((ram[REG_A] & 0xffff) == 0x0000))
            {
                ram[REG_Z] = addr;
                if (debugMode) debug += ", Z = " + toHex(ram[REG_Z]);
                mct = 1;
            }
            else
            {
                if (debugMode) debug += ", Z = " + toHex(ram[REG_Z]);
                mct = 2;
            }
        }

        private void do_ca(int inst)
        {
            int addr;
            int value;
            addr = inst & 0xfff;
            value = readMem(addr);
            writeMem(addr, value);
            writeMem(REG_A, value);
            if (debugMode) debug += "CA " + toHex(addr) + ", A = " + toHex(ram[REG_A]);
            if (debugMode && addr < 2048) debug += ", [" + toHex(addr) + "] = " + toHex(ram[addr]);
            mct = 2;
        }

        private void do_ccs(int inst)
        {
            int addr;
            int value;
            addr = inst & 0x3ff;
            value = readMem(addr);
            writeMem(addr,value);
            if (value == 0) ram[REG_Z] += 1;
            else if ((value & 0xc000) == 0x8000) { ram[REG_Z] += 2; value = 0; }
            else if ((value & 0xc000) == 0x4000) { ram[REG_Z] += 0; value = 0; }
            else if (value == 0xffff) ram[REG_Z] += 3;
            else if (value >= 0x4000) ram[REG_Z] += 2;
            if ((value & 0x8000) == 0x8000) value = ((value & 0x3fff) ^ 0x3fff);
            if (value > 0) value--;
            ram[REG_A] = value;
            if (debugMode) debug += "CCS " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", Z = " + toHex(ram[REG_Z]) + ", [" + toHex(addr) + "] = " + toHex(readMem(addr));
            mct = 2;
        }

        private void do_cs(int inst)
        {
            int addr;
            int value;
            addr = inst & 0xfff;
            value = readMem(addr);
            writeMem(addr, value);
            value = value ^ 0xffff;
            writeMem(REG_A, value);
            if (debugMode) debug += "CA " + toHex(addr) + ", A = " + toHex(ram[REG_A]);
            if (debugMode && addr < 2048) debug += ", [" + toHex(addr) + "] = " + toHex(ram[addr]);
            mct = 2;
        }

        private void do_das(int inst)
        {
            int addr;
            int value1;
            int value2;
            int carry;
            addr = (inst & 0x3ff) - 1;
            value1 = ram[REG_L];
            value2 = readMem(addr+1);
            value1 = add(value1, value2, 0);
            writeMem(addr+1, value1);
            carry = 0;
            if ((value1 & 0xc000) == 0x4000) carry = 1;
            if ((value1 & 0xc000) == 0x8000) carry = -1;
            value1 = ram[REG_A];
            value2 = readMem(addr);
            value1 = add(value1, value2, carry);
            writeMem(addr, value1);
            ram[REG_L] = 0x0000;
            if ((value1 & 0xc000) == 0x4000) ram[REG_A] = 0x0001;
            else if ((value1 & 0xc000) == 0x8000) ram[REG_A] = 0xfffe;
            else ram[REG_A] = 0x0000;
            if (debugMode) debug += "DAS " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", L = " + toHex(ram[REG_L]) + ", [" + toHex(addr) + "] = " + toHex(readMem(addr)) + ", [" + toHex(addr + 1) + "] = " + toHex(readMem(addr + 1));
            mct = 3;
        }

        private void do_dca(int inst)
        {
            int addr;
            int value;
            addr = (inst & 0xfff) - 1;
            value = readMem(addr + 1);
            writeMem(addr + 1, value);
            writeMem(REG_L, value);
            value = readMem(addr);
            writeMem(REG_A, value);
            writeMem(addr, value);
            value = overflowCorrect(readMem(REG_L));
            value |= ((value & 0x4000) << 1);
            writeMem(REG_L, value);
            if (debugMode) debug += "DCA " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", L = " + toHex(ram[REG_L]);
            mct = 3;
        }

        private void do_dcs(int inst)
        {
            int addr;
            int value;
            addr = (inst & 0xfff) - 1;
            value = readMem(addr + 1);
            writeMem(addr + 1, value);
            writeMem(REG_L, value ^ 0xffff);
            value = readMem(addr);
            writeMem(REG_A, value ^ 0xffff);
            writeMem(addr, value);
            value = overflowCorrect(readMem(REG_L));
            value |= ((value & 0x4000) << 1);
            writeMem(REG_L, value);
            if (debugMode) debug += "DCS " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", L = " + toHex(ram[REG_L]);
            mct = 3;
        }

        private void do_dim(int inst)
        {
            int addr;
            int value1;
            addr = inst & 0x3ff;
            value1 = readMem(addr);
            if (value1 != 0 && value1 != 0xffff)
            {
                if (value1 == 1) value1 = 0xffff;
                else if ((value1 & 0x8000) == 0x8000) value1++;
                else value1--;
            }
            writeMem(addr, value1);
            if (debugMode) debug += "DIM " + toHex(addr) + ", [" + toHex(addr) + "] = " + toHex(ram[REG_A]);
            mct = 2;
        }

        private void do_dv(int inst)
        {
            int addr;
            int dividend;
            int divisor;
            int sign;
            int a, l;
            int quotient;
            int remainder;
            quotient = 0;
            remainder = 0;
            addr = inst & 0xfff;
            a = overflowCorrect(readMem(REG_A));
            l = overflowCorrect(readMem(REG_L));
            dividend = convertDP(a, l);
            divisor = overflowCorrect(readMem(addr));
            sign = 0;
            if (dividend < 0)
            {
                sign |= 2;
                dividend = -dividend;
            }
            if (dividend == 0 && (a & 0x4000) == 0x4000)
            {
                sign |= 2;
            }
            if ((divisor & 0x4000) == 0x4000)
            {
                sign |= 1;
                divisor = (divisor ^ 0x7fff) & 0x3fff;
            }
            if (dividend == divisor)
            {
                quotient = 0x3fff;
                if ((sign & 1) != ((sign & 2) >> 1)) quotient ^= 0xffff;
                remainder = a;
            }
            else
            {
                if (divisor != 0)
                {
                    quotient = dividend / divisor;
                    remainder = dividend % divisor;
                    if ((sign & 1) != ((sign & 2) >> 1)) quotient ^= 0xffff;
                    if ((sign & 2) == 2) remainder ^= 0xffff;
                }
            }
            quotient |= ((quotient & 0x4000) << 1);
            remainder |= ((remainder & 0x4000) << 1);
            writeMem(REG_A, quotient);
            writeMem(REG_L, remainder);
            if (debugMode) debug += "DV " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", L = " + toHex(ram[REG_L]);
            mct = 6;
        }

        private void do_dxch(int inst)
        {
            int addr;
            int value1a, value2a;
            int value1b, value2b;
            addr = (inst-1) & 0x3ff;
            value1a = readMem(addr);
            value1b = readMem(addr + 1);
            value2a = ram[REG_A];
            value2b = ram[REG_L];
            if (addr != 1)
            {
                writeMem(addr, value2a);
                writeMem(addr + 1, value2b);
                writeMem(REG_A, value1a);
                writeMem(REG_L, value1b);
            }
            else
            {
                writeMem(REG_L, value2a);
                writeMem(REG_A, value1b);
                writeMem(REG_Q, value2b);
            }
            if (debugMode) debug += "DXCH " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", L = " + toHex(ram[REG_L]) + ", [" + toHex(addr) + "] = " + toHex(readMem(addr)) + ", [" + toHex(addr+1) + "] = " + toHex(readMem(addr+1));
            mct = 3;
        }

        private void do_edrupt(int inst)
        {
            ie = false;
            writeMem(REG_ZRUPT, readMem(REG_Z));
            writeMem(REG_Z, 0);
        }

        private void do_extend()
        {
            extend = true;
            if (debugMode) debug += "EXTEND";
            mct = 1;
        }

        private void do_incr(int inst)
        {
            int addr;
            int value;
            addr = inst & 0x3ff;
            value = readMem(addr);
            if (value == 0xffff) value = 1; else value++;
            writeMem(addr, value);
            if (debugMode) debug += "INCR " + toHex(addr) + ", [" + toHex(addr) + "] = " + toHex(readMem(addr));
            mct = 2;
        }

        private void do_index(int inst, char mode)
        {
            int addr;
            addr = (mode == 'B') ? inst & 0x3ff : inst & 0xfff;
            index = readMem(addr);
            if (addr < 3) index = overflowCorrect(index);
            if (addr > 2) writeMem(addr, index);
            index &= 0x7fff;
            if (debugMode) debug += "INDEX " + toHex(addr);
            mct = 2;
        }

        private void do_inhint()
        {
            inhint = true;
            if (debugMode) debug += "INHINT";
            mct = 1;
        }

        private void do_lxch(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x3ff;
            value1 = readMem(addr);
            value2 = readMem(REG_L);
            writeMem(addr, value2);
            writeMem(REG_L, value1);
            if (debugMode) debug += "LXCH " + toHex(addr) + ", L = " + toHex(ram[REG_L]) + ", [" + toHex(addr) + "] = " + toHex(readMem(addr));
            mct = 2;
        }

        private void do_mask(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0xfff;
            value2 = readMem(addr);
            value1 = readMem(REG_A);
            if (addr > 2) value1 = overflowCorrect(value1);
            value1 &= value2;
            if (addr > 2)
            {
                value1 &= 0x7fff;
                value1 |= ((value1 & 0x4000) << 1);
            }
            writeMem(REG_A, value1);
            if (debugMode) debug += "MASK " + toHex(addr) + ", A = " + toHex(ram[REG_A]);
            mct = 2;
        }

        private void do_mp(int inst)
        {
            int addr;
            int value1;
            int value2;
            int sign;
            addr = inst & 0xfff;
            value1 = overflowCorrect(readMem(REG_A));
            value2 = overflowCorrect(readMem(addr));
            sign = 0;
            if ((value1 & 0x4000) == 0x4000)
            {
                sign |= 1;
                value1 ^= 0x7fff;
            }
            if ((value2 & 0x4000) == 0x4000)
            {
                sign |= 2;
                value2 ^= 0x7fff;
            }
            if (value2 == 0) sign = 0;
            if (value1 == 0)
            {
                sign = ((sign  & 1) == ((sign & 2) >> 1)) ? 0 : 1;
            }
            value1 *= value2;
            value1 <<= 1;
            value2 = value1 & 0x7fff;
            value1 >>= 15;
            value1 &= 0x7fff;
            if (sign == 1 || sign == 2)
            {
                value1 ^= 0x7fff;
                value2 ^= 0x7fff;
            }
            value1 |= ((value1 & 0x4000) << 1);
            value2 |= ((value2 & 0x4000) << 1);
            writeMem(REG_A, value1);
            writeMem(REG_L, value2);
            if (debugMode) debug += "MP " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", L = " + toHex(ram[REG_L]);
            mct = 3;
        }

        private void do_msu(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x3ff;
            value1 = ram[REG_A] & 0xffff;
            value2 = readMem(addr) & 0xffff;
            if (addr > 2) value2 &= 0x7fff;
            value1 -= value2;
            if (value1 <= 0) value1 = (-value1) ^ 0xffff;
            value1 &= 0x7fff;
            value1 |= ((value1 & 0x4000) << 1);
            ram[REG_A] = value1;
            writeMem(addr, value2);
            if (debugMode) debug += "MSU " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", [" + toHex(addr) + "] = " + toHex(readMem(addr));
            mct = 2;
        }

        private void do_qxch(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x3ff;
            value1 = readMem(addr);
            value2 = readMem(REG_Q);
            writeMem(addr, value2);
            writeMem(REG_Q, value1);
            if (debugMode) debug += "QXCH " + toHex(addr) + ", Q = " + toHex(ram[REG_Q]) + ", [" + toHex(addr) + "] = " + toHex(readMem(addr));
            mct = 2;
        }

        private void do_rand(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x1ff;
            value2 = inPort(addr);
            value1 = readMem(REG_A);
            if (addr > 2) value1 = overflowCorrect(value1);
            value1 &= value2;
            if (addr > 2)
            {
                value1 &= 0x7fff;
                value1 |= ((value1 & 0x4000) << 1);
            }
            writeMem(REG_A, value1);
            mct = 2;
            if (debugMode) debug += "RAND " + toHex(addr) + ", A = " + toHex(ram[REG_A]);
        }

        private void do_read(int inst)
        {
            int addr;
            int value;
            addr = inst & 0x1ff;
            value = inPort(addr);
            writeMem(REG_A, value);
            if (debugMode) debug += "READ " + toHex(addr) + ", A = " + toHex(ram[REG_A]);
            mct = 2;
        }

        private void do_relint()
        {
            inhint = false;
            if (debugMode) debug += "RELINT";
            mct = 1;
        }

        private void do_resume()
        {
            writeMem(REG_Z, readMem(REG_ZRUPT));
            execFromBrupt = true;
            ie = true;
            if (debugMode) debug += "RESUME , Z = " + toHex(ram[REG_Z]);
            mct = 2;
        }

        private void do_ror(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x1ff;
            value2 = inPort(addr);
            value1 = readMem(REG_A);
            if (addr > 2) value1 = overflowCorrect(value1);
            value1 |= value2;
            if (addr > 2)
            {
                value1 &= 0x7fff;
                value1 |= ((value1 & 0x4000) << 1);
            }
            writeMem(REG_A, value1);
            if (debugMode) debug += "ROR " + toHex(addr) + ", A = " + toHex(ram[REG_A]);
            mct = 2;
        }

        private void do_rxor(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x1ff;
            value2 = inPort(addr);
            value1 = readMem(REG_A);
            if (addr > 2) value1 = overflowCorrect(value1);
            value1 ^= value2;
            if (addr > 2)
            {
                value1 &= 0x7fff;
                value1 |= ((value1 & 0x4000) << 1);
            }
            writeMem(REG_A, value1);
            if (debugMode) debug += "RXOR " + toHex(addr) + ", A = " + toHex(ram[REG_A]);
            mct = 2;
        }

        private void do_su(int inst)
        {
            int addr;
            int value1;
            int value2;
            int sign;
            Boolean overflow;
            addr = inst & 0xfff;
            value1 = ram[REG_A];
            value2 = readMem(addr);
            overflow = (value1 & 0x4000) != (value2 & 0x4000);
            writeMem(addr, value2);
            sign = value1 & 0x8000;
            value2 = value2 ^ 0xffff;
            value1 = (value1 & 0x7fff) + (value2 & 0x7fff);
            if ((value1 & 0x8000) == 0x8000) value1++;
            value1 = (value1 & 0x7fff) | sign;
            if (!overflow)
            {
                value1 &= 0x7fff;
                value1 |= ((value1 & 0x4000) << 1);
            }
            writeMem(REG_A, value1);
            if (debugMode) debug += "SU " + toHex(addr) + ", A = " + toHex(ram[REG_A]);
            if (debugMode && addr < 2048) debug += ", [" + toHex(addr) + "] = " + toHex(ram[addr]);
            mct = 2;
        }

        private void do_tc(int inst)
        {
            int addr;
            addr = inst & 0xfff;
            if (addr != 2) writeMem(REG_Q,readMem(REG_Z));
            writeMem(REG_Z,addr);
            if (debugMode) debug += "TC " + toHex(addr) + ", Z = " + toHex(addr);
            mct = 1;
        }

        private void do_tcf(int inst)
        {
            int addr;
            addr = inst & 0xfff;
            writeMem(REG_Z, addr);
            if (debugMode) debug += "TCF " + toHex(addr) + ", Z = " + toHex(addr);
            mct = 1;
        }

        private void do_ts(int inst)
        {
            int addr;
            int value;
            addr = inst & 0x3ff;
            value = readMem(REG_A);
            if (addr != 0)
            {
                writeMem(addr, value);
                if ((value & 0xc000) == 0x4000) writeMem(REG_A, 1);
                else if ((value & 0xc000) == 0x8000) writeMem(REG_A, 0xffff);
            }
            if ((value & 0xc000) == 0x4000 || (value & 0xc000) == 0x8000) ram[REG_Z]++;
            if (debugMode) debug += "TS " + toHex(addr) + ", ["+toHex(addr)+"] = " + toHex(ram[addr]);
            if ((value & 0xc000) == 0x4000 || (value & 0xc000) == 0x8000) if (debugMode) debug += ", Skip";
            mct = 2;
        }

        private void do_wand(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x1ff;
            value2 = inPort(addr);
            value1 = readMem(REG_A);
            if (addr > 2) value1 = overflowCorrect(value1);
            value1 &= value2;
            if (addr > 2)
            {
                value1 &= 0x7fff;
                value1 |= ((value1 & 0x4000) << 1);
            }
            writeMem(REG_A, value1);
            outPort(addr, value1);
            if (debugMode) debug += "WAND " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", p[" + addr + "] = " + toHex(inPort(addr));
            mct = 2;
        }

        private void do_wor(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x1ff;
            value2 = inPort(addr);
            value1 = readMem(REG_A);
            if (addr > 2) value1 = overflowCorrect(value1);
            value1 |= value2;
            if (addr > 2)
            {
                value1 &= 0x7fff;
                value1 |= ((value1 & 0x4000) << 1);
            }
            writeMem(REG_A, value1);
            outPort(addr, value1);
            if (debugMode) debug += "WOR " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", p[" + addr + "] = " + toHex(inPort(addr));
            mct = 2;
        }

        private void do_write(int inst)
        {
            int addr;
            int value;
            addr = inst & 0x1ff;
            value = readMem(REG_A);
            outPort(addr, value);
            if (debugMode) debug += "WRITE " + toHex(addr) + ", p[" + toHex(addr) + "] = " + toHex(port[addr]);
            mct = 2;
        }

        private void do_xch(int inst)
        {
            int addr;
            int value1;
            int value2;
            addr = inst & 0x3ff;
            value1 = readMem(REG_A);
            value2 = readMem(addr);
            writeMem(addr, value1);
            writeMem(REG_A, value2);
            if (debugMode) debug += "XCH " + toHex(addr) + ", A = " + toHex(ram[REG_A]) + ", [" + toHex(addr) + "] = " + toHex(readMem(addr));
            mct = 2;
        }

        public void addUpSequence(int code)
        {
            upQueue[upEnd++] = code;
            if (upEnd >= UP_QUEUE_SIZE) upEnd = 0;
            if (upEnd == upStart)
            {
                upStart++;
                if (upStart >= UP_QUEUE_SIZE) upStart = 0;
            }
        }

        public Boolean checkUpSequence()
        {
            if (upStart == upEnd) return false;
            switch (upQueue[upStart])
            {
                case PINC_1:
                    ram[REG_TIME1]++;
                    if (ram[REG_TIME1] >= 0x4000)
                    {
                        ram[REG_TIME1] = 0;
                        ram[REG_TIME2]++;
                        if (ram[REG_TIME2] > 0x2000)
                        {
                            ram[REG_TIME2] = 0;
                            ram[REG_TIME1] = 0;
                        }
                    }
                    break;
                case PINC_3:
                    ram[REG_TIME3] = (ram[REG_TIME3]+1) & 0x7fff;
                    if (ram[REG_TIME3] == 0x4000)
                    {
                        interrupts |= INT_T3RUPT;
                        ram[REG_TIME3] = 0;
                    }
                    break;
                case PINC_4:
                    ram[REG_TIME4] = (ram[REG_TIME4]+1) & 0x7fff;
                    if (ram[REG_TIME4] == 0x4000)
                    {
                        interrupts |= INT_T4RUPT;
                        ram[REG_TIME4] = 0;
                    }
                    break;
                case PINC_5:
                    ram[REG_TIME5] = (ram[REG_TIME5]+1) & 0x7fff;
                    if (ram[REG_TIME5] == 0x4000)
                    {
                        interrupts |= INT_T5RUPT;
                        ram[REG_TIME5] = 0;
                    }
                    break;
                case DINC_6:
                    if ((port[0x11] & 0x4000) == 0x4000)
                    {
                        if ((ram[REG_TIME6] & 0x4000) != 0x4000)
                        {
                            if (ram[REG_TIME6] != 0)
                            {
                                ram[REG_TIME6]--;
                                if (ram[REG_TIME6] == 0)
                                {
                                    interrupts |= INT_T6RUPT;
                                    port[0x11] ^= 0x4000;
                                }
                            }
                        }
                        else
                        {
                            if (ram[REG_TIME6] != 0x7fff)
                            {
                                ram[REG_TIME6]++;
                                if (ram[REG_TIME6] == 0x7fff)
                                {
                                    interrupts |= INT_T6RUPT;
                                    port[0x11] ^= 0x4000;
                                }
                            }
                        }
                    }
                    break;
            }
            upStart++;
            if (upStart >= UP_QUEUE_SIZE) upStart = 0;
            return true;
        }

        public void dskyIntr()
        {
            interrupts |= INT_DSKY;
        }

        private int checkForInterrupts()
        {
            if (ie == false || inhint == true || interrupts == 0 || extend || index != 0) return -1;
            if (((ram[REG_A] & 0x8000) >> 1) != (ram[REG_A] & 0x4000)) return -1;
            if ((interrupts & INT_T6RUPT) == INT_T6RUPT)
            {
                interrupts ^= INT_T6RUPT;
                return 0x804;
            }
            if ((interrupts & INT_T5RUPT) == INT_T5RUPT)
            {
                interrupts ^= INT_T5RUPT;
                return 0x808;
            }
            if ((interrupts & INT_T3RUPT) == INT_T3RUPT)
            {
                interrupts ^= INT_T3RUPT;
                return 0x80c;
            }
            if ((interrupts & INT_T4RUPT) == INT_T4RUPT)
            {
                interrupts ^= INT_T4RUPT;
                return 0x810;
            }
            if ((interrupts & INT_DSKY) == INT_DSKY)
            {
                interrupts ^= INT_DSKY;
                return 0x814;
            }
            return -1;
        }

        public void cycle()
        {
            int inst;
            int code;
            int qc;
            int pc;
            int intr;
            sentPort = -1;
            if (nextPinc1-- <= 0)
            {
                addUpSequence(PINC_1);
                nextPinc1 = 850;
            }
            if (nextPinc3-- <= 0)
            {
                addUpSequence(PINC_3);
                nextPinc3 = 850;
            }
            if (nextPinc4-- <= 0)
            {
                addUpSequence(PINC_4);
                nextPinc4 = 850;
            }
            if (nextPinc5-- <= 0)
            {
                addUpSequence(PINC_5);
                nextPinc5 = 850;
            }
            if ((port[11] & 0x4000) == 0x4000 && nextDinc6-- <= 0)
            {
                addUpSequence(DINC_6);
                nextDinc6 = 53;
            }
            if (mct > 0)
            {
                mct--;
                return;
            }
            mct = 0;
            if (checkUpSequence()) return;
            inst = (execFromBrupt) ? ram[REG_BRUPT] & 0x7fff : readMem(ram[REG_Z]) & 0x7fff;
            execFromBrupt = false;
            inst += index;
            index = 0;
            intr = checkForInterrupts();
            if (intr >= 0)
            {
                ram[REG_ZRUPT] = ram[REG_Z];
                ram[REG_BRUPT] = inst;
                ram[REG_Z] = intr;
                ie = false;
                mct = 0;
                return;
            }
            if (debugMode) debug += toHex(ram[REG_Z]) + ": " + toHex(inst) + "  ";
            ram[REG_Z]++;
            if (ram[REG_Z] > 0xfff) ram[REG_Z] = 0x000;

            code = (inst >> 12) & 0x7;
            qc = (inst >> 10) & 0x3;
            pc = (inst >> 9) & 0x7;
            if (extend == false && inst == 6) do_extend();
            else if (extend == false && inst == 3) do_relint();
            else if (extend == false && inst == 4) do_inhint();
            else if (extend == false && inst == 0x500f) do_resume();
            else if (!extend)
            {
                switch (code)
                {
                    case 0: do_tc(inst); break;
                    case 1: if (qc == 0) do_ccs(inst); else do_tcf(inst);
                        break;
                    case 2: switch (qc)
                        {
                            case 0: do_das(inst); break;
                            case 1: do_lxch(inst); break;
                            case 2: do_incr(inst); break;
                            case 3: do_ads(inst); break;
                        }
                        break;
                    case 3: do_ca(inst); break;
                    case 4: do_cs(inst); break;
                    case 5: switch (qc)
                        {
                            case 0: do_index(inst,'B'); break;
                            case 1: do_dxch(inst); break;
                            case 2: do_ts(inst); break;
                            case 3: do_xch(inst); break;
                        }
                        break;
                    case 6: do_ad(inst); break;
                    case 7: do_mask(inst); break;
                }
            }
            else
            {
                switch (code)
                {
                    case 0: switch (pc)
                        {
                            case 0: do_read(inst); break;
                            case 1: do_write(inst); break;
                            case 2: do_rand(inst); break;
                            case 3: do_wand(inst); break;
                            case 4: do_ror(inst); break;
                            case 5: do_wor(inst); break;
                            case 6: do_rxor(inst); break;
                            case 7: do_edrupt(inst); break;
                        }
                        break;
                    case 1: if (qc == 0) do_dv(inst); else do_bzf(inst);
                        break;
                    case 2: switch (qc)
                        {
                            case 0: do_msu(inst); break;
                            case 1: do_qxch(inst); break;
                            case 2: do_aug(inst); break;
                            case 3: do_dim(inst); break;
                        }
                        break;
                    case 3: do_dca(inst); break;
                    case 4: do_dcs(inst); break;
                    case 5: do_index(inst,'E'); break;
                    case 6: if (qc == 0) do_su(inst); else do_bzmf(inst);
                        break;
                    case 7: do_mp(inst); break;
                }
                if (code != 5) extend = false;
            }
            mct--;
            if (debugMode) debug += "\r\n";
        }
    }
}
