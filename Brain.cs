using System.Collections.Generic;

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

        public void Tick()
        {
            if (MeatPuppet.LevelID != Game.Level.ID)
                return;

            if (!MeatPuppet.Awake) return;

            if (MeatPuppet.HasEffect(StatusType.Stun))
            {
                MeatPuppet.Pass(Game.StandardActionLength);
                return;
            }

            List<Room> route = 
                Util.FindRouteToPoint(
                    MeatPuppet.xy, Game.Player.xy
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

            Point target = offset + MeatPuppet.xy;

            if (Game.Level.ActorOnTile(target) == Game.Player)
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

                MeatPuppet.Pass();
            }
            else
            {
                List<Point> possibleMoves = MeatPuppet.GetPossibleMoves(true);

                bool xy =
                    possibleMoves.Contains(offset) ||
                    DoorAt(MeatPuppet.xy + offset, Door.Closed);
                bool x = possibleMoves.Contains(
                    new Point(offset.x, 0)) ||
                    DoorAt(MeatPuppet.xy + new Point(offset.x, 0), Door.Closed);
                bool y = possibleMoves.Contains(
                    new Point(0, offset.y)) ||
                    DoorAt(MeatPuppet.xy + new Point(0, offset.y), Door.Closed);

                     if (!xy && x) offset.y = 0;
                else if (!xy && y) offset.x = 0;

                Point moveTo = MeatPuppet.xy + offset;
                
                if (xy || x || y)
                {
                    if (Game.Level.Map[
                        moveTo.x,
                        moveTo.y
                    ].Door == Door.Closed) {
                        Game.Level.Map[moveTo.x, moveTo.y].Door = Door.Open;
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

                MeatPuppet.Pass(true);
            }
        }

        private bool DoorAt(Point p, Door d)
        {
            if (Game.Level.Map[p.x, p.y] == null) return false;
            return Game.Level.Map[p.x, p.y].Door == d;
        }
    }
}
