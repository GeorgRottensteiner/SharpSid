using System;

namespace SharpSid
{
#if DEBUG_CPU
    public static class Disassembler
    {
        public static String Opcode2String(int code, int pc, short x, short y, short a, Player player)
        {
            int n = -1;
            int address = -1;

            switch (GetAdrMode(code))
            {
                case immediate:
                    n = player.mem_readMemByte(pc + 1) & 0xff;
                    break;

                case indirect:
                    {
                        int lowAdr = player.mem_readMemByte(pc + 1) & 0xff;
                        int highAdr = player.mem_readMemByte(pc + 2) & 0xff;
                        address = (player.mem_readMemByte(highAdr + lowAdr) & 0xff) + ((player.mem_readMemByte(highAdr + ((lowAdr + 1) & 0xff)) & 0xff) << 8);
                        n = (player.mem_readMemByte(pc + 1) & 0xff) + ((player.mem_readMemByte(pc + 2) & 0xff) << 8);
                    }
                    break;

                case absolute_x:
                    {
                        int lowAdr = (player.mem_readMemByte(pc + 1) & 0xff) + ((int)x & 0xff);
                        address = (lowAdr + (player.mem_readMemByte(pc + 2) << 8)) & 0xffff;
                        n = (player.mem_readMemByte(pc + 1) & 0xff) + ((player.mem_readMemByte(pc + 2) & 0xff) << 8);
                    }
                    break;

                case absolute_y:
                    {
                        int lowAdr = (player.mem_readMemByte(pc + 1) & 0xff) + ((int)y & 0xff);
                        address = (lowAdr + (player.mem_readMemByte(pc + 2) << 8)) & 0xffff;
                        n = (player.mem_readMemByte(pc + 1) & 0xff) + ((player.mem_readMemByte(pc + 2) & 0xff) << 8);
                    }
                    break;

                case zero_x:
                    address = (player.mem_readMemByte(pc + 1) & 0xff) + ((int)x & 0xff);
                    n = player.mem_readMemByte(pc + 1) & 0xff;
                    break;

                case zero_y:
                    address = (player.mem_readMemByte(pc + 1) & 0xff) + ((int)y & 0xff);
                    n = player.mem_readMemByte(pc + 1) & 0xff;
                    break;

                case indirect_x:
                    {
                        int p = ((player.mem_readMemByte(pc + 1) & 0xff) + (int)x) & 0xff;
                        address = (player.mem_readMemByte(p) & 0xff) + ((player.mem_readMemByte((p + 1) & 0xff) & 0xff) << 8);
                        n = player.mem_readMemByte(pc + 1) & 0xff;
                    }
                    break;

                case indirect_y:
                    {
                        int p = player.mem_readMemByte(pc + 1) & 0xff;
                        int lowAdr = (player.mem_readMemByte(p) & 0xff) + (int)y;
                        address = (lowAdr + ((player.mem_readMemByte((p + 1) & 0xff) & 0xff) << 8)) & 0xffff;
                        n = player.mem_readMemByte(pc + 1) & 0xff;
                    }
                    break;

                case relative:
                    n = player.mem_readMemByte(pc + 1) + pc;
                    break;

                case zero:
                    n = player.mem_readMemByte(pc + 1) & 0xff;
                    break;

                case absolute:
                case absolute_wide:
                    n = (player.mem_readMemByte(pc + 1) & 0xff) + ((player.mem_readMemByte(pc + 2) & 0xff) << 8);
                    break;
            }

            String s = GetCodeStr(code);
            if (n > -1)
            {
                s += " #$" + n.ToString("X");
            }
            if (address > -1)
            {
                s += " $" + address.ToString("X4") + " ($" + (player.mem_readMemByte(address) & 0xff).ToString("X2") + ")";
            }

            for (int i = s.Length; i < 30; i++)
            {
                s += " ";
            }

            s += " A=$" + a.ToString("X2") + "  X=$" + x.ToString("X2") + "  Y=$" + y.ToString("X2");

            return s;
        }

        private const int absolute = 0;
        private const int absolute_x = 1;
        private const int absolute_y = 2;
        private const int absolute_wide = 3;
        private const int immediate = 4;
        private const int relative = 5;
        private const int implied = 6;
        private const int indirect = 7;
        private const int indirect_x = 8;
        private const int indirect_y = 9;
        private const int zero = 10;
        private const int zero_x = 11;
        private const int zero_y = 12;

        public static String GetCodeStr(int code)
        {
            String codeStr;

            switch (code)
            {
                case IOpCode.ADCa:
                    codeStr = "ADCa";
                    break;
                case IOpCode.ADCax:
                    codeStr = "ADCax";
                    break;
                case IOpCode.ADCay:
                    codeStr = "ADCay";
                    break;
                case IOpCode.ADCb:
                    codeStr = "ADCb";
                    break;
                case IOpCode.ADCix:
                    codeStr = "ADCix";
                    break;
                case IOpCode.ADCiy:
                    codeStr = "ADCiy";
                    break;
                case IOpCode.ADCz:
                    codeStr = "ADCz";
                    break;
                case IOpCode.ADCzx:
                    codeStr = "ADC";
                    break;
                case IOpCode.ANCb:
                    codeStr = "ANCb";
                    break;
                case IOpCode.ANCb_1:
                    codeStr = "ANCb";
                    break;
                case IOpCode.ANDa:
                    codeStr = "ANDa";
                    break;
                case IOpCode.ANDax:
                    codeStr = "ANDax";
                    break;
                case IOpCode.ANDay:
                    codeStr = "ANDay";
                    break;
                case IOpCode.ANDb:
                    codeStr = "ANDb";
                    break;
                case IOpCode.ANDix:
                    codeStr = "ANDix";
                    break;
                case IOpCode.ANDiy:
                    codeStr = "ANDiy";
                    break;
                case IOpCode.ANDz:
                    codeStr = "ANDz";
                    break;
                case IOpCode.ANDzx:
                    codeStr = "ANDzx";
                    break;
                case IOpCode.ANEb:
                    codeStr = "ANEb";
                    break;
                case IOpCode.ARRb:
                    codeStr = "ARRb";
                    break;
                case IOpCode.ASLa:
                    codeStr = "ASLa";
                    break;
                case IOpCode.ASLax:
                    codeStr = "ASLax";
                    break;
                case IOpCode.ASLn:
                    codeStr = "ASLn";
                    break;
                case IOpCode.ASLz:
                    codeStr = "ASLz";
                    break;
                case IOpCode.ASLzx:
                    codeStr = "ASLzx";
                    break;
                case IOpCode.ASRb:
                    codeStr = "ASRb";
                    break;

                case IOpCode.BCCr:
                    codeStr = "BCCr";
                    break;
                case IOpCode.BCSr:
                    codeStr = "BCSr";
                    break;
                case IOpCode.BEQr:
                    codeStr = "BEQr";
                    break;
                case IOpCode.BITa:
                    codeStr = "BITa";
                    break;
                case IOpCode.BITz:
                    codeStr = "BITz";
                    break;
                case IOpCode.BMIr:
                    codeStr = "BMIr";
                    break;
                case IOpCode.BNEr:
                    codeStr = "BNEr";
                    break;
                case IOpCode.BPLr:
                    codeStr = "BPLr";
                    break;
                case IOpCode.BRKn:
                    codeStr = "BRKn";
                    break;
                case IOpCode.BVCr:
                    codeStr = "BVCr";
                    break;
                case IOpCode.BVSr:
                    codeStr = "BVSr";
                    break;

                case IOpCode.CLCn:
                    codeStr = "CLCn";
                    break;
                case IOpCode.CLDn:
                    codeStr = "CLDn";
                    break;
                case IOpCode.CLIn:
                    codeStr = "CLIn";
                    break;
                case IOpCode.CLVn:
                    codeStr = "CLVn";
                    break;
                case IOpCode.CMPa:
                    codeStr = "CMPa";
                    break;
                case IOpCode.CMPax:
                    codeStr = "CMPax";
                    break;
                case IOpCode.CMPay:
                    codeStr = "CMPay";
                    break;
                case IOpCode.CMPb:
                    codeStr = "CMPb";
                    break;
                case IOpCode.CMPix:
                    codeStr = "CMPix";
                    break;
                case IOpCode.CMPiy:
                    codeStr = "CMPiy";
                    break;
                case IOpCode.CMPz:
                    codeStr = "CMPz";
                    break;
                case IOpCode.CMPzx:
                    codeStr = "CMPzx";
                    break;
                case IOpCode.CPXa:
                    codeStr = "CPXa";
                    break;
                case IOpCode.CPXb:
                    codeStr = "CPXb";
                    break;
                case IOpCode.CPXz:
                    codeStr = "CPXz";
                    break;
                case IOpCode.CPYa:
                    codeStr = "CPYa";
                    break;
                case IOpCode.CPYb:
                    codeStr = "CPYb";
                    break;
                case IOpCode.CPYz:
                    codeStr = "CPYz";
                    break;

                case IOpCode.DCPa:
                    codeStr = "DCPa";
                    break;
                case IOpCode.DCPax:
                    codeStr = "DCPax";
                    break;
                case IOpCode.DCPay:
                    codeStr = "DCPay";
                    break;
                case IOpCode.DCPix:
                    codeStr = "DCPix";
                    break;
                case IOpCode.DCPiy:
                    codeStr = "DCPiy";
                    break;
                case IOpCode.DCPz:
                    codeStr = "DCPz";
                    break;
                case IOpCode.DCPzx:
                    codeStr = "DCPzx";
                    break;
                case IOpCode.DECa:
                    codeStr = "DECa";
                    break;
                case IOpCode.DECax:
                    codeStr = "DECax";
                    break;
                case IOpCode.DECz:
                    codeStr = "DECz";
                    break;
                case IOpCode.DECzx:
                    codeStr = "DECzx";
                    break;
                case IOpCode.DEXn:
                    codeStr = "DEXn";
                    break;
                case IOpCode.DEYn:
                    codeStr = "DEYn";
                    break;

                case IOpCode.EORa:
                    codeStr = "EORa";
                    break;
                case IOpCode.EORax:
                    codeStr = "EORax";
                    break;
                case IOpCode.EORay:
                    codeStr = "EORay";
                    break;
                case IOpCode.EORb:
                    codeStr = "EORb";
                    break;
                case IOpCode.EORix:
                    codeStr = "EORix";
                    break;
                case IOpCode.EORiy:
                    codeStr = "EORiy";
                    break;
                case IOpCode.EORz:
                    codeStr = "EORz";
                    break;
                case IOpCode.EORzx:
                    codeStr = "EORzx";
                    break;

                case IOpCode.INCa:
                    codeStr = "INCa";
                    break;
                case IOpCode.INCax:
                    codeStr = "INCax";
                    break;
                case IOpCode.INCz:
                    codeStr = "INCz";
                    break;
                case IOpCode.INCzx:
                    codeStr = "INCzx";
                    break;
                case IOpCode.INXn:
                    codeStr = "INXn";
                    break;
                case IOpCode.INYn:
                    codeStr = "INYn";
                    break;
                case IOpCode.ISBa:
                    codeStr = "ISBa";
                    break;
                case IOpCode.ISBax:
                    codeStr = "ISBax";
                    break;
                case IOpCode.ISBay:
                    codeStr = "ISBay";
                    break;
                case IOpCode.ISBix:
                    codeStr = "ISBix";
                    break;
                case IOpCode.ISBiy:
                    codeStr = "ISBiy";
                    break;
                case IOpCode.ISBz:
                    codeStr = "ISBz";
                    break;
                case IOpCode.ISBzx:
                    codeStr = "ISBzx";
                    break;

                case IOpCode.JMPi:
                    codeStr = "JMPi";
                    break;
                case IOpCode.JMPw:
                    codeStr = "JMPw";
                    break;
                case IOpCode.JSRw:
                    codeStr = "JSRw";
                    break;

                case IOpCode.LASay:
                    codeStr = "LASay";
                    break;
                case IOpCode.LAXa:
                    codeStr = "LAXa";
                    break;
                case IOpCode.LAXay:
                    codeStr = "LAXay";
                    break;
                case IOpCode.LAXix:
                    codeStr = "LAXix";
                    break;
                case IOpCode.LAXiy:
                    codeStr = "LAXiy";
                    break;
                case IOpCode.LAXz:
                    codeStr = "LAXz";
                    break;
                case IOpCode.LAXzy:
                    codeStr = "LAXzy";
                    break;
                case IOpCode.LDAa:
                    codeStr = "LDAa";
                    break;
                case IOpCode.LDAax:
                    codeStr = "LDAax";
                    break;
                case IOpCode.LDAay:
                    codeStr = "LDAay";
                    break;
                case IOpCode.LDAb:
                    codeStr = "LDAb";
                    break;
                case IOpCode.LDAix:
                    codeStr = "LDAix";
                    break;
                case IOpCode.LDAiy:
                    codeStr = "LDAiy";
                    break;
                case IOpCode.LDAz:
                    codeStr = "LDAz";
                    break;
                case IOpCode.LDAzx:
                    codeStr = "LDAzx";
                    break;
                case IOpCode.LDXa:
                    codeStr = "LDXa";
                    break;
                case IOpCode.LDXay:
                    codeStr = "LDXay";
                    break;
                case IOpCode.LDXb:
                    codeStr = "LDXb";
                    break;
                case IOpCode.LDXz:
                    codeStr = "LDXz";
                    break;
                case IOpCode.LDXzy:
                    codeStr = "LDXzy";
                    break;
                case IOpCode.LDYa:
                    codeStr = "LDYa";
                    break;
                case IOpCode.LDYax:
                    codeStr = "LDYax";
                    break;
                case IOpCode.LDYb:
                    codeStr = "LDYb";
                    break;
                case IOpCode.LDYz:
                    codeStr = "LDYz";
                    break;
                case IOpCode.LDYzx:
                    codeStr = "LDYzx";
                    break;
                case IOpCode.LSRa:
                    codeStr = "LSRa";
                    break;
                case IOpCode.LSRax:
                    codeStr = "LSRax";
                    break;
                case IOpCode.LSRn:
                    codeStr = "LSRn";
                    break;
                case IOpCode.LSRz:
                    codeStr = "LSRz";
                    break;
                case IOpCode.LSRzx:
                    codeStr = "LSRzx";
                    break;
                case IOpCode.LXAb:
                    codeStr = "LXAb";
                    break;

                case IOpCode.NOPa:
                    codeStr = "NOPa";
                    break;
                case IOpCode.NOPax:
                case IOpCode.NOPax_1:
                case IOpCode.NOPax_2:
                case IOpCode.NOPax_3:
                case IOpCode.NOPax_4:
                case IOpCode.NOPax_5:
                    codeStr = "NOPax";
                    break;
                case IOpCode.NOPb:
                case IOpCode.NOPb_1:
                case IOpCode.NOPb_2:
                case IOpCode.NOPb_3:
                case IOpCode.NOPb_4:
                    codeStr = "NOPb";
                    break;
                case IOpCode.NOPn:
                case IOpCode.NOPn_1:
                case IOpCode.NOPn_2:
                case IOpCode.NOPn_3:
                case IOpCode.NOPn_4:
                case IOpCode.NOPn_5:
                case IOpCode.NOPn_6:
                    codeStr = "NOPn";
                    break;
                case IOpCode.NOPz:
                case IOpCode.NOPz_1:
                case IOpCode.NOPz_2:
                    codeStr = "NOPz";
                    break;
                case IOpCode.NOPzx:
                case IOpCode.NOPzx_1:
                case IOpCode.NOPzx_2:
                case IOpCode.NOPzx_3:
                case IOpCode.NOPzx_4:
                case IOpCode.NOPzx_5:
                    codeStr = "NOPzx";
                    break;

                case IOpCode.ORAa:
                    codeStr = "ORAa";
                    break;
                case IOpCode.ORAax:
                    codeStr = "ORAax";
                    break;
                case IOpCode.ORAay:
                    codeStr = "ORAay";
                    break;
                case IOpCode.ORAb:
                    codeStr = "ORAb";
                    break;
                case IOpCode.ORAix:
                    codeStr = "ORAix";
                    break;
                case IOpCode.ORAiy:
                    codeStr = "ORAiy";
                    break;
                case IOpCode.ORAz:
                    codeStr = "ORAz";
                    break;
                case IOpCode.ORAzx:
                    codeStr = "ORAzx";
                    break;

                case IOpCode.PHAn:
                    codeStr = "PHAn";
                    break;
                case IOpCode.PHPn:
                    codeStr = "PHPn";
                    break;
                case IOpCode.PLAn:
                    codeStr = "PLAn";
                    break;
                case IOpCode.PLPn:
                    codeStr = "PLPn";
                    break;

                case IOpCode.RLAa:
                    codeStr = "RLAa";
                    break;
                case IOpCode.RLAax:
                    codeStr = "RLAax";
                    break;
                case IOpCode.RLAay:
                    codeStr = "RLAay";
                    break;
                case IOpCode.RLAix:
                    codeStr = "RLAix";
                    break;
                case IOpCode.RLAiy:
                    codeStr = "RLAiy";
                    break;
                case IOpCode.RLAz:
                    codeStr = "RLAz";
                    break;
                case IOpCode.RLAzx:
                    codeStr = "RLAzx";
                    break;
                case IOpCode.ROLa:
                    codeStr = "ROLa";
                    break;
                case IOpCode.ROLax:
                    codeStr = "ROLax";
                    break;
                case IOpCode.ROLn:
                    codeStr = "ROLn";
                    break;
                case IOpCode.ROLz:
                    codeStr = "ROLz";
                    break;
                case IOpCode.ROLzx:
                    codeStr = "ROLzx";
                    break;
                case IOpCode.RORa:
                    codeStr = "RORa";
                    break;
                case IOpCode.RORax:
                    codeStr = "RORax";
                    break;
                case IOpCode.RORn:
                    codeStr = "RORn";
                    break;
                case IOpCode.RORz:
                    codeStr = "RORz";
                    break;
                case IOpCode.RORzx:
                    codeStr = "RORzx";
                    break;

                case IOpCode.RRAa:
                    codeStr = "RRAa";
                    break;
                case IOpCode.RRAax:
                    codeStr = "RRAax";
                    break;
                case IOpCode.RRAay:
                    codeStr = "RRAay";
                    break;
                case IOpCode.RRAix:
                    codeStr = "RRAix";
                    break;
                case IOpCode.RRAiy:
                    codeStr = "RRAiy";
                    break;
                case IOpCode.RRAz:
                    codeStr = "RRAz";
                    break;
                case IOpCode.RRAzx:
                    codeStr = "RRAzx";
                    break;

                case IOpCode.RTIn:
                    codeStr = "RTIn";
                    break;

                case IOpCode.RTSn:
                    codeStr = "RTSn";
                    break;

                case IOpCode.SAXa:
                    codeStr = "SAXa";
                    break;
                case IOpCode.SAXix:
                    codeStr = "SAXix";
                    break;
                case IOpCode.SAXz:
                    codeStr = "SAXz";
                    break;
                case IOpCode.SAXzy:
                    codeStr = "SAXzy";
                    break;
                case IOpCode.SBCa:
                    codeStr = "SBCa";
                    break;
                case IOpCode.SBCax:
                    codeStr = "SBCax";
                    break;
                case IOpCode.SBCay:
                    codeStr = "SBCay";
                    break;
                case IOpCode.SBCb:
                case IOpCode.SBCb_1:
                    codeStr = "SBCb";
                    break;
                case IOpCode.SBCix:
                    codeStr = "SBCix";
                    break;
                case IOpCode.SBCiy:
                    codeStr = "SBCiy";
                    break;
                case IOpCode.SBCz:
                    codeStr = "SBCz";
                    break;
                case IOpCode.SBCzx:
                    codeStr = "SBCzx";
                    break;
                case IOpCode.SBXb:
                    codeStr = "SBXb";
                    break;
                case IOpCode.SECn:
                    codeStr = "SECn";
                    break;
                case IOpCode.SEDn:
                    codeStr = "SEDn";
                    break;
                case IOpCode.SEIn:
                    codeStr = "SEIn";
                    break;
                case IOpCode.SHAay:
                    codeStr = "SHAay";
                    break;
                case IOpCode.SHAiy:
                    codeStr = "SHAiy";
                    break;
                // case IOpCode.SHSay:
                // codeStr = "SHS";
                case IOpCode.SHXay:
                    codeStr = "SHXay";
                    break;
                case IOpCode.SHYax:
                    codeStr = "SHYax";
                    break;
                case IOpCode.SLOa:
                    codeStr = "SLOa";
                    break;
                case IOpCode.SLOax:
                    codeStr = "SLOax";
                    break;
                case IOpCode.SLOay:
                    codeStr = "SLOay";
                    break;
                case IOpCode.SLOix:
                    codeStr = "SLOix";
                    break;
                case IOpCode.SLOiy:
                    codeStr = "SLOiy";
                    break;
                case IOpCode.SLOz:
                    codeStr = "SLOz";
                    break;
                case IOpCode.SLOzx:
                    codeStr = "SLOzx";
                    break;
                case IOpCode.SREa:
                    codeStr = "SREa";
                    break;
                case IOpCode.SREax:
                    codeStr = "SREax";
                    break;
                case IOpCode.SREay:
                    codeStr = "SREay";
                    break;
                case IOpCode.SREix:
                    codeStr = "SREix";
                    break;
                case IOpCode.SREiy:
                    codeStr = "SREiy";
                    break;
                case IOpCode.SREz:
                    codeStr = "SREz";
                    break;
                case IOpCode.SREzx:
                    codeStr = "SREzx";
                    break;
                case IOpCode.STAa:
                    codeStr = "STAa";
                    break;
                case IOpCode.STAax:
                    codeStr = "STAax";
                    break;
                case IOpCode.STAay:
                    codeStr = "STAay";
                    break;
                case IOpCode.STAix:
                    codeStr = "STAix";
                    break;
                case IOpCode.STAiy:
                    codeStr = "STAiy";
                    break;
                case IOpCode.STAz:
                    codeStr = "STAz";
                    break;
                case IOpCode.STAzx:
                    codeStr = "STAzx";
                    break;
                case IOpCode.STXa:
                    codeStr = "STXa";
                    break;
                case IOpCode.STXz:
                    codeStr = "STXz";
                    break;
                case IOpCode.STXzy:
                    codeStr = "STXzy";
                    break;
                case IOpCode.STYa:
                    codeStr = "STYa";
                    break;
                case IOpCode.STYz:
                    codeStr = "STYz";
                    break;
                case IOpCode.STYzx:
                    codeStr = "STYzx";
                    break;

                case IOpCode.TASay:
                    codeStr = "TASay";
                    break;
                case IOpCode.TAXn:
                    codeStr = "TAXn";
                    break;
                case IOpCode.TAYn:
                    codeStr = "TAYn";
                    break;
                case IOpCode.TSXn:
                    codeStr = "TSXn";
                    break;
                case IOpCode.TXAn:
                    codeStr = "TXAn";
                    break;
                case IOpCode.TXSn:
                    codeStr = "TXSn";
                    break;
                case IOpCode.TYAn:
                    codeStr = "TYAn";
                    break;

                // case IOpCode.XAAb:
                // codeStr = "XAA";

                default:
                    codeStr = "unknown " + code.ToString();
                    break;
            }

            return codeStr;
        }

        public static int GetAdrMode(int code)
        {
            switch (code)
            {
                case IOpCode.BRKn:
                case IOpCode.RTIn:
                case IOpCode.RTSn:
                case IOpCode.PHPn:
                case IOpCode.PLPn:
                case IOpCode.PHAn:
                case IOpCode.PLAn:
                case IOpCode.DEYn:
                case IOpCode.TAYn:
                case IOpCode.INYn:
                case IOpCode.INXn:
                case IOpCode.ASLn:
                case IOpCode.ROLn:
                case IOpCode.LSRn:
                case IOpCode.RORn:
                case IOpCode.TXAn:
                case IOpCode.TAXn:
                case IOpCode.DEXn:
                case IOpCode.NOPn:
                case IOpCode.NOPn_1:
                case IOpCode.NOPn_2:
                case IOpCode.NOPn_3:
                case IOpCode.NOPn_4:
                case IOpCode.NOPn_5:
                case IOpCode.NOPn_6:
                case IOpCode.CLCn:
                case IOpCode.SECn:
                case IOpCode.CLIn:
                case IOpCode.SEIn:
                case IOpCode.TYAn:
                case IOpCode.CLVn:
                case IOpCode.CLDn:
                case IOpCode.SEDn:
                case IOpCode.TXSn:
                case IOpCode.TSXn:
                    return implied;

                case IOpCode.JSRw:
                case IOpCode.JMPw:
                    return absolute_wide;

                case IOpCode.NOPb:
                case IOpCode.NOPb_1:
                case IOpCode.NOPb_2:
                case IOpCode.NOPb_3:
                case IOpCode.NOPb_4:
                case IOpCode.LDYb:
                case IOpCode.CPYb:
                case IOpCode.CPXb:
                case IOpCode.LDXb:
                case IOpCode.ORAb:
                case IOpCode.ANDb:
                case IOpCode.EORb:
                case IOpCode.ADCb:
                case IOpCode.LDAb:
                case IOpCode.CMPb:
                case IOpCode.SBCb:
                case IOpCode.SBCb_1:
                case IOpCode.ANCb:
                case IOpCode.ANCb_1:
                case IOpCode.ASRb:
                case IOpCode.ARRb:
                case IOpCode.ANEb:
                // case IOpCode.XAAb :
                case IOpCode.LXAb:
                case IOpCode.SBXb:
                    return immediate;

                case IOpCode.ORAix:
                case IOpCode.ANDix:
                case IOpCode.EORix:
                case IOpCode.ADCix:
                case IOpCode.STAix:
                case IOpCode.LDAix:
                case IOpCode.CMPix:
                case IOpCode.SBCix:
                case IOpCode.SLOix:
                case IOpCode.RLAix:
                case IOpCode.SREix:
                case IOpCode.RRAix:
                case IOpCode.SAXix:
                case IOpCode.LAXix:
                case IOpCode.DCPix:
                case IOpCode.ISBix:
                    return indirect_x;

                case IOpCode.NOPz:
                case IOpCode.NOPz_1:
                case IOpCode.NOPz_2:
                case IOpCode.BITz:
                case IOpCode.STYz:
                case IOpCode.LDYz:
                case IOpCode.CPYz:
                case IOpCode.CPXz:
                case IOpCode.ORAz:
                case IOpCode.ANDz:
                case IOpCode.EORz:
                case IOpCode.ADCz:
                case IOpCode.STAz:
                case IOpCode.LDAz:
                case IOpCode.CMPz:
                case IOpCode.SBCz:
                case IOpCode.ASLz:
                case IOpCode.ROLz:
                case IOpCode.LSRz:
                case IOpCode.RORz:
                case IOpCode.STXz:
                case IOpCode.LDXz:
                case IOpCode.DECz:
                case IOpCode.INCz:
                case IOpCode.SLOz:
                case IOpCode.RLAz:
                case IOpCode.SREz:
                case IOpCode.RRAz:
                case IOpCode.SAXz:
                case IOpCode.LAXz:
                case IOpCode.DCPz:
                case IOpCode.ISBz:
                    return zero;

                case IOpCode.NOPa:
                case IOpCode.BITa:
                case IOpCode.STYa:
                case IOpCode.LDYa:
                case IOpCode.CPYa:
                case IOpCode.CPXa:
                case IOpCode.ORAa:
                case IOpCode.ANDa:
                case IOpCode.EORa:
                case IOpCode.ADCa:
                case IOpCode.STAa:
                case IOpCode.LDAa:
                case IOpCode.CMPa:
                case IOpCode.SBCa:
                case IOpCode.ASLa:
                case IOpCode.ROLa:
                case IOpCode.LSRa:
                case IOpCode.RORa:
                case IOpCode.STXa:
                case IOpCode.LDXa:
                case IOpCode.DECa:
                case IOpCode.INCa:
                case IOpCode.SLOa:
                case IOpCode.RLAa:
                case IOpCode.SREa:
                case IOpCode.RRAa:
                case IOpCode.SAXa:
                case IOpCode.LAXa:
                case IOpCode.DCPa:
                case IOpCode.ISBa:
                    return absolute;

                case IOpCode.JMPi:
                    return indirect;

                case IOpCode.BPLr:
                case IOpCode.BMIr:
                case IOpCode.BVCr:
                case IOpCode.BVSr:
                case IOpCode.BCCr:
                case IOpCode.BCSr:
                case IOpCode.BNEr:
                case IOpCode.BEQr:
                    return relative;

                case IOpCode.ORAiy:
                case IOpCode.ANDiy:
                case IOpCode.EORiy:
                case IOpCode.ADCiy:
                case IOpCode.STAiy:
                case IOpCode.LDAiy:
                case IOpCode.CMPiy:
                case IOpCode.SBCiy:
                case IOpCode.SLOiy:
                case IOpCode.RLAiy:
                case IOpCode.SREiy:
                case IOpCode.RRAiy:
                case IOpCode.SHAiy:
                case IOpCode.LAXiy:
                case IOpCode.DCPiy:
                case IOpCode.ISBiy:
                    return indirect_y;

                case IOpCode.NOPzx:
                case IOpCode.NOPzx_1:
                case IOpCode.NOPzx_2:
                case IOpCode.NOPzx_3:
                case IOpCode.NOPzx_4:
                case IOpCode.NOPzx_5:
                case IOpCode.STYzx:
                case IOpCode.LDYzx:
                case IOpCode.ORAzx:
                case IOpCode.ANDzx:
                case IOpCode.EORzx:
                case IOpCode.ADCzx:
                case IOpCode.STAzx:
                case IOpCode.LDAzx:
                case IOpCode.CMPzx:
                case IOpCode.SBCzx:
                case IOpCode.ASLzx:
                case IOpCode.ROLzx:
                case IOpCode.LSRzx:
                case IOpCode.RORzx:
                case IOpCode.DECzx:
                case IOpCode.INCzx:
                case IOpCode.SLOzx:
                case IOpCode.RLAzx:
                case IOpCode.SREzx:
                case IOpCode.RRAzx:
                case IOpCode.DCPzx:
                case IOpCode.ISBzx:
                    return zero_x;

                case IOpCode.STXzy:
                case IOpCode.LDXzy:
                case IOpCode.SAXzy:
                case IOpCode.LAXzy:
                    return zero_y;

                case IOpCode.ORAay:
                case IOpCode.ANDay:
                case IOpCode.EORay:
                case IOpCode.ADCay:
                case IOpCode.STAay:
                case IOpCode.LDAay:
                case IOpCode.CMPay:
                case IOpCode.SBCay:
                case IOpCode.SLOay:
                case IOpCode.RLAay:
                case IOpCode.SREay:
                case IOpCode.RRAay:
                case IOpCode.SHSay:
                // case IOpCode.TASay :
                case IOpCode.LASay:
                case IOpCode.DCPay:
                case IOpCode.ISBay:
                case IOpCode.SHXay:
                case IOpCode.LDXay:
                case IOpCode.SHAay:
                case IOpCode.LAXay:
                    return absolute_y;

                case IOpCode.NOPax:
                case IOpCode.NOPax_1:
                case IOpCode.NOPax_2:
                case IOpCode.NOPax_3:
                case IOpCode.NOPax_4:
                case IOpCode.NOPax_5:
                case IOpCode.SHYax:
                case IOpCode.LDYax:
                case IOpCode.ORAax:
                case IOpCode.ANDax:
                case IOpCode.EORax:
                case IOpCode.ADCax:
                case IOpCode.STAax:
                case IOpCode.LDAax:
                case IOpCode.CMPax:
                case IOpCode.SBCax:
                case IOpCode.ASLax:
                case IOpCode.ROLax:
                case IOpCode.LSRax:
                case IOpCode.RORax:
                case IOpCode.DECax:
                case IOpCode.INCax:
                case IOpCode.SLOax:
                case IOpCode.RLAax:
                case IOpCode.SREax:
                case IOpCode.RRAax:
                case IOpCode.DCPax:
                case IOpCode.ISBax:
                    return absolute_x;
            }

            return -1;
        }
    }
#endif
}