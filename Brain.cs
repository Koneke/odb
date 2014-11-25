﻿using System;
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
            if (!Game.Level.WorldActors.Contains(MeatPuppet))
                return;

            List<Room> route = 
                Util.FindRouteToPoint(
                    MeatPuppet.xy, Game.player.xy
                );
            Point goal;
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

            if (Game.Level.ActorOnTile(target) == Game.player)
            {
                Spell touchAttack = null;
                //if we have an available, guaranteed castable touch attack
                //just use that instead
                //in the future, consider using non guaranteed as well
                //and actually use them at range at times
                foreach (Spell spell in MeatPuppet.Spellbook)
                    if (
                        spell.CastDifficulty <=
                        MeatPuppet.Get(Stat.Intelligence) + 1 &&
                        spell.Range >= 1
                    ) touchAttack = spell;
                if (touchAttack == null)
                    MeatPuppet.Attack(Game.player);
                else
                    MeatPuppet.Cast(touchAttack, Game.player.xy, true);
                MeatPuppet.Pass();
            }
            else
            {
                Point moveTo;

                //todo: respect other actors

                if (Game.Level.Map[target.x, target.y].solid == false)
                {
                    moveTo = MeatPuppet.xy + offset;
                }
                else if (
                    Game.Level.Map[
                        target.x,
                        MeatPuppet.xy.y
                    ].solid == false
                ) {
                    moveTo = MeatPuppet.xy + new Point(offset.x, 0);
                }
                else if (
                    Game.Level.Map[
                        MeatPuppet.xy.x,
                        target.y
                    ].solid == false
                ) {
                    moveTo = MeatPuppet.xy + new Point(0, offset.y);
                }
                else
                {
                    throw new Exception(
                        "Bad things are happening to either me or mathematics."
                    );
                }

                if (Game.Level.Map[moveTo.x, moveTo.y].door == Door.Closed)
                {
                    Game.Level.Map[moveTo.x, moveTo.y].door = Door.Open;
                    if (Game.player.Vision[moveTo.x, moveTo.y])
                    {
                        Game.log.Add(
                            MeatPuppet.Definition.name +
                            " opens a door."
                        );
                    }
                }
                else MeatPuppet.xy = moveTo;

                MeatPuppet.Pass(true);
            }
        }
    }
}
