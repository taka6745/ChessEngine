﻿/*
 *	DATA.C
 *	Tom Kerrigan's Simple Chess Program (TSCP)
 *
 *	Copyright 1997 Tom Kerrigan
 */

/* with fen and null move capabilities - N.Blais 3/5/05 */
using System;
namespace TSCP_Sharp
{
    public partial class TSCP
    {

        /* the board representation */

        int[] color = new int [64];  /* LIGHT, DARK, or EMPTY */
        int[] piece = new int[64];  /* PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING, or EMPTY */
        int side;  /* the side to move */
        int xside;  /* the side not to move */
        int castle; // a bitfield with the castle permissions. if 1 is set,
                    //white can still castle kingside. 2 is white queenside.
				    // 4 is black kingside. 8 is black queenside. */
        int ep;  // the en passant square. if white moves e2e4, the en passant
                 // square is set to e3, because that's where a pawn would move
			     // in an en passant capture */
        int fifty; // the number of moves since a capture or pawn move, used
                   // to handle the fifty-move-draw rule */
        int hash;  // a (more or less) unique number that corresponds to the
                   // position */
        int ply;  // the number of half-moves (ply) since the
                  // root of the search tree */
        int hply; // h for history; the number of ply since the beginning
                  //of the game */

        /* gen_dat is some memory for move lists that are created by the move
           generators. The move list for ply n starts at first_move[n] and ends
           at first_move[n + 1]. */
        gen_t[] gen_dat = new gen_t[GEN_STACK];
        int[] first_move = new int[MAX_PLY];

        /* the history heuristic array (used for move ordering) */
        int[] history = new int[64 * 64];

        /* we need an array of hist_t's so we can take back the
         moves we make */
        hist_t[] hist_dat = new hist_t[HIST_STACK];

        /* the engine will search for max_time milliseconds or until it finishes
   searching max_depth ply. */
        int max_time;
        int max_depth;

        /* the time when the engine starts searching, and when it should stop */
        int start_time;
        int stop_time;

        int nodes;  /* the number of nodes we've searched */

        /* a "triangular" PV array; for a good explanation of why a triangular
         array is needed, see "How Computers Play Chess" by Levy and Newborn. */
        move[] pv = new move [MAX_PLY*MAX_PLY];
        int[] pv_length = new int [MAX_PLY];
        bool follow_pv;

        /* random numbers used to compute hash; see set_hash() in board.c */
        int[] hash_piece = new int[2*6*64];  /* indexed by piece [color][type][square] */
        int hash_side;
        int[] hash_ep = new int[64];

        /* Now we have the mailbox array, so called because it looks like a
           mailbox, at least according to Bob Hyatt. This is useful when we
           need to figure out what pieces can go where. Let's say we have a
           rook on square a4 (32) and we want to know if it can move one
           square to the left. We subtract 1, and we get 31 (h5). The rook
           obviously can't move to h5, but we don't know that without doing
           a lot of annoying work. Sooooo, what we do is figure out a4's
           mailbox number, which is 61. Then we subtract 1 from 61 (60) and
           see what mailbox[60] is. In this case, it's -1, so it's out of
           bounds and we can forget it. You can see how mailbox[] is used
           in attack() in board.c. */



        int[] mailbox = new int[120] {
	         -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	         -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	         -1,  0,  1,  2,  3,  4,  5,  6,  7, -1,
	         -1,  8,  9, 10, 11, 12, 13, 14, 15, -1,
	         -1, 16, 17, 18, 19, 20, 21, 22, 23, -1,
	         -1, 24, 25, 26, 27, 28, 29, 30, 31, -1,
	         -1, 32, 33, 34, 35, 36, 37, 38, 39, -1,
	         -1, 40, 41, 42, 43, 44, 45, 46, 47, -1,
	         -1, 48, 49, 50, 51, 52, 53, 54, 55, -1,
	         -1, 56, 57, 58, 59, 60, 61, 62, 63, -1,
	         -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	         -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };


       int[] mailbox64= new int[64] {
	    21, 22, 23, 24, 25, 26, 27, 28,
	    31, 32, 33, 34, 35, 36, 37, 38,
	    41, 42, 43, 44, 45, 46, 47, 48,
	    51, 52, 53, 54, 55, 56, 57, 58,
	    61, 62, 63, 64, 65, 66, 67, 68,
	    71, 72, 73, 74, 75, 76, 77, 78,
	    81, 82, 83, 84, 85, 86, 87, 88,
	    91, 92, 93, 94, 95, 96, 97, 98
       };

        /* slide, offsets, and offset are basically the vectors that
   pieces can move in. If slide for the piece is FALSE, it can
   only move one square in any one direction. offsets is the
   number of directions it can move in, and offset is an array
   of the actual directions. */

        bool[] slide = new bool[6] {
	        false, false, true , true , true, false
        };


        int[] offsets = new int[6] {
	        0, 8, 4, 4, 8, 8
        };

        int[] offset = new int[6*8] {
	         0, 0, 0, 0, 0, 0, 0, 0 ,
	         -21, -19, -12, -8, 8, 12, 19, 21 ,
	         -11, -9, 9, 11, 0, 0, 0, 0 ,
	         -10, -1, 1, 10, 0, 0, 0, 0 ,
	         -11, -10, -9, -1, 1, 9, 10, 11 ,
	         -11, -10, -9, -1, 1, 9, 10, 11 
        };

/* This is the castle_mask array. We can use it to determine
the castling permissions after a move. What we do is
logical-AND the castle bits with the castle_mask bits for
both of the move's squares. Let's say castle is 1, meaning
that white can still castle kingside. Now we play a move
where the rook on h1 gets captured. We AND castle with
castle_mask[63], so we have 1&14, and castle becomes 0 and
white can't castle kingside anymore. */

        int[] castle_mask = new int[64] {
	         7, 15, 15, 15,  3, 15, 15, 11,
	        15, 15, 15, 15, 15, 15, 15, 15,
	        15, 15, 15, 15, 15, 15, 15, 15,
	        15, 15, 15, 15, 15, 15, 15, 15,
	        15, 15, 15, 15, 15, 15, 15, 15,
	        15, 15, 15, 15, 15, 15, 15, 15,
	        15, 15, 15, 15, 15, 15, 15, 15,
	        13, 15, 15, 15, 12, 15, 15, 14
        };
        /* the piece letters, for print_board() */
       char[] piece_char = new char[6] {
	        'P', 'N', 'B', 'R', 'Q', 'K'
        };

        /* the initial board state */

        int[] init_color = new int[64] {
	        1, 1, 1, 1, 1, 1, 1, 1,
	        1, 1, 1, 1, 1, 1, 1, 1,
	        6, 6, 6, 6, 6, 6, 6, 6,
	        6, 6, 6, 6, 6, 6, 6, 6,
	        6, 6, 6, 6, 6, 6, 6, 6,
	        6, 6, 6, 6, 6, 6, 6, 6,
	        0, 0, 0, 0, 0, 0, 0, 0,
	        0, 0, 0, 0, 0, 0, 0, 0
        };

        int[] init_piece = new int[64] {
	        3, 1, 2, 4, 5, 2, 1, 3,
	        0, 0, 0, 0, 0, 0, 0, 0,
	        6, 6, 6, 6, 6, 6, 6, 6,
	        6, 6, 6, 6, 6, 6, 6, 6,
	        6, 6, 6, 6, 6, 6, 6, 6,
	        6, 6, 6, 6, 6, 6, 6, 6,
	        0, 0, 0, 0, 0, 0, 0, 0,
	        3, 1, 2, 4, 5, 2, 1, 3
        };

        const string spos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";

        readonly string[] square_name = new string[64] {
        "a8","b8","c8","d8","e8","f8","g8","h8",
        "a7","b7","c7","d7","e7","f7","g7","h7",
        "a6","b6","c6","d6","e6","f6","g6","h6",
        "a5","b5","c5","d5","e5","f5","g5","h5",
        "a4","b4","c4","d4","e4","f4","g4","h4",
        "a3","b3","c3","d3","e3","f3","g3","h3",
        "a2","b2","c2","d2","e2","f2","g2","h2",
        "a1","b1","c1","d1","e1","f1","g1","h1"
        };

        const string num = "0123456789";
    }
}
