using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Agc
{
    public partial class Form1 : Form
    {
        private CPU cpu;
        private int[] ports;
        private AgcAssembler asm;
        private int reg1PMFlags;
        private int reg2PMFlags;
        private int reg3PMFlags;
        private Boolean stepOK;
        private int[] breakPoints;

        public Form1()
        {
            int i;
            Font = new Font(Font.Name, 8.25f * 96f / CreateGraphics().DpiX, Font.Style, Font.Unit, Font.GdiCharSet, Font.GdiVerticalFont);
            InitializeComponent();
            cpu = new CPU();
            ports = cpu.getPorts();
            cpu.reset();
            asm = new AgcAssembler(cpu);
            reg1PMFlags = 0;
            reg2PMFlags = 0;
            reg3PMFlags = 0;
            erasableButton.Checked = true;
            setErasableBanks();
            stepOK = false;
            breakPoints = new int[10];
            for (i = 0; i < 10; i++) breakPoints[i] = -1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CpuTests test;
            test = new CpuTests();
            debugOutput.Text = test.runTests();
        }

        private void assembly_Click(object sender, EventArgs e)
        {
            asm.setSource(assemblySource.Lines);
            assemblyOutput.Text = asm.assemble();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            cpu.reset();
        }

        private char convertForDisplay(int i)
        {
            switch (i)
            {
                case 0: return ' ';
                case 21: return '0';
                case 3: return '1';
                case 25: return '2';
                case 27: return '3';
                case 15: return '4';
                case 30: return '5';
                case 28: return '6';
                case 19: return '7';
                case 29: return '8';
                case 31: return '9';
            }
            return ' ';
        }

        private void processDskyPort()
        {
            int value;
            int a, b, c, d;
            value = ports[8];
            a = (value >> 11) & 0xf;
            b = (value >> 10) & 0x1;
            c = (value >> 5) & 0x1f;
            d = value & 0x1f;
            switch (a)
            {
                case 12: velLight.BackColor = ((value & 8) == 8) ? Color.Red : Color.White;
                    noAttLight.BackColor = ((value & 16) == 16) ? Color.Red : Color.White;
                    altLight.BackColor = ((value & 32) == 32) ? Color.Red : Color.White;
                    gimbleLockLight.BackColor = ((value & 64) == 64) ? Color.Red : Color.White;
                    trackerLight.BackColor = ((value & 256) == 256) ? Color.Red : Color.White;
                    progLight.BackColor = ((value & 512) == 512) ? Color.Red : Color.White;
                    break;
                case 11: prog1.Text = convertForDisplay(c).ToString();
                    prog2.Text = convertForDisplay(d).ToString();
                    break;
                case 10: verb1.Text = convertForDisplay(c).ToString();
                    verb2.Text = convertForDisplay(d).ToString();
                    break;
                case 9: noun1.Text = convertForDisplay(c).ToString();
                    noun2.Text = convertForDisplay(d).ToString();
                    break;
                case 8: reg11.Text = convertForDisplay(d).ToString();
                    break;
                case 7: reg12.Text = convertForDisplay(c).ToString();
                    reg13.Text = convertForDisplay(d).ToString();
                    if (b == 1) reg1PMFlags |= 2; else reg1PMFlags &= 1;
                    if ((reg1PMFlags & 1) == 1) reg1PM.Text = "-";
                    else if ((reg1PMFlags & 2) == 2) reg1PM.Text = "+";
                    else reg1PM.Text = " ";
                    break;
                case 6: reg14.Text = convertForDisplay(c).ToString();
                    reg15.Text = convertForDisplay(d).ToString();
                    if (b == 1) reg1PMFlags |= 1; else reg1PMFlags &= 2;
                    if ((reg1PMFlags & 1) == 1) reg1PM.Text = "-";
                    else if ((reg1PMFlags & 2) == 2) reg1PM.Text = "+";
                    else reg1PM.Text = " ";
                    break;
                case 5: reg21.Text = convertForDisplay(c).ToString();
                    reg22.Text = convertForDisplay(d).ToString();
                    if (b == 1) reg2PMFlags |= 2; else reg2PMFlags &= 1;
                    if ((reg2PMFlags & 1) == 1) reg2PM.Text = "-";
                    else if ((reg2PMFlags & 2) == 2) reg2PM.Text = "+";
                    else reg2PM.Text = " ";
                    break;
                case 4: reg23.Text = convertForDisplay(c).ToString();
                    reg24.Text = convertForDisplay(d).ToString();
                    if (b == 1) reg2PMFlags |= 1; else reg2PMFlags &= 2;
                    if ((reg2PMFlags & 1) == 1) reg2PM.Text = "-";
                    else if ((reg2PMFlags & 2) == 2) reg2PM.Text = "+";
                    else reg2PM.Text = " ";
                    break;
                case 3: reg25.Text = convertForDisplay(c).ToString();
                    reg31.Text = convertForDisplay(d).ToString();
                    break;
                case 2: reg32.Text = convertForDisplay(c).ToString();
                    reg33.Text = convertForDisplay(d).ToString();
                    if (b == 1) reg3PMFlags |= 2; else reg3PMFlags &= 1;
                    if ((reg3PMFlags & 1) == 1) reg3PM.Text = "-";
                    else if ((reg3PMFlags & 2) == 2) reg3PM.Text = "+";
                    else reg3PM.Text = " ";
                    break;
                case 1: reg34.Text = convertForDisplay(c).ToString();
                    reg35.Text = convertForDisplay(d).ToString();
                    if (b == 1) reg3PMFlags |= 1; else reg3PMFlags &= 2;
                    if ((reg3PMFlags & 1) == 1) reg3PM.Text = "-";
                    else if ((reg3PMFlags & 2) == 2) reg3PM.Text = "+";
                    else reg3PM.Text = " ";
                    break;
            }
        }

        private void processDskyPort2()
        {
            int value;
            value = ports[9];
            compActyLight.BackColor = ((value & 4) == 4) ? Color.Lime : Color.DarkGreen;
            uplinkActyLight.BackColor = ((value & 8) == 8) ? Color.Red : Color.White;
            tempLight.BackColor = ((value & 16) == 16) ? Color.Red : Color.White;
            keyRelLight.BackColor = ((value & 32) == 32) ? Color.Red : Color.White;
            oprErrLight.BackColor = ((value & 128) == 128) ? Color.Red : Color.White;
        }

        private void processDskyPort3()
        {
            int value;
            value = ports[11];
            stbyLight.BackColor = ((value & 0x800) == 0x800) ? Color.Red : Color.White;
        }

        private void processPort(int port)
        {
            if (port == 8) processDskyPort();
            if (port == 9) processDskyPort2();
            if (port == 11) processDskyPort3();
        }

        private void updateCpuInfo()
        {
            int[] ram;
            ram = cpu.getRam();
            aOutput.Text = hexString(ram[0]);
            lOutput.Text = hexString(ram[1]);
            qOutput.Text = hexString(ram[2]);
            zOutput.Text = hexString(ram[5]);
            ebOutput.Text = hexString(ram[3]);
            fbOutput.Text = hexString(ram[4]);
            bbOutput.Text = hexString(ram[6]);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int i;
            int j;
            int[] ram;
            if (powerOn.Checked == false)
            {
                timer1.Enabled = true;
                return;
            }
            if (singleStep.Checked)
            {
                if (!stepOK)
                {
                    timer1.Enabled = true;
                    return;
                }
                stepOK = false;
                cpu.cycle();
                if (enableDebug.Checked)
                {
                    debugOutput.AppendText(cpu.getDebug());
                }
                if (cpu.getSentPort() >= 0)
                {
                    processPort(cpu.getSentPort());
                }
                if (tabControl1.SelectedTab == tabCpuDiag)
                {
                    updateCpuInfo();
                }
                while (cpu.getMct() != 0) cpu.cycle();
                timer1.Enabled = true;
                return;
            }
            for (i = 0; i < 850; i++)
            {
                cpu.cycle();
                if (enableDebug.Checked)
                {
                    debugOutput.AppendText(cpu.getDebug());
                }
                if (cpu.getSentPort() >= 0)
                {
                    processPort(cpu.getSentPort());
                }
                if (tabControl1.SelectedTab == tabCpuDiag)
                {
                    updateCpuInfo();
                }
                if (enableBreakpoints.Checked)
                {
                    ram = cpu.getRam();
                    for (j = 0; j < 10; j++)
                        if (breakPoints[j] == ram[5])
                        {
                            stepOK = false;
                            singleStep.Checked = true;
                            enableDebug.Checked = true;
                            i = 9999;
                        }
                }
            }
            timer1.Enabled = true;
        }

        private void enableDebug_CheckedChanged(object sender, EventArgs e)
        {
            cpu.setDebugMode(enableDebug.Checked);
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            debugOutput.Text = "";
        }

        private void powerOff_CheckedChanged(object sender, EventArgs e)
        {
            if (powerOff.Checked)
            {
                reg1PM.Text = "";
                reg11.Text = "";
                reg12.Text = "";
                reg13.Text = "";
                reg14.Text = "";
                reg15.Text = "";
                reg2PM.Text = "";
                reg21.Text = "";
                reg22.Text = "";
                reg23.Text = "";
                reg24.Text = "";
                reg25.Text = "";
                reg3PM.Text = "";
                reg31.Text = "";
                reg32.Text = "";
                reg33.Text = "";
                reg34.Text = "";
                reg35.Text = "";
                prog1.Text = "";
                prog2.Text = "";
                noun1.Text = "";
                noun2.Text = "";
                verb1.Text = "";
                verb2.Text = "";
                velLight.BackColor = Color.White;
                noAttLight.BackColor = Color.White;
                altLight.BackColor = Color.White;
                gimbleLockLight.BackColor = Color.White;
                trackerLight.BackColor = Color.White;
                progLight.BackColor = Color.White;
                compActyLight.BackColor = Color.DarkGreen;
                uplinkActyLight.BackColor = Color.White;
                tempLight.BackColor = Color.White;
                keyRelLight.BackColor = Color.White;
                oprErrLight.BackColor = Color.White;
                stbyLight.BackColor = Color.White;
            }
        }

        private void dskyKey_Click(object sender, EventArgs e)
        {
            int tag;
            tag = Convert.ToInt32(((Button)sender).Tag);
            ports[13] = tag;
            cpu.dskyIntr();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            String filename;
            StreamReader file;
            String line;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
                file = new StreamReader(filename);
                assemblySource.Text = "";
                while (file.EndOfStream == false)
                {
                    line = file.ReadLine();
                    assemblySource.AppendText(line+"\r\n");
                }
                file.Close();
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            int i;
            String filename;
            StreamWriter file;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filename = saveFileDialog1.FileName;
                file = new StreamWriter(filename);
                for (i = 0; i < assemblySource.Lines.Length; i++)
                {
                    file.WriteLine(assemblySource.Lines[i]);
                }
                file.Close();
            }
        }

        private void newButton_Click(object sender, EventArgs e)
        {
            assemblySource.Text = "";
        }

        private void bankUsage_CheckedChanged(object sender, EventArgs e)
        {
            asm.setShowBankUsage(bankUsage.Checked);
        }

        private void stepButton_Click(object sender, EventArgs e)
        {
            stepOK = true;
        }

        private void setErasableBanks()
        {
            int i;
            bankSelect.Items.Clear();
            for (i = 0; i < 8; i++) bankSelect.Items.Add(i.ToString());
            bankSelect.SelectedIndex = 0;
            showMemory();
        }

        private void setFixedBanks()
        {
            int i;
            bankSelect.Items.Clear();
            for (i = 0; i < 36; i++) bankSelect.Items.Add(i.ToString());
            bankSelect.SelectedIndex = 0;
            showMemory();
        }

        private void erasableButton_CheckedChanged(object sender, EventArgs e)
        {
            if (erasableButton.Checked) setErasableBanks();
        }

        private void fixedButton_CheckedChanged(object sender, EventArgs e)
        {
            if (fixedButton.Checked) setFixedBanks();
        }

        private String hexString(int value)
        {
            String ret;
            int i;
            int nybble;
            ret = "";
            for (i = 0; i < 4; i++)
            {
                nybble = (value & 0xf) + '0';
                if (nybble > '9') nybble += ('@' - '9');
                ret = Convert.ToChar(nybble) + ret;
                value >>= 4;
            }
            return ret;
        }

        private String octalString(int value)
        {
            String ret;
            int i;
            ret = "";
            for (i = 0; i < 6; i++)
            {
                ret = Convert.ToChar((value & 0x7) + '0') + ret;
                value >>= 3;
            }
            return ret;
        }

        private void showMemory()
        {
            int[] memory;
            int size;
            int count;
            int address;
            int offset;
            String line;
            if (bankSelect.Items.Count < 1 || bankSelect.SelectedIndex < 0) return;
            memoryBox.Clear();
            if (erasableButton.Checked)
            {
                memory = cpu.getRam();
                size = 256;
            }
            else
            {
                memory = cpu.getRom();
                size = 1024;
            }
            offset = size * Convert.ToInt32(bankSelect.Text);
            line = "";
            address = 0;
            count = 0;
            for (address = 0; address < size; address++)
            {
                if (line.Length == 0) line = ( (hexButton.Checked) ? hexString(address) : octalString(address)) + ":";
                line += " " + ((hexButton.Checked) ? hexString(memory[address+offset]) : octalString(memory[address+offset]) );
                count++;
                if (count == 8) line += "  ";
                if (count == 16)
                {
                    memoryBox.AppendText(line + "\r\n");
                    line = "";
                    count = 0;
                }
            }
            if (line.Length > 0) memoryBox.AppendText(line);
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            showMemory();
        }

        private void bankSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (bankSelect.Items.Count > 0) showMemory();
        }

        private void hexButton_CheckedChanged(object sender, EventArgs e)
        {
            showMemory();
        }

        private void octalButton_CheckedChanged(object sender, EventArgs e)
        {
            showMemory();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabCpuDiag)
            {
                updateCpuInfo();
                bp1.Text = (breakPoints[0] >= 0) ? hexString(breakPoints[0]) : "";
                bp2.Text = (breakPoints[1] >= 0) ? hexString(breakPoints[1]) : "";
                bp3.Text = (breakPoints[2] >= 0) ? hexString(breakPoints[2]) : "";
                bp4.Text = (breakPoints[3] >= 0) ? hexString(breakPoints[3]) : "";
                bp5.Text = (breakPoints[4] >= 0) ? hexString(breakPoints[4]) : "";
                bp6.Text = (breakPoints[5] >= 0) ? hexString(breakPoints[5]) : "";
                bp7.Text = (breakPoints[6] >= 0) ? hexString(breakPoints[6]) : "";
                bp8.Text = (breakPoints[7] >= 0) ? hexString(breakPoints[7]) : "";
                bp9.Text = (breakPoints[8] >= 0) ? hexString(breakPoints[8]) : "";
                bp10.Text = (breakPoints[9] >= 0) ? hexString(breakPoints[9]) : "";
            }
        }

        private int strToHex(String s)
        {
            int ret;
            ret = 0;
            while (s.Length > 0)
            {
                if (s[0] >= '0' && s[0] <= '9')
                {
                    ret *= 16;
                    ret += (s[0] - '0');
                }
                if (s[0] >= 'A' && s[0] <= 'F')
                {
                    ret *= 16;
                    ret += (s[0] - 'A' + 10);
                }
                if (s[0] >= 'a' && s[0] <= 'f')
                {
                    ret *= 16;
                    ret += (s[0] - 'a' + 10);
                }
                s = s.Substring(1);
            }
            return ret;
        }

        private void bp1_TextChanged(object sender, EventArgs e)
        {
            int tag;
            tag = Convert.ToInt32(((TextBox)sender).Tag);
            breakPoints[tag] = strToHex(((TextBox)sender).Text);
        }


    }
}
