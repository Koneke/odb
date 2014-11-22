using System;
using System.Collections.Generic;

namespace ODB
{
    class Brain
    {
        //maybe I should stop littering these Game1s about
        //but they are comf... conv(enient)-y..?
        //coinin' it. convy.
        public static Game1 Game;
        Actor meatPuppet;
        public int Cooldown;

        public Brain(Actor meatPuppet)
        {
            this.meatPuppet = meatPuppet;
        }

        public void Tick()
        {
            List<Room> route = 
                Util.FindRouteToPoint(
                    meatPuppet.xy, Game.player.xy
                );
            Point goal;
            //if (route.Count > 0)
            if (route != null)
                goal = Util.NextGoalOnRoute(meatPuppet.xy, route);
            else
                goal = Game.player.xy;

            Point offset = new Point(
                goal.x - meatPuppet.xy.x,
                goal.y - meatPuppet.xy.y 
            );
            if (offset.x > 1) offset.x = 1;
            if (offset.x <-1) offset.x = -1;
            if (offset.y > 1) offset.y = 1;
            if (offset.y <-1) offset.y = -1;

            Point target = offset + meatPuppet.xy;

            if (Util.ActorsOnTile(target).Contains(Game.player))
            {
                meatPuppet.Attack(Game.player);
                Cooldown = 10; //combat cost
            }
            else
            {
                Point moveTo;

                //todo: respect other actors

                if (Game.map[target.x, target.y].solid == false)
                    moveTo = meatPuppet.xy + offset;
                else if (Game.map[target.x, meatPuppet.xy.y].solid == false)
                    moveTo = meatPuppet.xy + new Point(offset.x, 0);
                else if (Game.map[meatPuppet.xy.x, target.y].solid == false)
                    moveTo = meatPuppet.xy + new Point(0, offset.y);
                else throw new Exception(
                    "Bad things are happening to either me or mathematics."
                );

                if (Game.map[moveTo.x, moveTo.y].doorState == Door.Closed)
                {
                    Game.map[moveTo.x, moveTo.y].doorState = Door.Open;
                    if (Game.vision[moveTo.x, moveTo.y])
                        Game.log.Add(meatPuppet.name + " opens a door.");
                }
                else
                {
                    meatPuppet.xy = moveTo;
                }

                Cooldown = 10; //movement cost
            }
        }
    }
}
