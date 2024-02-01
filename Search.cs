﻿/*
 *	SEARCH.C
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
      const bool GONULL = true;
      bool stop_search;

/* think() calls search() iteratively. Search statistics
are printed depending on the value of output:
0 = no output
1 = normal output
2 = xboard format output */
      void think(int output)
      {
          int i, j, x;

          /* try the opening book first */
          pv[0].u = book_move();
          if (pv[0].u != -1)
              return;

          //some code that lets us longjmp back here and return
          //   from think() when our time is up */

          stop_search = false;
          start_time = Environment.TickCount;
          stop_time = start_time + max_time;

          ply = 0;
          nodes = 0;

          Array.Clear(pv, 0, pv.Length);
          Array.Clear(history, 0, history.Length);

          if (output == 1)
              Console.WriteLine("ply      nodes  score  pv");
          for (i = 1; i <= max_depth; ++i)
          {
              follow_pv = true;
              x = search(-10000, 10000, i, true);
              if (stop_search)
              {
                  break;
              }
              if (output == 1)
                  Console.Write("{0,3}  {1,9}  {2,5} ", i, nodes, x);
              else if (output == 2)
                  Console.Write("{0} {1} {2} {3}",
                          i, x, (Environment.TickCount - start_time) / 10, nodes);
              if (output>0)
              {
                  for (j = 0; j < pv_length[0]; ++j)
                      Console.Write(" {0}", move_str(pv[0 * MAX_PLY + j].b));
                  Console.WriteLine();
                  Console.Out.Flush();
              }
              if (x > 9000 || x < -9000)
                  break;
          }
      }

      /* search() does just that, in negamax fashion */

      int search(int alpha, int beta, int depth, bool null_move)
      {
          int i, j, x;
          bool c, f;
          int  nullmat;
          int o_side;
          int o_xside;
          int o_ep;
          int o_fifty;
          int o_hash;
          int o_castle;

          // we're as deep as we want to be; call quiesce() to get
          //   a reasonable score and return it. */
          if (depth<=0)
              return quiesce(alpha, beta);
          ++nodes;

          /* do some housekeeping every 1024 nodes */
          if ((nodes & 1023) == 0)
              checkup();

          pv_length[ply] = ply;

          // if this isn't the root of the search tree (where we have
          //   to pick a move and can't simply return 0) then check to
          //   see if the position is a repeat. if so, we can assume that
          //   this line is a draw and return 0. */
          if (ply>0 && reps()>0)
              return 0;

          /* are we too deep? */
          if (ply >= MAX_PLY - 1)
              return eval();
          if (hply >= HIST_STACK - 1)
              return eval();

          /* are we in check? if so, we want to search deeper */
          c = in_check(side);
          if (c)
              ++depth;

          /* null move */
          if (GONULL && !c && null_move && ply>0)
          {
              nullmat = 0;
              for (i = 0; i < 64; ++i)
              {
                  if (piece[i] != EMPTY && piece[i] != PAWN && color[i] == side)
                  {
                      nullmat += piece_value[piece[i]];
                  }
              }
              if (depth > (nullmat > 1500 ? 3 :2))
              {
                  o_side = side;
                  o_xside = xside;
                  o_ep = ep;
                  o_fifty = fifty;
                  o_hash = hash;
                  o_castle = castle;
                  ep = -1;
                  fifty = 0;
                  side = xside;
                  xside = o_side;
                  x = -search(-beta, -beta + 1, depth - 1 - (nullmat > 1500 ? 3 : 2), false);
                  side = o_side;
                  xside = o_xside;
                  ep = o_ep;
                  fifty = o_fifty;
                  hash = o_hash;
                  castle = o_castle;
                  if (stop_search)
                  {
                      return 0;
                  }
                  if (x >= beta)
                  {
                      return beta;
                  }
              }
          }

          gen();
          if (follow_pv)  /* are we following the PV? */
              sort_pv();
          f = false;

          /* loop through the moves */
          for (i = first_move[ply]; i < first_move[ply + 1]; ++i)
          {
              sort(i);
              if (!makemove(ref gen_dat[i].m.b))
                  continue;
              f = true;
              x = -search(-beta, -alpha, depth - 1, true);
              takeback();
              if (stop_search)
              {
                  return 0;
              }
              if (x > alpha)
              {
                  //this move caused a cutoff, so increase the history
                  //   value so it gets ordered high next time we can
                  //   search it */
                  history[(int)gen_dat[i].m.b.from* 64 + (int)gen_dat[i].m.b.to] += depth;
                  if (x >= beta)
                      return beta;
                  alpha = x;

                  /* update the PV */
                  pv[ply * MAX_PLY + ply] = gen_dat[i].m;
                  for (j = ply + 1; j < pv_length[ply + 1]; ++j)
                      pv[ply * MAX_PLY + j] = pv[(ply + 1) * MAX_PLY + j];
                  pv_length[ply] = pv_length[ply + 1];
              }
          }

          /* no legal moves? then we're in checkmate or stalemate */
          if (!f)
          {
              if (c)
                  return -10000 + ply;
              else
                  return 0;
          }

          /* fifty move draw rule */
          if (fifty >= 100)
              return 0;
          return alpha;
      }

/* quiesce() is a recursive minimax search function with
alpha-beta cutoffs. In other words, negamax. It basically
only searches capture sequences and allows the evaluation
function to cut the search off (and set alpha). The idea
is to find a position where there isn't a lot going on
so the static evaluation function will work. */

      int quiesce(int alpha, int beta)
      {
          int i, j, x;

          ++nodes;

          /* do some housekeeping every 1024 nodes */
          if ((nodes & 1023) == 0)
              checkup();

          pv_length[ply] = ply;

          /* are we too deep? */
          if (ply >= MAX_PLY - 1)
              return eval();
          if (hply >= HIST_STACK - 1)
              return eval();

          /* check with the evaluation function */
          x = eval();
          if (x >= beta)
              return beta;
          if (x > alpha)
              alpha = x;

          gen_caps();
          if (follow_pv)  /* are we following the PV? */
              sort_pv();

          /* loop through the moves */
          for (i = first_move[ply]; i < first_move[ply + 1]; ++i)
          {
              sort(i);
              if (!makemove(ref gen_dat[i].m.b))
                  continue;
              x = -quiesce(-beta, -alpha);
              takeback();
              if (stop_search)
              {
                  return 0;
              }
              if (x > alpha)
              {
                  if (x >= beta)
                      return beta;
                  alpha = x;

                  /* update the PV */
                  pv[ply * MAX_PLY + ply] = gen_dat[i].m;
                  for (j = ply + 1; j < pv_length[ply + 1]; ++j)
                      pv[ply * MAX_PLY + j] = pv[(ply + 1) * MAX_PLY + j];
                  pv_length[ply] = pv_length[ply + 1];
              }
          }
          return alpha;
      }


/* reps() returns the number of times the current position
has been repeated. It compares the current value of hash
to previous values. */
        int reps()
        {
            int i;
            int r = 0;

            for (i = hply - fifty; i < hply; ++i)
                if (hist_dat[i].hash == hash)
                    ++r;
            return r;
        }

/* sort_pv() is called when the search function is following
the PV (Principal Variation). It looks through the current
ply's move list to see if the PV move is there. If so,
it adds 10,000,000 to the move's score so it's played first
by the search function. If not, follow_pv remains FALSE and
search() stops calling sort_pv(). */

        void sort_pv()
        {
            int i;

            follow_pv = false;
            for (i = first_move[ply]; i < first_move[ply + 1]; ++i)
                if (gen_dat[i].m.u == pv[0 * MAX_PLY + ply].u)
                {
                    follow_pv = true;
                    gen_dat[i].score += 10000000;
                    return;
                }
        }

/* sort() searches the current ply's move list from 'from'
to the end to find the move with the highest score. Then it
swaps that move and the 'from' move so the move with the
highest score gets searched next, and hopefully produces
a cutoff. */

        void sort(int from)
        {
            int i;
            int bs;  /* best score */
            int bi;  /* best i */
            gen_t g;

            bs = -1;
            bi = from;
            for (i = from; i < first_move[ply + 1]; ++i)
                if (gen_dat[i].score > bs)
                {
                    bs = gen_dat[i].score;
                    bi = i;
                }
            g = gen_dat[from];
            gen_dat[from] = gen_dat[bi];
            gen_dat[bi] = g;
        }

        /* checkup() is called once in a while during the search. */

        void checkup()
        {
            //is the engine's time up? if so, longjmp back to the
            //   beginning of think() */
            if (Environment.TickCount >= stop_time)
            {
                stop_search = true;
            }
        }

    }
}
