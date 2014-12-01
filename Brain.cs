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
            if (!Game.Level.WorldActors.Contains(MeatPuppet))
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
                else MeatPuppet.Cast(touchAttack, Game.Player.xy, true);

                MeatPuppet.Pass();
            }
            else
            {
                Point moveTo = new Point(0, 0);

                List<Point> possibleMoves = MeatPuppet.GetPossibleMoves(true);

                bool xy = possibleMoves.Contains(new Point(offset.x, offset.y));
                bool x = possibleMoves.Contains(new Point(offset.x, 0));
                bool y = possibleMoves.Contains(new Point(0, offset.y));

                if (xy)  moveTo = MeatPuppet.xy + offset; 
                else if (x) moveTo = MeatPuppet.xy + new Point(offset.x, 0);
                else if (y) moveTo = MeatPuppet.xy + new Point(0, offset.y);
                
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
                                MeatPuppet.Definition.Name +
                                " opens a door."
                            );
                        }
                    }
                    else MeatPuppet.TryMove(offset);
                }

                MeatPuppet.Pass(true);
            }
        }
    }
}
