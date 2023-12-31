﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScheduleViewer
{
    public class SchedulesPage : IClickableMenu
    {
        public const int spriteSize = 64, partitionSize = 8, rowHeight = 112, slotsOnPage = 5;

        private string hoverText = "";

        /// <summary>Key in the format "{slot}-{line#}, Value is Rectangle containing the bounds of the hover text and the hover text string"</summary>
        private readonly Dictionary<string, Tuple<Rectangle, string>> hoverTextOptions = new();

        private readonly ClickableTextureComponent upButton;

        private readonly ClickableTextureComponent downButton;

        private readonly ClickableTextureComponent scrollBar;

        private Rectangle scrollBarRunner;

        private bool scrolling;

        /// <summary>Index of the NPC that's at the top of the menu</summary>
        private int slotPosition;

        public readonly List<ClickableTextureComponent> characterSlots = new();

        private readonly List<Schedule.NPCSchedule> schedules = new();

        private readonly List<ClickableTextureComponent> sprites = new();

        private readonly Texture2D emptySprite = ModEntry.ModHelper.ModContent.Load<Texture2D>("assets/Unknown.png");

        public Friendship emptyFriendship = new();


        public SchedulesPage(int initialSlotPosition = 0)
            : base(Game1.uiViewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2, Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 800 + 36 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, showUpperRightCloseButton: true)
        {
            this.slotPosition = initialSlotPosition;
            // filter npcs
            IEnumerable<KeyValuePair<string, Schedule.NPCSchedule>> filteredSchedules = Schedule.GetSchedules(ModEntry.Config.OnlyShowSocializableNPCs, ModEntry.Config.OnlyShowMetNPCs);
            // sort npcs
            filteredSchedules = ModEntry.Config.NPCSortOrder switch
            {
                ModConfig.SortType.AlphabeticalDescending => filteredSchedules.OrderByDescending(x => x.Value.DisplayName),
                ModConfig.SortType.HeartsAscending => filteredSchedules.OrderBy(x => Game1.player.getFriendshipLevelForNPC(x.Key)).ThenBy(x => x.Value.DisplayName),
                ModConfig.SortType.HeartsDescending => filteredSchedules.OrderByDescending(x => Game1.player.getFriendshipLevelForNPC(x.Key)).ThenBy(x => x.Value.DisplayName),
                _ => filteredSchedules.OrderBy(x => x.Value.DisplayName),
            };

            // map schedules into slots
            int itemIndex = 0;
            foreach (var item in filteredSchedules)
            {
                // if not host then need to get sprite info
                if (item.Value.Sprite == null || item.Value.MugShotSourceRect == null)
                {
                    NPC npc = Game1.getCharacterFromName(item.Key);
                    item.Value.Sprite = npc?.Sprite;
                    item.Value.MugShotSourceRect = npc?.getMugShotSourceRect();
                }

                this.schedules.Add(item.Value);
                this.sprites.Add(new ClickableTextureComponent("", new Rectangle(base.xPositionOnScreen + IClickableMenu.borderWidth + 4, base.yPositionOnScreen + IClickableMenu.borderWidth + spriteSize / 2, 260, spriteSize), null, "", item.Value.Sprite?.Texture ?? emptySprite, item.Value.MugShotSourceRect ?? new Rectangle(0, 0, 16, 24), 4f));
                this.characterSlots.Add(new ClickableTextureComponent(new Rectangle(base.xPositionOnScreen + IClickableMenu.borderWidth, 0, base.width - IClickableMenu.borderWidth * 2, rowHeight), null, new Rectangle(0, 0, 0, 0), 4f)
                {
                    myID = itemIndex,
                    downNeighborID = itemIndex + 1,
                    upNeighborID = itemIndex - 1,
                });
                itemIndex++;
            }

            base.initializeUpperRightCloseButton();

            // init scroll section
            this.upButton = new ClickableTextureComponent(new Rectangle(base.xPositionOnScreen + base.width + 16, base.yPositionOnScreen + 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
            this.downButton = new ClickableTextureComponent(new Rectangle(base.xPositionOnScreen + base.width + 16, base.yPositionOnScreen + base.height - 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
            this.scrollBar = new ClickableTextureComponent(new Rectangle(this.upButton.bounds.X + 12, this.upButton.bounds.Y + this.upButton.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
            this.scrollBarRunner = new Rectangle(this.scrollBar.bounds.X, this.upButton.bounds.Y + this.upButton.bounds.Height + 4, this.scrollBar.bounds.Width, base.height - 128 - this.upButton.bounds.Height - 8);
            // set the scoll bar postion and sets yPos for characterSlots and sprites
            this.SetScrollBarToCurrentIndex();
        }

        #region override methods
        public override void applyMovementKey(int direction)
        {
            base.applyMovementKey(direction);
            if (base.currentlySnappedComponent != null && this.characterSlots.Contains(base.currentlySnappedComponent))
            {
                int index = this.characterSlots.IndexOf(base.currentlySnappedComponent as ClickableTextureComponent);
                if (index < this.slotPosition)
                {
                    this.slotPosition = index;
                }
                else if (index >= this.slotPosition + slotsOnPage)
                {
                    this.slotPosition = index - slotsOnPage + 1;
                }
                this.SetScrollBarToCurrentIndex();
                if (Game1.options.snappyMenus && Game1.options.gamepadControls)
                {
                    this.snapCursorToCurrentSnappedComponent();
                }

            }
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            Game1.drawDialogueBox(base.xPositionOnScreen, base.yPositionOnScreen, base.width, base.height, false, true);

            b.End();
            b.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, Utility.ScissorEnabled);
            base.drawHorizontalPartition(b, base.yPositionOnScreen + IClickableMenu.borderWidth + 128 + 4, small: true);
            base.drawHorizontalPartition(b, base.yPositionOnScreen + IClickableMenu.borderWidth + 192 + 32 + 20, small: true);
            base.drawHorizontalPartition(b, base.yPositionOnScreen + IClickableMenu.borderWidth + 320 + 36, small: true);
            base.drawHorizontalPartition(b, base.yPositionOnScreen + IClickableMenu.borderWidth + 384 + 32 + 52, small: true);

            // draw slots
            for (int i = this.slotPosition; i < this.slotPosition + slotsOnPage; i++)
            {
                if (i < this.sprites.Count)
                {
                    this.DrawNPCSlot(b, i);
                }
            }

            Rectangle origClip = b.GraphicsDevice.ScissorRectangle;
            Rectangle newClip = origClip;
            newClip.Y = 0;
            newClip.Height -= newClip.Y;
            if (newClip.Height > 0)
            {
                b.GraphicsDevice.ScissorRectangle = newClip;
                try
                {
                    base.drawVerticalPartition(b, base.xPositionOnScreen + 256 + 12, small: true);
                }
                finally
                {
                    b.GraphicsDevice.ScissorRectangle = origClip;
                }
            }

            this.upButton.draw(b);
            this.downButton.draw(b);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this.scrollBarRunner.X, this.scrollBarRunner.Y, this.scrollBarRunner.Width, this.scrollBarRunner.Height, Color.White, 4f);
            this.scrollBar.draw(b);
            if (!this.hoverText.Equals(""))
            {
                IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont);
            }
            // draw close button
            base.draw(b);
            // draw cursor
            base.drawMouse(b);

            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            Game1.activeClickableMenu = new SchedulesPage(this.slotPosition);
        }

        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);
            if (this.scrolling)
            {
                int y2 = this.scrollBar.bounds.Y;
                this.scrollBar.bounds.Y = Math.Min(base.yPositionOnScreen + base.height - 64 - 12 - this.scrollBar.bounds.Height, Math.Max(y, base.yPositionOnScreen + this.upButton.bounds.Height + 20));
                float percentage = (float)(y - this.scrollBarRunner.Y) / (float)this.scrollBarRunner.Height;
                this.slotPosition = Math.Min(this.sprites.Count - slotsOnPage, Math.Max(0, (int)((float)this.sprites.Count * percentage)));
                this.SetScrollBarToCurrentIndex();
                if (y2 != this.scrollBar.bounds.Y)
                {
                    Game1.playSound("shiny4");
                }
            }
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            this.upButton.tryHover(x, y);
            this.downButton.tryHover(x, y);
            if (!ModEntry.Config.DisableHover)
            {
                string newHoverText = "";
                foreach (var hoverTextOption in this.hoverTextOptions)
                {
                    if (hoverTextOption.Value != null && hoverTextOption.Value.Item1.Contains(x, y))
                    {
                        newHoverText = hoverTextOption.Value.Item2;
                        break;
                    }
                }
                this.hoverText = newHoverText;
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            if (this.upButton.containsPoint(x, y) && this.slotPosition > 0)
            {
                this.UpArrowPressed();
                Game1.playSound("shwip");
                return;
            }
            if (this.downButton.containsPoint(x, y) && this.slotPosition < this.sprites.Count - slotsOnPage)
            {
                this.DownArrowPressed();
                Game1.playSound("shwip");
                return;
            }
            if (this.scrollBar.containsPoint(x, y))
            {
                this.scrolling = true;
                return;
            }
            if (!this.downButton.containsPoint(x, y) && x > base.xPositionOnScreen + base.width && x < base.xPositionOnScreen + base.width + 128 && y > base.yPositionOnScreen && y < base.yPositionOnScreen + base.height)
            {
                this.scrolling = true;
                this.leftClickHeld(x, y);
                this.releaseLeftClick(x, y);
                return;
            }
            // TODO: Saving this for showing the NPC's full schedule in a new window
            //for (int i = 0; i < this.characterSlots.Count; i++)
            //{
            //    if (i < this.slotPosition || i >= this.slotPosition + 5 || !this.characterSlots[i].bounds.Contains(x, y))
            //    {
            //        continue;
            //    }
            //    bool fail = true;
            //    if (this.names[i] is string)
            //    {
            //        Character character = Game1.getCharacterFromName((string)this.names[i]);
            //        if (character != null && Game1.player.friendshipData.ContainsKey(character.name))
            //        {
            //            fail = false;
            //            Game1.playSound("bigSelect");
            //            int cached_slot_position = this.slotPosition;
            //            ProfileMenu menu = new ProfileMenu(character);
            //            menu.exitFunction = delegate
            //            {
            //                if (((GameMenu)(Game1.activeClickableMenu = new GameMenu(2, -1, playOpeningSound: false))).GetCurrentPage() is SocialPage socialPage)
            //                {
            //                    Character character2 = menu.GetCharacter();
            //                    if (character2 != null)
            //                    {
            //                        for (int j = 0; j < socialPage.names.Count; j++)
            //                        {
            //                            if (socialPage.names[j] is string && character2.Name == (string)socialPage.names[j])
            //                            {
            //                                socialPage.slotPosition = cached_slot_position;
            //                                socialPage._SelectSlot(socialPage.characterSlots[j]);
            //                                break;
            //                            }
            //                        }
            //                    }
            //                }
            //            };
            //            Game1.activeClickableMenu = menu;
            //            if (Game1.options.SnappyMenus)
            //            {
            //                menu.snapToDefaultClickableComponent();
            //            }
            //            return;
            //        }
            //    }
            //    if (fail)
            //    {
            //        Game1.playSound("shiny4");
            //    }
            //    break;
            //}
            this.slotPosition = Math.Max(0, Math.Min(this.sprites.Count - slotsOnPage, this.slotPosition));
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);
            if (direction > 0 && this.slotPosition > 0)
            {
                this.UpArrowPressed();
                this.ConstrainSelectionToVisibleSlots();
                Game1.playSound("shiny4");
            }
            else if (direction < 0 && this.slotPosition < Math.Max(0, this.sprites.Count - slotsOnPage))
            {
                this.DownArrowPressed();
                this.ConstrainSelectionToVisibleSlots();
                Game1.playSound("shiny4");
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            base.releaseLeftClick(x, y);
            this.scrolling = false;
        }

        public override void snapCursorToCurrentSnappedComponent()
        {
            if (base.currentlySnappedComponent != null && this.characterSlots.Contains(base.currentlySnappedComponent))
            {
                Game1.setMousePosition(base.currentlySnappedComponent.bounds.Left + 64, base.currentlySnappedComponent.bounds.Center.Y);
            }
            else
            {
                base.snapCursorToCurrentSnappedComponent();
            }
        }

        public override void snapToDefaultClickableComponent()
        {
            if (this.slotPosition < this.characterSlots.Count)
            {
                base.currentlySnappedComponent = this.characterSlots[this.slotPosition];
            }
            this.snapCursorToCurrentSnappedComponent();
        }
        #endregion

        #region private methods
        private void ConstrainSelectionToVisibleSlots()
        {
            if (this.characterSlots.Contains(base.currentlySnappedComponent))
            {
                int index = this.characterSlots.IndexOf(base.currentlySnappedComponent as ClickableTextureComponent);
                if (index < this.slotPosition)
                {
                    index = this.slotPosition;
                }
                else if (index >= this.slotPosition + slotsOnPage)
                {
                    index = this.slotPosition + slotsOnPage - 1;
                }
                base.currentlySnappedComponent = this.characterSlots[index];
                if (Game1.options.snappyMenus && Game1.options.gamepadControls)
                {
                    this.snapCursorToCurrentSnappedComponent();
                }
            }
        }

        private void DownArrowPressed()
        {
            this.slotPosition++;
            this.downButton.scale = 3.5f;
            this.SetScrollBarToCurrentIndex();
        }

        private void DrawNPCSlot(SpriteBatch b, int i)
        {
            // highlight which NPC the mouse is over
            if (this.characterSlots[i].bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
            {
                b.Draw(Game1.staminaRect, new Rectangle(base.xPositionOnScreen + IClickableMenu.borderWidth - 4, this.sprites[i].bounds.Y - 4, this.characterSlots[i].bounds.Width, this.characterSlots[i].bounds.Height - 12), Color.White * 0.25f);
            }
            this.sprites[i].draw(b);

            var (displayName, entries, currentLocation) = schedules[i];

            float lineHeight = Game1.smallFont.MeasureString("W").Y;
            float russianOffsetY = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko) ? ((0f - lineHeight) / 2f) : 0f);
            b.DrawString(Game1.dialogueFont, displayName, new Vector2((float)(base.xPositionOnScreen + IClickableMenu.borderWidth * 3 / 2 + 64 - 20 + 96) - Game1.dialogueFont.MeasureString(displayName).X / 2f, (float)(this.sprites[i].bounds.Y + 48) + russianOffsetY - 20), Game1.textColor);

            int x = this.sprites[i].bounds.Right + partitionSize;
            int y = this.sprites[i].bounds.Y - 4;

            if (currentLocation == null)
            {
                float yOffset = 0;
                int activeEntryIndex = 0;
                for (int j = 0; j < entries.Count; j++)
                {
                    if (entries[j].Time <= Game1.timeOfDay)
                    {
                        activeEntryIndex = j;
                    }
                }

                Dictionary<int, Schedule.ScheduleEntry> lines = new();
                int line1Index = activeEntryIndex == 0 ? activeEntryIndex : activeEntryIndex - 1;
                lines.Add(line1Index, entries.ElementAtOrDefault(line1Index));
                lines.Add(line1Index + 1, entries.ElementAtOrDefault(line1Index + 1));
                lines.Add(line1Index + 2, entries.ElementAtOrDefault(line1Index + 2));
                foreach (var line in lines)
                {
                    string entryString = line.Value?.ToString();
                    string key = $"{i - this.slotPosition}-{line.Key - line1Index}";
                    if (string.IsNullOrEmpty(entryString))
                    {
                        this.hoverTextOptions[key] = null;
                    }
                    else
                    {
                        this.hoverTextOptions[key] = Tuple.Create(new Rectangle(x, y + (int)yOffset, (int)Game1.smallFont.MeasureString(entryString).X + 2, (int)lineHeight), line.Value.GetHoverText());
                    }

                    if (line.Value != null)
                    {
                        if (line.Key == activeEntryIndex)
                        {
                            Utility.drawBoldText(b, entryString, Game1.smallFont, new Vector2(x, y + yOffset), Game1.textColor);
                        }
                        else
                        {
                            b.DrawString(Game1.smallFont, entryString, new Vector2(x, y + yOffset), Game1.textColor);
                        }
                        yOffset += lineHeight;
                    }
                }
            }
            else
            {
                b.DrawString(Game1.smallFont, ModEntry.ModHelper.Translation.Get(entries == null ? "not_following_schedule_today" : "ignoring_schedule_today"), new Vector2(x, y), Game1.textColor);
                Utility.drawBoldText(b, currentLocation, Game1.smallFont, new Vector2(x, y + lineHeight), Game1.textColor);
                // clear hover text options
                this.hoverTextOptions[$"{i - this.slotPosition}-0"] = null;
                this.hoverTextOptions[$"{i - this.slotPosition}-1"] = null;
                this.hoverTextOptions[$"{i - this.slotPosition}-2"] = null;
            }
        }

        private int RowPosition(int i)
        {
            int j = i - this.slotPosition;
            return base.yPositionOnScreen + IClickableMenu.borderWidth + 160 + 4 + j * rowHeight;
        }

        private void SetScrollBarToCurrentIndex()
        {
            if (this.sprites.Count > 0)
            {
                this.scrollBar.bounds.Y = this.scrollBarRunner.Height / Math.Max(1, this.sprites.Count - slotsOnPage + 1) * this.slotPosition + this.upButton.bounds.Bottom + 4;
                if (this.slotPosition == this.sprites.Count - slotsOnPage)
                {
                    this.scrollBar.bounds.Y = this.downButton.bounds.Y - this.scrollBar.bounds.Height - 4;
                }
            }
            this.UpdateSlots();
        }

        private void UpArrowPressed()
        {
            this.slotPosition--;
            this.downButton.scale = 3.5f;
            this.SetScrollBarToCurrentIndex();
        }

        private void UpdateSlots()
        {
            // update y position of all characterSlots
            int index = 0;
            foreach (var slot in characterSlots)
            {
                slot.bounds.Y = this.RowPosition(index - 1);
                index++;
            }
            // update y position for visible sprites
            for (int i = this.slotPosition; i < this.slotPosition + 5; i++)
            {
                if (this.sprites.Count > i)
                {
                    this.sprites[i].bounds.Y = base.yPositionOnScreen + IClickableMenu.borderWidth + 32 + rowHeight * (i - this.slotPosition) + 32;
                }
            }
            base.populateClickableComponentList();
        }
        #endregion
    }
}
