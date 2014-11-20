﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using System.Globalization;
using System.Threading;

namespace Twilight_s_Auto_Carry___Kalista
{
    class Program
    {
        public static HpBarIndicator hpi = new HpBarIndicator();
        private static Menu Config;
        private static Obj_AI_Hero target;
        private static Obj_AI_Hero myHero;
        private static Spell Q = new Spell(SpellSlot.Q,1450);
        private static Spell W = new Spell(SpellSlot.W,5500);
        private static Spell E = new Spell(SpellSlot.E,1050);
        private static Spell R = new Spell(SpellSlot.R,1200);
        
        static void Main(string[] args)
        {
            if (myHero.ChampionName != "Kalista")
                return;
            Game.PrintChat("=========================");
            Game.PrintChat("| Twilight Auto Carry   |");
            Game.PrintChat("=========================");
            Game.PrintChat("Loading kalista plugin!");
            CustomEvents.Game.OnGameLoad += Load;
            Game.PrintChat("Kalista loaded!");
            Game.PrintChat("=========================");
        }
        public static void Load(EventArgs args)
        {
            Config = new Menu("TAC: Kalista", "Kalista", true);
            Config.AddItem(new MenuItem("", "Version: 1.0.5"));
            Config.AddItem(new MenuItem("", "============"));
            Config.AddItem(new MenuItem("", "Not working:"));
            Config.AddItem(new MenuItem("", "WallHop options/draw"));
            Config.AddItem(new MenuItem("", "============"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            var orbwalkerMenu = new Menu("LX-Orbwalker", "Orbwalker");
            LXOrbwalker.AddToMenu(orbwalkerMenu);
            Config.AddSubMenu(orbwalkerMenu);
            
            Config.AddSubMenu(new Menu("AutoCarry options", "ac"));
            Config.SubMenu("ac").AddItem(new MenuItem("UseQAC", "Use Q").SetValue(true));
            Config.SubMenu("ac").AddItem(new MenuItem("UseEAC", "Use E").SetValue(true));
            Config.SubMenu("ac").AddItem(new MenuItem("Detonate", "Use E at").SetValue(new Slider(1, 1, 40)));
            Config.SubMenu("ac").AddItem(new MenuItem("DetonateAuto", "Auto E").SetValue(true));
            Config.SubMenu("ac").AddItem(new MenuItem("QManaMinAC", "Min Q Mana %").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("ac").AddItem(new MenuItem("EManaMinAC", "Min E Mana %").SetValue(new Slider(35, 1, 100)));

            Config.AddSubMenu(new Menu("Harass options", "harass"));
            Config.SubMenu("harass").AddItem(new MenuItem("stackE", "E stacks to cast").SetValue(new Slider(1, 1, 10)));
            Config.SubMenu("harass").AddItem(new MenuItem("EManaMinHS", "Min E Mana %").SetValue(new Slider(35, 1, 100)));


            Config.AddSubMenu(new Menu("Wall Hop options", "wh"));
            Config.SubMenu("wh").AddItem(new MenuItem("drawSpot", "Draw WallHop spots").SetValue(true));
            Config.SubMenu("wh").AddItem(new MenuItem("whk", "WallHop key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            
            var extras = new Menu("Extras", "Extras");
            new PotionManager(extras);
            Config.AddSubMenu(extras);

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("drawText", "Draw text").SetValue(true)));
            Config.AddToMainMenu();

            Config.AddItem(new MenuItem("Packets", "Packet Casting").SetValue(true));

            Config.AddItem(new MenuItem("debug", "Debug").SetValue(true));


            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnEndScene += OnEndScene;
        }
        public static float getPerValue(bool mana)
        {
            if (mana) return (myHero.Mana / myHero.MaxMana) * 100;
            return (myHero.Health / myHero.MaxHealth) * 100;
        }
        public static void OnGameUpdate(EventArgs args)
        {
            if (myHero.IsDead) return;
            if (Config.Item("whk").GetValue<bool>()) WallHop();
            switch (LXOrbwalker.CurrentMode)
            {
                case LXOrbwalker.Mode.Combo:
                    Combo();
                    break;
                case LXOrbwalker.Mode.Harass:
                    Harass();
                    break;
            }

        }
        public static void WallHop()
        {

        }
        private static float GetRealDistance(GameObject target)
        {
            return ObjectManager.Player.Position.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
            target.BoundingRadius;
        }
        public static void Combo()
        {
            var ManaQ = Config.Item("QManaMinAC").GetValue<Slider>().Value;
            var ManaE = Config.Item("EManaMinAC").GetValue<Slider>().Value;
            var distance = GetRealDistance(target);
            if (Q.IsReady() && distance > Q.Range && getPerValue(true) >= ManaQ)
            {
                Q.Cast(target, packetCast());
            }
            if (E.IsReady() && distance > E.Range && target.Health >= getDamageToTarget(target) && getPerValue(true) >= ManaE)
            {
                E.Cast(target, packetCast());
            }
        }
        private static bool packetCast()
        {
            return Config.Item("Packets").GetValue<bool>();
        }
        public static void Harass()
        {
            //var minList = MinionManager.GetMinions(myHero.Position, 550f).Where(min => min.Health < Q.GetDamage(min));
            
            var ManaE = Config.Item("EManaMinHS").GetValue<Slider>().Value;
            foreach (var buff in target.Buffs.Where(buff => buff.DisplayName.ToLower() == "kalistarend").Where(buff => buff.Count == Config.Item("stackE").GetValue<Slider>().Value))
            {
                if (E.IsReady() && getPerValue(true) >= ManaE)
                    E.Cast(target, packetCast());
            }

        }
        public static int getDamageToTarget(Obj_AI_Hero target)
        {
            foreach (var buff in target.Buffs.Where(buff => buff.DisplayName.ToLower() == "kalistarend").Where(buff => buff.Count == 6))
            {
                return (int)target.GetSpellDamage(myHero, SpellSlot.E) * buff.Count;
            }
            return 0;
        }

        private static void OnEndScene(EventArgs args)
        {
            if (Config.Item("drawDamage").GetValue<bool>())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                {
                    hpi.unit = enemy;
                    hpi.drawDmg(CalculateRendDamage(enemy), Color.Yellow);
                }
            }
        }
        private static int getTotalAttacksE(Obj_AI_Hero target)
        {
            int first = (int)(target.Health/myHero.GetAutoAttackDamage(target));
            int second = (int)(target.Health - (E.GetDamage(target)*first));

            return first;
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if(Config.Item("drawText").GetValue<bool>())
            {
                if (target != null && !target.IsDead && !myHero.IsDead)
                {
                    var wts = Drawing.WorldToScreen(target.Position);
                    int tan = getTotalAttacksE(target);

                    Drawing.DrawText(wts[0] - 40, wts[1] + 70, Color.OrangeRed, "Combo "+getTotalAttacksE(target)+" AA + E");
                }

            }



            var drawQ = Config.Item("QRange").GetValue<Circle>();
            if (drawQ.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, Q.Range, drawQ.Color);
            }

            var drawW = Config.Item("WRange").GetValue<Circle>();
            if (drawW.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, W.Range, drawW.Color);
            }
            var drawE = Config.Item("ERange").GetValue<Circle>();
            if (drawE.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, E.Range, drawE.Color);
            }

            var drawR = Config.Item("RRange").GetValue<Circle>();
            if (drawR.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, R.Range, drawR.Color);
            }
            try
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                {
                    hpi.unit = enemy;
                    if (CalculateRendDamage(enemy) >= enemy.Health)
                    {
                        hpi.drawDmg(CalculateRendDamage(enemy), Color.Red);
                    }
                    else
                    {
                        hpi.drawDmg(CalculateRendDamage(enemy), Color.Yellow);
                    }
                }
            }
            catch (Exception ex)
            {
                Game.PrintChat("Failed to draw HP bar damage! => " + ex);
            }
        }

        public static float CalculateRendDamage(Obj_AI_Hero eTarget)
        {
            if (E.IsReady())
            {
//                var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
                if (eTarget.IsValidTarget(E.Range))
                {
                    foreach (var buff in eTarget.Buffs.Where(buff => buff.DisplayName.ToLower() == "kalistarend").Where(buff => buff.Count == 6))
                    {
                        Game.PrintChat("Total stacks on target " + eTarget.ChampionName + " Count: " + buff.Count + " Total Damage: " + eTarget.GetSpellDamage(myHero,SpellSlot.E)*buff.Count);
                        return (float)eTarget.GetSpellDamage(myHero, SpellSlot.E) * buff.Count;
//                        E.Cast();
                    }
                }

            }
            return (float)0;
        }
    }
    class HpBarIndicator
    {

        public static SharpDX.Direct3D9.Device dxDevice = Drawing.Direct3DDevice;
        public static SharpDX.Direct3D9.Line dxLine;

        public Obj_AI_Hero unit { get; set; }

        public float width = 104;

        public float hight = 9;


        public HpBarIndicator()
        {
            dxLine = new Line(dxDevice) { Width = 9 };

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;

        }


        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            dxLine.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            dxLine.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            dxLine.OnLostDevice();
        }

        private Vector2 Offset
        {
            get
            {
                if (unit != null)
                {
                    return unit.IsAlly ? new Vector2(34, 9) : new Vector2(10, 20);
                }

                return new Vector2();
            }
        }

        public Vector2 startPosition
        {

            get { return new Vector2(unit.HPBarPosition.X + Offset.X, unit.HPBarPosition.Y + Offset.Y); }
        }


        private float getHpProc(float dmg = 0)
        {
            float health = ((unit.Health - dmg) > 0) ? (unit.Health - dmg) : 0;
            return (health / unit.MaxHealth);
        }

        private Vector2 getHpPosAfterDmg(float dmg)
        {
            float w = getHpProc(dmg) * width;
            return new Vector2(startPosition.X + w, startPosition.Y);
        }

        public void drawDmg(float dmg, System.Drawing.Color color)
        {
            var hpPosNow = getHpPosAfterDmg(0);
            var hpPosAfter = getHpPosAfterDmg(dmg);

            fillHPBar(hpPosNow, hpPosAfter, color);
            //fillHPBar((int)(hpPosNow.X - startPosition.X), (int)(hpPosAfter.X- startPosition.X), color);
        }

        private void fillHPBar(int to, int from, System.Drawing.Color color)
        {
            Vector2 sPos = startPosition;

            for (int i = from; i < to; i++)
            {
                Drawing.DrawLine(sPos.X + i, sPos.Y, sPos.X + i, sPos.Y + 9, 1, color);
            }
        }

        private void fillHPBar(Vector2 from, Vector2 to, System.Drawing.Color color)
        {
            dxLine.Begin();

            dxLine.Draw(new[]
                                    {
                                        new Vector2((int)from.X, (int)from.Y + 4f),
                                        new Vector2( (int)to.X, (int)to.Y + 4f)
                                    }, new ColorBGRA(255, 255, 00, 90));
            // Vector2 sPos = startPosition;
            //Drawing.DrawLine((int)from.X, (int)from.Y + 9f, (int)to.X, (int)to.Y + 9f, 9f, color);

            dxLine.End();
        }

    }
}
