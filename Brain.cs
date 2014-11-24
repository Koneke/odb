using System;
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
            this.MeatPuppet = meatPuppet;
        }

        public void Tick()
        {
            if (!Game.worldActors.Contains(MeatPuppet))
                return;

            List<Room> route = 
                Util.FindRouteToPoint(
                    MeatPuppet.xy, Game.player.xy
                );
            Point goal;
            //if (route.Count > 0)
            if (route != null)
                goal = Util.NextGoalOnRoute(MeatPuppet.xy, route);
            else
                goal = Game.player.xy;

            Point offset = new Point(
                goal.x - MeatPuppet.xy.x,
                goal.y - MeatPuppet.xy.y 
            );
            if (offset.x > 1) offset.x = 1;
            if (offset.x <-1) offset.x = -1;
            if (offset.y > 1) offset.y = 1;
            if (offset.y <-1) offset.y = -1;

            Point target = offset + MeatPuppet.xy;

            if (Util.ActorsOnTile(target).Contains(Game.player))
            {
                MeatPuppet.Attack(Game.player);
                MeatPuppet.Cooldown = 10; //combat cost
            }
            else
            {
                Point moveTo;

                //todo: respect other actors

                if (Game.map[target.x, target.y].solid == false)
                    moveTo = MeatPuppet.xy + offset;
                else if (Game.map[target.x, MeatPuppet.xy.y].solid == false)
                    moveTo = MeatPuppet.xy + new Point(offset.x, 0);
                else if (Game.map[MeatPuppet.xy.x, target.y].solid == false)
                    moveTo = MeatPuppet.xy + new Point(0, offset.y);
                else throw new Exception(
                    "Bad things are happening to either me or mathematics."
                );

                if (Game.map[moveTo.x, moveTo.y].doorState == Door.Closed)
                {
                    Game.map[moveTo.x, moveTo.y].doorState = Door.Open;
                    if (Game.player.Vision[moveTo.x, moveTo.y])
                        Game.log.Add(MeatPuppet.Definition.name +
                            " opens a door."
                        );
                }
                else
                {
                    MeatPuppet.xy = moveTo;
                }

                MeatPuppet.Cooldown = 10; //movement cost
            }
        }
    }
}
