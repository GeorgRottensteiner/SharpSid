﻿using System;

namespace SharpSid
{
    public static class memPowerOn
    {
        public static short[] POWERON = {
            /* addr,   off,  rle, values */
            /*$0003*/ 0x83, 0x04, 0xaa, 0xb1, 0x91, 0xb3, 0x22,
            /*$000b*/ 0x03,       0x4c,
            /*$000f*/ 0x03,       0x04,
            /*$0016*/ 0x86, 0x05, 0x19, 0x16, 0x00, 0x0a, 0x76, 0xa3,
            /*$0022*/ 0x86, 0x03, 0x40, 0xa3, 0xb3, 0xbd,
            /*$002b*/ 0x85, 0x01, 0x01, 0x08,
            /*$0034*/ 0x07,       0xa0,
            /*$0038*/ 0x03,       0xa0,
            /*$003a*/ 0x01,       0xff,
            /*$0042*/ 0x07,       0x08,
            /*$0047*/ 0x04,       0x24,
            /*$0053*/ 0x8b, 0x01, 0x03, 0x4c,
            /*$0061*/ 0x0c,       0x8d,
            /*$0063*/ 0x02,       0x10,
            /*$0069*/ 0x84, 0x02, 0x8c, 0xff, 0xa0,
            /*$0071*/ 0x85, 0x1e, 0x0a, 0xa3, 0xe6, 0x7a, 0xd0, 0x02, 0xe6, 0x7b, 0xad, 0x00, 0x08, 0xc9, 0x3a, 0xb0, 0x0a, 0xc9, 0x20, 0xf0, 0xef, 0x38, 0xe9, 0x30, 0x38, 0xe9, 0xd0, 0x60, 0x80, 0x4f, 0xc7, 0x52, 0x58,
            /*$0091*/ 0x01,       0xff,
            /*$009a*/ 0x08,       0x03,
            /*$00b2*/ 0x97, 0x01, 0x3c, 0x03,
            /*$00c2*/ 0x8e, 0x03, 0xa0, 0x30, 0xfd, 0x01,
            /*$00c8*/ 0x82, 0x82, 0x03,
            /*$00cb*/ 0x80, 0x81, 0x01,
            /*$00ce*/ 0x01,       0x20,
            /*$00d1*/ 0x82, 0x01, 0x18, 0x05,
            /*$00d5*/ 0x82, 0x02, 0x27, 0x07, 0x0d,
            /*$00d9*/ 0x81, 0x86, 0x84,
            /*$00e0*/ 0x80, 0x85, 0x85,
            /*$00e6*/ 0x80, 0x86, 0x86,
            /*$00ed*/ 0x80, 0x85, 0x87,
            /*$00f3*/ 0x80, 0x03, 0x18, 0xd9, 0x81, 0xeb,
            /*$0176*/ 0x7f,       0x00,
            /*$01f6*/ 0x7f,       0x00,
            /*$0276*/ 0x7f,       0x00,
            /*$0282*/ 0x8b, 0x0a, 0x08, 0x00, 0xa0, 0x00, 0x0e, 0x00, 0x04, 0x0a, 0x00, 0x04, 0x10,
            /*$028f*/ 0x82, 0x01, 0x48, 0xeb,
            /*$0300*/ 0xef, 0x0b, 0x8b, 0xe3, 0x83, 0xa4, 0x7c, 0xa5, 0x1a, 0xa7, 0xe4, 0xa7, 0x86, 0xae,
            /*$0310*/ 0x84, 0x02, 0x4c, 0x48, 0xb2,
            /*$0314*/ 0x81, 0x1f, 0x31, 0xea, 0x66, 0xfe, 0x47, 0xfe, 0x4a, 0xf3, 0x91, 0xf2, 0x0e, 0xf2, 0x50, 0xf2, 0x33, 0xf3, 0x57, 0xf1, 0xca, 0xf1, 0xed, 0xf6, 0x3e, 0xf1, 0x2f, 0xf3, 0x66, 0xfe, 0xa5, 0xf4, 0xed, 0xf5,

            /*Total 217*/
        };
    }
}