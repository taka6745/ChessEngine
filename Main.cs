﻿/*
 *	MAIN.C
 *	Tom Kerrigan's Simple Chess Program (TSCP)
 *
 *	Copyright 1997 Tom Kerrigan
 */
/* with fen and null move capabilities - N.Blais 3/5/05 */
using System;
namespace TSCP_Sharp
{
    public partial class TSCP: IDisposable 
    {

        public TSCP()
        {
            for (int x = 0; x < gen_dat.Length; x++)
                gen_dat[x] = new gen_t();

            Main();
        }
        public void Dispose() 
        { close_book(); }
        
/* main() is basically an infinite loop that either calls
think() when it's the computer's turn to move or prompts
the user for a command (and deciphers it). */
        void Main()
        {  
            int computer_side = 0;
            string s = String.Empty;
            string fens = String.Empty;
            int arg = 0 , p = 0;
            int m = 0;

            Console.WriteLine();
            Console.WriteLine("Tom Kerrigan's Simple Chess Program (TSCP)");
            Console.Write("version 1.81, 2/5/03");
            Console.WriteLine(" (fen and null move capabilities - N.Blais 3/5/05)");
            Console.WriteLine("Copyright 1997 Tom Kerrigan");
            Console.WriteLine();
            Console.WriteLine("\"help\" displays a list of commands.");
            Console.WriteLine();
            init_hash();
            init_board(spos);
            open_book();
            gen();
            computer_side = EMPTY;
            max_time = 1 << 25;
            max_depth = 4;

            while (true)
            {
                if (side == computer_side)/* computer's turn */
                {
                    /* think about the move and make it */
                    think(1);
                    if (pv[0].u <= 0)
                    {
                        Console.WriteLine("(no legal moves)");
                        computer_side = EMPTY;
                        continue;
                    }
                    Console.WriteLine("Computer's move: {0}", move_str(pv[0].b));
                    makemove(ref pv[0].b);
                    ply = 0;
                    gen();
                    print_result();
                    continue;
                }
                /* get user input */
                Console.Write("tscp> ");

                s = Console.ReadLine();

                if ((p = s.IndexOf(' ')) > -1)
                {

                    if ("sb" == s.Substring(0, 2))
                        fens = s.Substring(3);
                    else if (!int.TryParse(s.Substring(p), out arg))
                        continue;

                    s = s.Substring(0, p);
                }

                Console.WriteLine(s);

                switch (s)
                {
                    case "on": computer_side = side;
                        continue;
                    case "off": computer_side = EMPTY;
                        continue;
                    case "fen": print_fen();
                        continue;
                    case "sb":
                        {
                            init_board(fens);
                            continue;
                        }

                    case "st":
                        {
                            max_time = arg * 1000;
                            max_depth = 32;
                            continue;
                        }

                    case "sd":
                        {
                            max_depth = arg;
                            max_time = 1 << 25;
                            continue;
                        }

                    case "undo":
                        {
                            if (hply == 0) continue;
                            computer_side = EMPTY;
                            takeback();
                            ply = 0;
                            gen();
                            continue;
                        }

                    case "new":
                        {
                            computer_side = EMPTY;
                            init_board(spos);
                            gen();
                            continue;
                        }

                    case "d":
                        {
                            print_board();
                            continue;
                        }

                    case "bench":
                        {
                            computer_side = EMPTY;
                            bench();
                            continue;
                        }

                    case "quit": return;

                    case "bye":
                        {
                            Console.WriteLine("Share and enjoy!");
                            return;
                        }

                    case "xboard":
                        {
                            xboard();
                            return;
                        }

                    case "help":
                        {
                            Console.WriteLine("on - computer plays for the side to move");
                            Console.WriteLine("off - computer stops playing");
                            Console.WriteLine("st n - search for n seconds per move");
                            Console.WriteLine("sd n - search n ply per move");
                            Console.WriteLine("undo - takes back a move");
                            Console.WriteLine("new - starts a new game");
                            Console.WriteLine("d - display the board");
                            Console.WriteLine("bench - run the built-in benchmark");
                            Console.WriteLine("bye - exit the program");
                            Console.WriteLine("xboard - switch to XBoard mode");
                            Console.WriteLine("sb s - set the board position using s (fen string)");
                            Console.WriteLine("fen - print the board position as a fen string");
                            Console.WriteLine("Enter moves in coordinate notation, e.g., e2e4, e7e8Q");
                            continue;
                        }

                }
                /* maybe the user entered a move? */
                m = parse_move(s);
                if (m == -1 || !makemove(ref gen_dat[m].m.b))
                    Console.WriteLine("Illegal move.");
                else
                {
                    ply = 0;
                    gen();
                    print_result();
                }
            }
          
        }

/* parse the move s (in coordinate notation) and return the move's
index in gen_dat, or -1 if the move is illegal */

        int parse_move(string s)
        {
            int from, to, i;

            /* make sure the string looks like a move */
            if (s.Length <4 || s[0] < 'a' || s[0] > 'h' ||
                 s[1] < '0' || s[1] > '9' ||
                 s[2] < 'a' || s[2] > 'h' ||
                 s[3] < '0' || s[3] > '9')
                return -1;

            from = s[0] - 'a';
            from += 8 * (8 - (s[1] - '0'));
            to = s[2] - 'a';
            to += 8 * (8 - (s[3] - '0'));

            for (i = 0; i < first_move[1]; ++i)
                if (gen_dat[i].m.b.from == from && gen_dat[i].m.b.to == to)
                {

                    // if the move is a promotion, handle the promotion piece;
                    //   assume that the promotion moves occur consecutively in
                    //   gen_dat. */
                    if ((gen_dat[i].m.b.bits & 32) != 0)
                        switch (s[4])
                        {
                            case 'N':
                            case 'n':
                                return i;
                            case 'B':
                            case 'b':
                                return i + 1;
                            case 'R':
                            case 'r':
                                return i + 2;
                            default:  /* assume it's a queen */
                                return i + 3;
                        }
                    return i;
                }

            /* didn't find the move */
            return -1;
        }

        /* move_str returns a string with move m in coordinate notation */

        string  move_str(move_bytes m)
        {
            string str;

	        char c;

            if ((m.bits & 32) != 0)
            {
                switch (m.promote)
                {
                    case KNIGHT:
                        c = 'n';
                        break;
                    case BISHOP:
                        c = 'b';
                        break;
                    case ROOK:
                        c = 'r';
                        break;
                    default:
                        c = 'q';
                        break;
                }
                str = string.Format("{0}{1:D}{2}{3:D}{4}",
                        (char)((m.from & 7) + 'a'),
                        8 - (m.from >> 3),
                        (char)((m.to & 7) + 'a'),
                        8 - (m.to >> 3),
                        c);
            }
            else
                str = string.Format("{0}{1:D}{2}{3:D}",
                        (char)((m.from & 7) + 'a'),
                        8 - (m.from >> 3),
                        (char)((m.to & 7) + 'a'),
                        8 - (m.to >> 3));
                           
	        return str;
        }

        /* print_board() prints the board */

        void print_board()
        {
            int i;
            Console.Write("\n8 ");
            for (i = 0; i < 64; ++i)
            {
                switch (color[i])
                {
                    case EMPTY:
                        Console.Write(" .");
                        break;
                    case LIGHT:
                        Console.Write(" "+piece_char[piece[i]]);
                        break;
                    case DARK:
                        Console.Write((" "+piece_char[piece[i]]).ToLower());
                        break;
                }
                if ((i + 1) % 8 == 0 && i != 63)
                    Console.Write("\n{0} ",7 - (i >> 3));
            }
            Console.WriteLine("\n\n" + "   a b c d e f g h\n");
        }


        void xboard()
        {
            int computer_side = 0;
            string command = String.Empty;
            int m = 0;
            int post = 0;
            int p = 0;
            int arg = 0;

            Console.WriteLine();
            init_board(spos);
            gen();
            computer_side = EMPTY;

            while (true)
            {
                Console.Out.Flush();
                if (side == computer_side)/* computer's turn */
                {
                    /* think about the move and make it */
                    think(post);
                    if (pv[0].u <= 0)
                    {
                        computer_side = EMPTY;
                        continue;
                    }
                    Console.WriteLine("move {0}", move_str(pv[0].b));
                    makemove(ref pv[0].b);
                    ply = 0;
                    gen();
                    print_result();
                    continue;
                }

                command = Console.ReadLine();

                if ((p = command.IndexOf(' ')) > -1)
                {
                    if (!int.TryParse(command.Substring(p), out arg)) continue;
                    command = command.Substring(0, p);
                }

                switch (command)
                {
                    case "xboard":continue;
                    case "new": 
                        {
                          init_board(spos);
                          gen();
                          computer_side = DARK;
                          continue;
                        }
                    case "quit": return;
                    case "force":
                        {
                          computer_side = EMPTY;
                          continue;
                        }
                    case "white":
                        {
                          side = LIGHT;
                          xside = DARK;
                          gen();
                          computer_side = DARK;
                          continue;
                        }
                    case "black":
                        {
                          side = DARK;
                          xside = LIGHT;
                          gen();
                          computer_side = LIGHT;
                          continue;
                        }
                    case "st":
                        {
                          max_time = arg * 1000;
                          max_depth = 32;
                          continue;
                        }
                    case "sd":
                        {
                          max_depth = arg;
                          max_time = 1 << 25;
                          continue;
                        }
                    case "time":
                        {
                          max_time = arg * 10; 
                          max_time /= 30;
                          max_depth = 32;
                          continue;
                        }
                    case "otim": continue;
                    case "go":
                        {
                          computer_side = side;
                          continue;
                        }
                    case "hint":
                        {
                          think(0);
                          if (pv[0].u <= 0)
                              continue;
                          Console.WriteLine("Hint: {0}", move_str(pv[0].b));
                          continue;
                        }
                    case "undo":
                        {
                          if (hply < 2 ) continue; 
                          computer_side = EMPTY;
                          takeback();
                          ply = 0;
                          gen();
                          continue;
                        }
                    case "remove":
                        {
                          if (hply < 2) continue;
                          takeback();
                          takeback();
                          ply = 0;
                          gen();
                          continue;
                        }
                    case "post":
                        {
                          post = 2;
                          continue;
                        }
                    case "nopost":
                        {
                          post = 0;
                          continue;
                        }
                }

                m = parse_move(command);
                if (m == -1 || !makemove(ref gen_dat[m].m.b))
                    Console.WriteLine("Illegal move.");
                else
                {
                    ply = 0;
                    gen();
                    print_result();
                }

            }

        }

/* print_result() checks to see if the game is over, and if so,
prints the result. */

        void print_result()
        {
            int i;

            /* is there a legal move? */
            for (i = 0; i < first_move[1]; ++i)
                if (makemove(ref gen_dat[i].m.b))
                {
                    takeback();
                    break;
                }
            if (i == first_move[1])
            {
                if (in_check(side))
                {
                    if (side == LIGHT)
                        Console.WriteLine("0-1 {Black mates}");
                    else
                        Console.WriteLine("1-0 {White mates}");
                }
                else
                    Console.WriteLine("1/2-1/2 {Stalemate}");
            }
            else if (reps() == 2)
                Console.WriteLine("1/2-1/2 {Draw by repetition}");
            else if (fifty >= 100)
                Console.WriteLine("1/2-1/2 {Draw by fifty move rule}");
        }

/* bench: This is a little benchmark code that calculates how many
   nodes per second TSCP searches.
   It sets the position to move 17 of Bobby Fischer vs. J. Sherwin,
   New Jersey State Open Championship, 9/2/1957.
   Then it searches five ply three times. It calculates nodes per
   second from the best time. */

        int[] bench_color = new int[64] {
	        6, 1, 1, 6, 6, 1, 1, 6,
	        1, 6, 6, 6, 6, 1, 1, 1,
	        6, 1, 6, 1, 1, 6, 1, 6,
	        6, 6, 6, 1, 6, 6, 0, 6,
	        6, 6, 1, 0, 6, 6, 6, 6,
	        6, 6, 0, 6, 6, 6, 0, 6,
	        0, 0, 0, 6, 6, 0, 0, 0,
	        0, 6, 0, 6, 0, 6, 0, 6};


        int[] bench_piece = new int[64] {
	        6, 3, 2, 6, 6, 3, 5, 6,
	        0, 6, 6, 6, 6, 0, 0, 0,
	        6, 0, 6, 4, 0, 6, 1, 6,
	        6, 6, 6, 1, 6, 6, 1, 6,
	        6, 6, 0, 0, 6, 6, 6, 6,
	        6, 6, 0, 6, 6, 6, 0, 6,
	        0, 0, 4, 6, 6, 0, 2, 0,
	        3, 6, 2, 6, 3, 6, 5, 6};

        void bench()
        {
	        int i;
	        int[] t = new int [3];
	        double nps;

	        // setting the position to a non-initial position confuses the opening
	        //   book code. */
	        close_book();

	        for (i = 0; i < 64; ++i) {
		        color[i] = bench_color[i];
		        piece[i] = bench_piece[i];
	        }
	        side = LIGHT;
	        xside = DARK;
	        castle = 0;
	        ep = -1;
	        fifty = 0;
	        ply = 0;
	        hply = 0;
	        set_hash();
	        print_board();
	        max_time = 1 << 25;
	        max_depth = 5;
	        for (i = 0; i < 3; ++i) {
		        think(1);
                t[i] = Environment.TickCount - start_time;
                Console.WriteLine("Time: {0} ms", t[i]);
	        }
	        if (t[1] < t[0])
		        t[0] = t[1];
	        if (t[2] < t[0])
		        t[0] = t[2];
            Console.WriteLine();
            Console.WriteLine("Nodes: {0}", nodes);
            Console.WriteLine("Best time: {0} ms", t[0]);

	        if (t[0] == 0) {
                Console.WriteLine("(invalid)");
		        return;
	        }
	        nps = (double)nodes / (double)t[0];
	        nps *= 1000.0;

	        /* Score: 1.000 = my Athlon XP 2000+ */
            Console.WriteLine("Nodes per second: {0} (Score: {1:0.###})", (int)nps, (float)nps / 243169.0);

	        init_board(spos);
	        open_book();
	        gen();
        }

    }
}
