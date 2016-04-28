using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Procedural_Story.Core.Life;
using Procedural_Story.Core;
using Microsoft.Xna.Framework.Input;

namespace Procedural_Story.UI {
    class CommandLine : TextBox {
        delegate string Command(string[] args);

        int c = 0;
        public List<string> messages;
        public List<string> prev;

        Dictionary<string, Command> Commands;

        public CommandLine(UIElement parent, string name, UDim2 position, UDim2 size, string text, string font, Color color) : base(parent, name, position, size, text, font, color) {
            messages = new List<string>();
            prev = new List<string>();
            DefaultText = ">";
            TextAlignment = AlignmentType.CenterLeft;

            Commands = new Dictionary<string, Command>();
            Commands.Add("give", GiveItem);
        }

        string GiveItem(string[] args) {
            if (args.Length > 0) {
                try {
                    uint id = Convert.ToUInt32(args[0]);
                    Item i = Item.FromID(id);
                    Main.Instance.Player.Inventory.Add(i);
                    
                    return "Success";
                } catch {
                    return "Failure";
                }
            }
            return "Failure";
        }

        public override void KeyPressed(Keys key) {
            if (key == Keys.Up || key == Keys.Down) {
                if (key == Keys.Up)
                    c++;
                else
                    c--;
                if (c < 1) c = 1;
                if (c > prev.Count) c = prev.Count;

                Text = prev[prev.Count - c];
            } else
                c = 0;

            base.KeyPressed(key);
        }

        public override void EnterPressed() {
            string msg = "Invalid command";

            string[] cmds = Text.Split(' ');
            if (cmds != null && cmds.Length > 0) {
                foreach (KeyValuePair<string, Command> c in Commands) {
                    if (c.Key == cmds[0]) {
                        string[] args = new string[cmds.Length - 1];
                        Array.Copy(cmds, 1, args, 0, cmds.Length - 1);
                        msg = c.Value(args);
                    }
                }
            }
            messages.Add(Text + "\n     " + msg);
            prev.Add(Text);
            TextLabel t = Tag as TextLabel;
            t.Text = "";
            foreach (string s in messages)
                t.Text += s + "\n";

            Text = "";
        }
    }
}
