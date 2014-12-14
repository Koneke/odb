using System;
using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    public class Brain
    {
        //maybe I should stop littering these Game1s about
        //but they are comf... conv(enient)-y..?
        //coinin' it. convy.
        public static Game1 Game;
        public Actor MeatPuppet;

        public Brain(Actor meatPuppet)
        {
            MeatPuppet = meatPuppet;
        }

        private Point NextMove(Point target)
        {
            List<Room> route = 
                Util.FindRouteToPoint(
                    MeatPuppet.xy, target 
                );
            Point goal = route == null ?
                Game.Player.xy :
                Util.NextGoalOnRoute(MeatPuppet.xy, route);

            Point offset = new Point(
                goal.x - MeatPuppet.xy.x,
                goal.y - MeatPuppet.xy.y 
            );

            if (offset.x > 1) offset.x = 1;
            if (offset.x <-1) offset.x = -1;
            if (offset.y > 1) offset.y = 1;
            if (offset.y <-1) offset.y = -1;

            return offset;
        }

        private bool CanAttack(Actor target)
        {
            return Game.Level
                .At(MeatPuppet.xy).Neighbours
                .Any(ti => ti.Actor == target);
        }

        private void Attack(Actor target)
        {
            //if we have an available, guaranteed castable touch attack
            //just use that instead
            //in the future, consider using non guaranteed as well
            //and actually use them at range at times
            Spell touchAttack = MeatPuppet.Spellbook.Find(
                spell =>
                    spell.CastDifficulty <=
                    MeatPuppet.Get(Stat.Intelligence) &&
                    spell.Range >= 1);

            if (touchAttack == null) MeatPuppet.Attack(Game.Player);
            else
            {
                Game.Caster = MeatPuppet;
                Game.QpAnswerStack.Push(IO.Write(Game.Player.xy));
                touchAttack.Cast();
            }
        }

        private void Chase()
        {
            //todo: currently hardcoded to player, absolutely fine for now
            Point offset = NextMove(Game.Player.xy);

            List<Point> possibleMoves = MeatPuppet.GetPossibleMoves(true);

            Func<Point, bool> isValid = p =>
                possibleMoves.Contains(p) ||
                Game.Level.At(MeatPuppet.xy + p).Door == Door.Closed
            ;

            bool xy = isValid(offset);
            bool x = isValid(new Point(offset.x, 0));
            bool y = isValid(new Point(0, offset.y));

                 if (!xy && x) offset.y = 0;
            else if (!xy && y) offset.x = 0;

            Point moveTo = MeatPuppet.xy + offset;

            if (!xy && !x && !y) return;

            if(Game.Level.At(moveTo).Door == Door.Closed)
            {
                Game.Level.At(moveTo).Door = Door.Open;
                if (Game.Player.Vision[moveTo.x, moveTo.y])
                {
                    Game.Log(
                        MeatPuppet.GetName("Name") +
                            " opens a door."
                        );
                }
            }
            else MeatPuppet.TryMove(offset);
        }

        public void Tick()
        {
            if (MeatPuppet.LevelID != Game.Level.ID)
                return;

            if (!MeatPuppet.Awake) return;
            if (!Game.Player.IsAlive) return;

            if (MeatPuppet.HasEffect(StatusType.Stun))
            {
                MeatPuppet.Pass(Game.StandardActionLength);
                return;
            }

            if(CanAttack(Game.Player))
            {
                Attack(Game.Player);
                MeatPuppet.Pass();
            }
            else
            {
                Chase();
                MeatPuppet.Pass(true);
            }
        }
    }
}
