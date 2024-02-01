/*
 *	DEFS.H
 *	Tom Kerrigan's Simple Chess Program (TSCP)
 *
 *	Copyright 1997 Tom Kerrigan
 */

/* with fen and null move capabilities - N.Blais 3/5/05 */
using System;
using System.Reflection;
using System.Runtime.InteropServices;
namespace TSCP_Sharp
{
    public partial class TSCP
    {
        struct move_bytes
        {
            public sbyte from;
            public sbyte to;
            public sbyte promote;
            public sbyte bits;
        }

        [StructLayout(LayoutKind.Explicit)]
         struct move
        {
            [FieldOffset(0)]
            public move_bytes b;
            [FieldOffset(0)]
            public int u;
        }

         class gen_t
        {
            public move m;
            public int score;
        }

         struct hist_t
        {
            public move m;
            public int capture;
            public int castle;
            public int ep;
            public int fifty;
            public int hash;
        }

        const int GEN_STACK = 1120;
        const int MAX_PLY = 32;
        const int HIST_STACK = 400;

        const int LIGHT = 0;
        const int DARK = 1;

        const int PAWN = 0;
        const int KNIGHT = 1;
        const int BISHOP = 2;
        const int ROOK = 3;
        const int QUEEN = 4;
        const int KING = 5;

        const int EMPTY = 6;

        const int A1 = 56;
        const int B1 = 57;
        const int C1 = 58;
        const int D1 = 59;
        const int E1 = 60;
        const int F1 = 61;
        const int G1 = 62;
        const int H1 = 63;
        const int A8 = 0;
        const int B8 = 1;
        const int C8 = 2;
        const int D8 = 3;
        const int E8 = 4;
        const int F8 = 5;
        const int G8 = 6;
        const int H8 = 7;

    }
}
