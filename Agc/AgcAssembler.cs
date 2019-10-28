using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace Agc
{
    class AgcAssembler
    {
        private const int F_FIXED = 1;      // Item must be in fixed memory
        private const int F_EXTEND = 2;     // Requires extend command
        private const int F_ERASE = 4;      // Item must be in eraseable memory
        private const int F_NOARG = 8;      // No argument 
        private const int F_NOOP = 16;      // special handling for noop instruction
        private const int F_TC = 32;        // Special handling for TC exceptions
        private const int F_POP = 64;       // indicates a pseudo-op
        private const int F_NOCODE = 128;   // Generates no code

        private const int POP_BANK = 1;
        private const int POP_DEC = 2;
        private const int POP_EBANK = 3;
        private const int POP_EQ = 4;
        private const int POP_EQUALS = 5;
        private const int POP_SETLOC = 6;
        private const int POP_FCADR = 7;
        private const int POP_ERASE = 8;
        private const int POP_2DEC = 9;
        private const int POP_BCADR = 10;
        private const int POP_OCT = 11;
        private const int POP_HEX = 12;

        private ArrayList patterns;
        private ArrayList opcodes;
        private ArrayList masks;
        private ArrayList flags;
        private ArrayList labels;
        private ArrayList labelValues;
        private ArrayList labelMode;
        private CPU cpu;
        private int[] ram;
        private int[] rom;
        private int[] banks;
        private int[] ebanks;
        private int pass;
        private String[] source;
        private String results;
        private int address;
        private String label;
        private String opcode;
        private String arg;
        private int lastEbank;
        private int lastBank;
        private Boolean assembleFixed;
        private int errorCount;
        private int erasableUsed;
        private int fixedUsed;
        private Boolean showBankUsage;
        private int dfraction;
        private int labelAddress;
        private char numType;

        public AgcAssembler(CPU c)
        {
            cpu = c;
            patterns = new ArrayList();
            opcodes = new ArrayList();
            masks = new ArrayList();
            flags = new ArrayList();
            labels = new ArrayList();
            labelValues = new ArrayList();
            labelMode = new ArrayList();
            showBankUsage = false;
            init();
        }

        public void setShowBankUsage(Boolean b)
        {
            showBankUsage = b;
        }

        public void setSource(String[] src)
        {
            source = src;
        }

        private void addMnemonic(String m, int opcode, int mask, int f)
        {
            patterns.Add(m);
            opcodes.Add(opcode);
            masks.Add(mask);
            flags.Add(f);
        }

        private int findOpcode(String m)
        {
            int i;
            for (i = 0; i < patterns.Count; i++)
                if (m.CompareTo(patterns[i]) == 0) return i;
            return -1;
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

        private int validHexNumber(String m)
        {
            int i;
            int value;
            m = m.Trim();
            if (m.Length < 3) return -1;
            if (m[0] != '0' || (m[1] != 'X' && m[1] != 'x')) return -1;
            numType = 'H';
            value = 0;
            for (i = 2; i < m.Length; i++)
            {
                value <<= 4;
                if (m[i] >= '0' && m[i] <= '9') value += (m[i] - '0');
                if (m[i] >= 'A' && m[i] <= 'F') value += (m[i] - 'A' + 10);
                if (m[i] >= 'a' && m[i] <= 'f') value += (m[i] - 'a' + 10);
            }
            return value;
        }

        private int validOctalNumber(String m)
        {
            int i;
            Boolean valid;
            int value;
            int start;
            valid = true;
            m = m.Trim();
            for (i = 0; i < m.Length; i++)
            {
                if (m[i] < '0' || m[i] > '7') valid = false;
                if (i == 0 && m[i] == '+') valid = true;
                if (i == 0 && m[i] == '-') valid = true;
            }
            if (m[0] != '+' && m[0] != '-' && m[0] != '0') valid = false;
            if ((m[0] == '+' || m[0] == '-') && m[1] != '0') valid = false;
            if (valid == false) return -1;
            numType = 'O';
            value = 0;
            start = (m[0] == '+' || m[0] == '-') ? 1 : 0;
            for (i = start; i < m.Length; i++)
            {
                value <<= 3;
                value += (m[i] - '0');
            }
            if (m[0] == '-') value ^= 0x7fff;
            return value;
        }

        private int validDecimalNumber(String m)
        {
            int i;
            Boolean valid;
            int value;
            valid = true;
            m = m.Trim();
            for (i = 0; i < m.Length; i++)
            {
                if (m[i] < '0' || m[i] > '9') valid = false;
                if (i == 0 && m[i] == '+') valid = true;
                if (i == 0 && m[i] == '-') valid = true;
            }
            if (valid == false) return -1;
            numType = 'D';
            if (m[0] == '+')
            {
                dfraction = Convert.ToInt16(m.Substring(1));
                return Convert.ToInt32(m.Substring(1));
            }
            if (m[0] == '-')
            {
                value = Convert.ToInt32(m.Substring(1));
                dfraction = value;
                value ^= 0x7fff;
                dfraction ^= 0x1fffffff;
                return value;
            }
            dfraction = Convert.ToInt32(m);
            return Convert.ToInt32(m);
        }

        private int ConvertToDecimal(String num)
        {
            double v;
            int mask;
            int dmask;
            int result;
            v = Convert.ToDouble(num);
            v -= (int)v;
            mask = 1 << 13;
            dmask = 1 << 27;
            result = 0;
            dfraction = 0;
            while (dmask > 0)
            {
                v *= 2;
                if (v >= 1.0)
                {
                    if (mask > 0) result |= mask;
                    dfraction |= dmask;
                }
                mask >>= 1;
                dmask >>= 1;
                v -= (int)v;
            }
            return result;
        }

        private int validDecimalFraction(String m)
        {
            int i;
            Boolean valid;
            int value;
            int dots;
            valid = true;
            dots = 0;
            m = m.Trim();
            for (i = 0; i < m.Length; i++)
            {
                if (m[i] < '0' || m[i] > '9') valid = false;
                if (i == 0 && m[i] == '+') valid = true;
                if (i == 0 && m[i] == '-') valid = true;
                if (m[i] == '.') { valid = true; dots++; }
            }
            if (valid == false || dots > 1 || dots == 0) return -1;
            numType = 'F';
            if (m[0] == '+')
            {
                return ConvertToDecimal(m.Substring(1));
            }
            if (m[0] == '-')
            {
                value = ConvertToDecimal(m.Substring(1));
                value ^= 0x7fff;
                dfraction ^= 0x1fffffff;
                return value;
            }
            return ConvertToDecimal(m);
        }

        private int convertToNumber(String m)
        {
            int value;
            value = validDecimalFraction(m);
            if (value >= 0) return value;
            value = validHexNumber(m);
            if (value >= 0) return value;
            value = validOctalNumber(m);
            if (value >= 0) return value;
            value = validDecimalNumber(m);
            if (value >= 0) return value;
            return -1;
        }

        private int findLabel(String m)
        {
            int i;
            int pos;
            int value;
            pos = -1;
            if ((m[0] >= '0' && m[0] <= '9') ||
                m[0] == '+' || m[0] == '-' || m[0] == '.') return convertToNumber(m);

            for (i = 0; i < labels.Count; i++)
            {
                if (m.CompareTo((String)labels[i]) == 0) pos = i;
            }
            if (pos >= 0)
            {
                value = (int)labelValues[pos];
                if ((char)labelMode[pos] == 'E')
                {
                    if (value > 0x2ff) value = (0x300) | (value & 0xff);
                }
                if ((char)labelMode[pos] == 'F')
                {
                    labelAddress = value;
                    if (value < 0x800 || value > 0xfff) value = (0x400) | (value & 0x3ff);
                }
                numType = 'L';
                return value;
            }
            return -1;
        }


        private int findLabelPos(String m)
        {
            int i;
            int pos;
            pos = -1;
            if ((m[0] >= '0' && m[0] <= '9') ||
                m[0] == '+' || m[0] == '-') return convertToNumber(m);

            for (i = 0; i < labels.Count; i++)
            {
                if (m.CompareTo((String)labels[i]) == 0) pos = i;
            }
            return pos;
        }


        private void addLabel(String m, int value, char mode)
        {
            int pos;
            int i;
            pos = -1;
            for (i = 0; i < labels.Count; i++)
            {
                if (m.CompareTo((String)labels[i]) == 0) pos = i;
            }
            if (pos >= 0)
            {
                labelValues[pos] = value;
                labelMode[pos] = mode;
            }
            else
            {
                labels.Add(m);
                labelValues.Add(value);
                labelMode.Add(mode);
            }
        }

        private void init()
        {
            addMnemonic("AD", 0x6000, 0xfff,0);
            addMnemonic("ADS", 0x2c00, 0x3ff,0);
            addMnemonic("AUG", 0x2800, 0x3ff, F_EXTEND);
            addMnemonic("BZF", 0x1000, 0xfff, F_FIXED + F_EXTEND);
            addMnemonic("BZMF", 0x6000, 0xfff, F_FIXED + F_EXTEND);
            addMnemonic("CA", 0x3000, 0xfff, 0);
            addMnemonic("CAE", 0x3000, 0xfff, F_ERASE);
            addMnemonic("CAF", 0x3000, 0xfff, F_FIXED);
            addMnemonic("CCS", 0x1000, 0x3ff, 0);
            addMnemonic("COM", 0x4000, 0, F_NOARG);
            addMnemonic("CS", 0x4000, 0xfff, 0);
            addMnemonic("DAS", 0x2001, 0x3ff, 0);
            addMnemonic("DCA", 0x3001, 0xfff, F_EXTEND);
            addMnemonic("DCOM", 0x4001, 0, F_EXTEND + F_NOARG);
            addMnemonic("DCS", 0x4001, 0xfff, F_EXTEND);
            addMnemonic("DDOUBL", 0x2001, 0, F_NOARG);
            addMnemonic("DIM", 0x2c00, 0x3ff, F_EXTEND);
            addMnemonic("DOUBLE", 0x6000, 0, F_NOARG);
            addMnemonic("DTCB", 0x5406, 0, F_NOARG);
            addMnemonic("DTCF", 0x5405, 0, F_NOARG);
            addMnemonic("DV", 0x1000, 0x3ff, F_EXTEND);
            addMnemonic("DXCH", 0x5401, 0x3ff, 0);
            addMnemonic("EDRUPT", 0x0e00, 0xfff, F_EXTEND);
            addMnemonic("EXTEND", 0x0006, 0, F_NOARG + F_NOCODE);
            addMnemonic("INCR", 0x2800, 0x3ff, 0);
            addMnemonic("INDEX", 0x5000, 0xfff, 0);
            addMnemonic("INHINT", 0x0004, 0, F_NOARG);
            addMnemonic("LXCH", 0x2400, 0x3ff, 0);
            addMnemonic("MASK", 0x7000, 0xfff, 0);
            addMnemonic("MP", 0x7000, 0xfff, F_EXTEND);
            addMnemonic("MSU", 0x2000, 0x3ff, F_EXTEND);
            addMnemonic("NDX", 0x5000, 0xfff, 0);
            addMnemonic("NOOP", 0x3000, 0, F_NOARG + F_NOOP);
            addMnemonic("OVSK", 0x5800, 0, F_NOARG);
            addMnemonic("QXCH", 0x2400, 0x3ff, F_EXTEND);
            addMnemonic("RAND", 0x0400, 0x1ff, F_EXTEND);
            addMnemonic("READ", 0x0000, 0x1ff, F_EXTEND);
            addMnemonic("RELINT", 0x0003, 0, F_NOARG);
            addMnemonic("RESUME", 0x500f, 0, F_NOARG);
            addMnemonic("RETURN", 0x0002, 0, F_NOARG);
            addMnemonic("ROR", 0x0800, 0x1ff, F_EXTEND);
            addMnemonic("RXOR", 0xc00, 0x1ff, F_EXTEND);
            addMnemonic("SQUARE", 0x7000, 0, F_NOARG + F_EXTEND);
            addMnemonic("SU", 0x6000, 0x3ff, F_EXTEND);
            addMnemonic("TC", 0x0000, 0xfff, F_TC);
            addMnemonic("TCR", 0x0000, 0xfff, F_TC);
            addMnemonic("TCAA", 0x5805, 0, 0);
            addMnemonic("TCF", 0x1000, 0xfff, F_FIXED);
            addMnemonic("TS", 0x5800, 0x3ff, 0);
            addMnemonic("WAND", 0x0600, 0x1ff, F_EXTEND);
            addMnemonic("WOR", 0x0a00, 0x1ff, F_EXTEND);
            addMnemonic("WRITE", 0x0200, 0x1ff, F_EXTEND);
            addMnemonic("XCH", 0x5c00, 0x3ff, 0);
            addMnemonic("XLQ", 0x0001, 0, F_NOARG);
            addMnemonic("XXALQ", 0x0000, 0, F_NOARG);
            addMnemonic("ZL", 0x2407, 0, F_NOARG);
            addMnemonic("ZQ", 0x2407, 0, F_NOARG + F_EXTEND);
            addMnemonic("BANK", POP_BANK, 0xff, F_POP);
            addMnemonic("EBANK", POP_EBANK, 7, F_POP);
            addMnemonic("DEC", POP_DEC, 0xffff, F_POP);
            addMnemonic("2DEC", POP_2DEC, 0xffff, F_POP);
            addMnemonic("OCT", POP_OCT, 0xffff, F_POP);
            addMnemonic("HEX", POP_HEX, 0xffff, F_POP);
            addMnemonic("=", POP_EQ, 0xffff, F_POP);
            addMnemonic("EQUALS", POP_EQUALS, 0xfff, F_POP);
            addMnemonic("SETLOC", POP_SETLOC, 0x8fff, F_POP);
            addMnemonic("2FCADR", POP_FCADR, 0x8fff, F_POP);
            addMnemonic("2BCADR", POP_BCADR, 0x8fff, F_POP);
            addMnemonic("ERASE", POP_ERASE, 0xff, F_POP);
        }
        
        private void defaultLabels()
        {
            addLabel("A", 0, 'E');
            addLabel("L", 1, 'E');
            addLabel("Q", 2, 'E');
            addLabel("EB", 3, 'E');
            addLabel("FB", 4, 'E');
            addLabel("Z", 5, 'E');
            addLabel("BB", 6, 'E');
            addLabel("ZERO", 7, 'E');
            addLabel("ARUPT", 8, 'E');
            addLabel("LRUPT", 9, 'E');
            addLabel("QRUPT", 10, 'E');
            addLabel("ZRUPT", 13, 'E');
            addLabel("BBRUPT", 14, 'E');
            addLabel("BRUPT", 15, 'E');
            addLabel("CYR", 16, 'E');
            addLabel("SR", 17, 'E');
            addLabel("CYL", 18, 'E');
            addLabel("EDOP", 19, 'E');
            addLabel("TIME2", 20, 'E');
            addLabel("TIME1", 21, 'E');
            addLabel("TIME3", 22, 'E');
            addLabel("TIME4", 23, 'E');
            addLabel("TIME5", 24, 'E');
            addLabel("TIME6", 25, 'E');
        }

        private void parse(String line)
        {
            int pos;
            int post;
            label = "";
            opcode = "";
            arg = "";
            pos = line.IndexOf('#');
            if (pos == 0) return;
            if (pos > 0)
            {
                line = line.Substring(0, pos);
            }
            if (line.Trim().Length == 0) return;
            line = line.ToUpper();
            if (line[0] != ' ' && line[0] != 8)
            {
                pos = line.IndexOf(' ');
                post = line.IndexOf('\t');
                if (pos < 0 || (post >= 0 && post < pos)) pos = post;
                if (pos < 0)
                {
                    label = line.Trim();
                    return;
                }
                label = line.Substring(0, pos);
                line = line.Substring(pos + 1);
            }
            line = line.Trim();
            pos = line.IndexOf(' ');
            post = line.IndexOf('\t');
            if (pos < 0 || (post >= 0 && post < pos)) pos = post;
            if (pos < 0)
            {
                opcode = line.Trim();
            }
            else
            {
                opcode = line.Substring(0, pos);
                arg = line.Substring(pos).Trim();
            }
        }

        private void writeMem(int value)
        {
            if (assembleFixed)
            {
                rom[address] = value;
                address++;
                lastBank = address >> 10;
                banks[lastBank] = address & 0x3ff;
                fixedUsed++;
            }
            else
            {
                ram[address] = value;
                address++;
                lastEbank = address >> 8;
                ebanks[lastEbank] = address & 0xff;
                erasableUsed++;
            }
        }

        private void error(String msg)
        {
            errorCount++;
            results += "ERROR: " + msg + "\r\n";
        }

        private void assemblyPass()
        {
            int i;
            int lvalue;
            int avalue;
            int ovalue;
            int pos;
            int t1, t2;
            for (i = 0; i < 36; i++) banks[i] = 0;
            for (i = 0; i < 8; i++) ebanks[i] = 0;
            ebanks[0] = 0x31;
            address = 0x800;
            lastEbank = 0;
            lastBank = 2;
            assembleFixed = true;
            for (i = 0; i < source.Length; i++)
            {
                lvalue = -1;
                avalue = -1;
                ovalue = -1;
                parse(source[i]);
                if (label.Length > 0)
                {
                    lvalue = findLabel(label);
                    if (pass == 1 && lvalue >= 0) error("Label multiply defined: " + label);
                    addLabel(label, address, ((assembleFixed) ? 'F' : 'E'));
                }
                if (opcode.Length > 0) ovalue = findOpcode(opcode);
                if (arg.Length > 0)
                {
                    avalue = findLabel(arg);
                    if (pass == 2 && avalue < 0) error("Label not found: " + arg);
                    if (pass == 1 && avalue < 0) avalue = 0;
                }
                if (opcode.Length > 0 && ovalue < 0 && pass == 2) error("Invalid opcode: " + opcode);
                if (ovalue >= 0 && ((int)flags[ovalue] & F_POP) == F_POP) {
                    if ((int)opcodes[ovalue] == POP_ERASE)
                    {
                        t1 = (avalue > 0) ? avalue : 1;
                        if (pass == 2) results += "      " + toHex(address) + "      ";
                        for (t2 = 0; t2 < t1; t2++) writeMem(0);
                    }
                    if ((int)opcodes[ovalue] == POP_SETLOC)
                    {
                        if (assembleFixed)
                        {
                            if (avalue < 0 || avalue > 0x8fff) error("Value out of range: " + avalue.ToString());
                            address = avalue;
                            if (pass == 2) results += "      " + toHex(address) + "      ";
                        }
                        else
                        {
                            if (avalue < 0 || avalue > 0x7ff) error("Value out of range: " + avalue.ToString());
                            address = avalue;
                            if (pass == 2) results += "      " + toHex(address) + "      ";
                        }
                    }
                    if ((int)opcodes[ovalue] == POP_EQ)
                    {
                        if (label.Length == 0 && pass == 2) error("No label given for =");
                        else
                        {
                            if (avalue < 0) avalue = address;
                            addLabel(label, avalue, 'V');
                            if (pass == 2) results += "      " + toHex(avalue) + "      ";
                        }
                    }
                    if ((int)opcodes[ovalue] == POP_EQUALS)
                    {
                        if (label.Length == 0 && pass == 2) error("No label given for EQUALS");
                        else if (avalue < 0 && pass == 2) error("No equivalence given for EQUALS");
                        else
                        {
                            pos = findLabelPos(label);
                            if (pos < 0 && pass == 2) error("Label not found" + label);
                            else
                            {
                                addLabel(label, avalue, (char)labelMode[pos]);
                            }
                            if (pass == 2) results += "      " + toHex(avalue) + "      ";
                        }
                    }
                    if ((int)opcodes[ovalue] == POP_EBANK)
                    {
                        assembleFixed = false;
                        if (avalue > 7 && pass == 2) error("EBank out of range: " + avalue.ToString());
                        if (avalue < 0) address = lastEbank * 256 + (int)ebanks[lastEbank];
                        if (avalue >= 0 && avalue <= 7)
                        {
                            address = avalue * 256 + (int)ebanks[avalue];
                        }
                        if (pass == 2) results += "      " + toHex(address) + "      ";
                        lastEbank = address >> 8;
                    }
                    if ((int)opcodes[ovalue] == POP_BANK)
                    {
                        assembleFixed = true;
                        if (avalue > 35 && pass == 2) error("Bank out of range: " + avalue.ToString());
                        if (avalue < 0) address = lastBank * 1024 + (int)banks[lastBank];
                        if (avalue >= 0 && avalue <= 35)
                        {
                            address = avalue * 1024 + (int)banks[avalue];
                        }
                        if (pass == 2) results += "      " + toHex(address) + "      ";
                    }
                    if ((int)opcodes[ovalue] == POP_DEC)
                    {
                        if (numType != 'D' && numType != 'F' && numType != 'L' && pass == 2) error("Value not specified in decimal: " + arg);
                        if ((avalue < 0 || avalue > 65535) && pass == 2) error("Value out of range: " + avalue.ToString());
                        if (avalue >= 0 && avalue <= 65535)
                        {
                            if (pass == 2) results += toHex(address) + ": " + "     " + toHex(avalue) + " ";
                            writeMem(avalue);
                           
                        }
                    }
                    if ((int)opcodes[ovalue] == POP_2DEC)
                    {
                        if (numType != 'D' && numType != 'F' && numType != 'L' && pass == 2) error("Value not specified in decimal: " + arg);
                        if ((dfraction < 0 || dfraction > 0x1fffffff) && pass == 2) error("Value out of range: " + dfraction.ToString());
                        if (dfraction >= 0 && dfraction <= 0x1fffffff)
                        {
                            if (pass == 2) results += toHex(address) + ": " + toHex(dfraction >> 14) + " " + toHex((dfraction & 0x3fff) | (dfraction & 0x10000000) >> 14) + " ";
                            writeMem(dfraction >> 14);
                            writeMem((dfraction & 0x3fff) | (dfraction & 0x10000000) >> 14);

                        }
                    }
                    if ((int)opcodes[ovalue] == POP_OCT)
                    {
                        if (numType != 'O' && numType != 'L' && pass == 2) error("Value not specified in octal: " + arg);
                        if ((avalue < 0 || avalue > 65535) && pass == 2) error("Value out of range: " + avalue.ToString());
                        if (avalue >= 0 && avalue <= 65535)
                        {
                            if (pass == 2) results += toHex(address) + ": " + "     " + toHex(avalue) + " ";
                            writeMem(avalue);

                        }
                    }
                    if ((int)opcodes[ovalue] == POP_HEX)
                    {
                        if (numType != 'H' && numType != 'L' && pass == 2) error("Value not specified in hex: " + arg);
                        if ((avalue < 0 || avalue > 65535) && pass == 2) error("Value out of range: " + avalue.ToString());
                        if (avalue >= 0 && avalue <= 65535)
                        {
                            if (pass == 2) results += toHex(address) + ": " + "     " + toHex(avalue) + " ";
                            writeMem(avalue);

                        }
                    }
                    if ((int)opcodes[ovalue] == POP_FCADR)
                    {
                        avalue = labelAddress;
                        if (avalue <0 || avalue > (1024*36) && pass == 2) error("Value out of range: " + avalue.ToString());
                        t1 = (avalue / 1024) << 10;
                        t2 = (avalue % 1024) + 0x400;
                        if (pass == 2) results += toHex(address) + ": " + toHex(t1) + " " + toHex(t2) + " ";
                        writeMem(t1);
                        writeMem(t2);
                    }
                    if ((int)opcodes[ovalue] == POP_BCADR)
                    {
                        avalue = labelAddress;
                        if (avalue < 0 || avalue > (1024 * 36) && pass == 2) error("Value out of range: " + avalue.ToString());
                        t1 = (avalue / 1024) << 10;
                        t2 = (avalue % 1024) + 0x400;
                        t1 |= lastEbank;
                        if (pass == 2) results += toHex(address) + ": " + toHex(t2) + " " + toHex(t1) + " ";
                        writeMem(t2);
                        writeMem(t1);
                    }

                }
                else if (ovalue >= 0 && ((int)flags[ovalue] & F_POP) != F_POP && ((int)flags[ovalue] & F_NOCODE) != F_NOCODE)
                {
                    if (pass == 2) results += toHex(address) + ": ";
                    if (((int)flags[ovalue] & F_NOARG) != F_NOARG)
                    {
                        if (((avalue & (int)masks[ovalue]) != avalue) && pass == 2) error("Argument is out of range: " + toHex(avalue));
                        avalue &= (int)masks[ovalue];
                    }
                    if (((int)flags[ovalue] & F_EXTEND) == F_EXTEND)
                    {
                        writeMem(0x0006);
                        if (pass == 2) results += "0006 ";
                    }
                    else if (pass == 2) results += "     ";
                    ovalue = (((int)flags[ovalue] & F_NOARG) != F_NOARG) ? (int)opcodes[ovalue] + avalue : (int)opcodes[ovalue];
                    writeMem(ovalue);
                    if (pass == 2) results += toHex(ovalue) + " ";
                }
//                if (pass == 2) results += toHex(lvalue) + "," + toHex(ovalue) + "," + toHex(avalue) + "\r\n";
                if (pass == 2) results += source[i] + "\r\n";
            }
        }

        private String newAssembly()
        {
            int i;
            String s;
            labels = new ArrayList();
            labelValues = new ArrayList();
            labelMode = new ArrayList();
            banks = new int[36];
            ebanks = new int[8];
            defaultLabels();
            rom = cpu.getRom();
            ram = cpu.getRam();
            errorCount = 0;
            pass = 1;
            results = "";

            assemblyPass();

            pass = 2;
            erasableUsed = 0;
            fixedUsed = 0;
            assemblyPass();
            results += "\r\n";
            results += "Lines Assembled : " + source.Length.ToString() + "\r\n";
            results += "Erasable Used   : " + toHex(erasableUsed) + "/0800\r\n";
            results += "Fixed Used      : " + toHex(fixedUsed) + "/9000\r\n";
            results += "Errors          : " + errorCount.ToString() + "\r\n";
            if (showBankUsage)
            {
                results += "\r\n";
                results += "Erasable Banks:\r\n";
                for (i = 0; i < 8; i++)
                {
                    s = "  Bank " + i.ToString() + ": " + toHex(ebanks[i]).Substring(1) + "/100";
                    while (s.Length < 19) s += " ";
                    results += s;
                    if ((i+1) % 3 == 0 || i == 7) results += "\r\n";
                }
                results += "\r\n";
                results += "Fixed Banks:\r\n";
                for (i = 0; i < 36; i++)
                {
                    s = "  Bank " + i.ToString() + ": " + toHex(banks[i]).Substring(1) + "/400";
                    while (s.Length < 19) s += " ";
                    results += s;
                    if ((i + 1) % 3 == 0 || i == 35) results += "\r\n";
                }
            }

            return results;
        }

        public String assemble()
        {
            return newAssembly();
        }
    }
}
